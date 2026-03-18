using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Presentation.MapGeneration
{
    public class MapDisplay : MonoBehaviour
    {
        public Renderer textureRender;

        public void DrawTexture(Texture2D texture)
        {
            // Use sharedMaterial so we can see updates in the Editor without hitting "Play"
            textureRender.sharedMaterial.mainTexture = texture;
            
            // Scale the plane so it matches the dimensions of our map
            // (A default Unity Plane is 10x10 units, so we divide by 10)
            textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
        }
    }
}
