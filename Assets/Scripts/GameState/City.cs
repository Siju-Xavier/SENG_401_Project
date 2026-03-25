namespace GameState {
    using UnityEngine;

    [System.Serializable]
    public class City {
        [SerializeField] private string cityName;
        [SerializeField] private int budget;
        [SerializeField] private bool isOnFire;
        [SerializeField] private int tileX;
        [SerializeField] private int tileY;

        public string CityName { get => cityName; set => cityName = value; }
        public int Budget { get => budget; set => budget = value; }
        public bool IsOnFire { get => isOnFire; set => isOnFire = value; }
        public int TileX => tileX;
        public int TileY => tileY;

        public City(string name, int x, int y, int initialBudget = 1000) {
            cityName = name;
            tileX = x;
            tileY = y;
            budget = initialBudget;
        }
    }
}
