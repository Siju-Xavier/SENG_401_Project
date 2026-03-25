namespace ScriptableObjects
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Config/Economy")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Continuous Income")]
        [Tooltip("Base income per second for each city.")]
        [SerializeField] private float baseIncomePerSecond = 1.0f;

        [Tooltip("Increase in income per second per level.")]
        [SerializeField] private float incomeIncreasePerSecondPerLevel = 0.2f;

        [Header("Round Rewards")]
        [Tooltip("Money awarded per unburnt tile in a city's region at the end of a round.")]
        [SerializeField] private int rewardPerUnburntTile = 2;

        [Header("Deployment Costs")]
        [Tooltip("Base cost to deploy a firefighter")]
        [SerializeField] private int baseDeploymentCost = 100;

        [Tooltip("Additional cost per level (additive: baseCost + level * this)")]
        [SerializeField] private int costIncreasePerLevel = 10;

        [Header("Initial Values")]
        [SerializeField] private int initialCityBudget = 1000;

        public float BaseIncomePerSecond => baseIncomePerSecond;
        public float IncomeIncreasePerSecondPerLevel => incomeIncreasePerSecondPerLevel;
        public int RewardPerUnburntTile => rewardPerUnburntTile;
        public int BaseDeploymentCost => baseDeploymentCost;
        public int CostIncreasePerLevel => costIncreasePerLevel;
        public int InitialCityBudget => initialCityBudget;
    }
}
