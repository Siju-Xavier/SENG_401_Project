namespace Presentation.MapGeneration
{
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using GameState;
    using ScriptableObjects;

    public class CityPlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private TreePlacer treePlacer;

        [Header("City Configs")]
        [Tooltip("Assign CityConfig assets here — one per city variation")]
        [SerializeField] private CityConfig[] cityConfigs;

        private GameObject cityContainer;

        public void ClearCities()
        {
            if (cityContainer != null)
            {
                if (Application.isPlaying) Destroy(cityContainer);
                else DestroyImmediate(cityContainer);
                cityContainer = null;
            }

            while (true)
            {
                var leftover = transform.Find("Cities");
                if (leftover == null) break;
                leftover.name = "Cities_Destroying";
                if (Application.isPlaying) Destroy(leftover.gameObject);
                else DestroyImmediate(leftover.gameObject);
            }
        }

        public void PlaceCities(GridSystem gridSystem)
        {
            ClearCities();

            cityContainer = new GameObject("Cities");
            cityContainer.transform.SetParent(transform);

            if (gridSystem == null || gridSystem.Regions == null) return;
            if (cityConfigs == null || cityConfigs.Length == 0) return;

            for (int i = 0; i < gridSystem.Regions.Count; i++)
            {
                Region region = gridSystem.Regions[i];
                City city = region.City;
                if (city == null) continue;

                // Pick a config (cycle through available configs)
                CityConfig config = cityConfigs[i % cityConfigs.Length];
                if (config == null || config.CityPrefab == null)
                {
                    Debug.LogWarning($"CityPlacer: No config/prefab for city {city.CityName}");
                    continue;
                }

                // Convert tile position to world position
                Vector3 worldPos;
                if (groundTilemap != null)
                {
                    Vector3Int tilePos = new Vector3Int(city.TileX, city.TileY, 0);
                    worldPos = groundTilemap.CellToWorld(tilePos) + groundTilemap.cellSize * 0.5f;
                }
                else
                {
                    worldPos = new Vector3(city.TileX, city.TileY, 0);
                }

                // Instantiate city prefab
                var cityGO = Instantiate(config.CityPrefab, cityContainer.transform);
                cityGO.name = city.CityName;
                cityGO.transform.position = new Vector3(worldPos.x, worldPos.y, -1f);
                cityGO.transform.localScale = Vector3.one * config.PrefabScale;

                // Clear decorations (trees, etc.) under the city footprint
                // Footprint is centered on the city tile
                int halfW = config.FootprintWidth / 2;
                int halfH = config.FootprintHeight / 2;
                int startX = city.TileX - halfW;
                int startY = city.TileY - halfH;

                // Clear vegetation under the city footprint
                if (treePlacer != null)
                {
                    treePlacer.RemoveTreesInArea(startX, startY, config.FootprintWidth, config.FootprintHeight);
                }

                // Mark tiles as occupied by this region
                for (int ty = startY; ty < startY + config.FootprintHeight; ty++)
                {
                    for (int tx = startX; tx < startX + config.FootprintWidth; tx++)
                    {
                        GameState.Tile tile = gridSystem.GetTileAt(tx, ty);
                        if (tile != null)
                        {
                            region.AddTile(tile);
                        }
                    }
                }

                Debug.Log($"Placed city '{city.CityName}' at ({city.TileX},{city.TileY}), footprint {config.FootprintWidth}x{config.FootprintHeight}");
            }
        }
    }
}
