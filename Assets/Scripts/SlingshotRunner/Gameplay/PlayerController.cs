using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerSpawnBehaviour))]
    [RequireComponent(typeof(PlayerShotBehaviour))]
    [RequireComponent(typeof(PlayerRunningBehaviour))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSpawnBehaviour spawnBehaviour;
        [SerializeField] private PlayerShotBehaviour shotBehaviour;
        [SerializeField] private PlayerRunningBehaviour runningBehaviour;

        public Transform BodyTransform => transform;
        public float Distance => spawnBehaviour != null ? spawnBehaviour.Distance : 0f;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            runningBehaviour?.ConfigureForRest();
        }

        public void ResetToSpawn(Vector3 position, Quaternion rotation)
        {
            CacheReferences();

            runningBehaviour?.ConfigureForRest();
            spawnBehaviour?.PlaceAt(position, rotation);
        }

        public void PreviewSlingshot(Vector2 pull)
        {
            if (shotBehaviour == null || spawnBehaviour == null)
            {
                return;
            }

            shotBehaviour.Preview(spawnBehaviour.SpawnPosition, spawnBehaviour.SpawnRotation, pull);
            spawnBehaviour.ResetVisualRotation();
        }

        public void StartRun(float power, Vector2 pull, int shotPowerLevel, int slideAbilityLevel)
        {
            if (spawnBehaviour == null || shotBehaviour == null || runningBehaviour == null)
            {
                return;
            }

            if (!runningBehaviour.PrepareForLaunch(slideAbilityLevel))
            {
                return;
            }

            if (!shotBehaviour.Launch(spawnBehaviour.SpawnRotation, power, pull, shotPowerLevel))
            {
                runningBehaviour.ConfigureForRest();
                return;
            }

            runningBehaviour.BeginRun();
        }

        public void SetSteerInput(float value)
        {
            runningBehaviour?.SetSteerInput(value);
        }

        public bool HasStopped()
        {
            return runningBehaviour != null && runningBehaviour.HasStopped();
        }

        public void StopPhysics()
        {
            runningBehaviour?.StopPhysics();
            spawnBehaviour?.ResetVisualRotation();
        }

        public void ApplyObstacleImpact(ObstacleType obstacleType, Vector3 obstaclePosition)
        {
            runningBehaviour?.ApplyObstacleImpact(obstacleType, obstaclePosition);
        }

        private void CacheReferences()
        {
            if (spawnBehaviour == null)
            {
                spawnBehaviour = GetComponent<PlayerSpawnBehaviour>();
            }

            if (shotBehaviour == null)
            {
                shotBehaviour = GetComponent<PlayerShotBehaviour>();
            }

            if (runningBehaviour == null)
            {
                runningBehaviour = GetComponent<PlayerRunningBehaviour>();
            }
        }
    }
}
