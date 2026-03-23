// ============================================================================
// TileRenderer.cs — Per-tile fire VFX overlay
// ============================================================================
// Subscribes to EventBroker events (FireStarted, FireSpread, FireExtinguished)
// and spawns/destroys animated fire sprites on the tilemap.
//
// The fire animation uses the orange fire PNGs from fire_fx_v1.0:
//   start → loop → end
// ============================================================================

namespace Presentation {
    using System.Collections;
    using System.Collections.Generic;
    using Core;
    using GameState;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    public class TileRenderer : MonoBehaviour {

        [Header("Fire Animation Sprites")]
        [Tooltip("Drag the 5 orange start frames here (burning_start_1 … 5).")]
        [SerializeField] private Sprite[] fireStartFrames;

        [Tooltip("Drag the 5 orange loop frames here (burning_loop_1 … 5).")]
        [SerializeField] private Sprite[] fireLoopFrames;

        [Tooltip("Drag the 5 orange end frames here (burning_end_1 … 5).")]
        [SerializeField] private Sprite[] fireEndFrames;

        [Header("Animation Settings")]
        [Tooltip("Seconds per animation frame.")]
        [SerializeField] private float frameRate = 0.12f;

        [Header("Tilemap")]
        [Tooltip("Reference to the ground Tilemap so fire overlays land on the correct world position.")]
        [SerializeField] private Tilemap groundTilemap;

        [Tooltip("Offset to align fire sprite with tile center.")]
        [SerializeField] private Vector3 fireOffset = new Vector3(0.5f, 0.5f, 0f);

        // Active fire overlays, keyed by tile grid position
        private Dictionary<Vector2Int, GameObject> activeFireOverlays
            = new Dictionary<Vector2Int, GameObject>();

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void OnEnable() {
            EventBroker.Instance.Subscribe(Core.EventType.FireStarted,      OnFireStarted);
            EventBroker.Instance.Subscribe(Core.EventType.FireSpread,       OnFireSpread);
            EventBroker.Instance.Subscribe(Core.EventType.FireExtinguished, OnFireExtinguished);
            EventBroker.Instance.Subscribe(Core.EventType.FireNoLongerEdge, OnFireNoLongerEdge);
        }

        private void OnDisable() {
            EventBroker.Instance.Unsubscribe(Core.EventType.FireStarted,      OnFireStarted);
            EventBroker.Instance.Unsubscribe(Core.EventType.FireSpread,       OnFireSpread);
            EventBroker.Instance.Unsubscribe(Core.EventType.FireExtinguished, OnFireExtinguished);
            EventBroker.Instance.Unsubscribe(Core.EventType.FireNoLongerEdge, OnFireNoLongerEdge);
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void OnFireStarted(object data)  => SpawnFireOverlay(data as GameState.Tile);
        private void OnFireSpread(object data)   => SpawnFireOverlay(data as GameState.Tile);

        private void OnFireExtinguished(object data) {
            var tile = data as GameState.Tile;
            if (tile == null) return;

            var key = new Vector2Int(tile.X, tile.Y);
            if (activeFireOverlays.TryGetValue(key, out var overlay)) {
                // Play the "end" animation, then destroy
                StartCoroutine(PlayEndAnimation(overlay, key));
            }
        }

        private void OnFireNoLongerEdge(object data) {
            var tile = data as GameState.Tile;
            if (tile == null) return;

            var key = new Vector2Int(tile.X, tile.Y);
            if (activeFireOverlays.TryGetValue(key, out var overlay)) {
                // Destroy the overlay without playing the end animation
                if (overlay != null) Destroy(overlay);
                activeFireOverlays.Remove(key);
            }
        }

        // ── Public API (for backward compatibility) ──────────────────────

        public void RenderIsometricSprite() { }

        public void PlayFireVFX()       {
            // Called externally if needed — fires are self-managed via events
        }

        public void PlayExtinguishVFX() {
            // Called externally if needed — handled via events
        }

        // ── Fire Overlay Management ──────────────────────────────────────

        private void SpawnFireOverlay(GameState.Tile tile) {
            if (tile == null) return;

            var key = new Vector2Int(tile.X, tile.Y);
            if (activeFireOverlays.ContainsKey(key)) return; // Already has fire overlay

            // Create a new GameObject with a SpriteRenderer
            var fireGO = new GameObject($"Fire_{tile.X}_{tile.Y}");
            fireGO.transform.SetParent(transform);

            // Use the tilemap's GetCellCenterWorld for correct positioning on isometric maps
            if (groundTilemap != null) {
                fireGO.transform.position = groundTilemap.GetCellCenterWorld(new Vector3Int(tile.X, tile.Y, 0)) + new Vector3(0f, 0.25f, 0f);
            } else {
                fireGO.transform.position = new Vector3(tile.X, tile.Y, 0) + fireOffset;
            }

            var sr = fireGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10; // Render above ground tiles

            activeFireOverlays[key] = fireGO;

            // Start the animation coroutine: start → loop
            StartCoroutine(PlayFireAnimation(fireGO, sr));
        }

        // ── Animation Coroutines ─────────────────────────────────────────

        /// <summary>Play start frames, then loop the burning frames indefinitely.</summary>
        private IEnumerator PlayFireAnimation(GameObject fireGO, SpriteRenderer sr) {
            // ── Start phase ──
            if (fireStartFrames != null && fireStartFrames.Length > 0) {
                foreach (var frame in fireStartFrames) {
                    if (fireGO == null) yield break;
                    sr.sprite = frame;
                    yield return new WaitForSeconds(frameRate);
                }
            }

            // ── Loop phase (loops forever until extinguished) ──
            if (fireLoopFrames != null && fireLoopFrames.Length > 0) {
                int index = 0;
                while (fireGO != null) {
                    sr.sprite = fireLoopFrames[index % fireLoopFrames.Length];
                    index++;
                    yield return new WaitForSeconds(frameRate);
                }
            }
        }

        /// <summary>Play the end/extinguish frames, then destroy the overlay.</summary>
        private IEnumerator PlayEndAnimation(GameObject fireGO, Vector2Int key) {
            if (fireGO == null) {
                activeFireOverlays.Remove(key);
                yield break;
            }

            var sr = fireGO.GetComponent<SpriteRenderer>();

            // ── End phase ──
            if (fireEndFrames != null && fireEndFrames.Length > 0 && sr != null) {
                // Stop any current animation on this object
                foreach (var frame in fireEndFrames) {
                    if (fireGO == null) break;
                    sr.sprite = frame;
                    yield return new WaitForSeconds(frameRate);
                }
            }

            // Destroy and clean up
            if (fireGO != null)
                Destroy(fireGO);

            activeFireOverlays.Remove(key);
        }
    }
}
