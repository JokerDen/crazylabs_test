using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class CollectibleCoin : MonoBehaviour
    {
        [SerializeField] private int baseValue = 1;
        [SerializeField] private Transform view;
        [SerializeField] private Transform obtainFx;

        private Collider triggerCollider;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool isAvailable = true;

        public int BaseValue => baseValue;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
            ResetPresentation();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isAvailable)
            {
                return;
            }

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                return;
            }

            if (!ServiceLocator.TryGet(out GameStateMachine stateMachine) || !stateMachine.Is(GameState.Running))
            {
                return;
            }

            isAvailable = false;
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            SetViewActive(false);
            if (ServiceLocator.TryGet(out RunSession runSession))
            {
                runSession.CollectCoin(this);
            }

            PlayObtainFx();
        }

        public void ResetCoin()
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            isAvailable = true;
            gameObject.SetActive(true);
            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
            }

            ResetPresentation();
        }

        private void ResetPresentation()
        {
            SetViewActive(true);
            StopObtainFx();
        }

        private void SetViewActive(bool active)
        {
            if (view != null)
            {
                view.gameObject.SetActive(active);
            }
        }

        private void PlayObtainFx()
        {
            if (obtainFx == null)
            {
                return;
            }

            obtainFx.gameObject.SetActive(true);
            ParticleSystem[] particles = obtainFx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear(true);
                particles[i].Play(true);
            }
        }

        private void StopObtainFx()
        {
            if (obtainFx == null)
            {
                return;
            }

            ParticleSystem[] particles = obtainFx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            obtainFx.gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            if (TryGetComponent(out Collider trigger))
            {
                trigger.isTrigger = true;
            }

            if (!Application.isPlaying && obtainFx != null)
            {
                obtainFx.gameObject.SetActive(false);
            }
        }
    }
}
