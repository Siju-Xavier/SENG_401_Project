namespace GameState {
    using UnityEngine;

    public enum UnitState { Idle, EnRoute, Extinguishing, Returning }

    [System.Serializable]
    public class ActiveResponseUnit {
        [SerializeField] private Vector2 currentLocation;
        [SerializeField] private int currentWater;
        [SerializeField] private UnitState state;
    }
}
