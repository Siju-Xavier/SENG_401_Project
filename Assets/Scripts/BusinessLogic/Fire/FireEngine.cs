// ============================================================================
// FireEngine.cs — Rate-based continuous fire simulation
// ============================================================================
// Per the UML:
//   FireEngine ..> WeatherSystem : reads_wind_&_season
//   FireEngine ..> EventBroker   : publishes & subscribes
//   FireEngine ..> Tile          : reads & modifies
//   Tile       ..> BiomeConfig   : reads (spreadMultiplier, baseMoisture)
//
// Fire ignition and spread use rate-based accumulators (fires/sec, spreads/sec)
// driven by ProgressionManager level scaling.
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

        [Header("Fire Tuning")]
        [Tooltip("How much wind direction boosts spread rate (0 = none, 1 = double).")]
        [SerializeField] private float windInfluence = 0.6f;

        [Tooltip("Intensity gained per second by a burning tile.")]
        [SerializeField] private float burnIntensityPerSecond = 0.1f;

        [Tooltip("Maximum fire intensity a tile can reach (burns out at this level).")]
        [SerializeField] private float burnoutThreshold = 5f;

        [Tooltip("Moisture consumed per second on a burning tile.")]
        [SerializeField] private float moistureDecayPerSecond = 0.05f;

        [Header("Initial Fires")]
        [Tooltip("Number of random fires to ignite on game start.")]
        [SerializeField] private int initialFireCount = 3;

        // ── Runtime state ────────────────────────────────────────────────
        private bool  isRunning;
        private float ignitionAccumulator;

        // Track all tiles that are currently on fire for efficient iteration
        private List<Tile> burningTiles = new List<Tile>();

        // Per-tile spread accumulators
        private Dictionary<Tile, float> spreadAccumulators = new Dictionary<Tile, float>();

        // ── Public API (called by GameManager) ───────────────────────────

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

            float dt = Time.deltaTime;

            // ── Auto-ignition (rate-based) ──
            UpdateIgnition(dt);

            // ── Process burning tiles ──
            UpdateBurningTiles(dt);
        }

        // ── Rate-Based Ignition ──────────────────────────────────────────

        /// <summary>Accumulate ignition rate and spawn fires when accumulator >= 1.</summary>
        private void UpdateIgnition(float dt) {
            int currentLevel = progressionManager != null ? progressionManager.CurrentLevel : 1;

            // No auto-ignition on level 1 (only initial fires from StartRandomFires)
            if (currentLevel <= 1) return;

            float ignitionRate = progressionManager != null ? progressionManager.GetIgnitionRate() : 0.1f;

            ignitionAccumulator += ignitionRate * dt;

            int maxAttempts = 10; // Prevent infinite loop if no valid tiles
            while (ignitionAccumulator >= 1f && maxAttempts > 0) {
                ignitionAccumulator -= 1f;
                maxAttempts--;

                // Pick a random tile to ignite
                int rx = Random.Range(0, gridSystem.Width);
                int ry = Random.Range(0, gridSystem.Height);
                Tile tile = gridSystem.GetTileAt(rx, ry);

                if (tile == null || tile.IsOnFire || tile.IsBurnt
                    || tile.Biome == null || tile.Biome.SpreadMultiplier <= 0f) continue;

                // Policy modifier can reduce ignition in protected regions
                float policyMod = PolicyManager.Instance != null
                    ? PolicyManager.Instance.GetSpawnModifierForRegion(tile.Region) : 1.0f;

                // Use policy as a probability gate (e.g. fire ban = 0.5 means 50% chance to skip)
                if (policyMod < 1.0f && Random.value > policyMod) continue;

                Debug.Log($"[FireEngine] Auto-ignited ({tile.X},{tile.Y}) rate={ignitionRate:F2}/s");
                IgniteTile(tile);
            }

            // Cap accumulator to prevent burst after pause
            ignitionAccumulator = Mathf.Min(ignitionAccumulator, 2f);
        }

        // ── Rate-Based Burning / Spread ──────────────────────────────────

        /// <summary>Update intensity, moisture, spread, and burnout for all burning tiles.</summary>
        private void UpdateBurningTiles(float dt) {
            var snapshot = new List<Tile>(burningTiles);
            var burnedOut = new List<Tile>();

            float baseSpreadRate = progressionManager != null ? progressionManager.GetSpreadRate() : 0.1f;

            Vector2 windDir = weatherSystem != null
                ? weatherSystem.GetNextWindDirection().normalized
                : Vector2.zero;

            foreach (var tile in snapshot) {
                if (!tile.IsOnFire) continue;

                // ── Grow intensity (per second) ──
                tile.FireIntensity = Mathf.Min(tile.FireIntensity + burnIntensityPerSecond * dt, burnoutThreshold);

                // ── Burn moisture (per second) ──
                tile.MoistureLevel = Mathf.Max(tile.MoistureLevel - moistureDecayPerSecond * dt, 0f);

                // ── Rate-based spread ──
                UpdateSpread(tile, baseSpreadRate, windDir, dt);

                // ── Burn out: tile reaches max intensity ──
                if (tile.FireIntensity >= burnoutThreshold) {
                    burnedOut.Add(tile);
                    tile.IsBurnt = true;
                }
            }

            // Process burned out tiles
            foreach (var tile in burnedOut) {
                tile.IsOnFire = false;
                tile.FireIntensity = 0f;
                burningTiles.Remove(tile);
                spreadAccumulators.Remove(tile);
                EventBroker.Instance.Publish(Core.EventType.FireExtinguished, tile);
            }

            // Clean up tiles extinguished by firefighters mid-frame
            burningTiles.RemoveAll(t => !t.IsOnFire);
        }

        /// <summary>
        /// Rate-based spread: accumulate spread rate per burning tile.
        /// When accumulator >= 1, attempt to spread to a random valid neighbour.
        /// Rate is affected by biome, wind, moisture, intensity, and policy.
        /// </summary>
        private void UpdateSpread(Tile sourceTile, float baseRate, Vector2 windDir, float dt) {
            if (gridSystem == null) return;

            // Get or create accumulator for this tile
            if (!spreadAccumulators.ContainsKey(sourceTile))
                spreadAccumulators[sourceTile] = 0f;

            // Intensity boost — more intense fires spread faster
            float intensityBoost = sourceTile.FireIntensity / burnoutThreshold; // 0 to 1

            // Policy modifier for spreading from this tile's region
            float policyMod = PolicyManager.Instance != null
                ? PolicyManager.Instance.GetSpreadModifierForRegion(sourceTile.Region) : 1.0f;

            // Accumulate spread rate
            float effectiveRate = baseRate * (1f + intensityBoost) * policyMod;
            spreadAccumulators[sourceTile] += effectiveRate * dt;

            // Cap to prevent burst
            spreadAccumulators[sourceTile] = Mathf.Min(spreadAccumulators[sourceTile], 3f);

            // Attempt spreads
            while (spreadAccumulators[sourceTile] >= 1f) {
                spreadAccumulators[sourceTile] -= 1f;

                var neighbours = gridSystem.GetNeighbours(sourceTile);
                // Build list of valid spread targets
                var validTargets = new List<Tile>();
                var targetWeights = new List<float>();

                foreach (var neighbour in neighbours) {
                    if (neighbour.IsOnFire || neighbour.IsBurnt) continue;
                    if (neighbour.Biome == null || neighbour.Biome.SpreadMultiplier <= 0f) continue;

                    // Wind bonus for this direction
                    Vector2 spreadDir = new Vector2(
                        neighbour.X - sourceTile.X,
                        neighbour.Y - sourceTile.Y
                    ).normalized;
                    float windDot = Vector2.Dot(windDir, spreadDir);
                    float windBonus = 1f + windDot * windInfluence;

                    // Moisture penalty
                    float moistureFactor = 1f - neighbour.MoistureLevel * 0.7f;

                    // Biome multiplier
                    float weight = neighbour.Biome.SpreadMultiplier * windBonus * moistureFactor;
                    if (weight > 0f) {
                        validTargets.Add(neighbour);
                        targetWeights.Add(weight);
                    }
                }

                if (validTargets.Count == 0) break;

                // Weighted random selection — fire prefers downwind, dry, flammable neighbours
                float totalWeight = 0f;
                foreach (var w in targetWeights) totalWeight += w;

                float roll = Random.value * totalWeight;
                float cumulative = 0f;
                for (int i = 0; i < validTargets.Count; i++) {
                    cumulative += targetWeights[i];
                    if (roll <= cumulative) {
                        IgniteTile(validTargets[i]);
                        break;
                    }
                }
            }
        }

        // ── Ignition / Extinguish ────────────────────────────────────────

        /// <summary>Set a tile on fire and publish events.</summary>
        public void IgniteTile(Tile tile) {
            if (tile == null || tile.IsOnFire || tile.IsBurnt) return;

            // Don't ignite tiles with no biome or water biome
            if (tile.Biome == null || tile.Biome.SpreadMultiplier <= 0f) return;

            tile.IsOnFire      = true;
            tile.FireIntensity = 0.5f; // Starts at 0.5f and grows until 'burnoutThreshold' (5.0f defaults)

            if (!burningTiles.Contains(tile))
                burningTiles.Add(tile);

            // Publish events so Presentation layer can update visuals
            EventBroker.Instance.Publish(Core.EventType.FireStarted, tile);
            EventBroker.Instance.Publish(Core.EventType.FireSpread, tile);

            Debug.Log($"[FireEngine] Tile ({tile.X},{tile.Y}) ignited.");
        }

        /// <summary>Extinguish a tile and publish events.</summary>
        public void ExtinguishTile(Tile tile) {
            if (tile == null || !tile.IsOnFire || tile.IsBurnt) return;

            tile.IsOnFire      = false;
            tile.FireIntensity = 0f;
            // Restore a bit of moisture
            tile.MoistureLevel = Mathf.Min(tile.MoistureLevel + 0.3f, 1f);

            burningTiles.Remove(tile);
            spreadAccumulators.Remove(tile);

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
                // Mark as burnt and extinguish directly (bypass ExtinguishTile guard)
                tile.IsBurnt = true;
                tile.IsOnFire = false;
                tile.FireIntensity = 0f;
                burningTiles.Remove(tile);
                EventBroker.Instance.Publish(Core.EventType.FireExtinguished, tile);
            }
        }

        /// <summary>
        /// Recovers burnt tiles at the edges of burn patches. Only burnt tiles
        /// adjacent to at least one healthy (non-burnt) tile can recover,
        /// creating a natural "healing inward" effect over multiple rounds.
        /// </summary>
        public void RecoverBurntTiles(float recoveryFraction) {
            if (gridSystem == null) return;
            int recoveredCount = 0;

            // First pass: collect edge burnt tiles (adjacent to a healthy tile)
            var edgeBurnt = new List<Tile>();
            for (int x = 0; x < gridSystem.Width; x++) {
                for (int y = 0; y < gridSystem.Height; y++) {
                    Tile tile = gridSystem.GetTileAt(x, y);
                    if (tile == null || !tile.IsBurnt) continue;

                    // Check if any neighbour is healthy (not burnt, not on fire, and flammable)
                    var neighbours = gridSystem.GetNeighbours(tile);
                    foreach (var neighbour in neighbours) {
                        if (!neighbour.IsBurnt && !neighbour.IsOnFire
                            && neighbour.Biome != null && neighbour.Biome.SpreadMultiplier > 0f) {
                            edgeBurnt.Add(tile);
                            break;
                        }
                    }
                }
            }

            // Second pass: randomly recover from the edge candidates
            foreach (var tile in edgeBurnt) {
                if (Random.value < recoveryFraction) {
                    tile.IsBurnt = false;
                    tile.MoistureLevel = tile.Biome != null ? tile.Biome.BaseMoisture : 1f;
                    EventBroker.Instance.Publish(Core.EventType.TileRecovered, tile);
                    recoveredCount++;
                }
            }

            Debug.Log($"[FireEngine] Recovered {recoveredCount}/{edgeBurnt.Count} edge burnt tiles.");
        }

        // ── Queries ──────────────────────────────────────────────────────

        /// <summary>Current number of actively burning tiles.</summary>
        public int BurningTileCount => burningTiles.Count;

        /// <summary>Get a read-only snapshot of all currently burning tiles.</summary>
        public List<Tile> GetBurningTiles() => new List<Tile>(burningTiles);
    }
}
