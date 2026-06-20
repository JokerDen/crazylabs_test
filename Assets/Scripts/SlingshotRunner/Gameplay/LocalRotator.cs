using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class LocalRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 speed = new Vector3(0f, 180f, 0f);

        public Vector3 Speed
        {
            get => speed;
            set => speed = value;
        }

        private void Update()
        {
            if (speed.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.Rotate(speed * Time.deltaTime, Space.Self);
        }
    }
}
