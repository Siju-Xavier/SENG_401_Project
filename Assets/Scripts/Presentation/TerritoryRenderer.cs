namespace Presentation
{
    using UnityEngine;
    using System.Collections.Generic;
    using GameState;
    using UnityEngine.Tilemaps;

    public class TerritoryRenderer : MonoBehaviour
    {
        public static TerritoryRenderer Instance { get; private set; }

        [Header("References")]
        [Tooltip("The main ground tilemap to align positions to")]
        [SerializeField] private Tilemap groundTilemap;

        [Header("Settings")]
        [SerializeField] private Color borderColor = new Color(0f, 0.8f, 1f, 0.5f);
        
        private List<GameObject> activeBorderHighlights = new List<GameObject>();
        private Sprite borderSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;

            // Generate a simple square texture for the highlight
            Texture2D tex = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();

            borderSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);

            if (groundTilemap == null)
                groundTilemap = GameObject.FindObjectOfType<UnityEngine.Tilemaps.Tilemap>();
        }

        public void ShowBorders(Region region, GridSystem gridSystem)
        {
            ClearBorders();

            if (region == null || gridSystem == null) return;

            foreach (var tile in region.Tiles)
            {
                bool isBorder = false;
                
                // Get neighbours. If any neighbour has a different region or is out of bounds, it's a border.
                var neighbours = gridSystem.GetNeighbours(tile);
                if (neighbours.Count < 4) 
                {
                    isBorder = true; // edge of map
                }
                else
                {
                    foreach (var n in neighbours)
                    {
                        if (n.Region != region)
                        {
                            isBorder = true;
                            break;
                        }
                    }
                }

                if (isBorder)
                {
                    SpawnBorderHighlight(tile);
                }
            }
        }

        public void ClearBorders()
        {
            foreach (var go in activeBorderHighlights)
            {
                if (go != null) Destroy(go);
            }
            activeBorderHighlights.Clear();
        }

        private void SpawnBorderHighlight(GameState.Tile tile)
        {
            GameObject highlightGO = new GameObject($"Border_{tile.X}_{tile.Y}");
            highlightGO.transform.SetParent(transform);

            if (groundTilemap != null)
            {
                highlightGO.transform.position = groundTilemap.GetCellCenterWorld(new Vector3Int(tile.X, tile.Y, 0)) + new Vector3(0f, 0.26f, 0f);
            }
            else
            {
                highlightGO.transform.position = new Vector3(tile.X, tile.Y, -0.5f);
            }

            SpriteRenderer sr = highlightGO.AddComponent<SpriteRenderer>();
            sr.sprite = borderSprite;
            sr.color = borderColor;
            sr.sortingOrder = 5; // Render under fire (10) but above ground

            activeBorderHighlights.Add(highlightGO);
        }
    }
}
