namespace Presentation.MapGeneration
{
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using GameState;
    using ScriptableObjects;

    public class BiomeTilemapRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Tilemap groundTilemap;

        private MapData mapData;

        public void RenderMap(MapData data)
        {
            mapData = data;

            if (data == null || data.BiomeGrid == null)
            {
                Debug.LogWarning("BiomeTilemapRenderer: No biome grid to render.");
                return;
            }

            groundTilemap.ClearAllTiles();

            for (int y = 0; y < data.Height; y++)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    BiomeConfig biome = data.BiomeGrid[x, y];
                    if (biome != null && biome.DefaultTile != null)
                    {
                        groundTilemap.SetTile(new Vector3Int(x, y, 0), biome.DefaultTile);
                    }
                }
            }
        }

        public void SetTileBurning(int x, int y)
        {
            BiomeConfig biome = GetBiomeAt(x, y);
            if (biome != null && biome.BurningTile != null)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), biome.BurningTile);
            }
        }

        public void SetTileDefault(int x, int y)
        {
            BiomeConfig biome = GetBiomeAt(x, y);
            if (biome != null && biome.DefaultTile != null)
            {
                groundTilemap.SetTile(new Vector3Int(x, y, 0), biome.DefaultTile);
            }
        }

        private BiomeConfig GetBiomeAt(int x, int y)
        {
            if (mapData?.BiomeGrid == null || x < 0 || x >= mapData.Width || y < 0 || y >= mapData.Height)
                return null;
            return mapData.BiomeGrid[x, y];
        }
    }
}
