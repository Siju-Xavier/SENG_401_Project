using UnityEngine;

namespace Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 5f;
        [SerializeField] private float stopDistance = 0.1f;
        [SerializeField] private bool autoFlip = true;

        private Vector3 _targetPosition;
        private SpriteRenderer _spriteRenderer;
        private bool _isMoving;

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
                MoveTowardsTarget();
            }
            else
            {
                _isMoving = false;
            }
        }

        private void MoveTowardsTarget()
        {
            Vector3 direction = (_targetPosition - transform.position).normalized;
            
            // Handle flipping
            if (autoFlip && Mathf.Abs(direction.x) > 0.01f)
            {
                // If moving right (direction.x > 0), set flipX = false (assuming original points right)
                // If moving left (direction.x < 0), set flipX = true (points left)
                // Wait, your firefighter sheet has him facing right.
                // So move right -> flipX = false.
                // Move left -> flipX = true.
                _spriteRenderer.flipX = direction.x < 0;
            }

            transform.position = Vector3.MoveTowards(transform.position, _targetPosition, speed * Time.deltaTime);
        }

        public void SetTarget(Vector3 newTarget)
        {
            _targetPosition = newTarget;
            // Keep the same Z to avoid clipping issues
            _targetPosition.z = transform.position.z;
        }

        public bool IsMoving => _isMoving;
    }
}
