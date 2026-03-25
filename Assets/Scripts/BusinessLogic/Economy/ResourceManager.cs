namespace BusinessLogic {
    using System.Collections.Generic;
    using GameState;
    using Core;
    using ScriptableObjects;
    using Presentation;
    using UnityEngine;

    public class ResourceManager : MonoBehaviour {
        [SerializeField] private UnitConfig firefighterConfig;
        [SerializeField] private EconomyConfig economyConfig;

        private FireEngine fireEngine;
        private GridSystem gridSystem;
        private UnityEngine.Tilemaps.Tilemap groundTilemap;
        private List<City> managedCities = new List<City>();
        private List<GameObject> activeUnits = new List<GameObject>();
        private List<ICityIncomeModifier> incomeModifiers = new List<ICityIncomeModifier>();

        private int globalAvailableBudget;

        public int GlobalBudget => globalAvailableBudget;

        private void Start() {
#if UNITY_EDITOR
            if (firefighterConfig == null) {
                firefighterConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitConfig>("Assets/Sprites/ScriptableObjects/UnitConfig.asset");
                Debug.LogWarning("[ResourceManager] Auto-assigned firefighterConfig from Assets.");
            }
            if (economyConfig == null) {
                economyConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/Sprites/ScriptableObjects/EconomyConfig.asset");
                Debug.LogWarning("[ResourceManager] Auto-assigned economyConfig from Assets.");
            }
#endif
            fireEngine = FindFirstObjectByType<FireEngine>();
            groundTilemap = FindFirstObjectByType<UnityEngine.Tilemaps.Tilemap>();
        }

        public void Initialize(List<Region> regions) {
            managedCities.Clear();
            if (regions == null) return;

            foreach (var region in regions) {
                if (region.City != null)
                    managedCities.Add(region.City);
            }

            RecalculateGlobalBudget();
        }

        public void SetGridSystem(GridSystem grid) {
            gridSystem = grid;
        }

        // ── Income Modifier Pipeline (Open-Closed) ───────────────────────

        public void RegisterIncomeModifier(ICityIncomeModifier modifier) {
            incomeModifiers.Add(modifier);
            incomeModifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        public void UnregisterIncomeModifier(ICityIncomeModifier modifier) {
            incomeModifiers.Remove(modifier);
        }

        // ── Economy ──────────────────────────────────────────────────────

        public int GetDeploymentCost(int currentLevel) {
            if (economyConfig == null) return 100;
            return economyConfig.BaseDeploymentCost + currentLevel * economyConfig.CostIncreasePerLevel;
        }

        public void AddRoundBudget(int currentLevel) {
            int baseIncome = economyConfig != null
                ? economyConfig.BaseIncomePerRound + currentLevel * economyConfig.IncomeIncreasePerLevel
                : 500;

            foreach (var city in managedCities) {
                int income = baseIncome;

                // Apply registered modifiers (policies, bonuses, etc.)
                foreach (var modifier in incomeModifiers) {
                    if (modifier.IsActive)
                        income = modifier.ModifyIncome(city, income, currentLevel);
                }

                city.Budget += income;
                EventBroker.Instance.Publish(Core.EventType.IncomeGenerated, city);
            }

            RecalculateGlobalBudget();
            EventBroker.Instance.Publish(Core.EventType.BudgetChanged, globalAvailableBudget);
            Debug.Log($"[ResourceManager] Round income distributed. Total: {globalAvailableBudget}");
        }

        // ── Deployment ───────────────────────────────────────────────────

        public void DeployFirefighter(Tile targetTile) {
            if (firefighterConfig == null || firefighterConfig.UnitPrefab == null) {
                Debug.LogWarning("[ResourceManager] No firefighter config or prefab assigned.");
                return;
            }

            // Skip already-assigned tiles
            if (TileAssignmentManager.Instance != null && TileAssignmentManager.Instance.IsAssigned(targetTile)) {
                Debug.LogWarning("[ResourceManager] Tile already has a firefighter assigned.");
                return;
            }

            int cost = GetDeploymentCost(GetCurrentLevel());
            City deployCity = FindNearestCityWithBudget(targetTile, cost);
            if (deployCity == null) {
                Debug.LogWarning("[ResourceManager] No city has enough budget to deploy.");
                return;
            }

            DeployFirefighterInternal(deployCity, targetTile);
        }

        public void DeployFirefighterFromCity(City deployCity) {
            if (deployCity == null) return;

            Tile targetFire = FindNearestUnassignedFireToCity(deployCity);
            if (targetFire == null) {
                Debug.LogWarning("[ResourceManager] No active unassigned fires to deploy to.");
                return;
            }

            int cost = GetDeploymentCost(GetCurrentLevel());
            if (deployCity.Budget < cost) {
                Debug.LogWarning($"[ResourceManager] {deployCity.CityName} doesn't have enough budget.");
                return;
            }

            DeployFirefighterInternal(deployCity, targetFire);
        }

        private void DeployFirefighterInternal(City deployCity, Tile targetTile) {
            int cost = GetDeploymentCost(GetCurrentLevel());
            deployCity.Budget -= cost;
            globalAvailableBudget -= cost;
            EventBroker.Instance.Publish(Core.EventType.BudgetChanged, globalAvailableBudget);
            EventBroker.Instance.Publish(Core.EventType.MoneySpent, deployCity);

            // Spawn firefighter at city position
            Vector3 spawnPos;
            if (groundTilemap != null) {
                spawnPos = groundTilemap.GetCellCenterWorld(new Vector3Int(deployCity.TileX, deployCity.TileY, 0));
            } else {
                spawnPos = new Vector3(deployCity.TileX, deployCity.TileY, 0);
            }

            var unitGO = Instantiate(firefighterConfig.UnitPrefab);
            spawnPos.z = unitGO.transform.position.z;
            unitGO.transform.position = spawnPos;
            unitGO.name = $"Firefighter_{deployCity.CityName}";

            var mover = unitGO.GetComponent<SpriteMover>();
            if (mover == null)
                mover = unitGO.AddComponent<SpriteMover>();

            var unit = unitGO.GetComponent<FirefighterUnit>();
            if (unit == null)
                unit = unitGO.AddComponent<FirefighterUnit>();

            Vector3 targetWorldPos;
            if (groundTilemap != null) {
                targetWorldPos = groundTilemap.GetCellCenterWorld(new Vector3Int(targetTile.X, targetTile.Y, 0));
            } else {
                targetWorldPos = new Vector3(targetTile.X, targetTile.Y, 0);
            }

            unit.Initialize(fireEngine, firefighterConfig, targetTile, spawnPos, targetWorldPos, gridSystem, groundTilemap);

            activeUnits.Add(unitGO);
            EventBroker.Instance.Publish(Core.EventType.UnitDeployed, targetTile);

            Debug.Log($"[ResourceManager] Deployed firefighter from {deployCity.CityName} (budget: {deployCity.Budget}, cost: {cost})");
        }

        // ── Transfers ────────────────────────────────────────────────────

        public void TransferResources(City fromCity, City toCity, int amount) {
            if (fromCity == null || toCity == null) return;
            if (fromCity.Budget < amount) {
                Debug.LogWarning($"[ResourceManager] {fromCity.CityName} doesn't have enough budget ({fromCity.Budget} < {amount}).");
                return;
            }

            fromCity.Budget -= amount;
            toCity.Budget += amount;
            EventBroker.Instance.Publish(Core.EventType.ResourceTransferred, fromCity);
            EventBroker.Instance.Publish(Core.EventType.BudgetChanged, GlobalBudget);
            Debug.Log($"[ResourceManager] Transferred {amount} from {fromCity.CityName} to {toCity.CityName}");
        }

        // ── Helpers ──────────────────────────────────────────────────────

        public void TrackAvailableResources() {
            activeUnits.RemoveAll(u => u == null);
            TileAssignmentManager.Instance?.CleanupStale();
        }

        private Tile FindNearestUnassignedFireToCity(City city) {
            if (fireEngine == null) return null;
            Tile nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var tile in fireEngine.GetBurningTiles()) {
                // Only target fires within this city's region
                if (tile.Region == null || tile.Region.City != city) continue;

                if (TileAssignmentManager.Instance != null && TileAssignmentManager.Instance.IsAssigned(tile))
                    continue;

                float dist = (city.TileX - tile.X) * (city.TileX - tile.X)
                           + (city.TileY - tile.Y) * (city.TileY - tile.Y);
                if (dist < nearestDist) {
                    nearestDist = dist;
                    nearest = tile;
                }
            }
            return nearest;
        }

        private City FindNearestCityWithBudget(Tile target, int minBudget) {
            City nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var city in managedCities) {
                if (city.Budget < minBudget) continue;
                float dist = (city.TileX - target.X) * (city.TileX - target.X)
                           + (city.TileY - target.Y) * (city.TileY - target.Y);
                if (dist < nearestDist) {
                    nearestDist = dist;
                    nearest = city;
                }
            }
            return nearest;
        }

        private void RecalculateGlobalBudget() {
            globalAvailableBudget = 0;
            foreach (var city in managedCities)
                globalAvailableBudget += city.Budget;
        }

        private int GetCurrentLevel() {
            var pm = FindFirstObjectByType<ProgressionManager>();
            return pm != null ? pm.CurrentLevel : 1;
        }

        public void MoveEntity(string id, string category, int amount) { }
    }
}
