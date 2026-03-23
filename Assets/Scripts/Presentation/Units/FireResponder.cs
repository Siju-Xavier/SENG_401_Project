using UnityEngine;
using System.Collections.Generic;

namespace Presentation
{
    [RequireComponent(typeof(SpriteMover))]
    public class FireResponder : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float searchInterval = 1f;
        
        private SpriteMover _mover;
        private float _lastSearchTime;

        private void Awake()
        {
            _mover = GetComponent<SpriteMover>();
        }

        private void Update()
        {
            if (Time.time - _lastSearchTime > searchInterval)
            {
                _lastSearchTime = Time.time;
                FindAndGoToFire();
            }
        }

        private void FindAndGoToFire()
        {
            FireSource[] fires = Object.FindObjectsOfType<FireSource>();
            if (fires.Length == 0) return;

            // Find closest fire
            FireSource closest = null;
            float minDistance = float.MaxValue;

            foreach (var fire in fires)
            {
                float dist = Vector3.Distance(transform.position, fire.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = fire;
                }
            }

            if (closest != null)
            {
                _mover.SetTarget(closest.transform.position);
            }
        }
    }
}
