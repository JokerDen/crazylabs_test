using UnityEngine;

namespace SlingshotRunner
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SlingshotLineView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform leftAnchor;
        [SerializeField] private Transform rightAnchor;
        [SerializeField] private Transform middleTarget;

        [Header("Shape")]
        [SerializeField] private Vector3 restMiddleOffset = new Vector3(0f, 0f, 0f);
        [SerializeField, Min(2)] private int segmentCount = 24;
        [SerializeField] private float launchStopOffset;

        private Vector3[] positions;
        private bool followTarget;
        private bool stopAtAnchorLine;

        private void Awake()
        {
            RenderAtRest();
        }

        private void OnEnable()
        {
            RenderEditorPreview();
        }

        private void OnValidate()
        {
            segmentCount = Mathf.Max(2, segmentCount);
            RenderEditorPreview();
        }

        private void Update()
        {
            RenderEditorPreview();
        }

        private void LateUpdate()
        {
            if (!followTarget)
            {
                return;
            }

            Vector3 middle = GetTargetMiddle();
            if (stopAtAnchorLine && HasReachedAnchorLine(middle))
            {
                RenderAtRest();
                return;
            }

            Render(middle);
        }

        public void BeginAiming()
        {
            followTarget = true;
            stopAtAnchorLine = false;
            Render(GetTargetMiddle());
        }

        public void BeginLaunchFollow()
        {
            followTarget = true;
            stopAtAnchorLine = true;
            Render(GetTargetMiddle());
        }

        public void RenderAtRest()
        {
            followTarget = false;
            stopAtAnchorLine = false;
            Render(GetRestMiddle());
        }

        private void RenderEditorPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            followTarget = false;
            stopAtAnchorLine = false;
            Render(GetTargetMiddle());
        }

        private void Render(Vector3 middle)
        {
            if (lineRenderer == null || leftAnchor == null || rightAnchor == null)
            {
                return;
            }

            int pointCount = Mathf.Max(3, segmentCount + 1);
            EnsurePositions(pointCount);

            Vector3 start = leftAnchor.position;
            Vector3 end = rightAnchor.position;
            Vector3 control = middle * 2f - (start + end) * 0.5f;

            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)(pointCount - 1);
                positions[i] = EvaluateQuadraticBezier(start, control, end, t);
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = pointCount;
            lineRenderer.SetPositions(positions);
        }

        private void EnsurePositions(int pointCount)
        {
            if (positions == null || positions.Length != pointCount)
            {
                positions = new Vector3[pointCount];
            }
        }

        private Vector3 GetTargetMiddle()
        {
            if (middleTarget == null)
            {
                return GetRestMiddle();
            }

            return middleTarget.position;
        }

        private Vector3 GetRestMiddle()
        {
            if (leftAnchor == null || rightAnchor == null)
            {
                return transform.TransformPoint(restMiddleOffset);
            }

            return (leftAnchor.position + rightAnchor.position) * 0.5f + transform.TransformVector(restMiddleOffset);
        }

        private bool HasReachedAnchorLine(Vector3 middle)
        {
            if (leftAnchor == null || rightAnchor == null)
            {
                return false;
            }

            Vector3 anchorCenter = (leftAnchor.position + rightAnchor.position) * 0.5f;
            return Vector3.Dot(middle - anchorCenter, transform.forward) >= launchStopOffset;
        }

        private static Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float inverseT = 1f - t;
            return inverseT * inverseT * start + 2f * inverseT * t * control + t * t * end;
        }
    }
}
