namespace BusinessLogic
{
    using System.Collections.Generic;
    using UnityEngine;
    using GameState;
    using ScriptableObjects;

    public class CityGenerator : MonoBehaviour
    {
        [Header("City Generation")]
        [SerializeField] private bool enableCityPlacement = true;
        [Tooltip("How many cities to place on the map")]
        [SerializeField] private int numberOfCities = 3;
        [Tooltip("Minimum distance between cities in tiles")]
        [SerializeField] private int minCityDistance = 15;

        public bool EnableCityPlacement => enableCityPlacement;

        public void GenerateCities(GridSystem grid, MapData mapData)
        {
            System.Random rng = new System.Random(mapData.Seed);
            List<Vector2Int> cityPositions = new List<Vector2Int>();
            int margin = 5;
            int maxAttempts = 500;

            for (int i = 0; i < numberOfCities; i++)
            {
                bool placed = false;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    int x = rng.Next(margin, mapData.Width - margin);
                    int y = rng.Next(margin, mapData.Height - margin);

                    BiomeConfig biome = mapData.BiomeGrid[x, y];
                    if (biome == null || !biome.AllowStructures)
                        continue;

                    bool tooClose = false;
                    foreach (var other in cityPositions)
                    {
                        int dx = x - other.x;
                        int dy = y - other.y;
                        if (dx * dx + dy * dy < minCityDistance * minCityDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    string cityName = $"City_{i + 1}";
                    City city = new City(cityName, x, y);
                    Region region = new Region(cityName, city);
                    grid.AddRegion(region);
                    cityPositions.Add(new Vector2Int(x, y));

                    Debug.Log($"Generated city '{cityName}' at ({x}, {y}) on biome {biome.name}");
                    placed = true;
                    break;
                }

                if (!placed)
                {
                    Debug.LogWarning($"Could not find valid position for city {i + 1} after {maxAttempts} attempts");
                }
            }
        }
    }
}
