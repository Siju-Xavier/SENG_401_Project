namespace Presentation
{
    using UnityEngine;
    using BusinessLogic;
    using GameState;

    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Tilemaps.Tilemap groundTilemap;
        [SerializeField] private Camera mainCamera;

        private ResourceManager resourceManager;
        private GridSystem gridSystem;

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (groundTilemap == null)
                groundTilemap = GameObject.FindObjectOfType<UnityEngine.Tilemaps.Tilemap>();

            resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        public void SetGridSystem(GridSystem grid)
        {
            gridSystem = grid;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }

        private void HandleClick()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) 
            {
                return; // Ignore clicks on the map if we're clicking on the UI
            }

            if (mainCamera == null || groundTilemap == null || gridSystem == null) return;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);

            Tile tile = gridSystem.GetTileAt(cellPos.x, cellPos.y);
            if (tile == null) return;

            // Check if user clicked a city's footprint
            if (tile.IsCityFootprint && tile.Region != null && tile.Region.City != null)
            {
                if (CityPanelController.Instance != null)
                {
                    CityPanelController.Instance.ShowPanel(tile.Region.City);
                }

                if (TerritoryRenderer.Instance != null)
                {
                    TerritoryRenderer.Instance.ShowBorders(tile.Region, gridSystem);
                }
                
                return; // Consume click for the city
            }

            // Normal tile click
            if (tile.IsOnFire && resourceManager != null)
            {
                resourceManager.DeployFirefighter(tile);
            }
        }
    }
}
