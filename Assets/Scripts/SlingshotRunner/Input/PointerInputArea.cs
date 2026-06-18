using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace SlingshotRunner
{
    [DisallowMultipleComponent]
    public sealed class PointerInputArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform pointerImageRect;
        [SerializeField] private Canvas pointerCanvas;
        [SerializeField] private bool requirePointerImageRaycast = true;

        public event Action<Vector2> PointerPressed;
        public event Action<Vector2, Vector2> PointerDragged;
        public event Action<Vector2, Vector2, float> PointerReleased;

        private const int NoPointer = int.MinValue;
        private const int SyntheticPointer = int.MinValue + 1;

        private int activePointerId = NoPointer;
        private Vector2 pressPosition;
        private Vector2 currentPosition;
        private float pressTime;
        private bool syntheticPointerActive;
        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        public bool HasActivePointer => activePointerId != NoPointer;

        private void Awake()
        {
            CacheReferences();
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            UpdateSyntheticPointer();
#endif
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer)
            {
                return;
            }

            if (!IsPointerImageHit(eventData.position, eventData.pressEventCamera))
            {
                return;
            }

            activePointerId = eventData.pointerId;
            pressPosition = eventData.position;
            currentPosition = pressPosition;
            pressTime = Time.unscaledTime;

            PointerPressed?.Invoke(pressPosition);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            currentPosition = eventData.position;
            PointerDragged?.Invoke(currentPosition - pressPosition, currentPosition);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            currentPosition = eventData.position;
            Vector2 dragDelta = currentPosition - pressPosition;
            float heldSeconds = Mathf.Max(0f, Time.unscaledTime - pressTime);

            activePointerId = NoPointer;
            syntheticPointerActive = false;
            PointerReleased?.Invoke(dragDelta, currentPosition, heldSeconds);
        }

        public void Cancel()
        {
            activePointerId = NoPointer;
            syntheticPointerActive = false;
        }

#if ENABLE_INPUT_SYSTEM
        private void UpdateSyntheticPointer()
        {
            if (!TryReadPointer(out Vector2 position, out bool pressedThisFrame, out bool isPressed, out bool releasedThisFrame))
            {
                return;
            }

            if (pressedThisFrame && activePointerId == NoPointer)
            {
                if (!IsPointerImageHit(position, GetEventCamera()))
                {
                    return;
                }

                activePointerId = SyntheticPointer;
                syntheticPointerActive = true;
                pressPosition = position;
                currentPosition = position;
                pressTime = Time.unscaledTime;
                PointerPressed?.Invoke(pressPosition);
            }

            if (!syntheticPointerActive)
            {
                return;
            }

            if (isPressed)
            {
                currentPosition = position;
                PointerDragged?.Invoke(currentPosition - pressPosition, currentPosition);
            }

            if (releasedThisFrame)
            {
                currentPosition = position;
                Vector2 dragDelta = currentPosition - pressPosition;
                float heldSeconds = Mathf.Max(0f, Time.unscaledTime - pressTime);

                activePointerId = NoPointer;
                syntheticPointerActive = false;
                PointerReleased?.Invoke(dragDelta, currentPosition, heldSeconds);
            }
        }

        private static bool TryReadPointer(out Vector2 position, out bool pressedThisFrame, out bool isPressed, out bool releasedThisFrame)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                TouchControl primaryTouch = touchscreen.primaryTouch;
                position = primaryTouch.position.ReadValue();
                pressedThisFrame = primaryTouch.press.wasPressedThisFrame;
                isPressed = primaryTouch.press.isPressed;
                releasedThisFrame = primaryTouch.press.wasReleasedThisFrame;
                if (pressedThisFrame || isPressed || releasedThisFrame)
                {
                    return true;
                }
            }

            Pointer pointer = Pointer.current;
            if (pointer != null)
            {
                position = pointer.position.ReadValue();
                pressedThisFrame = pointer.press.wasPressedThisFrame;
                isPressed = pointer.press.isPressed;
                releasedThisFrame = pointer.press.wasReleasedThisFrame;
                return pressedThisFrame || isPressed || releasedThisFrame;
            }

            position = default;
            pressedThisFrame = false;
            isPressed = false;
            releasedThisFrame = false;
            return false;
        }
#endif

        private void CacheReferences()
        {
            if (pointerImageRect == null)
            {
                pointerImageRect = transform as RectTransform;
            }

            if (pointerCanvas == null)
            {
                pointerCanvas = GetComponentInParent<Canvas>();
            }
        }

        private bool IsPointerImageHit(Vector2 screenPosition, Camera eventCamera)
        {
            CacheReferences();

            Camera camera = eventCamera != null ? eventCamera : GetEventCamera();
            if (pointerImageRect == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(pointerImageRect, screenPosition, camera))
            {
                return false;
            }

            if (!requirePointerImageRaycast)
            {
                return true;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return true;
            }

            raycastResults.Clear();
            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = screenPosition
            };
            eventSystem.RaycastAll(pointerData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject hitObject = raycastResults[i].gameObject;
                if (hitObject == null)
                {
                    continue;
                }

                return hitObject == gameObject;
            }

            return false;
        }

        private Camera GetEventCamera()
        {
            CacheReferences();
            if (pointerCanvas == null || pointerCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return pointerCanvas.worldCamera;
        }
    }
}
