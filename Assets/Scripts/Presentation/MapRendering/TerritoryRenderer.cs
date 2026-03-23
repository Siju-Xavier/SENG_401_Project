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
        [SerializeField] private Tilemap groundTilemap;

        [Header("Border Settings")]
        [SerializeField] private Color borderColor = new Color(0f, 0.8f, 1f, 0.9f);
        [SerializeField] private float borderWidth = 0.06f;
        [SerializeField] private Material lineMaterial;

        private List<GameObject> activeLineObjects = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (groundTilemap == null)
                groundTilemap = FindObjectOfType<Tilemap>();
        }

        public void ShowBorders(Region region, GridSystem gridSystem)
        {
            ClearBorders();

            if (region == null || gridSystem == null || groundTilemap == null) return;

            // 1. Build HashSet for fast region membership lookup
            var regionSet = new HashSet<Vector2Int>();
            foreach (var tile in region.Tiles)
                regionSet.Add(new Vector2Int(tile.X, tile.Y));

            // 2. Compute diamond corner offsets from cell size
            Vector3 cellSize = groundTilemap.cellSize;
            float hw = cellSize.x * 0.5f; // half width
            float hh = cellSize.y * 0.5f; // half height

            // 3. Collect border edge segments
            //    Each segment is an ordered pair (start → end) going clockwise around the region
            var segments = new Dictionary<long, long>(); // encoded point → encoded point

            foreach (var tile in region.Tiles)
            {
                Vector3 center = groundTilemap.GetCellCenterWorld(new Vector3Int(tile.X, tile.Y, 0));

                // Diamond corners
                Vector2 top    = new Vector2(center.x,      center.y + hh);
                Vector2 right  = new Vector2(center.x + hw, center.y);
                Vector2 bottom = new Vector2(center.x,      center.y - hh);
                Vector2 left   = new Vector2(center.x - hw, center.y);

                // Check each neighbor — if missing, that edge is a border
                // Top-right neighbor (x+1, y) shares edge: Top → Right
                if (!regionSet.Contains(new Vector2Int(tile.X + 1, tile.Y)))
                    AddSegment(segments, top, right);

                // Top-left neighbor (x, y+1) shares edge: Left → Top
                if (!regionSet.Contains(new Vector2Int(tile.X, tile.Y + 1)))
                    AddSegment(segments, left, top);

                // Bottom-left neighbor (x-1, y) shares edge: Bottom → Left
                if (!regionSet.Contains(new Vector2Int(tile.X - 1, tile.Y)))
                    AddSegment(segments, bottom, left);

                // Bottom-right neighbor (x, y-1) shares edge: Right → Bottom
                if (!regionSet.Contains(new Vector2Int(tile.X, tile.Y - 1)))
                    AddSegment(segments, right, bottom);
            }

            // 4. Chain segments into closed loops
            var loops = ChainSegments(segments);

            // 5. Create a LineRenderer for each loop
            foreach (var loop in loops)
            {
                CreateBorderLine(loop);
            }
        }

        public void ClearBorders()
        {
            foreach (var go in activeLineObjects)
            {
                if (go != null) Destroy(go);
            }
            activeLineObjects.Clear();
        }

        // ── Segment collection ──────────────────────────────────────────

        // Encode a Vector2 to a long key for reliable dictionary matching
        private long EncodePoint(Vector2 p)
        {
            int x = Mathf.RoundToInt(p.x * 10000f);
            int y = Mathf.RoundToInt(p.y * 10000f);
            return ((long)x << 32) | (uint)y;
        }

        private Vector2 DecodePoint(long encoded)
        {
            int x = (int)(encoded >> 32);
            int y = (int)(encoded & 0xFFFFFFFF);
            return new Vector2(x / 10000f, y / 10000f);
        }

        private void AddSegment(Dictionary<long, long> segments, Vector2 from, Vector2 to)
        {
            segments[EncodePoint(from)] = EncodePoint(to);
        }

        // ── Segment chaining ────────────────────────────────────────────

        private List<List<Vector3>> ChainSegments(Dictionary<long, long> segments)
        {
            var loops = new List<List<Vector3>>();
            var remaining = new Dictionary<long, long>(segments);

            while (remaining.Count > 0)
            {
                // Pick any starting point
                long startKey = 0;
                foreach (var kvp in remaining) { startKey = kvp.Key; break; }

                var loop = new List<Vector3>();
                long current = startKey;

                do
                {
                    Vector2 point = DecodePoint(current);
                    loop.Add(new Vector3(point.x, point.y, 0f));

                    long next = remaining[current];
                    remaining.Remove(current);
                    current = next;
                }
                while (current != startKey && remaining.ContainsKey(current));

                if (loop.Count >= 3)
                    loops.Add(loop);
            }

            return loops;
        }

        // ── LineRenderer creation ───────────────────────────────────────

        private void CreateBorderLine(List<Vector3> loop)
        {
            var go = new GameObject("RegionBorder");
            go.transform.SetParent(transform);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = loop.Count;
            lr.SetPositions(loop.ToArray());

            lr.startWidth = borderWidth;
            lr.endWidth = borderWidth;
            lr.startColor = borderColor;
            lr.endColor = borderColor;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 2;

            if (lineMaterial != null)
            {
                lr.material = lineMaterial;
            }
            else
            {
                lr.material = new Material(Shader.Find("Sprites/Default"));
            }
            lr.material.color = borderColor;

            lr.sortingOrder = 5;

            activeLineObjects.Add(go);
        }
    }
}
