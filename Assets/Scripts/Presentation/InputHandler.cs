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
            if (mainCamera == null || groundTilemap == null || gridSystem == null) return;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);

            Tile tile = gridSystem.GetTileAt(cellPos.x, cellPos.y);
            if (tile == null) return;

            if (tile.IsOnFire && resourceManager != null)
            {
                resourceManager.DeployFirefighter(tile);
            }
        }
    }
}
