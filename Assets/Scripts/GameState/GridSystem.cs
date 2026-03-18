namespace GameState {
    using System.Collections.Generic;
    using UnityEngine;

    public class GridSystem : MonoBehaviour {
        [SerializeField] private int width;
        [SerializeField] private int height;
        private Tile[,] gridArray;
        private List<Region> regions = new List<Region>();

        public int Width => width;
        public int Height => height;
        public List<Region> Regions => regions;

        public void Initialize(int w, int h) {
            width = w;
            height = h;
            regions.Clear();
            gridArray = new Tile[width, height];

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    gridArray[x, y] = new Tile(x, y);
                }
            }
        }

        public Tile GetTileAt(int x, int y) {
            if (gridArray == null || x < 0 || x >= width || y < 0 || y >= height)
                return null;
            return gridArray[x, y];
        }

        public List<Tile> GetNeighbours(Tile tile) {
            var neighbours = new List<Tile>();
            int[] dx = { -1, 0, 1, 0 };
            int[] dy = { 0, 1, 0, -1 };

            for (int i = 0; i < 4; i++) {
                var n = GetTileAt(tile.X + dx[i], tile.Y + dy[i]);
                if (n != null) neighbours.Add(n);
            }
            return neighbours;
        }

        public void AddRegion(Region region) {
            regions.Add(region);
        }

        public Region GetRegionAt(int x, int y) {
            var tile = GetTileAt(x, y);
            return tile?.Region;
        }
    }
}
