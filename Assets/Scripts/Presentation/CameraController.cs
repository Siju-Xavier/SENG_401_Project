using UnityEngine;
using UnityEngine.InputSystem;

namespace Presentation
{
    /// <summary>
    /// Attach to Main Camera. Handles WASD pan and scroll zoom directly.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 20f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 60f;

        private Camera cam;
        private float targetZoom;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            targetZoom = cam.orthographicSize;
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

            Vector3 move = new Vector3(input.x, input.y, 0f) * (moveSpeed * Time.deltaTime);
            Vector3 pos = transform.position + move;

            // Snap to pixel grid to prevent tile border flickering
            const float ppu = 32f;
            pos.x = Mathf.Round(pos.x * ppu) / ppu;
            pos.y = Mathf.Round(pos.y * ppu) / ppu;

            transform.position = pos;
        }

        private void HandleZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.y.ReadValue();
            if (scroll != 0f)
                targetZoom = Mathf.Clamp(targetZoom - scroll * zoomSpeed * Time.deltaTime, minZoom, maxZoom);

            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, 10f * Time.deltaTime);
        }

        public void CenterOn(Vector3 worldPos)
        {
            worldPos.z = transform.position.z;
            transform.position = worldPos;
        }
    }
}
