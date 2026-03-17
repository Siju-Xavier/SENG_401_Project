namespace GameState {
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class Region {
        [SerializeField] private string regionName;
        [SerializeField] private List<City> cities;

        public List<Tile> GetTiles() {
            return null;
        }
    }
}
