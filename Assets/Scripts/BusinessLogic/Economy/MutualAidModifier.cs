namespace BusinessLogic
{
    using System.Collections.Generic;
    using GameState;
    using UnityEngine;

    public class MutualAidModifier : ICityIncomeModifier
    {
        public string ModifierName => "Mutual Aid Pact";
        public int Priority => 100; // runs after other modifiers
        public bool IsActive => true;

        private List<City> allCities;

        public MutualAidModifier(List<City> cities)
        {
            allCities = cities;
        }

        public int ModifyIncome(City city, int currentIncome, int currentLevel)
        {
            if (city == null || PolicyManager.Instance == null) return currentIncome;

            // Find this city's region
            Region region = FindRegionForCity(city);
            if (region == null) return currentIncome;

            float redistRate = PolicyManager.Instance.GetIncomeRedistributionRate(region);
            if (redistRate <= 0f) return currentIncome;

            // Calculate amount to redistribute
            int redistributeAmount = Mathf.FloorToInt(currentIncome * redistRate);
            if (redistributeAmount <= 0) return currentIncome;

            // Count other cities to distribute to
            int otherCount = 0;
            foreach (var c in allCities)
                if (c != city) otherCount++;

            if (otherCount == 0) return currentIncome;

            // Distribute evenly to other cities
            int perCity = redistributeAmount / otherCount;
            if (perCity > 0)
            {
                foreach (var c in allCities)
                {
                    if (c != city)
                        c.Budget += perCity;
                }
            }

            return currentIncome - redistributeAmount;
        }

        private Region FindRegionForCity(City city)
        {
            // Find region via GridSystem
            var gridSystem = Object.FindFirstObjectByType<GameState.GridSystem>();
            if (gridSystem == null) return null;
            foreach (var region in gridSystem.Regions)
            {
                if (region.City == city) return region;
            }
            return null;
        }
    }
}
