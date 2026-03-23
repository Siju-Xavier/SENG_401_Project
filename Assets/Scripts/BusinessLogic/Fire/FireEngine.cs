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

        private void Start() {
            // Auto-find references to make setup easier
            if (weatherSystem == null) weatherSystem = FindFirstObjectByType<WeatherSystem>();
            if (gridSystem == null) gridSystem = FindFirstObjectByType<GridSystem>();
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
                }
            }

            // Burn out tiles that reached max intensity
            foreach (var tile in burnedOut) {
                tile.IsOnFire = false;
                tile.FireIntensity = 0f;
                burningTiles.Remove(tile);
                EventBroker.Instance.Publish(Core.EventType.FireExtinguished, tile);
            }

            // Remove tiles that somehow got extinguished mid-tick
            burningTiles.RemoveAll(t => !t.IsOnFire);
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
                // Skip tiles already on fire
                if (neighbour.IsOnFire) continue;

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
                float probability = baseSpreadChance
                    * biomeMultiplier
                    * windBonus
                    * (1f + intensityBoost)
                    * (1f - moisturePenalty * 0.7f);

                probability = Mathf.Clamp01(probability);

                if (Random.value < probability) {
                    IgniteTile(neighbour);
                }
            }
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

            int firesToStart = count > 0 ? count : initialFireCount;
            int attempts = 0;
            int started  = 0;
            int nullTiles = 0;
            int alreadyOnFire = 0;
            int waterTiles = 0;

            while (started < firesToStart && attempts < firesToStart * 20) {
                attempts++;
                int rx = Random.Range(0, gridSystem.Width);
                int ry = Random.Range(0, gridSystem.Height);
                Tile tile = gridSystem.GetTileAt(rx, ry);

                if (tile == null) { nullTiles++; continue; }
                if (tile.IsOnFire) { alreadyOnFire++; continue; }

                // Skip water biome (very high moisture / no spread)
                if (tile.Biome != null && tile.Biome.SpreadMultiplier <= 0f) { waterTiles++; continue; }

                IgniteTile(tile);
                started++;
            }

            Debug.Log($"[FireEngine] Started {started} random fires. (attempts={attempts}, nullTiles={nullTiles}, water={waterTiles}, alreadyOnFire={alreadyOnFire})");
        }

        // ── Queries ──────────────────────────────────────────────────────

        /// <summary>Current number of actively burning tiles.</summary>
        public int BurningTileCount => burningTiles.Count;

        /// <summary>Get a read-only snapshot of all currently burning tiles.</summary>
        public List<Tile> GetBurningTiles() => new List<Tile>(burningTiles);
    }
}
