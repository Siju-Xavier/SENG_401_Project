namespace Presentation.MapGeneration
{
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using GameState;

    public class CityPlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap groundTilemap;

        [Header("City Sprites")]
        [Tooltip("Assign City_1, City_2, City_3 sprites here")]
        [SerializeField] private Sprite[] citySprites;

        [Header("City Display")]
        [SerializeField] private float citySpriteScale = 3f;

        private GameObject cityContainer;

        public void ClearCities()
        {
            // Destroy tracked container
            if (cityContainer != null)
            {
                if (Application.isPlaying) Destroy(cityContainer);
                else DestroyImmediate(cityContainer);
                cityContainer = null;
            }

            // Also destroy any leftover Cities objects from previous editor sessions
            while (true)
            {
                var leftover = transform.Find("Cities");
                if (leftover == null) break;
                
                // Rename it so transform.Find doesn't find the exact same object on the next iteration
                // since Destroy() doesn't immediately remove the object from the hierarchy in Play mode.
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

            for (int i = 0; i < gridSystem.Regions.Count; i++)
            {
                Region region = gridSystem.Regions[i];
                City city = region.City;
                if (city == null) continue;

                // Pick a sprite (cycle through available sprites)
                Sprite sprite = null;
                if (citySprites != null && citySprites.Length > 0)
                    sprite = citySprites[i % citySprites.Length];

                if (sprite == null)
                {
                    Debug.LogWarning($"CityPlacer: No sprite for city {city.CityName}");
                    continue;
                }

                // Convert tile position to world position using the tilemap
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

                // Create city GameObject with SpriteRenderer
                var cityGO = new GameObject(city.CityName);
                cityGO.transform.SetParent(cityContainer.transform);
                cityGO.transform.position = new Vector3(worldPos.x, worldPos.y, -1f); // render above tiles

                var sr = cityGO.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 10; // above ground tilemap
                cityGO.transform.localScale = Vector3.one * citySpriteScale;

                Debug.Log($"Placed city '{city.CityName}' at tile ({city.TileX}, {city.TileY}) -> world {worldPos}");
            }
        }
    }
}
