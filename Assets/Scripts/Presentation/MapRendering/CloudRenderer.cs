namespace Presentation.MapRendering
{
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using Core;
    using TilemapTile = UnityEngine.Tilemaps.Tile;

    /// <summary>
    /// Renders animated clouds on a tilemap using Perlin noise.
    /// Uses a small number of opacity tiers (not per-tile colors) so
    /// tiles batch together efficiently. Single Tilemap, minimal batches.
    /// </summary>
    public class CloudRenderer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The cloud tilemap layer (create one above ground)")]
        [SerializeField] private Tilemap cloudTilemap;

        [Tooltip("Reads map size from here. Auto-found if empty.")]
        [SerializeField] private MapGenerationOrchestrator mapOrchestrator;

        [Tooltip("Drag your cloud tile asset here")]
        [SerializeField] private TileBase cloudTile;

        [Header("Noise Settings")]
        [Tooltip("Scale of the main cloud layer")]
        [SerializeField] private float noiseScale = 30f;

        [Tooltip("Scale of the second layer that morphs the cloud shapes")]
        [SerializeField] private float morphScale = 20f;

        [Tooltip("How fast the morph layer evolves (units per second)")]
        [SerializeField] private float morphSpeed = 1.5f;

        [Tooltip("How much the morph layer affects the result (0 = none, 1 = full)")]
        [Range(0f, 1f)]
        [SerializeField] private float morphStrength = 0.4f;

        [Tooltip("Noise values below this are clear sky (no cloud)")]
        [Range(0f, 1f)]
        [SerializeField] private float threshold = 0.45f;

        [Header("Movement")]
        [Tooltip("How fast clouds drift (units per second)")]
        [SerializeField] private Vector2 windSpeed = new Vector2(2f, 0.5f);

        [Tooltip("Seconds between cloud updates")]
        [SerializeField] private float updateInterval = 0.3f;

        [Header("Appearance")]
        [Tooltip("Max opacity of the densest clouds")]
        [Range(0f, 1f)]
        [SerializeField] private float maxOpacity = 0.35f;

        [Tooltip("Number of opacity levels (more = smoother, fewer = better batching)")]
        [Range(2, 10)]
        [SerializeField] private int opacityTiers = 5;

        // Runtime state
        private TilemapTile[] tierTiles;  // one tile per opacity tier, batches together
        private Vector2 currentOffset;
        private Vector2 morphOffset;      // separate offset for the morph layer
        private float updateTimer;
        private int mapWidth;
        private int mapHeight;
        private bool initialized;
        private int lastOpacityTiers;
        private float lastMaxOpacity;

        private void Start()
        {
            if (mapOrchestrator == null)
                mapOrchestrator = FindFirstObjectByType<MapGenerationOrchestrator>();

            currentOffset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));
            morphOffset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));
        }

        private void Update()
        {
            // Wait until the map has been generated before initializing
            if (!initialized)
            {
                if (mapOrchestrator == null || mapOrchestrator.MapData == null) return;

                mapWidth = mapOrchestrator.MapData.Width;
                mapHeight = mapOrchestrator.MapData.Height;
                BuildTierTiles();
                initialized = true;
            }

            // Respect the orchestrator's toggle
            if (!mapOrchestrator.enableClouds)
            {
                if (cloudTilemap != null) cloudTilemap.ClearAllTiles();
                return;
            }

            // Rebuild tier tiles if appearance settings changed at runtime
            if (opacityTiers != lastOpacityTiers || !Mathf.Approximately(maxOpacity, lastMaxOpacity))
                BuildTierTiles();

            // Drift the main layer (movement) and morph layer (shape change)
            currentOffset += windSpeed * Time.deltaTime;
            morphOffset += new Vector2(morphSpeed, morphSpeed * 0.7f) * Time.deltaTime;

            // Only update the tilemap on a timer (not every frame)
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateClouds();
            }
        }

        /// <summary>
        /// Create one tile per opacity tier from the source cloud tile's sprite.
        /// Tiles sharing the same TileBase batch together, so 5 tiers = max 5 batches.
        /// </summary>
        private void BuildTierTiles()
        {
            if (cloudTile == null)
            {
                Debug.LogWarning("[CloudRenderer] No cloud tile assigned.");
                return;
            }

            tierTiles = new TilemapTile[opacityTiers];

            // Extract sprite from whichever tile type the user assigned
            Sprite sourceSprite = null;
            if (cloudTile is TilemapTile tilemapTile)
                sourceSprite = tilemapTile.sprite;

            // Fallback: place the tile temporarily and read the sprite from the tilemap
            if (sourceSprite == null && cloudTilemap != null)
            {
                var tempPos = new Vector3Int(-9999, -9999, 0);
                cloudTilemap.SetTile(tempPos, cloudTile);
                sourceSprite = cloudTilemap.GetSprite(tempPos);
                cloudTilemap.SetTile(tempPos, null);
            }

            if (sourceSprite == null)
            {
                Debug.LogWarning("[CloudRenderer] Could not get sprite from cloud tile.");
                return;
            }

            Debug.Log($"[CloudRenderer] Building {opacityTiers} tiers, sprite: {sourceSprite.name}, maxOpacity: {maxOpacity}");

            lastOpacityTiers = opacityTiers;
            lastMaxOpacity = maxOpacity;

            for (int i = 0; i < opacityTiers; i++)
            {
                float alpha = maxOpacity * (i + 1) / opacityTiers;

                var tile = ScriptableObject.CreateInstance<TilemapTile>();
                tile.sprite = sourceSprite;
                tile.color = new Color(1f, 1f, 1f, alpha);
                tierTiles[i] = tile;
            }
        }

        /// <summary>
        /// Sample Perlin noise at each cell. Cells above threshold get a cloud tile
        /// chosen from a small set of opacity tiers (for efficient batching).
        /// </summary>
        private void UpdateClouds()
        {
            if (cloudTilemap == null || tierTiles == null) return;

            cloudTilemap.ClearAllTiles();

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // Main layer: large cloud shapes that drift with the wind
                    float mainX = (x + currentOffset.x) / noiseScale;
                    float mainY = (y + currentOffset.y) / noiseScale;
                    float main = Mathf.PerlinNoise(mainX, mainY);

                    // Morph layer: different scale and offset, evolves independently
                    // This makes cloud edges shift and reshape over time
                    float morphX = (x + morphOffset.x) / morphScale;
                    float morphY = (y + morphOffset.y) / morphScale;
                    float morph = Mathf.PerlinNoise(morphX, morphY);

                    // Blend: main shape + morph distortion
                    float noise = main * (1f - morphStrength) + morph * morphStrength;

                    if (noise < threshold) continue;

                    float t = (noise - threshold) / (1f - threshold);
                    int tier = Mathf.Clamp(Mathf.FloorToInt(t * opacityTiers), 0, opacityTiers - 1);

                    cloudTilemap.SetTile(new Vector3Int(x, y, 0), tierTiles[tier]);
                }
            }
        }
    }
}
