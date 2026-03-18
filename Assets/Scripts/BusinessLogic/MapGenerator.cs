using UnityEngine;
using Presentation.MapGeneration;
using ScriptableObjects;
using GameState;
using System.Collections.Generic;

namespace BusinessLogic.MapGeneration
{
    public class MapGenerator : MonoBehaviour
    {
        public int mapWidth = 100;
        public int mapHeight = 100;
        public float noiseScale = 20f;

        public int octaves = 4;
        [Range(0, 1)]
        public float persistance = 0.5f;
        public float lacunarity = 2f;

        public int seed;
        public Vector2 offset;

        public bool autoUpdate;

        public TerrainType[] regions;

        [Header("Biome Configs (sorted by maxHeight, low to high)")]
        public BiomeConfig[] biomes;

        [Header("City / Region Settings")]
        [Tooltip("Number of cities/regions to generate")]
        public int cityCount = 3;

        [Tooltip("City sprites to randomly assign")]
        public Sprite[] citySprites;

        [Tooltip("Names for the cities")]
        public string[] cityNames = { "Oakridge", "Pinehaven", "Maplewood" };

        [Tooltip("Minimum distance between cities (in tiles)")]
        public int minCityDistance = 20;

        // The result: which biome each cell belongs to
        private BiomeConfig[,] biomeGrid;
        private float[,] noiseMap;
        private GridSystem gridSystem;

        public BiomeConfig[,] BiomeGrid => biomeGrid;
        public float[,] NoiseMap => noiseMap;
        public GridSystem GridSystem => gridSystem;

        private void Start()
        {
            GenerateMap();
        }

        public void GenerateMap()
        {
            // 1. Generate the raw 0-1 noise map
            noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

            // 2. Build the biome grid and color map
            biomeGrid = new BiomeConfig[mapWidth, mapHeight];
            Color[] colorMap = new Color[mapWidth * mapHeight];

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    float currentHeight = noiseMap[x, y];

                    // Assign biome from BiomeConfig array
                    if (biomes != null && biomes.Length > 0)
                    {
                        for (int i = 0; i < biomes.Length; i++)
                        {
                            if (currentHeight <= biomes[i].MaxHeight)
                            {
                                biomeGrid[x, y] = biomes[i];
                                break;
                            }
                        }
                    }

                    // Still build the color map for the texture preview
                    for (int i = 0; i < regions.Length; i++)
                    {
                        if (currentHeight <= regions[i].height)
                        {
                            colorMap[y * mapWidth + x] = regions[i].color;
                            break;
                        }
                    }
                }
            }

            // 3. Send the color map to MapDisplay (texture preview still works)
            MapDisplay display = FindFirstObjectByType<MapDisplay>();
            if (display != null)
            {
                display.DrawTexture(TextureGenerator.TextureFromColourMap(colorMap, mapWidth, mapHeight));
            }

            // 4. Render the biome grid onto the Tilemap
            BiomeTilemapRenderer tilemapRenderer = FindFirstObjectByType<BiomeTilemapRenderer>();
            if (tilemapRenderer != null)
            {
                tilemapRenderer.RenderMap();
            }

            // 5. Initialize GridSystem, place cities, and assign regions
            GenerateRegions();

            // 6. Place city sprites on the map
            CityPlacer cityPlacer = FindFirstObjectByType<CityPlacer>();
            if (cityPlacer != null)
            {
                cityPlacer.PlaceCities(gridSystem);
            }
        }

        private void GenerateRegions()
        {
            // Use existing GridSystem or add one to this GameObject
            gridSystem = FindFirstObjectByType<GridSystem>();
            if (gridSystem == null)
            {
                gridSystem = gameObject.AddComponent<GridSystem>();
            }
            gridSystem.Initialize(mapWidth, mapHeight);

            // Assign biomes to tiles
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    Tile tile = gridSystem.GetTileAt(x, y);
                    tile.Biome = biomeGrid[x, y];
                    tile.MoistureLevel = biomeGrid[x, y] != null ? biomeGrid[x, y].BaseMoisture : 0f;
                }
            }

            // Collect valid land tiles for city placement (not water, not sand)
            var landTiles = new List<Vector2Int>();
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    BiomeConfig biome = biomeGrid[x, y];
                    if (biome != null && biome.name != "WaterBiome" && biome.name != "SandBiome")
                    {
                        landTiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (landTiles.Count == 0)
            {
                Debug.LogWarning("MapGenerator: No land tiles found for city placement!");
                return;
            }

            // Randomly place cities with minimum distance constraint
            int actualSeed = seed != 0 ? seed : System.Environment.TickCount;
            System.Random rng = new System.Random(actualSeed);
            var cityPositions = new List<Vector2Int>();

            int attempts = 0;
            int maxAttempts = 1000;
            int citiesToPlace = Mathf.Min(cityCount, landTiles.Count);

            while (cityPositions.Count < citiesToPlace && attempts < maxAttempts)
            {
                int idx = rng.Next(landTiles.Count);
                Vector2Int candidate = landTiles[idx];
                attempts++;

                // Check minimum distance from existing cities
                bool tooClose = false;
                foreach (var existing in cityPositions)
                {
                    if (Vector2Int.Distance(candidate, existing) < minCityDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    cityPositions.Add(candidate);
                    attempts = 0; // reset attempts after success
                }
            }

            // Create City and Region objects
            var regionList = new List<Region>();
            for (int i = 0; i < cityPositions.Count; i++)
            {
                Vector2Int pos = cityPositions[i];
                string name = i < cityNames.Length ? cityNames[i] : $"City_{i + 1}";

                City city = new City(name, pos.x, pos.y);
                Region region = new Region(name + " Region", city);
                regionList.Add(region);
                gridSystem.AddRegion(region);
            }

            // Voronoi partitioning: assign each land tile to nearest city's region
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    BiomeConfig biome = biomeGrid[x, y];
                    if (biome != null && biome.name == "WaterBiome")
                        continue; // water tiles don't belong to any region

                    float minDist = float.MaxValue;
                    Region closest = null;

                    foreach (var region in regionList)
                    {
                        float dist = Vector2Int.Distance(
                            new Vector2Int(x, y),
                            new Vector2Int(region.City.TileX, region.City.TileY));

                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = region;
                        }
                    }

                    if (closest != null)
                    {
                        Tile tile = gridSystem.GetTileAt(x, y);
                        closest.AddTile(tile);
                    }
                }
            }

            Debug.Log($"Generated {regionList.Count} regions with cities placed on the map.");
            foreach (var region in regionList)
            {
                Debug.Log($"  {region.RegionName}: City at ({region.City.TileX}, {region.City.TileY}), {region.Tiles.Count} tiles");
            }
        }

        public BiomeConfig GetBiomeAt(int x, int y)
        {
            if (biomeGrid == null || x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
                return null;
            return biomeGrid[x, y];
        }

        private void OnValidate()
        {
            if (mapWidth < 1) mapWidth = 1;
            if (mapHeight < 1) mapHeight = 1;
            if (lacunarity < 1) lacunarity = 1;
            if (octaves < 0) octaves = 0;
            if (cityCount < 1) cityCount = 1;
            if (minCityDistance < 1) minCityDistance = 1;
        }
    }

    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color color;
    }
}
