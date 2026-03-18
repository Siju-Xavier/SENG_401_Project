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

        private void Update()
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

            // Snap to pixel grid to prevent tile edge flickering
            const float ppu = 32f;
            pos.x = Mathf.Round(pos.x * ppu) / ppu;
            pos.y = Mathf.Round(pos.y * ppu) / ppu;

            transform.position = pos;
        }
    }
}
