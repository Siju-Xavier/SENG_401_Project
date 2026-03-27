// ============================================================================
// CascadingFailureManager.cs — Cascading failure system (REQ-007)
// ============================================================================
// When a region's fire damage crosses a threshold, neighboring regions
// receive increased fire ignition and spread pressure. This creates a
// chain reaction that punishes neglect and rewards proactive response.
//
// Integration:
//   FireEngine queries GetIgnitionMultiplier / GetSpreadMultiplier per region.
//   These return 1.0 when no cascade is active, and > 1.0 when neighbors
//   are heavily damaged.
// ============================================================================

namespace BusinessLogic {
    using System.Collections.Generic;
    using GameState;
    using Core;
    using UnityEngine;

    public class CascadingFailureManager : MonoBehaviour {

        // ── Inspector tunables ───────────────────────────────────────────

        [Header("Cascade Thresholds")]
        [Tooltip("Region damage ratio (0-1) at which cascade begins affecting neighbors.")]
        [SerializeField] private float cascadeThreshold = 0.3f;

        [Tooltip("Maximum multiplier applied to neighbor ignition/spread rates at 100% damage.")]
        [SerializeField] private float maxCascadeMultiplier = 2.0f;

        [Header("Cascade Tuning")]
        [Tooltip("How much cascade affects ignition rate relative to spread rate (0-1). " +
                 "1.0 = full effect on ignition, 0.5 = half effect on ignition.")]
        [SerializeField] private float ignitionCascadeWeight = 0.7f;

        [Tooltip("How much cascade affects spread rate relative to max (0-1). " +
                 "1.0 = full effect on spread, 0.5 = half effect on spread.")]
        [SerializeField] private float spreadCascadeWeight = 1.0f;

        [Tooltip("Seconds between recalculating cascade pressure (performance optimization).")]
        [SerializeField] private float updateInterval = 1.0f;

        // ── References ──────────────────────────────────────────────────

        private GridSystem gridSystem;
        private float updateTimer;

        // Per-region cached cascade data
        private Dictionary<Region, float> regionDamageRatios = new Dictionary<Region, float>();
        private Dictionary<Region, float> regionIgnitionMultipliers = new Dictionary<Region, float>();
        private Dictionary<Region, float> regionSpreadMultipliers = new Dictionary<Region, float>();

        // Track which regions have already triggered a cascade event (to avoid spamming)
        private HashSet<Region> cascadeTriggeredRegions = new HashSet<Region>();

        // ── Public API ──────────────────────────────────────────────────

        public void SetGridSystem(GridSystem grid) {
            gridSystem = grid;
            ResetCascadeState();
        }

        /// <summary>
        /// Ignition multiplier for a specific region due to cascading failures
        /// from damaged neighbors. Returns 1.0 when no cascade is active.
        /// </summary>
        public float GetIgnitionMultiplier(Region region) {
            if (region == null) return 1f;
            return regionIgnitionMultipliers.TryGetValue(region, out float mult) ? mult : 1f;
        }

        /// <summary>
        /// Spread multiplier for a specific region due to cascading failures
        /// from damaged neighbors. Returns 1.0 when no cascade is active.
        /// </summary>
        public float GetSpreadMultiplier(Region region) {
            if (region == null) return 1f;
            return regionSpreadMultipliers.TryGetValue(region, out float mult) ? mult : 1f;
        }

        /// <summary>
        /// Current damage ratio (0-1) for a region. Useful for UI display.
        /// </summary>
        public float GetDamageRatio(Region region) {
            if (region == null) return 0f;
            return regionDamageRatios.TryGetValue(region, out float ratio) ? ratio : 0f;
        }

        /// <summary>
        /// Whether a region is currently causing cascade pressure on its neighbors.
        /// </summary>
        public bool IsCascading(Region region) {
            if (region == null) return false;
            return regionDamageRatios.TryGetValue(region, out float ratio) && ratio >= cascadeThreshold;
        }

        /// <summary>Reset all cascade state (e.g. on new game or round reset).</summary>
        public void ResetCascadeState() {
            regionDamageRatios.Clear();
            regionIgnitionMultipliers.Clear();
            regionSpreadMultipliers.Clear();
            cascadeTriggeredRegions.Clear();
        }

        // ── Unity Lifecycle ─────────────────────────────────────────────

        private void Update() {
            if (gridSystem == null) return;

            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0f) {
                updateTimer = updateInterval;
                RecalculateCascade();
            }
        }

        // ── Core Cascade Logic ──────────────────────────────────────────

        private void RecalculateCascade() {
            var regions = gridSystem.Regions;
            if (regions == null || regions.Count <= 1) return;

            // Step 1: Calculate damage ratio for each region
            foreach (var region in regions) {
                float damageRatio = CalculateDamageRatio(region);
                regionDamageRatios[region] = damageRatio;

                // Publish event the first time a region crosses the threshold
                if (damageRatio >= cascadeThreshold && !cascadeTriggeredRegions.Contains(region)) {
                    cascadeTriggeredRegions.Add(region);
                    EventBroker.Instance.Publish(Core.EventType.CascadeTriggered, region);
                    Debug.Log($"[Cascade] Region '{region.RegionName}' crossed cascade threshold " +
                              $"({damageRatio:P0} >= {cascadeThreshold:P0}) — pressuring neighbors.");
                }

                // Clear trigger if region recovers below threshold
                if (damageRatio < cascadeThreshold && cascadeTriggeredRegions.Contains(region)) {
                    cascadeTriggeredRegions.Remove(region);
                    Debug.Log($"[Cascade] Region '{region.RegionName}' recovered below cascade threshold.");
                }
            }

            // Step 2: For each region, sum cascade pressure from all OTHER damaged regions
            foreach (var targetRegion in regions) {
                float totalIgnitionPressure = 0f;
                float totalSpreadPressure = 0f;

                foreach (var sourceRegion in regions) {
                    if (sourceRegion == targetRegion) continue;

                    float sourceDamage = regionDamageRatios.TryGetValue(sourceRegion, out float d) ? d : 0f;
                    if (sourceDamage < cascadeThreshold) continue;

                    // Scale pressure linearly from threshold to 1.0
                    float pressure = (sourceDamage - cascadeThreshold) / (1f - cascadeThreshold);
                    pressure = Mathf.Clamp01(pressure);

                    totalIgnitionPressure += pressure;
                    totalSpreadPressure += pressure;
                }

                // Convert pressure to multipliers (capped at maxCascadeMultiplier)
                float ignitionMult = 1f + totalIgnitionPressure * (maxCascadeMultiplier - 1f) * ignitionCascadeWeight;
                float spreadMult = 1f + totalSpreadPressure * (maxCascadeMultiplier - 1f) * spreadCascadeWeight;

                regionIgnitionMultipliers[targetRegion] = Mathf.Min(ignitionMult, maxCascadeMultiplier);
                regionSpreadMultipliers[targetRegion] = Mathf.Min(spreadMult, maxCascadeMultiplier);
            }
        }

        /// <summary>
        /// Calculates what fraction of a region's flammable tiles are burning or burnt.
        /// </summary>
        private float CalculateDamageRatio(Region region) {
            var tiles = region.Tiles;
            if (tiles == null || tiles.Count == 0) return 0f;

            int flammableCount = 0;
            int damagedCount = 0;

            foreach (var tile in tiles) {
                // Only count flammable tiles (skip water, etc.)
                if (tile.Biome == null || tile.Biome.SpreadMultiplier <= 0f) continue;

                flammableCount++;
                if (tile.IsOnFire || tile.IsBurnt) {
                    damagedCount++;
                }
            }

            if (flammableCount == 0) return 0f;
            return (float)damagedCount / flammableCount;
        }
    }
}
