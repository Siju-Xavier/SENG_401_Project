using UnityEngine;
using ScriptableObjects;
using GameState;

namespace BusinessLogic.MapGeneration
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Map Settings")]
        public int mapWidth = 100;
        public int mapHeight = 100;
        public float noiseScale = 20f;

        public int octaves = 4;
        [Range(0, 1)]
        public float persistance = 0.5f;
        public float lacunarity = 2f;

        public int seed;
        public Vector2 offset;

        [Header("Biome Configs (sorted by maxHeight, low to high)")]
        public BiomeConfig[] biomes;

        private MapData mapData;

        public MapData MapData => mapData;

        public MapData GenerateNoiseAndBiomes(int seed)
        {
            mapData = new MapData
            {
                Width = mapWidth,
                Height = mapHeight,
                Seed = seed,
                Biomes = biomes
            };

            mapData.NoiseMap = Noise.GenerateNoiseMap(
                mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

            mapData.BiomeGrid = new BiomeConfig[mapWidth, mapHeight];

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    float currentHeight = mapData.NoiseMap[x, y];

                    if (biomes != null && biomes.Length > 0)
                    {
                        for (int i = 0; i < biomes.Length; i++)
                        {
                            if (currentHeight <= biomes[i].MaxHeight)
                            {
                                mapData.BiomeGrid[x, y] = biomes[i];
                                break;
                            }
                        }
                    }
                }
            }

            return mapData;
        }

        public BiomeConfig GetBiomeAt(int x, int y)
        {
            if (mapData?.BiomeGrid == null || x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
                return null;
            return mapData.BiomeGrid[x, y];
        }

        private void OnValidate()
        {
            if (mapWidth < 1) mapWidth = 1;
            if (mapHeight < 1) mapHeight = 1;
            if (lacunarity < 1) lacunarity = 1;
            if (octaves < 0) octaves = 0;
        }
    }
}
