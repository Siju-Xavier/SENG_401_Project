namespace GameState {
    using UnityEngine;

    [System.Serializable]
    public class City {
        [SerializeField] private string cityName;
        [SerializeField] private int budget;
        [SerializeField] private int reputation;
        [SerializeField] private bool isOnFire;
    }
}
