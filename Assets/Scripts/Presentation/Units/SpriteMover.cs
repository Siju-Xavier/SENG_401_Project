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

        private Vector3 _targetPosition;
        private SpriteRenderer _spriteRenderer;
        private bool _isMoving;
        private bool _wasMoving;

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
            }
            else
            {
                _isMoving = false;
                if (_wasMoving)
                {
                    _wasMoving = false;
                    OnArrived?.Invoke();
                }
            }
        }

        private void MoveTowardsTarget()
        {
            Vector3 direction = (_targetPosition - transform.position).normalized;

            // Swap sprite based on horizontal movement direction
            if (Mathf.Abs(direction.x) > 0.01f)
            {
                if (spriteBottomLeft != null && spriteBottomRight != null)
                {
                    _spriteRenderer.sprite = direction.x < 0 ? spriteBottomLeft : spriteBottomRight;
                }
                else
                {
                    // Fallback to flipX if no directional sprites assigned
                    _spriteRenderer.flipX = direction.x < 0;
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, speed * Time.deltaTime);
        }

        public void SetTarget(Vector3 newTarget)
        {
            _targetPosition = newTarget;
            _targetPosition.z = transform.position.z;
        }

        public void SetDirectionalSprites(Sprite bottomLeft, Sprite bottomRight)
        {
            spriteBottomLeft = bottomLeft;
            spriteBottomRight = bottomRight;
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public bool IsMoving => _isMoving;
    }
}
