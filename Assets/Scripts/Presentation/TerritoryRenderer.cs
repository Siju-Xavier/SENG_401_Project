namespace Presentation
{
    using UnityEngine;
    using System.Collections.Generic;
    using GameState;
    using UnityEngine.Tilemaps;
    using TilemapTile = UnityEngine.Tilemaps.Tile;

    public class TerritoryRenderer : MonoBehaviour
    {
        public static TerritoryRenderer Instance { get; private set; }

        [Header("References")]
        [Tooltip("The main ground tilemap to align positions to")]
        [SerializeField] private Tilemap groundTilemap;

        [Tooltip("Dedicated tilemap for border outlines (auto-created if empty)")]
        [SerializeField] private Tilemap borderTilemap;

        [Header("Settings")]
        [SerializeField] private Color borderColor = new Color(0f, 0.8f, 1f, 0.9f);
        [SerializeField] private int borderThickness = 3;
        [SerializeField] private int textureSize = 64;

        // 16 edge-combination tiles: index = 4-bit mask (top|right|bottom|left)
        private TilemapTile[] edgeTiles;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (groundTilemap == null)
                groundTilemap = FindObjectOfType<Tilemap>();

            if (borderTilemap == null)
                CreateBorderTilemap();

            GenerateEdgeTiles();
        }

        private void CreateBorderTilemap()
        {
            var go = new GameObject("BorderTilemap");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            var grid = groundTilemap != null ? groundTilemap.layoutGrid : FindObjectOfType<Grid>();
            if (grid != null)
                go.transform.SetParent(grid.transform);

            borderTilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = 5;
        }

        private void GenerateEdgeTiles()
        {
            edgeTiles = new TilemapTile[16];

            for (int mask = 1; mask < 16; mask++)
            {
                var tex = new Texture2D(textureSize, textureSize);
                tex.filterMode = FilterMode.Point;

                // Start with fully transparent
                var clear = new Color[textureSize * textureSize];
                for (int i = 0; i < clear.Length; i++)
                    clear[i] = Color.clear;
                tex.SetPixels(clear);

                int t = borderThickness;

                // Bit 0 = top edge
                if ((mask & 1) != 0)
                    FillRect(tex, 0, textureSize - t, textureSize, t);

                // Bit 1 = right edge
                if ((mask & 2) != 0)
                    FillRect(tex, textureSize - t, 0, t, textureSize);

                // Bit 2 = bottom edge
                if ((mask & 4) != 0)
                    FillRect(tex, 0, 0, textureSize, t);

                // Bit 3 = left edge
                if ((mask & 8) != 0)
                    FillRect(tex, 0, 0, t, textureSize);

                tex.Apply();

                var sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, textureSize, textureSize),
                    new Vector2(0.5f, 0.5f),
                    textureSize
                );

                var tile = ScriptableObject.CreateInstance<TilemapTile>();
                tile.sprite = sprite;
                tile.color = borderColor;
                edgeTiles[mask] = tile;
            }
        }

        private void FillRect(Texture2D tex, int startX, int startY, int width, int height)
        {
            for (int y = startY; y < startY + height && y < textureSize; y++)
            {
                for (int x = startX; x < startX + width && x < textureSize; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }

        public void ShowBorders(Region region, GridSystem gridSystem)
        {
            ClearBorders();

            if (region == null || gridSystem == null || borderTilemap == null) return;

            // Build a HashSet for fast "is this tile in the region?" lookups
            var regionTiles = new HashSet<Vector2Int>();
            foreach (var tile in region.Tiles)
                regionTiles.Add(new Vector2Int(tile.X, tile.Y));

            foreach (var tile in region.Tiles)
            {
                int mask = 0;

                // Top neighbor (y+1)
                if (!regionTiles.Contains(new Vector2Int(tile.X, tile.Y + 1)))
                    mask |= 1;

                // Right neighbor (x+1)
                if (!regionTiles.Contains(new Vector2Int(tile.X + 1, tile.Y)))
                    mask |= 2;

                // Bottom neighbor (y-1)
                if (!regionTiles.Contains(new Vector2Int(tile.X, tile.Y - 1)))
                    mask |= 4;

                // Left neighbor (x-1)
                if (!regionTiles.Contains(new Vector2Int(tile.X - 1, tile.Y)))
                    mask |= 8;

                if (mask != 0)
                {
                    borderTilemap.SetTile(new Vector3Int(tile.X, tile.Y, 0), edgeTiles[mask]);
                }
            }
        }

        public void ClearBorders()
        {
            if (borderTilemap != null)
                borderTilemap.ClearAllTiles();
        }
    }
}
