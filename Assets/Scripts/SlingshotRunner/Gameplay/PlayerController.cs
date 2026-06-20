using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerSpawnBehaviour))]
    [RequireComponent(typeof(PlayerShotBehaviour))]
    [RequireComponent(typeof(PlayerRunningBehaviour))]
    [RequireComponent(typeof(PlayerCharacterView))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerSpawnBehaviour spawnBehaviour;
        [SerializeField] private PlayerShotBehaviour shotBehaviour;
        [SerializeField] private PlayerRunningBehaviour runningBehaviour;
        [SerializeField] private PlayerCharacterView characterView;

        public Transform BodyTransform => transform;
        public float Distance => spawnBehaviour != null ? spawnBehaviour.Distance : 0f;
        public float Speed => runningBehaviour != null ? runningBehaviour.Speed : 0f;

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
            characterView?.ApplyCurrentStateFacingDirection();
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

        public void StartRun(float power, Vector2 pull, int shotPowerLevel, int slideUpgradeCount)
        {
            if (spawnBehaviour == null || shotBehaviour == null || runningBehaviour == null)
            {
                return;
            }

            if (!runningBehaviour.PrepareForLaunch(slideUpgradeCount))
            {
                return;
            }

            if (!shotBehaviour.Launch(spawnBehaviour.SpawnRotation, power, pull, shotPowerLevel, out Vector3 launchVelocity))
            {
                runningBehaviour.ConfigureForRest();
                return;
            }

            runningBehaviour.BeginRun(launchVelocity);
            characterView?.SetState(GameState.Running, true);
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
            characterView?.ResetFacingDirection();
        }

        public void ApplyObstacleImpact(ObstacleType obstacleType, Vector3 obstaclePosition)
        {
            runningBehaviour?.ApplyObstacleImpact(obstacleType, obstaclePosition);
        }

        public void SetViewState(GameState state)
        {
            CacheReferences();
            characterView?.SetState(state);
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

            if (characterView == null)
            {
                characterView = GetComponent<PlayerCharacterView>();
            }
        }
    }
}
