using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class PlayerCharacterView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Rigidbody body;
        [SerializeField] private RuntimeAnimatorController animatorController;

        [Header("Animation States")]
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string aimingStateName = "SlingshotIdle";
        [SerializeField] private string flyingStateName = "Flying";
        [SerializeField] private string slidingStateName = "Slide";
        [SerializeField] private string runEndedStateName = "RunEnded";
        [SerializeField] private float crossFadeSeconds = 0.08f;

        [Header("Movement Animation")]
        [SerializeField] private float groundedContactNormalY = 0.45f;
        [SerializeField] private float groundedMemorySeconds = 0.12f;
        [SerializeField] private float maxGroundedVerticalSpeed = 0.65f;
        [SerializeField] private float slideAnimationLockSeconds = 0.42f;

        [Header("Speed Direction")]
        [SerializeField] private float minFacingSpeed = 0.15f;
        [SerializeField] private float rotationDegreesPerSecond = 720f;

        [Header("State Facing")]
        [SerializeField] private float upgradesFacingYawDegrees = 180f;
        [SerializeField] private float aimingFacingYawDegrees = 0f;

        private GameState currentState = GameState.Menu;
        private Quaternion visualSpawnRotation = Quaternion.identity;
        private string currentAnimatorState;
        private bool visualSpawnRotationCached;
        private float groundedUntil;
        private float slideLockedUntil;
        private bool touchedGroundDuringRun;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            CacheVisualSpawnRotation();
            ApplyAnimatorController();
            SetState(currentState, true);
        }

        private void LateUpdate()
        {
            if (currentState == GameState.Running)
            {
                RotateTowardVelocity();
                UpdateRunningAnimation();
            }
        }

        public void SetState(GameState state, bool force = false)
        {
            CacheReferences();
            CacheVisualSpawnRotation();
            ApplyAnimatorController();

            currentState = state;
            if (state != GameState.Running)
            {
                ApplyStateFacingDirection(state);
            }
            else
            {
                groundedUntil = 0f;
                slideLockedUntil = 0f;
                touchedGroundDuringRun = false;
            }

            PlayState(GetAnimatorStateName(state), force);
        }

        public void ApplyCurrentStateFacingDirection()
        {
            ApplyStateFacingDirection(currentState);
        }

        public void ResetFacingDirection()
        {
            ApplyFacingYaw(aimingFacingYawDegrees);
        }

        private void ApplyStateFacingDirection(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                    ApplyFacingYaw(upgradesFacingYawDegrees);
                    return;
                case GameState.Aiming:
                case GameState.RunEnded:
                case GameState.Running:
                default:
                    ApplyFacingYaw(aimingFacingYawDegrees);
                    return;
            }
        }

        private void ApplyFacingYaw(float yawDegrees)
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localRotation = Quaternion.AngleAxis(yawDegrees, Vector3.up) * visualSpawnRotation;
        }

        private void CacheReferences()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
                visualSpawnRotationCached = false;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void CacheVisualSpawnRotation()
        {
            if (visualRoot == null || visualSpawnRotationCached)
            {
                return;
            }

            visualSpawnRotation = visualRoot.localRotation;
            visualSpawnRotationCached = true;
        }

        private void ApplyAnimatorController()
        {
            if (animator == null)
            {
                return;
            }

            EnsureAnimationEventReceiver();
            animator.applyRootMotion = false;
            if (animatorController != null && animator.runtimeAnimatorController != animatorController)
            {
                animator.runtimeAnimatorController = animatorController;
            }
        }

        private void EnsureAnimationEventReceiver()
        {
            if (!animator.TryGetComponent<PlayerAnimationEventReceiver>(out _))
            {
                animator.gameObject.AddComponent<PlayerAnimationEventReceiver>();
            }
        }

        private void PlayState(string stateName, bool force)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            if (!force && currentAnimatorState == stateName)
            {
                return;
            }

            currentAnimatorState = stateName;
            animator.speed = 1f;
            if (crossFadeSeconds > 0f && animator.isInitialized)
            {
                animator.CrossFadeInFixedTime(stateName, crossFadeSeconds);
                return;
            }

            animator.Play(stateName, 0, 0f);
        }

        private string GetAnimatorStateName(GameState state)
        {
            switch (state)
            {
                case GameState.Aiming:
                    return aimingStateName;
                case GameState.Running:
                    return flyingStateName;
                case GameState.RunEnded:
                    return runEndedStateName;
                case GameState.Menu:
                default:
                    return idleStateName;
            }
        }

        private void UpdateRunningAnimation()
        {
            PlayState(GetRunningAnimatorStateName(), false);
        }

        private string GetRunningAnimatorStateName()
        {
            if (Time.time < slideLockedUntil)
            {
                return slidingStateName;
            }

            if (touchedGroundDuringRun)
            {
                return slidingStateName;
            }

            return IsGrounded() ? slidingStateName : flyingStateName;
        }

        private bool IsGrounded()
        {
            if (body == null || body.isKinematic)
            {
                return true;
            }

            if (Mathf.Abs(body.linearVelocity.y) > maxGroundedVerticalSpeed)
            {
                return false;
            }

            return Time.time <= groundedUntil;
        }

        private void OnCollisionEnter(Collision collision)
        {
            RecordGroundedContact(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            RecordGroundedContact(collision);
        }

        private void RecordGroundedContact(Collision collision)
        {
            if (currentState != GameState.Running)
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y >= groundedContactNormalY)
                {
                    bool wasGrounded = Time.time <= groundedUntil;
                    groundedUntil = Time.time + groundedMemorySeconds;
                    if (!touchedGroundDuringRun)
                    {
                        touchedGroundDuringRun = true;
                        PlaySlideFromStart();
                    }
                    else if (!wasGrounded)
                    {
                        PlaySlideFromStart();
                    }

                    return;
                }
            }
        }

        private void PlaySlideFromStart()
        {
            slideLockedUntil = Time.time + GetSlideAnimationLockSeconds();
            PlayStateImmediate(slidingStateName);
        }

        private void PlayStateImmediate(string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return;
            }

            currentAnimatorState = stateName;
            animator.speed = 1f;
            animator.Play(stateName, 0, 0f);
        }

        private float GetSlideAnimationLockSeconds()
        {
            float lockSeconds = slideAnimationLockSeconds;
            AnimationClip clip = GetAnimationClip(slidingStateName);
            if (clip != null)
            {
                lockSeconds = Mathf.Max(lockSeconds, clip.length);
            }

            return lockSeconds;
        }

        private AnimationClip GetAnimationClip(string clipName)
        {
            if (animator == null || string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                return null;
            }

            AnimationClip[] clips = controller.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null && clip.name == clipName)
                {
                    return clip;
                }
            }

            return null;
        }

        private void RotateTowardVelocity()
        {
            if (body == null || visualRoot == null)
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude < minFacingSpeed * minFacingSpeed)
            {
                return;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(velocity.normalized);
            localVelocity.y = 0f;
            if (localVelocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float yaw = Mathf.Atan2(localVelocity.x, localVelocity.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(yaw, Vector3.up) * visualSpawnRotation;
            if (rotationDegreesPerSecond <= 0f)
            {
                visualRoot.localRotation = targetRotation;
                return;
            }

            visualRoot.localRotation = Quaternion.RotateTowards(
                visualRoot.localRotation,
                targetRotation,
                rotationDegreesPerSecond * Time.deltaTime);
        }

        private void OnValidate()
        {
            crossFadeSeconds = Mathf.Max(0f, crossFadeSeconds);
            groundedContactNormalY = Mathf.Clamp01(groundedContactNormalY);
            groundedMemorySeconds = Mathf.Max(0f, groundedMemorySeconds);
            maxGroundedVerticalSpeed = Mathf.Max(0f, maxGroundedVerticalSpeed);
            slideAnimationLockSeconds = Mathf.Max(0f, slideAnimationLockSeconds);
            minFacingSpeed = Mathf.Max(0f, minFacingSpeed);
            rotationDegreesPerSecond = Mathf.Max(0f, rotationDegreesPerSecond);
        }
    }
}
