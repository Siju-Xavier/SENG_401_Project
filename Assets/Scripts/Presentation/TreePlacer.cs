namespace Presentation.MapGeneration
{
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using ScriptableObjects;

    public class TreePlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap groundTilemap;

        [Header("Density Thresholds")]
        [Tooltip("Normalized position within biome range below which no vegetation spawns (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float minThreshold = 0.2f;

        [Tooltip("Normalized position within biome range above which no vegetation spawns (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float maxThreshold = 0.8f;

        [Header("Display")]
        [SerializeField] private float spriteScale = 1f;
        [SerializeField] private int sortingOrder = 5;

        private GameObject treeContainer;

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

        public void PlaceTrees(float[,] noiseMap, BiomeConfig[,] biomeGrid, BiomeConfig[] biomes, int seed)
        {
            ClearTrees();

            treeContainer = new GameObject("Trees");
            treeContainer.transform.SetParent(transform);

            int width = biomeGrid.GetLength(0);
            int height = biomeGrid.GetLength(1);
            System.Random rng = new System.Random(seed);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    BiomeConfig biome = biomeGrid[x, y];
                    if (biome == null || biome.VegetationSprites == null || biome.VegetationSprites.Length == 0)
                        continue;

                    // Find this biome's height range from the sorted biomes array
                    float rangeMin = 0f;
                    float rangeMax = biome.MaxHeight;
                    for (int i = 0; i < biomes.Length; i++)
                    {
                        if (biomes[i] == biome)
                        {
                            rangeMin = i > 0 ? biomes[i - 1].MaxHeight : 0f;
                            break;
                        }
                    }

                    // Normalize the noise value within the biome's range (0-1)
                    float noiseValue = noiseMap[x, y];
                    float normalized = (noiseValue - rangeMin) / (rangeMax - rangeMin);

                    // Only place vegetation within the threshold band
                    if (normalized < minThreshold || normalized > maxThreshold)
                        continue;

                    // Pick a sprite deterministically
                    Sprite sprite = biome.VegetationSprites[rng.Next(biome.VegetationSprites.Length)];

                    // Convert tile position to world center
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    Vector3 worldPos = groundTilemap.GetCellCenterWorld(tilePos);

                    // Create vegetation GameObject
                    var treeGO = new GameObject($"Tree_{x}_{y}");
                    treeGO.transform.SetParent(treeContainer.transform);
                    treeGO.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
                    treeGO.transform.localScale = Vector3.one * spriteScale;

                    var sr = treeGO.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = sortingOrder + (height - y);
                }
            }

            Debug.Log($"TreePlacer: Placed {treeContainer.transform.childCount} trees.");
        }
    }
}
