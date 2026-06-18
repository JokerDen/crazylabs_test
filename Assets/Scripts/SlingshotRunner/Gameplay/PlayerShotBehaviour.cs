using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerShotBehaviour : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody body;

        [Header("Shot")]
        [SerializeField] private float minShotSpeed = 7f;
        [SerializeField] private float maxShotSpeed = 30f;
        [SerializeField] private float shotPowerSpeedPerLevel = 2.25f;
        [SerializeField] private float upwardShotRatio = 0.16f;
        [SerializeField] private float aimSideInfluence = 0.45f;
        [SerializeField] private float maxPreviewBackDistance = 3.6f;
        [SerializeField] private float maxPreviewSideDistance = 1.3f;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        public void Preview(Vector3 spawnPosition, Quaternion spawnRotation, Vector2 pull)
        {
            Vector2 clampedPull = Vector2.ClampMagnitude(pull, 1f);
            float backDistance = clampedPull.magnitude * maxPreviewBackDistance;
            float sideDistance = Mathf.Clamp(clampedPull.x, -1f, 1f) * maxPreviewSideDistance;

            transform.SetPositionAndRotation(
                spawnPosition + spawnRotation * new Vector3(sideDistance, 0f, -backDistance),
                spawnRotation);
        }

        public bool Launch(Quaternion spawnRotation, float power, Vector2 pull, int shotPowerLevel)
        {
            CacheReferences();
            if (body == null)
            {
                return false;
            }

            float clampedPower = Mathf.Clamp01(power);
            Vector2 clampedPull = Vector2.ClampMagnitude(pull, 1f);
            Vector3 direction = GetShotDirection(spawnRotation, clampedPull);
            float upgradedMaxSpeed = maxShotSpeed + shotPowerLevel * shotPowerSpeedPerLevel;
            float speed = Mathf.Lerp(minShotSpeed, upgradedMaxSpeed, clampedPower);

            body.AddForce(direction * speed, ForceMode.VelocityChange);
            return true;
        }

        private void CacheReferences()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }
        }

        private Vector3 GetShotDirection(Quaternion spawnRotation, Vector2 pull)
        {
            Vector3 localDirection = new Vector3(-pull.x * aimSideInfluence, upwardShotRatio, 1f);
            return (spawnRotation * localDirection).normalized;
        }
    }
}
