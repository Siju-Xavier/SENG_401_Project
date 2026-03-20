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
            Debug.Log("GenerateMap: START");

            // 1. Generate the raw 0-1 noise map
            Debug.Log("GenerateMap: Generating Noise Map");
            noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

            // 2. Build the biome grid and color map
            Debug.Log("GenerateMap: Building Biome Grid");
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

            // 4b. Place vegetation on biomes that have vegetation sprites
            TreePlacer treePlacer = FindFirstObjectByType<TreePlacer>();
            if (treePlacer != null)
            {
                treePlacer.PlaceTrees(noiseMap, biomeGrid, biomes, seed);
            }

            // 5. Initialize GridSystem without regions/cities for now
            Debug.Log("GenerateMap: Initializing empty GridSystem");
            gridSystem = FindFirstObjectByType<GridSystem>();
            if (gridSystem == null)
            {
                gridSystem = gameObject.AddComponent<GridSystem>();
            }
            gridSystem.Initialize(mapWidth, mapHeight);

            // Directly assign biome config to tiles (without region/city logic)
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    Tile tile = gridSystem.GetTileAt(x, y);
                    if (tile != null)
                    {
                        tile.Biome = biomeGrid[x, y];
                        tile.MoistureLevel = biomeGrid[x, y] != null ? biomeGrid[x, y].BaseMoisture : 0f;
                    }
                }
            }

            Debug.Log("GenerateMap: DONE");
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
