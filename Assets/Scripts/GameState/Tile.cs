namespace GameState {
    using UnityEngine;
    using ScriptableObjects;

    [System.Serializable]
    public class Tile {
        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private bool isOnFire;
        [SerializeField] private float fireIntensity;
        [SerializeField] private float moistureLevel;
        [SerializeField] private bool isCityFootprint;
        [SerializeField] private bool isBurnt;

        private Region region;
        private BiomeConfig biome;

        public int X => x;
        public int Y => y;
        public bool IsOnFire { get => isOnFire; set => isOnFire = value; }
        public float FireIntensity { get => fireIntensity; set => fireIntensity = value; }
        public float MoistureLevel { get => moistureLevel; set => moistureLevel = value; }
        public bool IsCityFootprint { get => isCityFootprint; set => isCityFootprint = value; }
        public bool IsBurnt { get => isBurnt; set => isBurnt = value; }
        public Region Region { get => region; set => region = value; }
        public BiomeConfig Biome { get => biome; set => biome = value; }

        public Tile(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }
}
