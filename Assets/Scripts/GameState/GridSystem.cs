namespace GameState {
    using System.Collections.Generic;
    using UnityEngine;

    public class GridSystem : MonoBehaviour {
        [SerializeField] private int width;
        [SerializeField] private int height;
        private Tile[,] gridArray;

        public Tile GetTileAt(int x, int y) {
            return null;
        }

        public List<Tile> GetNeighbours(Tile tile) {
            return null;
        }
    }
}
