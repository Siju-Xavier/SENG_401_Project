using UnityEngine;

namespace Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteMover : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float stopDistance = 0.1f;

        private Sprite _standTopLeft, _standTopRight, _standBottomLeft, _standBottomRight;
        private Sprite _runTopLeft, _runTopRight, _runBottomLeft, _runBottomRight;

        private Vector3 _targetPosition;
        private Vector3 _lastDirection;
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

                _lastDirection = (_targetPosition - transform.position).normalized;
                _spriteRenderer.sprite = PickSprite(_lastDirection, running: true);
                transform.position = Vector3.MoveTowards(transform.position, _targetPosition, speed * Time.deltaTime);
            }
            else
            {
                _isMoving = false;
                if (_wasMoving)
                {
                    _wasMoving = false;
                    // Switch to standing sprite facing the last movement direction
                    _spriteRenderer.sprite = PickSprite(_lastDirection, running: false);
                    OnArrived?.Invoke();
                }
            }
        }

        private Sprite PickSprite(Vector3 direction, bool running)
        {
            // Isometric quadrant: sign of x and y determines direction
            if (running)
            {
                if (direction.x >= 0)
                    return direction.y >= 0 ? _runTopRight : _runBottomRight;
                else
                    return direction.y >= 0 ? _runTopLeft : _runBottomLeft;
            }
            else
            {
                if (direction.x >= 0)
                    return direction.y >= 0 ? _standTopRight : _standBottomRight;
                else
                    return direction.y >= 0 ? _standTopLeft : _standBottomLeft;
            }
        }

        public void SetDirectionalSprites(
            Sprite standTL, Sprite standTR, Sprite standBL, Sprite standBR,
            Sprite runTL, Sprite runTR, Sprite runBL, Sprite runBR)
        {
            _standTopLeft = standTL;
            _standTopRight = standTR;
            _standBottomLeft = standBL;
            _standBottomRight = standBR;
            _runTopLeft = runTL;
            _runTopRight = runTR;
            _runBottomLeft = runBL;
            _runBottomRight = runBR;
        }

        public void SetTarget(Vector3 newTarget)
        {
            _targetPosition = newTarget;
            _targetPosition.z = transform.position.z;
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public bool IsMoving => _isMoving;
    }
}
