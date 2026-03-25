using System.Collections.Generic;
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
        private Queue<Vector3> _waypoints = new Queue<Vector3>();

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
                // Reached current waypoint — check for more
                if (_waypoints.Count > 0)
                {
                    _targetPosition = _waypoints.Dequeue();
                    _targetPosition.z = transform.position.z;
                }
                else
                {
                    _isMoving = false;
                    if (_wasMoving)
                    {
                        _wasMoving = false;
                        _spriteRenderer.sprite = PickSprite(_lastDirection, running: false);
                        OnArrived?.Invoke();
                    }
                }
            }
        }

        private Sprite PickSprite(Vector3 direction, bool running)
        {
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
            _waypoints.Clear();
            _targetPosition = newTarget;
            _targetPosition.z = transform.position.z;
        }

        public void SetWaypoints(List<Vector3> waypoints)
        {
            _waypoints.Clear();
            if (waypoints == null || waypoints.Count == 0) return;

            // First waypoint becomes immediate target
            _targetPosition = waypoints[0];
            _targetPosition.z = transform.position.z;

            // Rest go into queue
            for (int i = 1; i < waypoints.Count; i++)
            {
                var wp = waypoints[i];
                wp.z = transform.position.z;
                _waypoints.Enqueue(wp);
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public bool IsMoving => _isMoving;
    }
}
