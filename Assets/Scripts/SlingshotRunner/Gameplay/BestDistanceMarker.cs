using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class BestDistanceMarker : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float zOffset;

        private void Reset()
        {
            target = transform;
        }

        private void Awake()
        {
            if (target == null)
            {
                target = transform;
            }
        }

        public void SetDistance(float distanceMeters)
        {
            if (target == null)
            {
                target = transform;
            }

            Vector3 position = target.position;
            position.z = zOffset + Mathf.Max(0f, distanceMeters);
            target.position = position;
        }
    }
}
