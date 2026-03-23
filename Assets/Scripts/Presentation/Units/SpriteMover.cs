using UnityEngine;

namespace Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 5f;
        [SerializeField] private float stopDistance = 0.1f;

        [Header("Directional Sprites")]
        [SerializeField] private Sprite spriteBottomLeft;
        [SerializeField] private Sprite spriteBottomRight;

        [Header("Animation")]
        [SerializeField] private Sprite[] walkSprites;
        [SerializeField] private float walkFrameRate = 12f;

        private Vector3 _targetPosition;
        private SpriteRenderer _spriteRenderer;
        private bool _isMoving;
        private bool _wasMoving;
        private float _animationTimer;
        private int _currentFrame;

        public System.Action OnArrived;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _targetPosition = transform.position;
        }

        private void Update()
        {
            float distance = Vector3.Distance(transform.position, _targetPosition);

            if (distance > stopDistance)
            {
                _isMoving = true;
                _wasMoving = true;
                MoveTowardsTarget();
                UpdateAnimation();
            }
            else
            {
                _isMoving = false;
                if (_wasMoving)
                {
                    _wasMoving = false;
                    _currentFrame = 0; // Reset to idle frame
                    UpdateSpriteFrame(0);
                    OnArrived?.Invoke();
                }
            }
        }

        private void UpdateAnimation()
        {
            if (walkSprites == null || walkSprites.Length == 0) return;

            _animationTimer += Time.deltaTime;
            float frameDuration = 1f / walkFrameRate;

            if (_animationTimer >= frameDuration)
            {
                _animationTimer -= frameDuration;
                _currentFrame = (_currentFrame + 1) % walkSprites.Length;
                UpdateSpriteFrame(Vector3.Distance(transform.position, _targetPosition) > stopDistance ? 1 : 0);
            }
        }

        private void UpdateSpriteFrame(int moveSign)
        {
            if (walkSprites != null && walkSprites.Length > 0)
            {
                _spriteRenderer.sprite = walkSprites[_currentFrame];
            }
        }

        private void MoveTowardsTarget()
        {
            Vector3 direction = (_targetPosition - transform.position).normalized;

            // Swap sprite horizontally based on direction
            if (Mathf.Abs(direction.x) > 0.01f)
            {
                _spriteRenderer.flipX = direction.x > 0;
            }

            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, speed * Time.deltaTime);
        }

        public void SetTarget(Vector3 newTarget)
        {
            _targetPosition = newTarget;
            _targetPosition.z = transform.position.z;
        }

        public void SetDirectionalSprites(Sprite bottomLeft, Sprite bottomRight, Sprite[] walkAnim = null)
        {
            spriteBottomLeft = bottomLeft;
            spriteBottomRight = bottomRight;
            walkSprites = walkAnim;
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public bool IsMoving => _isMoving;
    }
}
