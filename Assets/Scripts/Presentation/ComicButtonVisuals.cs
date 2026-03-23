using UnityEngine;
using UnityEngine.EventSystems;

namespace Presentation
{
    public class ComicButtonVisuals : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("The front face of the button that moves when pressed.")]
        public RectTransform frontFace;
        
        [Tooltip("How far the button presses down/left.")]
        public Vector2 pressOffset = new Vector2(-10f, -10f);

        private Vector2 originalPosition;

        private void Start()
        {
            if (frontFace != null)
            {
                originalPosition = frontFace.anchoredPosition;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (frontFace != null)
            {
                frontFace.anchoredPosition = originalPosition + pressOffset;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (frontFace != null)
            {
                frontFace.anchoredPosition = originalPosition;
            }
        }
    }
}
