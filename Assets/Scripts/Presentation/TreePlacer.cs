namespace Presentation.MapGeneration
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using GameState;
    using ScriptableObjects;

    public class TreePlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap groundTilemap;

        [Header("Density Thresholds")]
        [Range(0f, 1f)]
        [SerializeField] private float minThreshold = 0.2f;
        [Range(0f, 1f)]
        [SerializeField] private float maxThreshold = 0.8f;

        [Header("Display")]
        [SerializeField] private float spriteScale = 1f;
        [SerializeField] private int baseSortingOrder = 5;

        private GameObject treeContainer;
        private Dictionary<Vector2Int, GameObject> treesByTile = new Dictionary<Vector2Int, GameObject>();

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleVegetation();
            }
        }

        public void ToggleVegetation()
        {
            if (treeContainer != null)
            {
                treeContainer.SetActive(!treeContainer.activeSelf);
            }
        }

        public void ClearTrees()
        {
            treesByTile.Clear();

            if (treeContainer != null)
            {
                if (Application.isPlaying) Destroy(treeContainer);
                else DestroyImmediate(treeContainer);
                treeContainer = null;
            }

            while (true)
            {
                var leftover = transform.Find("Trees");
                if (leftover == null) break;
                leftover.name = "Trees_Destroying";
                if (Application.isPlaying) Destroy(leftover.gameObject);
                else DestroyImmediate(leftover.gameObject);
            }
        }

        public void RemoveTreesInArea(int startX, int startY, int width, int height)
        {
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    var key = new Vector2Int(x, y);
                    if (treesByTile.TryGetValue(key, out GameObject treeGO))
                    {
                        if (treeGO != null)
                        {
                            if (Application.isPlaying) Destroy(treeGO);
                            else DestroyImmediate(treeGO);
                        }
                        treesByTile.Remove(key);
                    }
                }
            }
        }

        public void PlaceTrees(MapData mapData)
        {
            ClearTrees();

            treeContainer = new GameObject("Trees");
            treeContainer.transform.SetParent(transform);

            int width = mapData.Width;
            int height = mapData.Height;
            System.Random rng = new System.Random(mapData.Seed);
            int treeCount = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    BiomeConfig biome = mapData.BiomeGrid[x, y];
                    if (biome == null || biome.VegetationSprites == null || biome.VegetationSprites.Length == 0)
                        continue;

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

                    float noiseValue = mapData.NoiseMap[x, y];
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

                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    Vector3 worldPos = groundTilemap.GetCellCenterWorld(tilePos);

                    var treeGO = new GameObject($"Tree_{x}_{y}");
                    treeGO.transform.SetParent(treeContainer.transform);
                    treeGO.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
                    treeGO.transform.localScale = Vector3.one * spriteScale;

                    var sr = treeGO.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    // Isometric sorting: same diagonal (x+y) = same depth
                    // Higher (x+y) = further from camera = lower sorting order
                    sr.sortingOrder = baseSortingOrder + (width + height) - (x + y);

                    treesByTile[new Vector2Int(x, y)] = treeGO;
                    treeCount++;
                }
            }

            Debug.Log($"TreePlacer: Placed {treeCount} trees.");
        }
    }
}
