namespace ScriptableObjects
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Config/Economy")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("City Income")]
        [Tooltip("Base income each city receives per round")]
        [SerializeField] private int baseIncomePerRound = 500;

        [Tooltip("Additional income per level (additive: baseIncome + level * this)")]
        [SerializeField] private int incomeIncreasePerLevel = 50;

        [Header("Deployment Costs")]
        [Tooltip("Base cost to deploy a firefighter")]
        [SerializeField] private int baseDeploymentCost = 100;

        [Tooltip("Additional cost per level (additive: baseCost + level * this)")]
        [SerializeField] private int costIncreasePerLevel = 10;

        [Header("Initial Values")]
        [SerializeField] private int initialCityBudget = 1000;

        public int BaseIncomePerRound => baseIncomePerRound;
        public int IncomeIncreasePerLevel => incomeIncreasePerLevel;
        public int BaseDeploymentCost => baseDeploymentCost;
        public int CostIncreasePerLevel => costIncreasePerLevel;
        public int InitialCityBudget => initialCityBudget;
    }
}
