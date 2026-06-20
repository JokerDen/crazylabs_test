using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class UiBouncer : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        private const float MinDuration = 0.01f;

        [Header("Target")]
        [SerializeField] private RectTransform target;

        [Header("Show Bounce")]
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private float showDelay;
        [SerializeField] private float showDuration = 0.36f;
        [SerializeField] private float showOvershoot = 1.2f;

        [Header("Press Bounce")]
        [SerializeField] private bool playOnPress = true;
        [SerializeField] private float pressScale = 0.9f;
        [SerializeField] private float pressDuration = 0.22f;
        [SerializeField] private float pressOvershoot = 1.35f;

        [Header("Constant Scale Bounce")]
        [SerializeField] private bool playConstantScaleBounce;
        [SerializeField] private float constantScale = 1.06f;
        [SerializeField] private float constantDuration = 0.9f;

        [Header("Timing")]
        [SerializeField] private bool useUnscaledTime = true;

        private Vector3 restingScale;
        private Sequence activeSequence;
        private Sequence constantSequence;
        private bool hasRestingScale;
        private Selectable selectable;

        private RectTransform Target
        {
            get
            {
                if (target == null)
                {
                    target = transform as RectTransform;
                }

                return target;
            }
        }

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
            CaptureRestingScale(true);
        }

        private void OnEnable()
        {
            CaptureRestingScale(true);

            if (playOnEnable)
            {
                PlayShow();
                return;
            }

            PlayConstantScaleBounce();
        }

        private void OnDisable()
        {
            KillAllSequences();
            RestoreScale();
        }

        private void OnDestroy()
        {
            KillAllSequences();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null || eventData.button == PointerEventData.InputButton.Left)
            {
                PlayPress();
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            PlayPress();
        }

        public void PlayShow()
        {
            RectTransform rectTransform = Target;
            if (rectTransform == null)
            {
                return;
            }

            CaptureRestingScale(false);
            KillActiveSequence();
            KillConstantSequence();

            rectTransform.localScale = Vector3.zero;
            activeSequence = DOTween.Sequence();
            activeSequence.SetUpdate(useUnscaledTime);

            if (showDelay > 0f)
            {
                activeSequence.AppendInterval(showDelay);
            }

            activeSequence.Append(
                rectTransform.DOScale(restingScale, showDuration)
                    .SetEase(Ease.OutBack, showOvershoot));
            activeSequence.OnComplete(PlayConstantScaleBounce);
        }

        public void PlayPress()
        {
            if (!CanPlayPressBounce())
            {
                return;
            }

            RectTransform rectTransform = Target;
            if (rectTransform == null)
            {
                return;
            }

            CaptureRestingScale(false);
            KillActiveSequence();
            KillConstantSequence();

            float normalizedPressDuration = Mathf.Max(MinDuration, pressDuration);
            Vector3 pressedScale = restingScale * Mathf.Clamp(pressScale, 0.01f, 1f);

            activeSequence = DOTween.Sequence();
            activeSequence.SetUpdate(useUnscaledTime);
            activeSequence.Append(
                rectTransform.DOScale(pressedScale, normalizedPressDuration * 0.35f)
                    .SetEase(Ease.OutQuad));
            activeSequence.Append(
                rectTransform.DOScale(restingScale, normalizedPressDuration * 0.65f)
                    .SetEase(Ease.OutBack, pressOvershoot));
            activeSequence.OnComplete(PlayConstantScaleBounce);
        }

        private bool CanPlayPressBounce()
        {
            return playOnPress
                && isActiveAndEnabled
                && (selectable == null || selectable.IsInteractable());
        }

        private void PlayConstantScaleBounce()
        {
            KillConstantSequence();
            if (!playConstantScaleBounce || !isActiveAndEnabled)
            {
                return;
            }

            RectTransform rectTransform = Target;
            if (rectTransform == null)
            {
                return;
            }

            CaptureRestingScale(false);
            float normalizedDuration = Mathf.Max(MinDuration, constantDuration);
            Vector3 peakScale = restingScale * Mathf.Max(1f, constantScale);

            rectTransform.localScale = restingScale;
            constantSequence = DOTween.Sequence();
            constantSequence.SetUpdate(useUnscaledTime);
            constantSequence.Append(
                rectTransform.DOScale(peakScale, normalizedDuration * 0.5f)
                    .SetEase(Ease.InOutSine));
            constantSequence.Append(
                rectTransform.DOScale(restingScale, normalizedDuration * 0.5f)
                    .SetEase(Ease.InOutSine));
            constantSequence.SetLoops(-1, LoopType.Restart);
        }

        private void CaptureRestingScale(bool forceRefresh)
        {
            if (hasRestingScale && !forceRefresh)
            {
                return;
            }

            RectTransform rectTransform = Target;
            if (rectTransform == null)
            {
                return;
            }

            restingScale = rectTransform.localScale == Vector3.zero
                ? Vector3.one
                : rectTransform.localScale;
            hasRestingScale = true;
        }

        private void RestoreScale()
        {
            if (hasRestingScale && target != null)
            {
                target.localScale = restingScale;
            }
        }

        private void KillActiveSequence()
        {
            if (activeSequence == null)
            {
                return;
            }

            activeSequence.Kill();
            activeSequence = null;
        }

        private void KillConstantSequence()
        {
            if (constantSequence == null)
            {
                return;
            }

            constantSequence.Kill();
            constantSequence = null;
        }

        private void KillAllSequences()
        {
            KillActiveSequence();
            KillConstantSequence();
        }

        private void OnValidate()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }

            showDelay = Mathf.Max(0f, showDelay);
            showDuration = Mathf.Max(MinDuration, showDuration);
            showOvershoot = Mathf.Max(0f, showOvershoot);
            pressScale = Mathf.Clamp(pressScale, 0.01f, 1f);
            pressDuration = Mathf.Max(MinDuration, pressDuration);
            pressOvershoot = Mathf.Max(0f, pressOvershoot);
            constantScale = Mathf.Max(1f, constantScale);
            constantDuration = Mathf.Max(MinDuration, constantDuration);
        }
    }
}
