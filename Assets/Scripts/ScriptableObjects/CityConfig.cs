namespace ScriptableObjects {
    using UnityEngine;

    [CreateAssetMenu(fileName = "CityConfig", menuName = "Config/City")]
    public class CityConfig : ScriptableObject {
        [Header("Visuals")]
        [SerializeField] private GameObject cityPrefab;
        [SerializeField] private float prefabScale = 1f;

        [Header("Footprint (in tiles)")]
        [Tooltip("How many tiles wide this city occupies")]
        [SerializeField] private int footprintWidth = 3;
        [Tooltip("How many tiles tall this city occupies")]
        [SerializeField] private int footprintHeight = 3;

        [Header("Placement Rules")]
        [Tooltip("Biomes this city can be placed on. Leave empty to allow all.")]
        [SerializeField] private BiomeConfig[] allowedBiomes;

        public GameObject CityPrefab => cityPrefab;
        public float PrefabScale => prefabScale;
        public int FootprintWidth => footprintWidth;
        public int FootprintHeight => footprintHeight;
        public BiomeConfig[] AllowedBiomes => allowedBiomes;
    }
}
