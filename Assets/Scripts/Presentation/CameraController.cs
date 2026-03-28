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
