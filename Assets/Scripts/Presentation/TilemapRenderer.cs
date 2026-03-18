namespace Presentation.MapGeneration
{
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using BusinessLogic.MapGeneration;
    using ScriptableObjects;

    public class TilemapRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private MapGenerator mapGenerator;

        public void RenderMap()
        {
            if (mapGenerator == null || mapGenerator.BiomeGrid == null)
            {
                Debug.LogWarning("TilemapRenderer: No biome grid to render. Generate the map first.");
                return;
            }

            groundTilemap.ClearAllTiles();

            BiomeConfig[,] biomeGrid = mapGenerator.BiomeGrid;
            int width = mapGenerator.mapWidth;
            int height = mapGenerator.mapHeight;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    BiomeConfig biome = biomeGrid[x, y];
                    if (biome != null && biome.DefaultTile != null)
                    {
                        Vector3Int tilePos = new Vector3Int(x, y, 0);
                        groundTilemap.SetTile(tilePos, biome.DefaultTile);
                    }
                }
            }
        }

        public void SetTileBurning(int x, int y)
        {
            BiomeConfig biome = mapGenerator.GetBiomeAt(x, y);
            if (biome != null && biome.BurningTile != null)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), biome.BurningTile);
            }
        }

        public void SetTileDefault(int x, int y)
        {
            BiomeConfig biome = mapGenerator.GetBiomeAt(x, y);
            if (biome != null && biome.DefaultTile != null)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), biome.DefaultTile);
            }
        }
    }
}
