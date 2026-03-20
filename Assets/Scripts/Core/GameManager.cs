namespace Core {
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using BusinessLogic;
    using BusinessLogic.MapGeneration;
    using GameState;
    using Persistence;

    public class GameManager : MonoBehaviour {
        [SerializeField] private ResourceManager resourceManager;
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
            EventBroker.Instance.Subscribe(EventType.GameEnded, EndGame);

            // Check if we were asked to restore a save (from MainMenuManager.ContinueGame)
            if (MainMenuManager.ShouldLoadSave && saveManager != null) {
                Debug.Log("[GameManager] ShouldLoadSave flag set — loading save data.");
                StartCoroutine(saveManager.LoadBestAvailable(playerId, save => {
                    LoadGame(save);
                }));
            } else {
                StartGame();
            }
        }

        public void StartGame() {
            Debug.Log("[GameManager] Starting new game.");
            currentTick = 0;
            currentRound = 1;

            // GridSystem is created by MapGenerator during GenerateMap()
            if (mapGenerator != null)
                gridSystem = mapGenerator.GridSystem;

            // Start the auto-save loop
            if (autoSaveController != null)
                autoSaveController.Run();
        }

        public void PauseGame() {
            Time.timeScale = 0f;
        }

        public void ResumeGame() {
            Time.timeScale = 1f;
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

            // Trigger a final save
            if (saveManager != null) {
                var saveData = BuildSaveData();
                StartCoroutine(saveManager.SaveToCloud(playerId, saveData,
                    displayName: "Game Over Save"));
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
                // WeatherSystem will pick up state on next UpdateWeatherState()
                Debug.Log($"[GameManager] Restored wind: ({save.wind.dirX}, {save.wind.dirY}), speed {save.wind.speed}");
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
            if (mapGenerator != null)
                gridSystem = mapGenerator.GridSystem;

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

            return data;
        }
    }
}

