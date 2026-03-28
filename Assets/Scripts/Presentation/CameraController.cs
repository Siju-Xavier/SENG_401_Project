using UnityEngine;
using UnityEngine.InputSystem;

namespace Presentation
{
    /// <summary>
    /// Cinemachine Targeted Camera follows this target.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 20f;

        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
        }

        private void Start()
        {
            // Center camera on the map once it generates
            StartCoroutine(CenterOnMapWhenReady());
        }

        private System.Collections.IEnumerator CenterOnMapWhenReady()
        {
            // Wait for MapGenerationOrchestrator to finish
            var orch = FindFirstObjectByType<Core.MapGenerationOrchestrator>();
            while (orch == null || orch.GridSystem == null)
            {
                yield return null;
                if (orch == null) orch = FindFirstObjectByType<Core.MapGenerationOrchestrator>();
            }

            CenterOnMap(orch.GridSystem.Width, orch.GridSystem.Height);
        }

        /// <summary>
        /// Position the camera target at the center of the isometric map
        /// and set zoom to fit the whole map.
        /// </summary>
        public void CenterOnMap(int mapWidth, int mapHeight)
        {
            // Find the ground tilemap to convert grid coords to world coords
            var tilemap = FindFirstObjectByType<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null) return;

            // Center tile in grid space
            var centerCell = new Vector3Int(mapWidth / 2, mapHeight / 2, 0);
            Vector3 worldCenter = tilemap.CellToWorld(centerCell) + tilemap.cellSize * 0.5f;

            transform.position = new Vector3(worldCenter.x, worldCenter.y, transform.position.z);

            // Zoom out to fit the map — estimate based on map size
            if (cam != null)
            {
                // For isometric, the visible area scales with orthographic size
                float targetSize = Mathf.Max(mapWidth, mapHeight) * 0.3f;
                cam.orthographicSize = Mathf.Clamp(targetSize, minZoom, maxZoom);
            }

            Debug.Log($"[Camera] Centered on map ({mapWidth}x{mapHeight}) at world pos {worldCenter}");
        }

        private void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            Vector2 input = Vector2.zero;

            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;

            if (input == Vector2.zero) return;

            input.Normalize();

            // Scale move speed proportionally to zoom so panning feels consistent
            float zoomFactor = cam != null ? cam.orthographicSize / ((minZoom + maxZoom) * 0.5f) : 1f;
            Vector3 move = new Vector3(input.x, input.y, 0f) * (moveSpeed * zoomFactor * Time.deltaTime);
            transform.position += move;
        }

        private void HandleZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || cam == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;

            float newSize = cam.orthographicSize - Mathf.Sign(scroll) * zoomSpeed * 0.5f;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}
