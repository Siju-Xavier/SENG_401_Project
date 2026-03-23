// ============================================================================
// BiomeTilemapRenderer.cs — Renders biome tiles onto a Unity Tilemap
// ============================================================================
// Also subscribes to fire events from EventBroker to swap ground tiles
// between their default and burning variants (from BiomeConfig).
// ============================================================================

namespace Presentation.MapGeneration
{
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using BusinessLogic.MapGeneration;
    using Core;
    using GameState;
    using ScriptableObjects;

    public class BiomeTilemapRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap groundTilemap;

        private MapData mapData;

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void OnEnable() {
            EventBroker.Instance.Subscribe(Core.EventType.FireStarted,      OnFireStarted);
            EventBroker.Instance.Subscribe(Core.EventType.FireSpread,       OnFireSpread);
            EventBroker.Instance.Subscribe(Core.EventType.FireExtinguished, OnFireExtinguished);
            EventBroker.Instance.Subscribe(Core.EventType.TileRecovered,    OnTileRecovered);
        }

        private void OnDisable() {
            EventBroker.Instance.Unsubscribe(Core.EventType.FireStarted,      OnFireStarted);
            EventBroker.Instance.Unsubscribe(Core.EventType.FireSpread,       OnFireSpread);
            EventBroker.Instance.Unsubscribe(Core.EventType.FireExtinguished, OnFireExtinguished);
            EventBroker.Instance.Unsubscribe(Core.EventType.TileRecovered,    OnTileRecovered);
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void OnFireStarted(object data)  => SwapToBurning(data as GameState.Tile);
        private void OnFireSpread(object data)   => SwapToBurning(data as GameState.Tile);
        private void OnFireExtinguished(object data) {
            var tile = data as GameState.Tile;
            if (tile == null) return;
            if (tile.IsBurnt) {
                SetTileBurnt(tile.X, tile.Y);
            } else {
                SetTileDefault(tile.X, tile.Y);
            }
        }

        private void OnTileRecovered(object data) => SwapToDefault(data as GameState.Tile);

        private void SwapToBurning(GameState.Tile tile) {
            if (tile == null) return;
            SetTileBurning(tile.X, tile.Y);
        }

        private void SwapToDefault(GameState.Tile tile) {
            if (tile == null) return;
            SetTileDefault(tile.X, tile.Y);
        }

        public void ClearMap()
        {
            if (groundTilemap != null)
            {
                groundTilemap.ClearAllTiles();
#if UNITY_EDITOR
                groundTilemap.ClearAllEditorPreviewTiles();
#endif
            }
            mapData = null;
        }

        // ── Map Rendering ────────────────────────────────────────────────

        public void RenderMap(MapData data)
        {
            mapData = data;

            if (data == null || data.BiomeGrid == null)
            {
                Debug.LogWarning("BiomeTilemapRenderer: No biome grid to render.");
                return;
            }

#if UNITY_EDITOR
            bool preview = !Application.isPlaying;
            if (preview)
                groundTilemap.ClearAllEditorPreviewTiles();
            else
#endif
                groundTilemap.ClearAllTiles();

            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    BiomeConfig biome = data.BiomeGrid[x, y];
                    if (biome != null && biome.DefaultTile != null)
                    {
                        var pos = new Vector3Int(x, y, 0);
#if UNITY_EDITOR
                        if (preview)
                            groundTilemap.SetEditorPreviewTile(pos, biome.DefaultTile);
                        else
#endif
                            groundTilemap.SetTile(pos, biome.DefaultTile);
                    }
                }
            }
        }

        // ── Tile Swap Helpers ────────────────────────────────────────────

        public void SetTileBurning(int x, int y)
        {
            BiomeConfig biome = GetBiomeAt(x, y);
            if (biome != null && biome.BurningTile != null)
            {
                var pos = new Vector3Int(x, y, 0);
                groundTilemap.SetTile(pos, biome.BurningTile);
                groundTilemap.SetTileFlags(pos, TileFlags.None);
                groundTilemap.SetColor(pos, Color.white);
            }
        }

        public void SetTileDefault(int x, int y)
        {
            BiomeConfig biome = GetBiomeAt(x, y);
            if (biome != null && biome.DefaultTile != null)
            {
                var pos = new Vector3Int(x, y, 0);
                groundTilemap.SetTile(pos, biome.DefaultTile);
                groundTilemap.SetTileFlags(pos, TileFlags.None);
                groundTilemap.SetColor(pos, Color.white);
            }
        }

        public void SetTileBurnt(int x, int y)
        {
            BiomeConfig biome = GetBiomeAt(x, y);
            if (biome != null && biome.DefaultTile != null)
            {
                var pos = new Vector3Int(x, y, 0);
                groundTilemap.SetTile(pos, biome.DefaultTile);
                groundTilemap.SetTileFlags(pos, TileFlags.None);
                // Tint to a dark grey indicating burnt state
                groundTilemap.SetColor(pos, new Color(0.25f, 0.25f, 0.25f, 1f));
            }
        }

        private BiomeConfig GetBiomeAt(int x, int y)
        {
            if (mapData?.BiomeGrid == null || x < 0 || x >= mapData.Width || y < 0 || y >= mapData.Height)
                return null;
            return mapData.BiomeGrid[x, y];
        }
    }
}
