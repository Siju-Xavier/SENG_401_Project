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

            // Scale move speed by zoom level: slower when zoomed in, faster when zoomed out
            float zoomFactor = cam != null ? cam.orthographicSize / maxZoom : 1f;
            Vector3 move = new Vector3(input.x, input.y, 0f) * (moveSpeed * zoomFactor * Time.deltaTime);
            Vector3 pos = transform.position + move;

            // Snap to pixel grid to prevent tile edge flickering
            const float ppu = 32f;
            pos.x = Mathf.Round(pos.x * ppu) / ppu;
            pos.y = Mathf.Round(pos.y * ppu) / ppu;

            transform.position = pos;
        }

        private void HandleZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || cam == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;

            float newSize = cam.orthographicSize - scroll * zoomSpeed * Time.unscaledDeltaTime;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}
