namespace GameState {
    using UnityEngine;

    public enum UnitState { Idle, EnRoute, Extinguishing, Returning }

    [System.Serializable]
    public class ActiveResponseUnit {
        [SerializeField] private string cityName;
        [SerializeField] private string unitType;
        [SerializeField] private Vector2 currentLocation;
        [SerializeField] private int currentWater;
        [SerializeField] private UnitState state;

        public string CityName        { get => cityName;        set => cityName = value; }
        public string UnitType        { get => unitType;        set => unitType = value; }
        public Vector2 CurrentLocation { get => currentLocation; set => currentLocation = value; }
        public int CurrentWater       { get => currentWater;    set => currentWater = value; }
        public UnitState State        { get => state;           set => state = value; }

        public ActiveResponseUnit() { }

        public ActiveResponseUnit(string cityName, string unitType, Vector2 location, int water, UnitState state) {
            this.cityName = cityName;
            this.unitType = unitType;
            this.currentLocation = location;
            this.currentWater = water;
            this.state = state;
        }
    }
}
