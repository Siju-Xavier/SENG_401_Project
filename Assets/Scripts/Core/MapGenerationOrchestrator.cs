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
        [SerializeField] private DecorationPlacer decorationPlacer;
        [SerializeField] private CityPlacer cityPlacer;

        [Header("Game State")]
        [SerializeField] private GridSystem gridSystem;
        [SerializeField] private CityGenerator cityGenerator;

        [Header("Seed")]
        [SerializeField] private int seed;

        [Header("Debug Toggles")]
        public bool enableNoiseAndBiomes = true;
        public bool enableBiomeTilemap = true;
        public bool enableVegetation = true;
        public bool enableDecorations = true;
        public bool enableGridSystem = true;
        public bool enableCities = true;
        public bool enableClouds = true;

        public bool autoUpdate;

        private MapData mapData;

        public GridSystem GridSystem => gridSystem;
        public MapData MapData => mapData;
        public int Seed => seed;

        public void SetSeed(int s) { seed = s; }

        private void Start()
        {
            // Apply player-selected settings from the Start Panel (if not loading a save)
            if (!Presentation.MainMenuManager.ShouldLoadSave && mapGenerator != null)
            {
                mapGenerator.mapWidth = Presentation.StartPanelController.SelectedMapWidth;
                mapGenerator.mapHeight = Presentation.StartPanelController.SelectedMapHeight;
                seed = Presentation.StartPanelController.SelectedSeed;

                if (cityGenerator != null)
                    cityGenerator.NumberOfCities = Presentation.StartPanelController.SelectedCityCount;
            }

            GenerateWorld();
        }

        public void ClearGeneratedContent()
        {
            if (biomeTilemapRenderer != null)
                biomeTilemapRenderer.ClearMap();

            if (treePlacer != null)
                treePlacer.ClearTrees();

            if (decorationPlacer != null)
                decorationPlacer.ClearDecorations();

            if (cityPlacer != null)
                cityPlacer.ClearCities();

            mapData = null;
        }

        public void GenerateWorld()
        {
            Debug.Log("MapGenerationOrchestrator: START");

            // 1. Generate noise and biome data
            if (enableNoiseAndBiomes)
            {
                mapData = mapGenerator.GenerateNoiseAndBiomes(seed);
                Debug.Log("  [1] Noise & Biomes: DONE");
            }

            if (mapData == null) { Debug.LogWarning("MapGenerationOrchestrator: No map data (noise disabled?)"); return; }

            // 2. Render biome tilemap
            if (enableBiomeTilemap && biomeTilemapRenderer != null)
            {
                biomeTilemapRenderer.RenderMap(mapData);
                Debug.Log("  [2] Biome Tilemap: DONE");
            }

            // 3. Place vegetation
            if (enableVegetation && treePlacer != null)
            {
                treePlacer.PlaceTrees(mapData);
                Debug.Log("  [3] Vegetation: DONE");
            }

            // 3b. Place decorations (mountain rocks, etc.)
            if (enableDecorations && decorationPlacer != null)
            {
                decorationPlacer.PlaceDecorations(mapData);
                Debug.Log("  [3b] Decorations: DONE");
            }

            // 4. Initialize grid system and assign biomes to tiles
            if (enableGridSystem)
            {
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
                Debug.Log("  [4] Grid System: DONE");
            }

            // 5. Generate and place cities
            if (enableCities && cityGenerator != null && cityGenerator.EnableCityPlacement)
            {
                cityGenerator.GenerateCities(gridSystem, mapData);

                if (cityPlacer != null)
                    cityPlacer.PlaceCities(gridSystem);
                Debug.Log("  [5] Cities: DONE");
            }

            Debug.Log("MapGenerationOrchestrator: DONE");
        }
    }
}
