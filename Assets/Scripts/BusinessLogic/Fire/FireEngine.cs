// ============================================================================
// FireEngine.cs — Tick-based fire simulation
// ============================================================================
// Per the UML:
//   FireEngine ..> WeatherSystem : reads_wind_&_season
//   FireEngine ..> EventBroker   : publishes & subscribes
//   FireEngine ..> Tile          : reads & modifies
//   Tile       ..> BiomeConfig   : reads (spreadMultiplier, baseMoisture)
//
// Each tick: grow existing fires, then attempt to spread to neighbours.
// Spread probability is affected by wind direction, biome, and moisture.
// ============================================================================

namespace BusinessLogic {
    using System.Collections.Generic;
    using GameState;
    using Core;
    using UnityEngine;

    public class FireEngine : MonoBehaviour {

        // ── Inspector tunables ───────────────────────────────────────────
        [Header("References")]
        [Tooltip("Auto-populated at runtime if left empty.")]
        [SerializeField] private WeatherSystem weatherSystem;
        [Tooltip("Auto-populated at runtime if left empty.")]
        [SerializeField] private GridSystem    gridSystem;
        [Tooltip("Auto-populated at runtime if left empty.")]
        [SerializeField] private ProgressionManager progressionManager;

        private void Start() {
            // Auto-find references to make setup easier
            if (weatherSystem == null) weatherSystem = FindFirstObjectByType<WeatherSystem>();
            if (gridSystem == null) gridSystem = FindFirstObjectByType<GridSystem>();
            if (progressionManager == null) progressionManager = FindFirstObjectByType<ProgressionManager>();
        }

        public void SetGridSystem(GridSystem grid) {
            gridSystem = grid;
        }

        [Header("Timing")]
        [Tooltip("Seconds between fire simulation ticks.")]
        [SerializeField] private float tickInterval = 1.5f;

        [Header("Fire Behaviour")]
        [Tooltip("Base probability (0-1) that fire spreads to a cardinal neighbour per tick.")]
        [SerializeField] private float baseSpreadChance = 0.15f;

        [Tooltip("How much wind direction boosts spread (0 = none, 1 = double).")]
        [SerializeField] private float windInfluence = 0.6f;

        [Tooltip("Intensity gained per tick by a burning tile.")]
        [SerializeField] private float intensityGrowthRate = 0.15f;

        [Tooltip("Maximum fire intensity a tile can reach.")]
        [SerializeField] private float maxIntensity = 5f;

        [Tooltip("Moisture consumed per tick on a burning tile.")]
        [SerializeField] private float moistureBurnRate = 0.08f;

        [Header("Auto-Ignition")]
        [Tooltip("Number of random fires to ignite on game start.")]
        [SerializeField] private int initialFireCount = 3;

        // ── Runtime state ────────────────────────────────────────────────
        private bool  isRunning;
        private float fireTickTimer;

        // Track all tiles that are currently on fire for efficient iteration
        private List<Tile> burningTiles = new List<Tile>();

        // ── Public API (called by GameManager) ───────────────────────────

        public void SetDifficulty(float spreadChance, float interval, int fireCount, float growthRate) {
            baseSpreadChance = spreadChance;
            tickInterval = interval;
            initialFireCount = fireCount;
            intensityGrowthRate = growthRate;
        }

        /// <summary>Start / resume the fire simulation loop.</summary>
        public void Resume() {
            isRunning = true;
            Debug.Log("[FireEngine] Simulation resumed.");
        }

        /// <summary>Pause the fire simulation (e.g. when GameManager pauses).</summary>
        public void Pause() {
            isRunning = false;
            Debug.Log("[FireEngine] Simulation paused.");
        }

        /// <summary>Is the fire simulation currently running?</summary>
        public bool IsRunning => isRunning;

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void Update() {
            if (!isRunning || gridSystem == null) return;

            fireTickTimer += Time.deltaTime;
            if (fireTickTimer >= tickInterval) {
                fireTickTimer = 0f;
                Tick();
            }
        }

        // ── Core Simulation ──────────────────────────────────────────────

        /// <summary>
        /// Advance fire simulation by one step.
        /// 1. Grow intensity of burning tiles.
        /// 2. Attempt to spread fire to neighbours.
        /// </summary>
        public void Tick() {
            if (gridSystem == null) return;

            // Random fire spawns based on level/policy
            AutoIgniteTick();

            // Work on a snapshot so new ignitions don't affect this tick
            var snapshot = new List<Tile>(burningTiles);

            var burnedOut = new List<Tile>();

            foreach (var tile in snapshot) {
                if (!tile.IsOnFire) continue;

                // ── Grow intensity ──
                tile.FireIntensity = Mathf.Min(tile.FireIntensity + intensityGrowthRate, maxIntensity);

                // ── Burn moisture ──
                tile.MoistureLevel = Mathf.Max(tile.MoistureLevel - moistureBurnRate, 0f);

                // ── Attempt spread ──
                CalculateSpread(tile);

                // ── Burn out: tile reaches max intensity and is destroyed ──
                if (tile.FireIntensity >= maxIntensity) {
                    burnedOut.Add(tile);
                    tile.IsBurnt = true; // Mark tile as permanently burnt out
                }
            }

            // Burn out tiles that reached max intensity
            foreach (var tile in burnedOut) {
                tile.IsOnFire = false;
                tile.FireIntensity = 0f;
                burningTiles.Remove(tile);
                EventBroker.Instance.Publish(Core.EventType.FireExtinguished, tile);
            }

            // Check for tiles that are no longer edges
            foreach (var tile in snapshot) {
                if (tile.IsOnFire && !IsEdgeTile(tile)) {
                    // It's still on fire under the hood, but visually we want the animation to stop
                    EventBroker.Instance.Publish(Core.EventType.FireNoLongerEdge, tile);
                }
            }

            // Remove tiles that somehow got extinguished mid-tick
            burningTiles.RemoveAll(t => !t.IsOnFire);
        }

        /// <summary>Attempt to spawn random fires based on level and policy modifiers.</summary>
        private void AutoIgniteTick() {
            if (gridSystem == null) return;
            
            int currentLevel = progressionManager != null ? progressionManager.CurrentLevel : 1;

            // Only start auto-igniting on level 2 and above, or make it extremely rare on level 1
            if (currentLevel == 1) return;

            // Base spawn chance per tick
            float baseSpawnChance = 0.05f; 
            
            // Global level multiplier
            float levelMultiplier = progressionManager != null ? progressionManager.GetGlobalSpawnMultiplier() : 1.0f;

            // Pick a random tile to potentially ignite
            int rx = Random.Range(0, gridSystem.Width);
            int ry = Random.Range(0, gridSystem.Height);
            Tile tile = gridSystem.GetTileAt(rx, ry);

            if (tile == null || tile.IsOnFire || tile.IsBurnt || tile.Biome == null || tile.Biome.SpreadMultiplier <= 0f) return;

            // Local policy modifier
            float policyModifier = PolicyManager.Instance != null ? PolicyManager.Instance.GetSpawnModifierForRegion(tile.Region) : 1.0f;

            float finalChance = baseSpawnChance * levelMultiplier * policyModifier;
            if (Random.value < finalChance) {
                Debug.Log($"[FireEngine] Auto-ignited ({tile.X},{tile.Y}) via AutoIgniteTick.");
                IgniteTile(tile);
            }
        }

        /// <summary>
        /// For each cardinal neighbour of a burning tile, calculate whether
        /// the fire spreads. Takes into account:
        ///   - BiomeConfig.SpreadMultiplier
        ///   - Tile moisture (higher moisture = lower chance)
        ///   - Wind direction from WeatherSystem (fire spreads faster downwind)
        /// </summary>
        public void CalculateSpread(Tile sourceTile) {
            if (gridSystem == null) return;

            var neighbours = gridSystem.GetNeighbours(sourceTile);
            Vector2 windDir = weatherSystem != null
                ? weatherSystem.GetNextWindDirection().normalized
                : Vector2.zero;

            foreach (var neighbour in neighbours) {
                // Skip tiles already on fire or burnt out
                if (neighbour.IsOnFire || neighbour.IsBurnt) continue;

                // Skip tiles with no biome (outside playable map) or water (SpreadMultiplier <= 0)
                if (neighbour.Biome == null) continue;
                if (neighbour.Biome.SpreadMultiplier <= 0f) continue;

                // ── Biome modifier ──
                float biomeMultiplier = neighbour.Biome.SpreadMultiplier;

                // ── Wind bonus ──
                // Dot product of wind direction and source→neighbour direction
                Vector2 spreadDir = new Vector2(
                    neighbour.X - sourceTile.X,
                    neighbour.Y - sourceTile.Y
                ).normalized;

                float windDot  = Vector2.Dot(windDir, spreadDir);   // -1 to +1
                float windBonus = 1f + windDot * windInfluence;     // 0.4 to 1.6

                // ── Moisture penalty ──
                float moisturePenalty = neighbour.MoistureLevel;    // 0 to 1

                // ── Intensity boost — more intense fires spread easier ──
                float intensityBoost = sourceTile.FireIntensity / maxIntensity; // 0 to 1

                // ── Final probability ──
                float levelMultiplier = progressionManager != null ? progressionManager.GetGlobalSpreadMultiplier() : 1.0f;
                float policyModifier = PolicyManager.Instance != null ? PolicyManager.Instance.GetSpreadModifierForRegion(neighbour.Region) : 1.0f;

                float probability = baseSpreadChance
                    * biomeMultiplier
                    * windBonus
                    * levelMultiplier
                    * policyModifier
                    * (1f + intensityBoost)
                    * (1f - moisturePenalty * 0.7f);

                probability = Mathf.Clamp01(probability);

                if (Random.value < probability) {
                    IgniteTile(neighbour);
                }
            }
        }

        /// <summary>Check if a tile has any non-burning flammable neighbors.</summary>
        private bool IsEdgeTile(Tile tile) {
            if (gridSystem == null) return true; // Default to true if no grid
            
            var neighbours = gridSystem.GetNeighbours(tile);
            foreach (var neighbour in neighbours) {
                // If there's a neighbour that is NOT on fire, BUT is flammable, we are still an edge
                if (!neighbour.IsOnFire && neighbour.Biome != null && neighbour.Biome.SpreadMultiplier > 0f) {
                    return true;
                }
            }
            return false;
        }

        // ── Ignition / Extinguish ────────────────────────────────────────

        /// <summary>Set a tile on fire and publish events.</summary>
        public void IgniteTile(Tile tile) {
            if (tile == null || tile.IsOnFire) return;

            // Don't ignite tiles with no biome or water biome
            if (tile.Biome == null || tile.Biome.SpreadMultiplier <= 0f) return;

            tile.IsOnFire      = true;
            tile.FireIntensity = 1f;

            if (!burningTiles.Contains(tile))
                burningTiles.Add(tile);

            // Publish events so Presentation layer can update visuals
            EventBroker.Instance.Publish(Core.EventType.FireStarted, tile);
            EventBroker.Instance.Publish(Core.EventType.FireSpread, tile);

            Debug.Log($"[FireEngine] Tile ({tile.X},{tile.Y}) ignited.");
        }

        /// <summary>Extinguish a tile and publish events.</summary>
        public void ExtinguishTile(Tile tile) {
            if (tile == null || !tile.IsOnFire) return;

            tile.IsOnFire      = false;
            tile.FireIntensity = 0f;
            // Restore a bit of moisture
            tile.MoistureLevel = Mathf.Min(tile.MoistureLevel + 0.3f, 1f);

            burningTiles.Remove(tile);

            EventBroker.Instance.Publish(Core.EventType.FireExtinguished, tile);
            Debug.Log($"[FireEngine] Tile ({tile.X},{tile.Y}) extinguished.");
        }

        /// <summary>
        /// Ignite a number of random non-water tiles to kick off the game.
        /// Called by GameManager.StartGame().
        /// </summary>
        public void StartRandomFires(int count = -1) {
            if (gridSystem == null) {
                Debug.LogWarning("[FireEngine] No GridSystem — cannot start fires.");
                return;
            }

            Debug.Log($"[FireEngine] GridSystem size: {gridSystem.Width}x{gridSystem.Height}");

            // Debug: check a sample tile
            Tile sampleTile = gridSystem.GetTileAt(0, 0);
            if (sampleTile == null) {
                Debug.LogWarning("[FireEngine] GetTileAt(0,0) returned null — grid not populated!");
            } else {
                Debug.Log($"[FireEngine] Sample tile (0,0): Biome={sampleTile.Biome}, Moisture={sampleTile.MoistureLevel}");
            }

            int currentLevel = progressionManager != null ? progressionManager.CurrentLevel : 1;
            int firesToStart = count > 0 ? count : (currentLevel == 1 ? 1 : initialFireCount);
            
            int attempts = 0;
            int started  = 0;
            int nullTiles = 0;
            int alreadyOnFire = 0;
            int waterTiles = 0;
            int burntTiles = 0;

            while (started < firesToStart && attempts < firesToStart * 20) {
                attempts++;
                int rx = Random.Range(0, gridSystem.Width);
                int ry = Random.Range(0, gridSystem.Height);
                Tile tile = gridSystem.GetTileAt(rx, ry);

                if (tile == null) { nullTiles++; continue; }
                if (tile.IsOnFire) { alreadyOnFire++; continue; }
                if (tile.IsBurnt) { burntTiles++; continue; }

                // Skip water biome (very high moisture / no spread)
                if (tile.Biome != null && tile.Biome.SpreadMultiplier <= 0f) { waterTiles++; continue; }

                IgniteTile(tile);
                started++;
            }

            Debug.Log($"[FireEngine] Started {started} random fires. (attempts={attempts}, nullTiles={nullTiles}, water={waterTiles}, alreadyOnFire={alreadyOnFire}, burnt={burntTiles})");
        }

        // ── Round Cleanup / Burnt Tiles ────────────────────────────────────
        
        /// <summary>Extinguishes all active fires gracefully.</summary>
        public void ExtinguishAllFires() {
            var allBurning = new List<Tile>(burningTiles);
            foreach (var tile in allBurning) {
                // Ensure actively burning tiles are marked permanently as burnt so they turn grey!
                tile.IsBurnt = true; 
                ExtinguishTile(tile);
            }
        }

        /// <summary>Randomly recovers a percentage (0.0 - 1.0) of burnt tiles, allowing them to burn again.</summary>
        public void RecoverBurntTiles(float recoveryFraction) {
            if (gridSystem == null) return;
            int recoveredCount = 0;
            
            for (int x = 0; x < gridSystem.Width; x++) {
                for (int y = 0; y < gridSystem.Height; y++) {
                    Tile tile = gridSystem.GetTileAt(x, y);
                    if (tile != null && tile.IsBurnt) {
                        if (Random.value < recoveryFraction) {
                            tile.IsBurnt = false;
                            tile.MoistureLevel = tile.Biome != null ? tile.Biome.BaseMoisture : 1f;
                            EventBroker.Instance.Publish(Core.EventType.TileRecovered, tile);
                            recoveredCount++;
                        }
                    }
                }
            }
            Debug.Log($"[FireEngine] Recovered {recoveredCount} burnt tiles.");
        }

        // ── Queries ──────────────────────────────────────────────────────

        /// <summary>Current number of actively burning tiles.</summary>
        public int BurningTileCount => burningTiles.Count;

        /// <summary>Get a read-only snapshot of all currently burning tiles.</summary>
        public List<Tile> GetBurningTiles() => new List<Tile>(burningTiles);
    }
}
