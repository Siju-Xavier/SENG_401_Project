namespace GameState {
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class Region {
        [SerializeField] private string regionName;
        [SerializeField] private City city;
        private List<Tile> tiles = new List<Tile>();

        public string RegionName { get => regionName; set => regionName = value; }
        public City City { get => city; set => city = value; }
        public List<Tile> Tiles => tiles;

        public Region(string name, City city) {
            regionName = name;
            this.city = city;
        }

        public void AddTile(Tile tile) {
            tiles.Add(tile);
            tile.Region = this;
        }

        public List<Tile> GetTiles() {
            return tiles;
        }
    }
}
