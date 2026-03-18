namespace GameState {
    using UnityEngine;

    [System.Serializable]
    public class City {
        [SerializeField] private string cityName;
        [SerializeField] private int budget;
        [SerializeField] private int reputation;
        [SerializeField] private bool isOnFire;
        [SerializeField] private int tileX;
        [SerializeField] private int tileY;

        public string CityName { get => cityName; set => cityName = value; }
        public int Budget { get => budget; set => budget = value; }
        public int Reputation { get => reputation; set => reputation = value; }
        public bool IsOnFire { get => isOnFire; set => isOnFire = value; }
        public int TileX => tileX;
        public int TileY => tileY;

        public City(string name, int x, int y) {
            cityName = name;
            tileX = x;
            tileY = y;
            budget = 1000;
            reputation = 50;
        }
    }
}
