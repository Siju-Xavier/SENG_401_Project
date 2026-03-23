namespace BusinessLogic {
    using System.Collections.Generic;
    using GameState;
    using Core;
    using ScriptableObjects;
    using Presentation;
    using UnityEngine;

    public class ResourceManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private UnitConfig firefighterConfig;
        [SerializeField] private int budgetPerRound = 500;

        private FireEngine fireEngine;
        private UnityEngine.Tilemaps.Tilemap groundTilemap;
        private List<City> managedCities = new List<City>();
        private List<GameObject> activeUnits = new List<GameObject>();

        private int globalAvailableBudget;

        public int GlobalBudget => globalAvailableBudget;

        private void Start() {
#if UNITY_EDITOR
            if (firefighterConfig == null) {
                firefighterConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjects.UnitConfig>("Assets/Sprites/ScriptableObjects/UnitConfig.asset");
                Debug.LogWarning("[ResourceManager] Auto-assigned firefighterConfig from Assets because it was missing in the Inspector.");
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

            globalAvailableBudget = 0;
            foreach (var city in managedCities)
                globalAvailableBudget += city.Budget;
        }

        public void DeployFirefighter(Tile targetTile) {
            if (firefighterConfig == null || firefighterConfig.UnitPrefab == null) {
                Debug.LogWarning("[ResourceManager] No firefighter config or prefab assigned.");
                return;
            }

            // Find nearest city with enough budget
            City deployCity = FindNearestCityWithBudget(targetTile, firefighterConfig.DeploymentCost);
            if (deployCity == null) {
                Debug.LogWarning("[ResourceManager] No city has enough budget to deploy.");
                return;
            }

            DeployFirefighterInternal(deployCity, targetTile);
        }

        public void DeployFirefighterFromCity(City deployCity) {
            if (deployCity == null) return;

            Tile targetFire = FindNearestFireToCity(deployCity);
            if (targetFire == null) {
                Debug.LogWarning("[ResourceManager] No active fires to deploy to.");
                return;
            }

            if (deployCity.Budget < firefighterConfig.DeploymentCost) {
                Debug.LogWarning($"[ResourceManager] {deployCity.CityName} doesn't have enough budget.");
                return;
            }

            DeployFirefighterInternal(deployCity, targetFire);
        }

        private Tile FindNearestFireToCity(City city) {
            if (fireEngine == null) return null;
            Tile nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var tile in fireEngine.GetBurningTiles()) {
                float dist = (city.TileX - tile.X) * (city.TileX - tile.X)
                           + (city.TileY - tile.Y) * (city.TileY - tile.Y);
                if (dist < nearestDist) {
                    nearestDist = dist;
                    nearest = tile;
                }
            }
            return nearest;
        }

        private void DeployFirefighterInternal(City deployCity, Tile targetTile) {
            // Deduct cost
            deployCity.Budget -= firefighterConfig.DeploymentCost;
            globalAvailableBudget -= firefighterConfig.DeploymentCost;
            EventBroker.Instance.Publish(Core.EventType.BudgetChanged, globalAvailableBudget);

            // Spawn firefighter at city position
            Vector3 spawnPos;
            if (groundTilemap != null) {
                spawnPos = groundTilemap.GetCellCenterWorld(new Vector3Int(deployCity.TileX, deployCity.TileY, 0));
            } else {
                spawnPos = new Vector3(deployCity.TileX, deployCity.TileY, 0);
            }

            var unitGO = Instantiate(firefighterConfig.UnitPrefab);
            unitGO.transform.position = spawnPos;
            unitGO.name = $"Firefighter_{deployCity.CityName}";

            // Ensure SpriteMover exists
            var mover = unitGO.GetComponent<SpriteMover>();
            if (mover == null)
                mover = unitGO.AddComponent<SpriteMover>();

            // Add and initialize FirefighterUnit
            var unit = unitGO.GetComponent<FirefighterUnit>();
            if (unit == null)
                unit = unitGO.AddComponent<FirefighterUnit>();

            // Calculate target world position
            Vector3 targetWorldPos;
            if (groundTilemap != null) {
                targetWorldPos = groundTilemap.GetCellCenterWorld(new Vector3Int(targetTile.X, targetTile.Y, 0));
            } else {
                targetWorldPos = new Vector3(targetTile.X, targetTile.Y, 0);
            }

            unit.Initialize(fireEngine, firefighterConfig, targetTile, spawnPos, targetWorldPos);

            activeUnits.Add(unitGO);
            EventBroker.Instance.Publish(Core.EventType.UnitDeployed, targetTile);

            Debug.Log($"[ResourceManager] Deployed firefighter from {deployCity.CityName} (budget: {deployCity.Budget})");
        }

        public void AddRoundBudget() {
            foreach (var city in managedCities) {
                city.Budget += budgetPerRound;
            }
            globalAvailableBudget = 0;
            foreach (var city in managedCities)
                globalAvailableBudget += city.Budget;

            EventBroker.Instance.Publish(Core.EventType.BudgetChanged, globalAvailableBudget);
            Debug.Log($"[ResourceManager] Round budget added. Total: {globalAvailableBudget}");
        }

        public void TransferResources(City fromCity, City toCity, int amount) {
            if (fromCity == null || toCity == null) return;
            if (fromCity.Budget < amount) {
                Debug.LogWarning($"[ResourceManager] {fromCity.CityName} doesn't have enough budget ({fromCity.Budget} < {amount}).");
                return;
            }

            fromCity.Budget -= amount;
            toCity.Budget += amount;
            EventBroker.Instance.Publish(Core.EventType.BudgetChanged, GlobalBudget);
            Debug.Log($"[ResourceManager] Transferred {amount} from {fromCity.CityName} to {toCity.CityName}");
        }

        public void TrackAvailableResources() {
            // Clean up destroyed unit references
            activeUnits.RemoveAll(u => u == null);
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

        public void MoveEntity(string id, string category, int amount) { }
    }
}
