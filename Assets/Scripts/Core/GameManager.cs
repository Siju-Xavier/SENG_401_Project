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
        [SerializeField] private AutoSaveController autoSaveController;
        [SerializeField] private ProgressionManager progressionManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private CascadingFailureManager cascadeManager;

        [Header("Player")]
        [SerializeField] private int playerId = 1;

        [Header("Round Settings")]
        [SerializeField] private float roundDuration = 45f;

        private GridSystem gridSystem;
        private InputHandler inputHandler;
        private int currentTick;
        private int currentRound;
        private float roundTimer;
        private bool roundActive;
        private bool anyCityBurnedRound;

        public int PlayerId => playerId;
        public int CurrentTick  { get => currentTick;  set => currentTick = value; }
        public int CurrentRound { get => currentRound; set => currentRound = value; }

        private void Start() {
            // Auto-find references not wired in Inspector
            if (mapOrchestrator == null) mapOrchestrator = FindFirstObjectByType<MapGenerationOrchestrator>();
            if (fireEngine == null) fireEngine = FindFirstObjectByType<FireEngine>();
            if (weatherSystem == null) weatherSystem = FindFirstObjectByType<WeatherSystem>();
            if (resourceManager == null) resourceManager = FindFirstObjectByType<ResourceManager>();
            if (progressionManager == null) progressionManager = FindFirstObjectByType<ProgressionManager>();
            if (saveManager == null) saveManager = FindFirstObjectByType<SaveManager>();
            if (autoSaveController == null) autoSaveController = FindFirstObjectByType<AutoSaveController>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (cascadeManager == null) cascadeManager = FindFirstObjectByType<CascadingFailureManager>();

            EventBroker.Instance.Subscribe(Core.EventType.GameEnded, EndGame);
            EventBroker.Instance.Subscribe(Core.EventType.FireStarted, OnFireStarted);

            // Check if we were asked to restore a save (from MainMenuManager.ContinueGame)
            if (Presentation.MainMenuManager.ShouldLoadSave && saveManager != null) {
                Debug.Log("[GameManager] ShouldLoadSave flag set — loading via active provider.");
                var save = saveManager.LoadFile();
                LoadGame(save);
            } else {
                StartGame();
            }
        }

        private void OnDestroy() {
            EventBroker.Instance.Unsubscribe(Core.EventType.GameEnded, EndGame);
            EventBroker.Instance.Unsubscribe(Core.EventType.FireStarted, OnFireStarted);
        }

        private void OnFireStarted(object data) {
            if (data is Tile tile && tile.IsCityFootprint) {
                anyCityBurnedRound = true;
            }
        }

        public void StartGame() {
            Debug.Log("[GameManager] Starting new game.");
            currentTick = 0;
            currentRound = 1;

            if (mapOrchestrator != null) {
                StartCoroutine(WaitForMapAndStartFires());
            } else {
                FinishStartGame();
            }
        }

        private IEnumerator WaitForMapAndStartFires() {
            // Wait until MapGenerationOrchestrator has initialized the GridSystem
            while (mapOrchestrator.GridSystem == null) {
                yield return null;
            }
            gridSystem = mapOrchestrator.GridSystem;
            FinishStartGame();
        }

        private void FinishStartGame() {
            // Initialize resource manager with city data
            if (resourceManager != null && gridSystem != null) {
                resourceManager.Initialize(gridSystem.Regions);
                resourceManager.SetGridSystem(gridSystem);
            }

            // Wire input handler
            inputHandler = FindFirstObjectByType<InputHandler>();
            if (inputHandler != null && gridSystem != null)
                inputHandler.SetGridSystem(gridSystem);

            // Initialize cascading failure tracking
            if (cascadeManager != null && gridSystem != null) {
                cascadeManager.SetGridSystem(gridSystem);
            }

            // Start the fire simulation
            if (fireEngine != null) {
                if (gridSystem != null) fireEngine.SetGridSystem(gridSystem);
                fireEngine.Resume();
            }

            // Start the auto-save loop
            if (autoSaveController != null)
                autoSaveController.Run();

            StartRound();
        }

        private void Update() {
            if (!roundActive) return;

            roundTimer -= Time.deltaTime;
            if (uiManager != null) uiManager.UpdateTimerDisplay(roundTimer);

            // End round ONLY when timer runs out
            if (roundTimer <= 0f) {
                EndRound();
            }
        }

        [Tooltip("Seconds to wait before fires ignite at the start of each round.")]
        [SerializeField] private float fireIgnitionDelay = 5f;

        public bool IsRoundActive => roundActive;

        private void StartRound() {
            Debug.Log($"[GameManager] Round {currentRound} started.");
            roundTimer = roundDuration;
            roundActive = true;
            anyCityBurnedRound = false;

            // Give the player a breather before fires ignite
            StartCoroutine(DelayedFireIgnition());
        }

        private IEnumerator DelayedFireIgnition() {
            yield return new WaitForSeconds(fireIgnitionDelay);

            if (roundActive && fireEngine != null)
                fireEngine.StartRandomFires();
        }

        private void EndRound() {
            roundActive = false;
            Debug.Log($"[GameManager] Round {currentRound} ended.");

            int burningCount = fireEngine != null ? fireEngine.BurningTileCount : 0;

            if (resourceManager != null) {
                int level = progressionManager != null ? progressionManager.CurrentLevel : 1;
                resourceManager.AddRoundBudget(level);
            }

            // Progress level if no city buildings burned
            if (!anyCityBurnedRound) {
                Debug.Log("[GameManager] No city burned! Progressing to next level.");
                if (progressionManager != null) {
                    progressionManager.ForceLevelUp();
                }
                if (fireEngine != null) {
                    fireEngine.ExtinguishAllFires();
                    float recoveryRate = progressionManager != null ? progressionManager.GetRecoveryRate() : 0.25f;
                    fireEngine.RecoverBurntTiles(recoveryRate);
                }
                // Reset cascade pressure after a successful round
                if (cascadeManager != null) cascadeManager.ResetCascadeState();
            } else {
                Debug.Log("[GameManager] A city was damaged this round. No level progression.");
            }

            EventBroker.Instance.Publish(Core.EventType.RoundComplete, currentRound);
            Debug.Log($"[GameManager] Round complete (fires remaining: {burningCount})");

            currentRound++;
            StartRound();
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
                if (mapOrchestrator != null)
                    gridSystem = mapOrchestrator.GridSystem;

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
                randomSeed   = mapOrchestrator != null ? mapOrchestrator.Seed : 0,
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

