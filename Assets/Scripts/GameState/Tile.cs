namespace GameState {
    using UnityEngine;

    [System.Serializable]
    public class Tile {
        [SerializeField] private Vector2 coordinates;
        [SerializeField] private bool isOnFire;
        [SerializeField] private float fireIntensity;
        [SerializeField] private float moistureLevel;
    }
}
