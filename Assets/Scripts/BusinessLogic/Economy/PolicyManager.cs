namespace BusinessLogic {
    using System.Collections.Generic;
    using GameState;
    using Core;
    using ScriptableObjects;
    using UnityEngine;

    public class PolicyManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progression;
        
        [Header("Debug")]
        [Tooltip("Check this to visualize policy UI modifiers in the City Panel even when no policies are active.")]
        public bool ForceShowUIModifiers = false;
        
        // Track active policies per Region
        private Dictionary<Region, List<PolicyConfig>> activePolicies = new Dictionary<Region, List<PolicyConfig>>();

        public static PolicyManager Instance { get; private set; }

        private void Awake() {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void AddPolicy(PolicyConfig policyConfig, Region region) {
            if (policyConfig == null || region == null) return;
            
            if (progression != null && progression.CurrentLevel < policyConfig.RequiredLevel) {
                Debug.LogWarning($"[PolicyManager] Cannot apply {policyConfig.name}: Requires Level {policyConfig.RequiredLevel}, current is {progression.CurrentLevel}");
                return;
            }

            if (!activePolicies.ContainsKey(region)) {
                activePolicies[region] = new List<PolicyConfig>();
            }

            if (!activePolicies[region].Contains(policyConfig)) {
                activePolicies[region].Add(policyConfig);
                Debug.Log($"[PolicyManager] Applied policy '{policyConfig.name}' to region '{region.RegionName}'");
            }
        }

        public void RemovePolicyFromEngine(PolicyConfig policyConfig, Region region) {
            if (activePolicies.ContainsKey(region) && activePolicies[region].Contains(policyConfig)) {
                activePolicies[region].Remove(policyConfig);
                Debug.Log($"[PolicyManager] Removed policy '{policyConfig.name}' from region '{region.RegionName}'");
            }
        }

        public float GetSpreadModifierForRegion(Region region) {
            if (region == null || !activePolicies.ContainsKey(region)) return 1.0f;

            float combinedModifier = 1.0f;
            foreach (var policy in activePolicies[region]) {
                combinedModifier *= policy.SpreadReductionModifier;
            }
            return combinedModifier;
        }

        public float GetSpawnModifierForRegion(Region region) {
            if (region == null || !activePolicies.ContainsKey(region)) return 1.0f;

            float combinedModifier = 1.0f;
            foreach (var policy in activePolicies[region]) {
                combinedModifier *= policy.SpawnReductionModifier;
            }
            return combinedModifier;
        }
    }
}
