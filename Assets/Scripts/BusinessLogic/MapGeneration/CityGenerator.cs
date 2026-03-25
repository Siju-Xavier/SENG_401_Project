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

        [Header("Economy Settings")]
        [SerializeField] private ScriptableObjects.EconomyConfig economyConfig;

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

                    string[] names = { "Banff", "Calgary", "Edmonton" };
                    string cityName = i < names.Length ? names[i] : $"City_{i + 1}";
                    int initialBudget = economyConfig != null ? economyConfig.InitialCityBudget : 1000;
                    City city = new City(cityName, x, y, initialBudget);
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

            AssignTerritories(grid, mapData);
        }

        private void AssignTerritories(GridSystem grid, MapData mapData)
        {
            if (grid.Regions.Count == 0) return;

            float noiseFreq = 0.05f;
            float noiseAmt = 8f;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    Tile tile = grid.GetTileAt(x, y);
                    if (tile == null) continue;

                    // Warp the coordinates with perlin noise to get an organic border
                    float nx = x + (Mathf.PerlinNoise(x * noiseFreq, y * noiseFreq) - 0.5f) * noiseAmt;
                    float ny = y + (Mathf.PerlinNoise(x * noiseFreq + 1000, y * noiseFreq + 1000) - 0.5f) * noiseAmt;
                    Vector2 warpedPos = new Vector2(nx, ny);

                    Region closestRegion = null;
                    float minDistance = float.MaxValue;

                    foreach (var region in grid.Regions)
                    {
                        City city = region.City;
                        Vector2 cityPos = new Vector2(city.TileX, city.TileY);
                        float dist = Vector2.Distance(warpedPos, cityPos);

                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestRegion = region;
                        }
                    }

                    if (closestRegion != null)
                    {
                        closestRegion.AddTile(tile);
                    }
                }
            }
            
            Debug.Log("Territories assigned via organic Voronoi partitioning.");
        }
    }
}
