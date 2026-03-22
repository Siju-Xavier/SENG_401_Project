namespace Presentation.MapGeneration
{
    using System.Collections.Generic;
    using UnityEngine;
    using GameState;
    using ScriptableObjects;
    using TilemapTile = UnityEngine.Tilemaps.Tile;

    public class DecorationPlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UnityEngine.Tilemaps.Tilemap decorationTilemap;

        [Header("Density Thresholds")]
        [Range(0f, 1f)]
        [SerializeField] private float minThreshold = 0.1f;
        [Range(0f, 1f)]
        [SerializeField] private float maxThreshold = 0.9f;

        private HashSet<Vector2Int> decorationTiles = new HashSet<Vector2Int>();
        private Dictionary<Sprite, TilemapTile> tileCache = new Dictionary<Sprite, TilemapTile>();

        public void ClearDecorations()
        {
            decorationTiles.Clear();
            tileCache.Clear();

            if (decorationTilemap != null)
                decorationTilemap.ClearAllTiles();
        }

        public void RemoveDecorationsInArea(int startX, int startY, int width, int height)
        {
            if (decorationTilemap == null) return;

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    decorationTilemap.SetTile(new Vector3Int(x, y, 0), null);
                    decorationTiles.Remove(new Vector2Int(x, y));
                }
            }
        }

        public void PlaceDecorations(MapData mapData)
        {
            ClearDecorations();

            if (decorationTilemap == null)
            {
                Debug.LogWarning("DecorationPlacer: decorationTilemap not assigned.");
                return;
            }

            int width = mapData.Width;
            int height = mapData.Height;
            System.Random rng = new System.Random(mapData.Seed + 7919);
            int count = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    BiomeConfig biome = mapData.BiomeGrid[x, y];
                    if (biome == null || biome.DecorationSprites == null || biome.DecorationSprites.Length == 0)
                        continue;

                    if (biome.DecorationDensity <= 0f)
                        continue;

                    // Find this biome's noise range
                    float rangeMin = 0f;
                    float rangeMax = biome.MaxHeight;
                    for (int i = 0; i < mapData.Biomes.Length; i++)
                    {
                        if (mapData.Biomes[i] == biome)
                        {
                            rangeMin = i > 0 ? mapData.Biomes[i - 1].MaxHeight : 0f;
                            break;
                        }
                    }

                    // Normalize noise within the biome's range (0-1)
                    float noiseValue = mapData.NoiseMap[x, y];
                    float normalized = (noiseValue - rangeMin) / (rangeMax - rangeMin);

                    // Spawn probability curve: 100% between thresholds, fades to 0% at edges
                    float spawnChance;
                    if (normalized >= minThreshold && normalized <= maxThreshold)
                        spawnChance = 1f;
                    else if (normalized < minThreshold)
                        spawnChance = normalized / minThreshold;
                    else
                        spawnChance = (1f - normalized) / (1f - maxThreshold);

                    spawnChance *= biome.DecorationDensity;

                    if ((float)rng.NextDouble() > spawnChance)
                        continue;

                    Sprite sprite = biome.DecorationSprites[rng.Next(biome.DecorationSprites.Length)];
                    TilemapTile tile = GetOrCreateTile(sprite);

                    decorationTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    decorationTiles.Add(new Vector2Int(x, y));
                    count++;
                }
            }

            Debug.Log($"DecorationPlacer: Placed {count} decorations on tilemap.");
        }

        private TilemapTile GetOrCreateTile(Sprite sprite)
        {
            if (!tileCache.TryGetValue(sprite, out TilemapTile tile))
            {
                tile = ScriptableObject.CreateInstance<TilemapTile>();
                tile.sprite = sprite;
                tileCache[sprite] = tile;
            }
            return tile;
        }
    }
}
