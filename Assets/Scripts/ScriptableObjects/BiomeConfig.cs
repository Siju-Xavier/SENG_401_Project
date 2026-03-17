namespace ScriptableObjects {
    using UnityEngine;

    [CreateAssetMenu(fileName = "BiomeConfig", menuName = "Config/Biome")]
    public class BiomeConfig : ScriptableObject {
        [SerializeField] private float spreadMultiplier;
        [SerializeField] private float baseMoisture;
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite burningSprite;
    }
}
