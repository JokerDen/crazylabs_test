using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class PlayerSpawnBehaviour : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;

        private Vector3 spawnPosition;
        private Quaternion spawnRotation = Quaternion.identity;
        private Quaternion visualSpawnRotation = Quaternion.identity;
        private bool visualSpawnRotationCached;

        public Vector3 SpawnPosition => spawnPosition;
        public Quaternion SpawnRotation => spawnRotation;
        public float Distance => Mathf.Max(0f, transform.position.z - spawnPosition.z);

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        public void PlaceAt(Vector3 position, Quaternion rotation)
        {
            CacheReferences();

            spawnPosition = position;
            spawnRotation = rotation;

            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            ResetVisualRotation();
        }

        public void ResetVisualRotation()
        {
            if (visualRoot != null)
            {
                visualRoot.localRotation = visualSpawnRotation;
            }
        }

        private void CacheReferences()
        {
            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
                visualSpawnRotationCached = false;
            }

            CacheVisualSpawnRotation();
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
    }
}
