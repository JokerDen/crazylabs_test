using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private float lateralAcceleration = 42f;
        [SerializeField] private float lateralReleaseDeceleration = 28f;
        [SerializeField] private float maxSteeringYawDegrees = 45f;
        [SerializeField] private float stopSpeed = 0.32f;
        [SerializeField] private float stopHoldSeconds = 0.9f;
        [SerializeField] private float stopGraceSeconds = 1.2f;

        [Header("Slide Friction")]
        [SerializeField] private PhysicsMaterial slidePhysicsMaterial;
        [FormerlySerializedAs("baseZFriction")]
        [FormerlySerializedAs("baseLinearDamping")]
        [SerializeField] private float baseSlideFriction = 0.3f;
        [FormerlySerializedAs("zFrictionReductionPerLevel")]
        [FormerlySerializedAs("slideDampingReductionPerLevel")]
        [SerializeField] private float slideFrictionReductionPerLevel = 0.025f;
        [SerializeField] private float distanceFrictionPerMeter = 0.002f;
        [FormerlySerializedAs("minZFriction")]
        [FormerlySerializedAs("minLinearDamping")]
        [SerializeField] private float minSlideFriction = 0.015f;
        [SerializeField] private float maxSlideFriction = 4f;

        [Header("Hidden Stop")]
        [SerializeField] private float hiddenStopFrictionMultiplier = 0.75f;

        private float steerInput;
        private float stoppedTimer;
        private float launchedAt;
        private float launchZ;
        private float currentSlideFriction;
        private int currentSlideUpgradeCount;
        private PhysicsMaterial runtimeSlideMaterial;
        private bool isRunning;
        private Vector3 launchHorizontalDirection = Vector3.forward;

        public float Speed
        {
            get
            {
                if (body == null || body.isKinematic)
                {
                    return 0f;
                }

                return new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;
            }
        }

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

            UpdateSlideFriction();
            ApplyHiddenStopFriction();
            ApplyVelocityRotationControl(
                steerInput,
                Mathf.Abs(steerInput) > 0.01f ? lateralAcceleration : lateralReleaseDeceleration);
            UpdateStoppedTimer();
        }

        public void ConfigureForRest()
        {
            CacheReferences();
            isRunning = false;
            steerInput = 0f;
            stoppedTimer = 0f;
            launchedAt = 0f;
            launchZ = transform.position.z;
            launchHorizontalDirection = FlattenDirection(transform.forward);
            currentSlideFriction = 0f;
            currentSlideUpgradeCount = 0;

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

        public bool PrepareForLaunch(int slideUpgradeCount)
        {
            CacheReferences();
            if (body == null)
            {
                return false;
            }

            isRunning = false;
            steerInput = 0f;
            stoppedTimer = 0f;
            launchZ = transform.position.z;
            currentSlideUpgradeCount = Mathf.Max(0, slideUpgradeCount);
            currentSlideFriction = CalculateSlideFriction(currentSlideUpgradeCount, 0f);

            body.isKinematic = false;
            body.useGravity = true;
            body.angularDamping = 3f;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            ApplyDefaultBodySettings();
            ApplySlideMaterialFriction(currentSlideFriction);
            return true;
        }

        public void BeginRun(Vector3 launchVelocity)
        {
            launchHorizontalDirection = FlattenDirection(launchVelocity);
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
            body.linearDamping = 0f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void ApplyVelocityRotationControl(float input, float changeRate)
        {
            float steering = Mathf.Clamp(input, -1f, 1f);
            Vector3 velocity = body.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;
            if (horizontalSpeed <= 0.01f)
            {
                return;
            }

            Vector3 baseDirection = launchHorizontalDirection.sqrMagnitude > 0.0001f
                ? launchHorizontalDirection.normalized
                : horizontalVelocity / horizontalSpeed;
            Vector3 targetDirection = Quaternion.AngleAxis(steering * maxSteeringYawDegrees, Vector3.up) * baseDirection;
            Vector3 currentDirection = horizontalVelocity / horizontalSpeed;
            float maxRadiansDelta = Mathf.Max(0f, changeRate) * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Vector3 rotatedHorizontalVelocity = Vector3.RotateTowards(
                currentDirection,
                targetDirection,
                maxRadiansDelta,
                0f) * horizontalSpeed;

            velocity.x = rotatedHorizontalVelocity.x;
            velocity.z = rotatedHorizontalVelocity.z;
            body.linearVelocity = velocity;
        }

        private static Vector3 FlattenDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
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

        private void UpdateSlideFriction()
        {
            currentSlideFriction = CalculateSlideFriction(currentSlideUpgradeCount, GetZDistanceFromLaunch());
            ApplySlideMaterialFriction(currentSlideFriction);
        }

        private void ApplyHiddenStopFriction()
        {
            float stopFriction = currentSlideFriction * hiddenStopFrictionMultiplier;
            if (stopFriction <= 0f)
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            if (Mathf.Abs(velocity.z) <= 0.001f)
            {
                return;
            }

            float zDamping = 1f - Mathf.Exp(-stopFriction * Time.fixedDeltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0f, Mathf.Abs(velocity.z) * zDamping);
            body.linearVelocity = velocity;
        }

        private float CalculateSlideFriction(int slideUpgradeCount, float distanceMeters)
        {
            float upgradeReduction = Mathf.Max(0, slideUpgradeCount) * slideFrictionReductionPerLevel;
            float distanceFriction = Mathf.Max(0f, distanceMeters) * distanceFrictionPerMeter;
            float friction = baseSlideFriction - upgradeReduction + distanceFriction;
            return Mathf.Clamp(friction, minSlideFriction, maxSlideFriction);
        }

        private float GetZDistanceFromLaunch()
        {
            return Mathf.Max(0f, transform.position.z - launchZ);
        }

        private void ApplySlideMaterialFriction(float friction)
        {
            if (capsule == null)
            {
                return;
            }

            EnsureRuntimeSlideMaterial();
            if (runtimeSlideMaterial == null)
            {
                return;
            }

            float clampedFriction = Mathf.Max(0f, friction);
            runtimeSlideMaterial.dynamicFriction = clampedFriction;
            runtimeSlideMaterial.staticFriction = clampedFriction;
            runtimeSlideMaterial.bounciness = 0f;
            runtimeSlideMaterial.frictionCombine = PhysicsMaterialCombine.Average;
            runtimeSlideMaterial.bounceCombine = PhysicsMaterialCombine.Minimum;
            capsule.material = runtimeSlideMaterial;
        }

        private void EnsureRuntimeSlideMaterial()
        {
            if (runtimeSlideMaterial != null)
            {
                return;
            }

            runtimeSlideMaterial = slidePhysicsMaterial != null
                ? Instantiate(slidePhysicsMaterial)
                : new PhysicsMaterial("Player Slide Physics Runtime");
            runtimeSlideMaterial.name = "Player Slide Physics Runtime";
        }

        private void OnValidate()
        {
            lateralAcceleration = Mathf.Max(0f, lateralAcceleration);
            lateralReleaseDeceleration = Mathf.Max(0f, lateralReleaseDeceleration);
            maxSteeringYawDegrees = Mathf.Max(0f, maxSteeringYawDegrees);
            stopSpeed = Mathf.Max(0f, stopSpeed);
            stopHoldSeconds = Mathf.Max(0f, stopHoldSeconds);
            stopGraceSeconds = Mathf.Max(0f, stopGraceSeconds);
            baseSlideFriction = Mathf.Max(0f, baseSlideFriction);
            slideFrictionReductionPerLevel = Mathf.Max(0f, slideFrictionReductionPerLevel);
            distanceFrictionPerMeter = Mathf.Max(0f, distanceFrictionPerMeter);
            minSlideFriction = Mathf.Max(0f, minSlideFriction);
            maxSlideFriction = Mathf.Max(minSlideFriction, maxSlideFriction);
            hiddenStopFrictionMultiplier = Mathf.Max(0f, hiddenStopFrictionMultiplier);
        }
    }
}
