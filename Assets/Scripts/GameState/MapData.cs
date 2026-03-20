namespace GameState {
    using ScriptableObjects;

    public class MapData {
        public int Width;
        public int Height;
        public int Seed;
        public float[,] NoiseMap;
        public BiomeConfig[,] BiomeGrid;
        public BiomeConfig[] Biomes;
    }
}
