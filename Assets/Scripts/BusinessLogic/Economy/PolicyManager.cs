namespace BusinessLogic {
    using System.Collections.Generic;
    using GameState;
    using ScriptableObjects;
    using UnityEngine;

    public class PolicyManager : MonoBehaviour {
        private ProgressionManager progressionManager;

        private Dictionary<Region, List<PolicyConfig>> activePolicies = new Dictionary<Region, List<PolicyConfig>>();
        private Dictionary<Region, float> costAccumulators = new Dictionary<Region, float>();

        public static PolicyManager Instance { get; private set; }

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start() {
            progressionManager = FindFirstObjectByType<ProgressionManager>();
            if (progressionManager == null)
                Debug.LogWarning("[PolicyManager] ProgressionManager not found in scene!");
        }

        private int CurrentLevel => progressionManager != null ? progressionManager.CurrentLevel : 1;

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        private void Update() {
            // Deduct per-second costs for active policies
            var regionsToClean = new List<Region>();

            foreach (var kvp in activePolicies) {
                var region = kvp.Key;
                var policies = kvp.Value;
                if (region?.City == null || policies.Count == 0) continue;

                float totalCost = 0f;
                foreach (var policy in policies)
                    totalCost += policy.CostPerSecond;

                if (totalCost <= 0f) continue;

                if (!costAccumulators.ContainsKey(region))
                    costAccumulators[region] = 0f;

                costAccumulators[region] += totalCost * Time.deltaTime;

                if (costAccumulators[region] >= 1f) {
                    int deduct = Mathf.FloorToInt(costAccumulators[region]);
                    costAccumulators[region] -= deduct;

                    if (region.City.Budget >= deduct) {
                        region.City.Budget -= deduct;
                    } else {
                        // City can't afford — remove most expensive policy
                        regionsToClean.Add(region);
                    }
                }
            }

            foreach (var region in regionsToClean)
                RemoveMostExpensivePolicy(region);
        }

        // ── Add / Remove ──

        public void AddPolicy(PolicyConfig policy, Region region) {
            if (policy == null || region == null) return;

            if (CurrentLevel < policy.RequiredLevel) {
                Debug.LogWarning($"[PolicyManager] Cannot apply {policy.PolicyName}: Requires Level {policy.RequiredLevel} (current: {CurrentLevel})");
                return;
            }

            if (!activePolicies.ContainsKey(region))
                activePolicies[region] = new List<PolicyConfig>();

            if (!activePolicies[region].Contains(policy)) {
                activePolicies[region].Add(policy);
                Debug.Log($"[PolicyManager] Enacted '{policy.PolicyName}' in '{region.RegionName}'");
            }
        }

        public void RemovePolicy(PolicyConfig policy, Region region) {
            if (region == null || policy == null) return;
            if (activePolicies.ContainsKey(region)) {
                activePolicies[region].Remove(policy);
                Debug.Log($"[PolicyManager] Revoked '{policy.PolicyName}' from '{region.RegionName}'");
            }
        }

        public bool IsPolicyActive(PolicyConfig policy, Region region) {
            if (region == null || policy == null) return false;
            return activePolicies.ContainsKey(region) && activePolicies[region].Contains(policy);
        }

        // ── Modifiers ──

        public float GetSpreadModifierForRegion(Region region) {
            if (region == null || !activePolicies.ContainsKey(region)) return 1.0f;
            float mod = 1.0f;
            foreach (var p in activePolicies[region])
                mod *= p.SpreadReductionModifier;
            return mod;
        }

        public float GetSpawnModifierForRegion(Region region) {
            if (region == null || !activePolicies.ContainsKey(region)) return 1.0f;
            float mod = 1.0f;
            foreach (var p in activePolicies[region])
                mod *= p.SpawnReductionModifier;
            return mod;
        }

        public float GetRecoveryBonusForRegion(Region region) {
            if (region == null || !activePolicies.ContainsKey(region)) return 0f;
            float bonus = 0f;
            foreach (var p in activePolicies[region])
                bonus += p.RecoveryRateBonus;
            return bonus;
        }

        public float GetIncomeRedistributionRate(Region region) {
            if (region == null || !activePolicies.ContainsKey(region)) return 0f;
            float rate = 0f;
            foreach (var p in activePolicies[region])
                rate += p.IncomeRedistributionRate;
            return Mathf.Clamp01(rate);
        }

        public List<PolicyConfig> GetActivePolicies(Region region) {
            if (region == null || !activePolicies.ContainsKey(region))
                return new List<PolicyConfig>();
            return new List<PolicyConfig>(activePolicies[region]);
        }

        // ── Helpers ──

        private void RemoveMostExpensivePolicy(Region region) {
            if (!activePolicies.ContainsKey(region) || activePolicies[region].Count == 0) return;
            PolicyConfig mostExpensive = null;
            float maxCost = -1f;
            foreach (var p in activePolicies[region]) {
                if (p.CostPerSecond > maxCost) {
                    maxCost = p.CostPerSecond;
                    mostExpensive = p;
                }
            }
            if (mostExpensive != null) {
                activePolicies[region].Remove(mostExpensive);
                Debug.LogWarning($"[PolicyManager] Auto-revoked '{mostExpensive.PolicyName}' in '{region.RegionName}' — city can't afford it");
            }
        }
    }
}
