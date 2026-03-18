namespace ScriptableObjects {
    using UnityEngine;
    using UnityEngine.Tilemaps;

    [CreateAssetMenu(fileName = "BiomeConfig", menuName = "Config/Biome")]
    public class BiomeConfig : ScriptableObject {
        [SerializeField] private float spreadMultiplier;
        [SerializeField] private float baseMoisture;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite burningSprite;

        [Header("Tilemap")]
        [SerializeField] private TileBase defaultTile;
        [SerializeField] private TileBase burningTile;

        [Header("Perlin Noise Threshold")]
        [Tooltip("Max Perlin height value for this biome (0-1)")]
        [SerializeField] private float maxHeight;

        public float SpreadMultiplier => spreadMultiplier;
        public float BaseMoisture => baseMoisture;
        public Sprite DefaultSprite => defaultSprite;
        public Sprite BurningSprite => burningSprite;
        public TileBase DefaultTile => defaultTile;
        public TileBase BurningTile => burningTile;
        public float MaxHeight => maxHeight;
    }
}
