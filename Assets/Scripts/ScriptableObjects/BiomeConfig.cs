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

        [Header("Vegetation")]
        [Tooltip("Sprites to place on tiles of this biome (e.g., trees, shrubs)")]
        [SerializeField] private Sprite[] vegetationSprites;
        [Tooltip("Sprite shown when vegetation is burning (optional — tree is removed if null)")]
        [SerializeField] private Sprite burningVegetationSprite;

        [Header("Decoration")]
        [Tooltip("Sprites for non-vegetation decoration (e.g., mountain rocks). Placed on a separate tilemap layer.")]
        [SerializeField] private Sprite[] decorationSprites;
        [Tooltip("How densely decorations spawn (0 = none, 1 = every tile)")]
        [Range(0f, 1f)]
        [SerializeField] private float decorationDensity = 0f;

        [Header("Structures")]
        [Tooltip("Whether cities/structures can be placed on this biome")]
        [SerializeField] private bool allowStructures = true;

        [Header("Perlin Noise Threshold")]
        [Tooltip("Max Perlin height value for this biome (0-1)")]
        [SerializeField] private float maxHeight;

        public float SpreadMultiplier => spreadMultiplier;
        public float BaseMoisture => baseMoisture;
        public Sprite DefaultSprite => defaultSprite;
        public Sprite BurningSprite => burningSprite;
        public TileBase DefaultTile => defaultTile;
        public TileBase BurningTile => burningTile;
        public Sprite[] VegetationSprites => vegetationSprites;
        public Sprite BurningVegetationSprite => burningVegetationSprite;
        public Sprite[] DecorationSprites => decorationSprites;
        public float DecorationDensity => decorationDensity;
        public bool AllowStructures => allowStructures;
        public float MaxHeight => maxHeight;
    }
}
