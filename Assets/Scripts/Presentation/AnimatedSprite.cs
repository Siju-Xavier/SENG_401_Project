using UnityEngine;

namespace Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AnimatedSprite : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameRate = 0.12f;
        [SerializeField] private bool loop = true;

        private SpriteRenderer _spriteRenderer;
        private int _currentFrame;
        private float _timer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;

            _timer += Time.deltaTime;
            if (_timer >= frameRate)
            {
                _timer -= frameRate;
                _currentFrame++;

                if (_currentFrame >= frames.Length)
                {
                    if (loop)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = frames.Length - 1;
                        enabled = false;
                    }
                }

                _spriteRenderer.sprite = frames[_currentFrame];
            }
        }

        public void SetFrames(Sprite[] newFrames)
        {
            frames = newFrames;
            _currentFrame = 0;
            _timer = 0;
            
            // Ensure _spriteRenderer is assigned even if called outside of Play mode
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (frames != null && frames.Length > 0 && _spriteRenderer != null)
                _spriteRenderer.sprite = frames[0];
        }
    }
}
