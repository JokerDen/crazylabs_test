using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Obstacle : MonoBehaviour
    {
        [SerializeField] private ObstacleType obstacleType;
        [SerializeField] private float spinSpeed;

        private bool hasBeenHit;

        private void Awake()
        {
            Collider hitArea = GetComponent<Collider>();
            hitArea.isTrigger = true;
        }

        private void Update()
        {
            if (Mathf.Abs(spinSpeed) > 0.01f)
            {
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasBeenHit)
            {
                return;
            }

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            hasBeenHit = true;
            player.ApplyObstacleImpact(obstacleType, transform.position);
        }

        public void ResetObstacle()
        {
            hasBeenHit = false;
        }
    }
}
