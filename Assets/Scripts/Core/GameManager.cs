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
        [SerializeField] private GameOverManager gameOverManager;

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
            if (gameOverManager == null) gameOverManager = FindFirstObjectByType<GameOverManager>();

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

            // Initialize game over tracking
            if (gameOverManager != null && gridSystem != null) {
                gameOverManager.SetGridSystem(gridSystem);
            }

            // Show tutorial before starting fires
            StartCoroutine(ShowTutorialThenStart());
        }

        private IEnumerator ShowTutorialThenStart() {
            // Gather city names for the tutorial text
            var cityNames = new System.Collections.Generic.List<string>();
            if (gridSystem != null) {
                foreach (var region in gridSystem.Regions) {
                    if (region.City != null)
                        cityNames.Add(region.City.CityName);
                }
            }

            // Launch tutorial (pauses timeScale if shown)
            var tutorial = FindFirstObjectByType<TutorialManager>();
            if (tutorial == null) {
                // Auto-create one if not in the scene
                var go = new GameObject("TutorialManager");
                tutorial = go.AddComponent<TutorialManager>();
            }
            tutorial.StartTutorial(cityNames);

            // Wait for tutorial to finish (uses realtime since timeScale is 0)
            while (tutorial != null && tutorial.IsTutorialActive) {
                yield return null;
            }

            // Now start the actual game
            if (fireEngine != null) {
                if (gridSystem != null) fireEngine.SetGridSystem(gridSystem);
                fireEngine.Resume();
            }

            if (autoSaveController != null)
                autoSaveController.Run();

            StartRound();
        }

        private void Update() {
            if (!roundActive) return;

            // Stop rounds if game is over (during the delay before panel shows)
            if (gameOverManager != null && gameOverManager.IsGameOver) {
                roundActive = false;
                return;
            }

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
            roundActive = false;

            // Stop fire simulation
            if (fireEngine != null) fireEngine.Pause();

            // Stop auto-save loop
            if (autoSaveController != null) autoSaveController.Stop();

            // Final save via the active IStorageProvider (local or cloud)
            if (saveManager != null) {
                saveManager.SaveFile();
            }

            // Close any open city panel
            if (CityPanelController.Instance != null)
                CityPanelController.Instance.HidePanel();

            // Show game over UI
            if (uiManager != null) {
                int level = progressionManager != null ? progressionManager.CurrentLevel : 1;
                uiManager.ShowGameOver(currentRound, level);
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
            StartCoroutine(WaitForMapAndRestoreSave(save));
        }

        private IEnumerator WaitForMapAndRestoreSave(SaveManager.GameSaveData save) {
            // Wait for the map to generate (same as new game)
            while (mapOrchestrator == null || mapOrchestrator.GridSystem == null) {
                yield return null;
            }
            gridSystem = mapOrchestrator.GridSystem;

            // ── Core state ──
            currentTick  = save.currentTick;
            currentRound = save.currentRound;
            roundTimer   = save.roundTimer > 0f ? save.roundTimer : roundDuration;
            roundActive  = true;

            // ── Progression ──
            if (progressionManager != null && save.progressionLevel > 0) {
                progressionManager.SetLevel(save.progressionLevel);
            }

            // ── Weather ──
            if (weatherSystem != null && save.wind != null) {
                weatherSystem.SetWind(
                    new Vector2(save.wind.dirX, save.wind.dirY),
                    save.wind.speed
                );
                Debug.Log($"[GameManager] Restored wind: ({save.wind.dirX}, {save.wind.dirY}), speed {save.wind.speed}");
            }

            // ── City budgets ──
            if (gridSystem != null && save.regions != null) {
                foreach (var regionSave in save.regions) {
                    foreach (var citySave in regionSave.cities) {
                        // Find the matching city in the grid
                        foreach (var region in gridSystem.Regions) {
                            if (region.City != null && region.City.CityName == citySave.cityName) {
                                region.City.Budget = citySave.budget;
                                Debug.Log($"[GameManager] Restored {citySave.cityName} budget: {citySave.budget}");
                                break;
                            }
                        }
                    }
                }
            }

            // ── Initialize resource manager ──
            if (resourceManager != null && gridSystem != null) {
                resourceManager.Initialize(gridSystem.Regions);
                resourceManager.SetGridSystem(gridSystem);
            }

            // ── Wire input handler ──
            inputHandler = FindFirstObjectByType<InputHandler>();
            if (inputHandler != null && gridSystem != null)
                inputHandler.SetGridSystem(gridSystem);

            // ── Initialize game over tracking ──
            if (gameOverManager != null && gridSystem != null) {
                gameOverManager.SetGridSystem(gridSystem);
            }

            // ── Burnt tiles ──
            if (gridSystem != null && save.burntTiles != null) {
                int restoredBurnt = 0;
                foreach (var burnt in save.burntTiles) {
                    var tile = gridSystem.GetTileAt(burnt.posX, burnt.posY);
                    if (tile != null) {
                        tile.IsBurnt = true;
                        tile.MoistureLevel = burnt.moisture;
                        restoredBurnt++;
                        // Notify renderers to show the burnt visual
                        EventBroker.Instance.Publish(Core.EventType.FireExtinguished, tile);
                    }
                }
                Debug.Log($"[GameManager] Restored {restoredBurnt} burnt tiles.");
            }

            // ── Active fires ──
            if (fireEngine != null) {
                if (gridSystem != null) fireEngine.SetGridSystem(gridSystem);

                if (save.activeFires != null) {
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

            // ── Active policies ──
            if (PolicyManager.Instance != null && save.savedPolicies != null && gridSystem != null) {
                // Find all available policy configs
                var allPolicies = Resources.FindObjectsOfTypeAll<ScriptableObjects.PolicyConfig>();
                foreach (var policySave in save.savedPolicies) {
                    // Find the region
                    Region targetRegion = null;
                    foreach (var region in gridSystem.Regions) {
                        if (region.RegionName == policySave.regionName) {
                            targetRegion = region;
                            break;
                        }
                    }
                    if (targetRegion == null) continue;

                    // Find the policy config
                    foreach (var config in allPolicies) {
                        if (config.PolicyName == policySave.policyName) {
                            PolicyManager.Instance.AddPolicy(config, targetRegion);
                            Debug.Log($"[GameManager] Restored policy '{policySave.policyName}' in '{policySave.regionName}'");
                            break;
                        }
                    }
                }
            }

            // ── Auto-save ──
            if (autoSaveController != null)
                autoSaveController.Run();

            // ── Update HUD ──
            if (uiManager != null) {
                int level = progressionManager != null ? progressionManager.CurrentLevel : 1;
                uiManager.UpdateLevelDisplay(level);
                uiManager.UpdateTimerDisplay(roundTimer);
            }

            Debug.Log($"[GameManager] Save restored — Round {currentRound}, Level {(progressionManager != null ? progressionManager.CurrentLevel : 1)}, Timer {roundTimer:F1}s");
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
                roundTimer   = roundTimer,
                roundActive  = roundActive,
                randomSeed   = mapOrchestrator != null ? mapOrchestrator.Seed : 0,
                mapWidth     = gridSystem != null ? gridSystem.Width  : 64,
                mapHeight    = gridSystem != null ? gridSystem.Height : 64,
                progressionLevel = progressionManager != null ? progressionManager.CurrentLevel : 1,
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

            // Burnt tiles (not on fire, but IsBurnt = true)
            if (gridSystem != null) {
                for (int x = 0; x < gridSystem.Width; x++) {
                    for (int y = 0; y < gridSystem.Height; y++) {
                        var tile = gridSystem.GetTileAt(x, y);
                        if (tile != null && tile.IsBurnt && !tile.IsOnFire) {
                            data.burntTiles.Add(new SaveManager.BurntTileSave {
                                posX = x,
                                posY = y,
                                moisture = tile.MoistureLevel
                            });
                        }
                    }
                }
            }

            // Active policies per region
            if (PolicyManager.Instance != null && gridSystem != null) {
                foreach (var region in gridSystem.Regions) {
                    var policies = PolicyManager.Instance.GetActivePolicies(region);
                    foreach (var policy in policies) {
                        data.savedPolicies.Add(new SaveManager.PolicySave {
                            regionName = region.RegionName,
                            policyName = policy.PolicyName
                        });
                    }
                }
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

