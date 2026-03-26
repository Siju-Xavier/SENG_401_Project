namespace ScriptableObjects {
    using UnityEngine;

    [CreateAssetMenu(fileName = "PolicyConfig", menuName = "Config/Policy")]
    public class PolicyConfig : ScriptableObject {
        [Header("Display")]
        [SerializeField] private string policyName = "";
        [TextArea(2, 4)]
        [SerializeField] private string description = "";

        [Header("Requirements")]
        [SerializeField] private int requiredLevel = 1;
        [SerializeField] private bool requiresMultipleCities;

        [Header("Ongoing Cost")]
        [Tooltip("Money deducted from city budget per second while active")]
        [SerializeField] private float costPerSecond;

        [Header("Fire Modifiers")]
        [Tooltip("Multiplier on fire spread chance (0.8 = 20% reduction)")]
        [SerializeField] private float spreadReductionModifier = 1.0f;
        [Tooltip("Multiplier on fire ignition chance (0.5 = 50% reduction)")]
        [SerializeField] private float spawnReductionModifier = 1.0f;

        [Header("Recovery")]
        [Tooltip("Additive bonus to land recovery rate at round end (0.25 = +25%)")]
        [SerializeField] private float recoveryRateBonus;

        [Header("Income Redistribution")]
        [Tooltip("Fraction of income redistributed to other cities (0.3 = 30%)")]
        [Range(0f, 1f)]
        [SerializeField] private float incomeRedistributionRate;

        public string PolicyName => string.IsNullOrEmpty(policyName) ? name : policyName;
        public string Description => description;
        public int RequiredLevel => requiredLevel;
        public bool RequiresMultipleCities => requiresMultipleCities;
        public float CostPerSecond => costPerSecond;
        public float SpreadReductionModifier => spreadReductionModifier;
        public float SpawnReductionModifier => spawnReductionModifier;
        public float RecoveryRateBonus => recoveryRateBonus;
        public float IncomeRedistributionRate => incomeRedistributionRate;
    }
}
