using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class CollectibleCoin : MonoBehaviour
    {
        [SerializeField] private int baseValue = 1;
        [SerializeField] private float spinSpeed = 180f;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool isAvailable = true;

        public int BaseValue => baseValue;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void Update()
        {
            if (isAvailable)
            {
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            }
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
            if (ServiceLocator.TryGet(out RunSession runSession))
            {
                runSession.CollectCoin(this);
            }

            gameObject.SetActive(false);
        }

        public void ResetCoin()
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            isAvailable = true;
            gameObject.SetActive(true);
        }
    }
}
