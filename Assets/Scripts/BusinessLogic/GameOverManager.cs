// ============================================================================
// GameOverManager.cs — City destruction tracking and game over logic
// ============================================================================
// Monitors city health via footprint tiles. When all footprint tiles of a
// city are burnt, the city is destroyed and its territory is scorched.
// When ALL cities are destroyed, the game ends.
//
// Also tracks "danger" state: when a city's footprint is partially on fire,
// an alert warns the player to act before the city is lost.
// ============================================================================

namespace BusinessLogic {
    using System.Collections;
    using System.Collections.Generic;
    using GameState;
    using Core;
    using UnityEngine;

    public class GameOverManager : MonoBehaviour {

        // ── Inspector tunables ───────────────────────────────────────────

        [Header("City Danger Thresholds")]
        [Tooltip("Fraction of city footprint tiles on fire/burnt to trigger danger alert (0-1).")]
        [SerializeField] private float dangerThreshold = 0.25f;

        [Tooltip("Fraction of city footprint tiles on fire/burnt to trigger critical alert (0-1).")]
        [SerializeField] private float criticalThreshold = 0.6f;

        [Tooltip("Seconds between city health checks (performance optimization).")]
        [SerializeField] private float checkInterval = 0.5f;

        [Header("Game Over")]
        [Tooltip("Seconds to wait after last city falls before showing game over panel.")]
        [SerializeField] private float gameOverDelay = 2f;

        // ── Runtime state ────────────────────────────────────────────────

        private GridSystem gridSystem;
        private FireEngine fireEngine;
        private bool gameOver;
        private float checkTimer;

        // Per-city state tracking
        private Dictionary<City, CityHealthState> cityStates = new Dictionary<City, CityHealthState>();
        private HashSet<City> destroyedCities = new HashSet<City>();
        private HashSet<City> dangerAlertedCities = new HashSet<City>();
        private HashSet<City> criticalAlertedCities = new HashSet<City>();

        // Track destruction order for game over summary
        private List<CityDestructionRecord> destructionLog = new List<CityDestructionRecord>();

        private int totalCityCount;

        public enum CityHealthStatus { Safe, Danger, Critical, Destroyed }

        public class CityHealthState {
            public City City;
            public Region Region;
            public List<Tile> FootprintTiles = new List<Tile>();
            public CityHealthStatus Status = CityHealthStatus.Safe;
            public float DamageRatio; // 0-1, fraction of footprint burnt/on fire
        }

        public class CityDestructionRecord {
            public string CityName;
            public int Level;
        }

        // ── Public API ──────────────────────────────────────────────────

        public bool IsGameOver => gameOver;
        public int DestroyedCityCount => destroyedCities.Count;
        public int TotalCityCount => totalCityCount;
        public List<CityDestructionRecord> DestructionLog => destructionLog;

        public void SetGridSystem(GridSystem grid) {
            gridSystem = grid;
            fireEngine = FindFirstObjectByType<FireEngine>();
            InitializeCityTracking();
        }

        /// <summary>Check if a specific city has been destroyed.</summary>
        public bool IsCityDestroyed(City city) {
            return city != null && destroyedCities.Contains(city);
        }

        /// <summary>Get the health status of a city.</summary>
        public CityHealthStatus GetCityStatus(City city) {
            if (city == null) return CityHealthStatus.Safe;
            return cityStates.TryGetValue(city, out var state) ? state.Status : CityHealthStatus.Safe;
        }

        /// <summary>Get the damage ratio (0-1) of a city's footprint.</summary>
        public float GetCityDamageRatio(City city) {
            if (city == null) return 0f;
            return cityStates.TryGetValue(city, out var state) ? state.DamageRatio : 0f;
        }

        // ── Initialization ──────────────────────────────────────────────

        private void InitializeCityTracking() {
            cityStates.Clear();
            destroyedCities.Clear();
            dangerAlertedCities.Clear();
            criticalAlertedCities.Clear();

            if (gridSystem == null) return;

            foreach (var region in gridSystem.Regions) {
                if (region.City == null) continue;

                var state = new CityHealthState {
                    City = region.City,
                    Region = region
                };

                // Collect all footprint tiles for this city
                foreach (var tile in region.Tiles) {
                    if (tile.IsCityFootprint) {
                        state.FootprintTiles.Add(tile);
                    }
                }

                cityStates[region.City] = state;
            }

            totalCityCount = cityStates.Count;
            Debug.Log($"[GameOver] Tracking {totalCityCount} cities.");
        }

        // ── Unity Lifecycle ─────────────────────────────────────────────

        private void Update() {
            if (gameOver || gridSystem == null) return;

            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f) {
                checkTimer = checkInterval;
                CheckAllCities();
            }
        }

        // ── Core Logic ──────────────────────────────────────────────────

        private void CheckAllCities() {
            foreach (var kvp in cityStates) {
                var city = kvp.Key;
                var state = kvp.Value;

                if (destroyedCities.Contains(city)) continue;

                // Calculate footprint damage
                int total = state.FootprintTiles.Count;
                if (total == 0) continue;

                int damaged = 0;
                foreach (var tile in state.FootprintTiles) {
                    if (tile.IsOnFire || tile.IsBurnt) damaged++;
                }

                state.DamageRatio = (float)damaged / total;

                // Check for full destruction (all footprint tiles burnt)
                bool allDestroyed = true;
                foreach (var tile in state.FootprintTiles) {
                    if (!tile.IsBurnt) { allDestroyed = false; break; }
                }

                if (allDestroyed) {
                    OnCityDestroyed(city, state);
                    continue;
                }

                // Check thresholds — only fire the highest applicable alert per tick
                if (state.DamageRatio >= criticalThreshold && !criticalAlertedCities.Contains(city)) {
                    // Mark danger as alerted too (so it doesn't fire later)
                    dangerAlertedCities.Add(city);
                    criticalAlertedCities.Add(city);
                    state.Status = CityHealthStatus.Critical;
                    EventBroker.Instance.Publish(Core.EventType.CityCritical, city);
                    Debug.Log($"[GameOver] {city.CityName} is CRITICAL! ({state.DamageRatio:P0} damaged)");
                }
                else if (state.DamageRatio >= dangerThreshold && !dangerAlertedCities.Contains(city)) {
                    dangerAlertedCities.Add(city);
                    state.Status = CityHealthStatus.Danger;
                    EventBroker.Instance.Publish(Core.EventType.CityInDanger, city);
                    Debug.Log($"[GameOver] {city.CityName} is in DANGER! ({state.DamageRatio:P0} damaged)");
                }
                else if (state.DamageRatio < dangerThreshold && dangerAlertedCities.Contains(city)) {
                    // City recovered — reset alerts
                    dangerAlertedCities.Remove(city);
                    criticalAlertedCities.Remove(city);
                    state.Status = CityHealthStatus.Safe;
                }
            }
        }

        private void OnCityDestroyed(City city, CityHealthState state) {
            state.Status = CityHealthStatus.Destroyed;
            destroyedCities.Add(city);

            // Log destruction for game over summary
            var pm = FindFirstObjectByType<ProgressionManager>();
            destructionLog.Add(new CityDestructionRecord {
                CityName = city.CityName,
                Level = pm != null ? pm.CurrentLevel : 1
            });

            Debug.Log($"[GameOver] {city.CityName} has been DESTROYED!");
            EventBroker.Instance.Publish(Core.EventType.CityDestroyed, city);

            // Scorch the entire territory — burn all flammable tiles in the region
            ScorchTerritory(state.Region);

            // Check if ALL cities are destroyed
            if (destroyedCities.Count >= totalCityCount) {
                StartCoroutine(DelayedGameOver());
            }
        }

        /// <summary>
        /// Burns all remaining flammable tiles in a destroyed city's territory.
        /// This visually marks the region as lost.
        /// </summary>
        private void ScorchTerritory(Region region) {
            if (region == null) return;

            int scorched = 0;
            foreach (var tile in region.Tiles) {
                if (tile.IsBurnt || tile.IsOnFire) continue;
                if (tile.Biome == null || tile.Biome.SpreadMultiplier <= 0f) continue;

                // Directly mark as burnt (skip fire animation for speed)
                tile.IsBurnt = true;
                tile.MoistureLevel = 0f;
                scorched++;
                EventBroker.Instance.Publish(Core.EventType.FireExtinguished, tile);
            }

            // Also extinguish any active fires in the region so fire VFX are removed
            if (fireEngine != null) {
                var burning = fireEngine.GetBurningTiles();
                foreach (var tile in burning) {
                    if (tile.Region != region) continue;
                    // ScorchTile sets IsBurnt=true BEFORE publishing, avoiding double events
                    fireEngine.ScorchTile(tile);
                    scorched++;
                }
            }

            Debug.Log($"[GameOver] Scorched {scorched} tiles in {region.RegionName}'s territory.");
        }

        private IEnumerator DelayedGameOver() {
            gameOver = true; // Stop health checks immediately
            Debug.Log($"[GameOver] ALL CITIES DESTROYED — showing game over in {gameOverDelay}s...");
            yield return new WaitForSeconds(gameOverDelay);
            Debug.Log("[GameOver] GAME OVER!");
            EventBroker.Instance.Publish(Core.EventType.GameEnded, null);
        }
    }
}
