namespace Core
{
    using UnityEngine;
    using BusinessLogic;
    using BusinessLogic.MapGeneration;
    using GameState;
    using Presentation.MapGeneration;

    public class MapGenerationOrchestrator : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private MapGenerator mapGenerator;

        [Header("Rendering")]
        [SerializeField] private BiomeTilemapRenderer biomeTilemapRenderer;
        [SerializeField] private TreePlacer treePlacer;
        [SerializeField] private CityPlacer cityPlacer;

        [Header("Game State")]
        [SerializeField] private GridSystem gridSystem;
        [SerializeField] private CityGenerator cityGenerator;

        public bool autoUpdate;

        private MapData mapData;

        public GridSystem GridSystem => gridSystem;
        public MapData MapData => mapData;

        private void Start()
        {
            GenerateWorld();
        }

        public void GenerateWorld()
        {
            Debug.Log("MapGenerationOrchestrator: START");

            // 1. Generate noise and biome data
            mapData = mapGenerator.GenerateNoiseAndBiomes();

            // 2. Render biome tilemap
            if (biomeTilemapRenderer != null)
                biomeTilemapRenderer.RenderMap(mapData);

            // 3. Place vegetation
            if (treePlacer != null)
                treePlacer.PlaceTrees(mapData);

            // 4. Initialize grid system and assign biomes to tiles
            if (gridSystem == null)
                gridSystem = gameObject.AddComponent<GridSystem>();

            gridSystem.Initialize(mapData.Width, mapData.Height);

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    Tile tile = gridSystem.GetTileAt(x, y);
                    if (tile != null)
                    {
                        var biome = mapData.BiomeGrid[x, y];
                        tile.Biome = biome;
                        tile.MoistureLevel = biome != null ? biome.BaseMoisture : 0f;
                    }
                }
            }

            // 5. Generate and place cities
            if (cityGenerator != null && cityGenerator.EnableCityPlacement)
            {
                cityGenerator.GenerateCities(gridSystem, mapData);

                if (cityPlacer != null)
                    cityPlacer.PlaceCities(gridSystem);
            }

            Debug.Log("MapGenerationOrchestrator: DONE");
        }
    }
}
