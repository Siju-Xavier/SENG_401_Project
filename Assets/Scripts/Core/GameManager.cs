namespace Core {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using BusinessLogic;
    using BusinessLogic.MapGeneration;
    using GameState;
    using Persistence;
    using Presentation;

    public class GameManager : MonoBehaviour {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private MapGenerationOrchestrator mapOrchestrator;
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private WeatherSystem weatherSystem;
        [SerializeField] private FireEngine fireEngine;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private ScoringSystem scoringSystem;
        [SerializeField] private AutoSaveController autoSaveController;

        /// <summary>Player ID used for cloud save operations.</summary>
        [Header("Player")]
        [SerializeField] private int playerId = 1;

        private GridSystem gridSystem;
        private int currentTick;
        private int currentRound;

        public int PlayerId => playerId;
        public int CurrentTick  { get => currentTick;  set => currentTick = value; }
        public int CurrentRound { get => currentRound; set => currentRound = value; }

        private void Start() {
            EventBroker.Instance.Subscribe(Core.EventType.GameEnded, EndGame);

            // Check if we were asked to restore a save (from MainMenuManager.ContinueGame)
            if (Presentation.MainMenuManager.ShouldLoadSave && saveManager != null) {
                Debug.Log("[GameManager] ShouldLoadSave flag set — loading via active provider.");
                var save = saveManager.LoadFile();
                LoadGame(save);
            } else {
                StartGame();
            }
        }

        public void StartGame() {
            Debug.Log("[GameManager] Starting new game.");
            currentTick = 0;
            currentRound = 1;

            if (mapOrchestrator != null || mapGenerator != null) {
                StartCoroutine(WaitForMapAndStartFires());
            } else {
                FinishStartGame();
            }
        }

        private IEnumerator WaitForMapAndStartFires() {
            if (mapOrchestrator != null) {
                // Wait until MapGenerationOrchestrator has initialized the GridSystem
                while (mapOrchestrator.GridSystem == null) {
                    yield return null;
                }
                gridSystem = mapOrchestrator.GridSystem;
            } else if (mapGenerator != null) {
                // Wait until MapGenerator has initialized the GridSystem
                while (mapGenerator.GridSystem == null) {
                    yield return null;
                }
                gridSystem = mapGenerator.GridSystem;
            }
            FinishStartGame();
        }

        private void FinishStartGame() {
            // Start the fire simulation (UML: GameManager --> FireEngine : controls_lifecycle)
            if (fireEngine != null) {
                if (gridSystem != null) fireEngine.SetGridSystem(gridSystem);
                fireEngine.Resume();
                fireEngine.StartRandomFires();
            }

            // Start the auto-save loop
            if (autoSaveController != null)
                autoSaveController.Run();
        }

        public void PauseGame() {
            Time.timeScale = 0f;
            if (fireEngine != null) fireEngine.Pause();
        }

        public void ResumeGame() {
            Time.timeScale = 1f;
            if (fireEngine != null) fireEngine.Resume();
        }

        public void EndGame(object data = null) {
            Debug.Log("[GameManager] Game ended — saving final state.");

            // Save game stats to cloud
            if (scoringSystem != null) {
                StartCoroutine(scoringSystem.SaveStatsToCloud(playerId, currentRound,
                    gridSystem != null ? gridSystem.Width : 64,
                    gridSystem != null ? gridSystem.Height : 64,
                    gridSystem != null ? gridSystem.Regions.Count : 0));
            }

            // Final save via the active IStorageProvider (local or cloud)
            if (saveManager != null) {
                saveManager.SaveFile();
            }
        }

        /// <summary>
        /// Restore game state from a previously loaded save.
        /// Called by MainMenuManager when the player selects "Continue".
        /// </summary>
        public void LoadGame(SaveManager.GameSaveData save) {
            if (save == null) {
                Debug.LogWarning("[GameManager] No save data to load — starting fresh.");
                StartGame();
                return;
            }

            Debug.Log("[GameManager] Restoring game from save data.");
            currentTick  = save.currentTick;
            currentRound = save.currentRound;

            // Restore weather
            if (weatherSystem != null && save.wind != null) {
                Debug.Log($"[GameManager] Restored wind: ({save.wind.dirX}, {save.wind.dirY}), speed {save.wind.speed}");
            }

            // Restore fires from save data and resume simulation
            if (fireEngine != null) {
                if (mapGenerator != null)
                    gridSystem = mapGenerator.GridSystem;

                if (gridSystem != null && save.activeFires != null) {
                    foreach (var fireSave in save.activeFires) {
                        var tile = gridSystem.GetTileAt(fireSave.posX, fireSave.posY);
                        if (tile != null) {
                            fireEngine.IgniteTile(tile);
                            tile.FireIntensity = fireSave.intensity;
                        }
                    }
                }
                fireEngine.Resume();
            }

            // Start the auto-save loop
            if (autoSaveController != null)
                autoSaveController.Run();
        }

        /// <summary>
        /// Collects the full game state into a serialisable snapshot.
        /// Used by SaveManager.SaveFile(), AutoSaveController, and EndGame().
        /// </summary>
        public SaveManager.GameSaveData BuildSaveData() {
            if (mapOrchestrator != null)
                gridSystem = mapOrchestrator.GridSystem;

            var data = new SaveManager.GameSaveData {
                currentTick  = currentTick,
                currentRound = currentRound,
                randomSeed   = mapGenerator != null ? mapGenerator.seed : 0,
                mapWidth     = gridSystem != null ? gridSystem.Width  : 64,
                mapHeight    = gridSystem != null ? gridSystem.Height : 64,
            };

            // Regions & Cities
            if (gridSystem != null) {
                data.cityCount = gridSystem.Regions.Count;
                foreach (var region in gridSystem.Regions) {
                    var rs = new SaveManager.RegionSave { name = region.RegionName };
                    if (region.City != null) {
                        rs.cities.Add(new SaveManager.CityStatusSave {
                            cityName    = region.City.CityName,
                            reputation  = region.City.Reputation,
                            budget      = region.City.Budget,
                            isUnderThreat = region.City.IsOnFire
                        });
                    }
                    data.regions.Add(rs);
                }
            }

            // Weather
            if (weatherSystem != null) {
                data.wind = new SaveManager.WindSave {
                    dirX  = weatherSystem.GetNextWindDirection().x,
                    dirY  = weatherSystem.GetNextWindDirection().y,
                    speed = weatherSystem.GetWindSpeed()
                };
            }

            // Resources
            if (resourceManager != null) {
                // ResourceManager fields are currently stubs; ready to populate
                // when ResourceManager exposes its budget/firefighter counts
            }

            // Active fires (from FireEngine)
            if (fireEngine != null) {
                foreach (var tile in fireEngine.GetBurningTiles()) {
                    data.activeFires.Add(new SaveManager.FireTileSave {
                        posX         = tile.X,
                        posY         = tile.Y,
                        intensity    = Mathf.RoundToInt(tile.FireIntensity),
                        containment  = 0f,
                        isDestroyed  = false,
                        ticksBurning = 0,
                        tileType     = tile.Biome != null ? tile.Biome.name : "unknown"
                    });
                }
            }

            return data;
        }
    }
}

