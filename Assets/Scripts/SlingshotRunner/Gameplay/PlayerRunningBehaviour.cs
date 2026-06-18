using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerRunningBehaviour : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody body;
        [SerializeField] private CapsuleCollider capsule;

        [Header("Running")]
        [SerializeField] private float lateralAcceleration = 24f;
        [SerializeField] private float maxLateralSpeed = 4.8f;
        [SerializeField] private float laneLimit = 4f;
        [SerializeField] private float baseLinearDamping = 0.45f;
        [SerializeField] private float slideDampingReductionPerLevel = 0.055f;
        [SerializeField] private float minLinearDamping = 0.05f;
        [SerializeField] private float stopSpeed = 0.32f;
        [SerializeField] private float stopHoldSeconds = 0.9f;
        [SerializeField] private float stopGraceSeconds = 1.2f;

        private float steerInput;
        private float stoppedTimer;
        private float launchedAt;
        private bool isRunning;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void FixedUpdate()
        {
            if (!isRunning || body == null || body.isKinematic)
            {
                return;
            }

            ApplySteeringDirection(steerInput, lateralAcceleration);
            ApplyLaneCorrection();
            UpdateStoppedTimer();
        }

        public void ConfigureForRest()
        {
            CacheReferences();
            isRunning = false;
            steerInput = 0f;
            stoppedTimer = 0f;
            launchedAt = 0f;

            if (body == null)
            {
                return;
            }

            body.isKinematic = true;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            ApplyDefaultBodySettings();
        }

        public bool PrepareForLaunch(int slideAbilityLevel)
        {
            CacheReferences();
            if (body == null)
            {
                return false;
            }

            isRunning = false;
            steerInput = 0f;
            stoppedTimer = 0f;

            body.isKinematic = false;
            body.useGravity = true;
            body.linearDamping = Mathf.Max(minLinearDamping, baseLinearDamping - slideAbilityLevel * slideDampingReductionPerLevel);
            body.angularDamping = 3f;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            ApplyDefaultBodySettings();
            ApplySlideFriction(slideAbilityLevel);
            return true;
        }

        public void BeginRun()
        {
            launchedAt = Time.time;
            stoppedTimer = 0f;
            isRunning = true;
        }

        public void SetSteerInput(float value)
        {
            steerInput = Mathf.Clamp(value, -1f, 1f);
        }

        public bool HasStopped()
        {
            return isRunning && Time.time - launchedAt >= stopGraceSeconds && stoppedTimer >= stopHoldSeconds;
        }

        public void StopPhysics()
        {
            if (body == null)
            {
                return;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            isRunning = false;
            steerInput = 0f;
        }

        public void ApplyObstacleImpact(ObstacleType obstacleType, Vector3 obstaclePosition)
        {
            if (!isRunning || body == null || body.isKinematic)
            {
                return;
            }

            if (obstacleType == ObstacleType.Slowdown)
            {
                body.linearVelocity *= 0.46f;
                body.AddForce(Vector3.up * 1.5f, ForceMode.VelocityChange);
                return;
            }

            Vector3 away = transform.position - obstaclePosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
            {
                away = Vector3.right;
            }

            Vector3 impulse = away.normalized * 3.8f + Vector3.up * 2.4f + transform.forward * 1.4f;
            body.AddForce(impulse, ForceMode.VelocityChange);
        }

        private void CacheReferences()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (capsule == null)
            {
                capsule = GetComponent<CapsuleCollider>();
            }
        }

        private void ApplyDefaultBodySettings()
        {
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void ApplySteeringDirection(float input, float changeRate)
        {
            float steering = Mathf.Clamp(input, -1f, 1f);
            if (Mathf.Abs(steering) <= 0.01f)
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;
            if (horizontalSpeed <= 0.01f)
            {
                return;
            }

            float targetX = Mathf.Clamp(steering * maxLateralSpeed, -horizontalSpeed, horizontalSpeed);
            float targetZSign = velocity.z < -0.01f ? -1f : 1f;
            float targetZ = Mathf.Sqrt(Mathf.Max(0f, horizontalSpeed * horizontalSpeed - targetX * targetX)) * targetZSign;
            Vector3 targetHorizontalVelocity = new Vector3(targetX, 0f, targetZ);
            Vector3 steeredHorizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetHorizontalVelocity,
                Mathf.Max(0f, changeRate) * Time.fixedDeltaTime);

            if (steeredHorizontalVelocity.sqrMagnitude > 0.0001f)
            {
                steeredHorizontalVelocity = steeredHorizontalVelocity.normalized * horizontalSpeed;
            }

            velocity.x = steeredHorizontalVelocity.x;
            velocity.z = steeredHorizontalVelocity.z;
            body.linearVelocity = velocity;
        }

        private void ApplyLaneCorrection()
        {
            float x = transform.position.x;
            if (Mathf.Abs(x) <= laneLimit)
            {
                return;
            }

            float directionToCenter = -Mathf.Sign(x);
            ApplySteeringDirection(directionToCenter, lateralAcceleration * 1.4f);
        }

        private void UpdateStoppedTimer()
        {
            float horizontalSpeed = new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;
            if (horizontalSpeed <= stopSpeed)
            {
                stoppedTimer += Time.fixedDeltaTime;
            }
            else
            {
                stoppedTimer = 0f;
            }
        }

        private void ApplySlideFriction(int slideAbilityLevel)
        {
            if (capsule == null)
            {
                return;
            }

            PhysicsMaterial material = capsule.material;
            float friction = Mathf.Clamp01(0.62f - slideAbilityLevel * 0.075f);
            material.dynamicFriction = friction;
            material.staticFriction = friction;
            material.bounciness = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
        }
    }
}
