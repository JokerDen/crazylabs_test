using Unity.Cinemachine;
using UnityEngine;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class CameraModeController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera menuCamera;
        [SerializeField] private CinemachineCamera runningCamera;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority;

        public void ShowMenu()
        {
            Activate(menuCamera);
        }

        public void ShowRunning()
        {
            Activate(runningCamera);
        }

        private void Activate(CinemachineCamera activeCamera)
        {
            SetPriority(menuCamera, menuCamera == activeCamera);
            SetPriority(runningCamera, runningCamera == activeCamera);
        }

        private void SetPriority(CinemachineCamera camera, bool isActive)
        {
            if (camera == null)
            {
                return;
            }

            camera.Priority = isActive ? activePriority : inactivePriority;
        }
    }
}
