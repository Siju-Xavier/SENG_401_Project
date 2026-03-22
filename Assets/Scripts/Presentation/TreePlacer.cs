namespace Presentation.MapGeneration
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using GameState;
    using ScriptableObjects;
    using TilemapTile = UnityEngine.Tilemaps.Tile;

    public class TreePlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap vegetationTilemap;

        [Header("Density Thresholds")]
        [Range(0f, 1f)]
        [SerializeField] private float minThreshold = 0.2f;
        [Range(0f, 1f)]
        [SerializeField] private float maxThreshold = 0.8f;

        [Header("Display")]
        [SerializeField] private float spriteScale = 1f;
        [SerializeField] private int baseSortingOrder = 5;

        private HashSet<Vector2Int> treeTiles = new HashSet<Vector2Int>();
        private Dictionary<Sprite, TilemapTile> tileCache = new Dictionary<Sprite, TilemapTile>();
        private MapData mapData;

        private void OnEnable()
        {
            Core.EventBroker.Instance.Subscribe(Core.EventType.FireStarted, OnFireStarted);
            Core.EventBroker.Instance.Subscribe(Core.EventType.FireSpread, OnFireSpread);
            Core.EventBroker.Instance.Subscribe(Core.EventType.FireExtinguished, OnFireExtinguished);
        }

        private void OnDisable()
        {
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.FireStarted, OnFireStarted);
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.FireSpread, OnFireSpread);
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.FireExtinguished, OnFireExtinguished);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleVegetation();
            }
        }

        private void OnFireStarted(object data) => SwapToBurning(data as GameState.Tile);
        private void OnFireSpread(object data) => SwapToBurning(data as GameState.Tile);
        private void OnFireExtinguished(object data) => RemoveBurnedTree(data as GameState.Tile);

        private void SwapToBurning(GameState.Tile tile)
        {
            if (tile == null || vegetationTilemap == null) return;

            var key = new Vector2Int(tile.X, tile.Y);
            if (!treeTiles.Contains(key)) return;

            BiomeConfig biome = tile.Biome;
            if (biome != null && biome.BurningVegetationSprite != null)
            {
                TilemapTile burningTile = GetOrCreateTile(biome.BurningVegetationSprite);
                vegetationTilemap.SetTile(new Vector3Int(tile.X, tile.Y, 0), burningTile);
            }
            else
            {
                // No burning sprite — just remove the tree immediately
                vegetationTilemap.SetTile(new Vector3Int(tile.X, tile.Y, 0), null);
                treeTiles.Remove(key);
            }
        }

        private void RemoveBurnedTree(GameState.Tile tile)
        {
            if (tile == null || vegetationTilemap == null) return;

            var key = new Vector2Int(tile.X, tile.Y);
            if (!treeTiles.Contains(key)) return;

            vegetationTilemap.SetTile(new Vector3Int(tile.X, tile.Y, 0), null);
            treeTiles.Remove(key);
        }

        public void ToggleVegetation()
        {
            if (vegetationTilemap != null)
            {
                vegetationTilemap.gameObject.SetActive(!vegetationTilemap.gameObject.activeSelf);
            }
        }

        public void ClearTrees()
        {
            treeTiles.Clear();
            tileCache.Clear();

            if (vegetationTilemap != null)
                vegetationTilemap.ClearAllTiles();
        }

        public void RemoveTreesInArea(int startX, int startY, int width, int height)
        {
            if (vegetationTilemap == null) return;

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    vegetationTilemap.SetTile(new Vector3Int(x, y, 0), null);
                    treeTiles.Remove(new Vector2Int(x, y));
                }
            }
        }

        public void PlaceTrees(MapData data)
        {
            ClearTrees();
            mapData = data;

            if (vegetationTilemap == null)
            {
                Debug.LogWarning("TreePlacer: vegetationTilemap not assigned.");
                return;
            }

            int width = data.Width;
            int height = data.Height;
            System.Random rng = new System.Random(data.Seed);
            int treeCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    BiomeConfig biome = data.BiomeGrid[x, y];
                    if (biome == null || biome.VegetationSprites == null || biome.VegetationSprites.Length == 0)
                        continue;

                    float rangeMin = 0f;
                    float rangeMax = biome.MaxHeight;
                    for (int i = 0; i < data.Biomes.Length; i++)
                    {
                        if (data.Biomes[i] == biome)
                        {
                            rangeMin = i > 0 ? data.Biomes[i - 1].MaxHeight : 0f;
                            break;
                        }
                    }

                    float noiseValue = data.NoiseMap[x, y];
                    float normalized = (noiseValue - rangeMin) / (rangeMax - rangeMin);

                    float spawnChance;
                    if (normalized >= minThreshold && normalized <= maxThreshold)
                        spawnChance = 1f;
                    else if (normalized < minThreshold)
                        spawnChance = normalized / minThreshold;
                    else
                        spawnChance = (1f - normalized) / (1f - maxThreshold);

                    if ((float)rng.NextDouble() > spawnChance)
                        continue;

                    Sprite sprite = biome.VegetationSprites[rng.Next(biome.VegetationSprites.Length)];
                    TilemapTile tile = GetOrCreateTile(sprite);

                    vegetationTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                    treeTiles.Add(new Vector2Int(x, y));
                    treeCount++;
                }
            }

            Debug.Log($"TreePlacer: Placed {treeCount} trees on tilemap (single draw call).");
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
