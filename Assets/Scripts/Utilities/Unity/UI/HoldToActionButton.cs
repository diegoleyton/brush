using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Flowbit.Utilities.Unity.UI
{
    /// <summary>
    /// Invokes one action on quick tap and another action when held long enough.
    /// Shows a progress bar that fades in after a delay and starts filling from that point.
    /// </summary>
    public sealed class HoldToActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Timing")]
        [SerializeField]
        private float holdDurationSeconds_ = 0.9f;

        [SerializeField]
        private float showDelaySeconds_ = 0.2f;

        [Header("Visuals")]
        [SerializeField]
        private Image holdProgressImage_;

        [SerializeField]
        private float fadeSpeed_ = 8f;

        [Header("Actions")]
        [SerializeField]
        private UnityEvent onTap_;

        [SerializeField]
        private UnityEvent onHoldCompleted_;

        private bool isPointerDown_;
        private bool holdTriggered_;
        private float holdTime_;

        private float targetAlpha_;
        private bool visualStarted_;

        private void Awake()
        {
            ResetVisualsImmediate();
        }

        private void OnDisable()
        {
            CancelHold();
            ResetVisualsImmediate();
        }

        private void Update()
        {
            UpdateHoldLogic();
            UpdateFade();
        }

        private void UpdateHoldLogic()
        {
            if (!isPointerDown_ || holdTriggered_)
            {
                return;
            }

            holdTime_ += Time.unscaledDeltaTime;

            // 👇 Trigger visual start AFTER delay
            if (!visualStarted_ && holdTime_ >= showDelaySeconds_)
            {
                visualStarted_ = true;
                targetAlpha_ = 1f;
            }

            // 👇 VISUAL progress (starts at 0 when shown)
            if (visualStarted_)
            {
                float visualTime = holdTime_ - showDelaySeconds_;
                float visualDuration = Mathf.Max(holdDurationSeconds_ - showDelaySeconds_, 0.0001f);

                float visualProgress = Mathf.Clamp01(visualTime / visualDuration);
                SetProgress(visualProgress);
            }

            // 👇 REAL hold completion (no cambio)
            if (holdTime_ >= holdDurationSeconds_)
            {
                holdTriggered_ = true;
                onHoldCompleted_?.Invoke();
            }
        }

        private void UpdateFade()
        {
            if (holdProgressImage_ == null)
            {
                return;
            }

            float current = holdProgressImage_.color.a;
            float next = Mathf.Lerp(current, targetAlpha_, Time.unscaledDeltaTime * fadeSpeed_);

            Color color = holdProgressImage_.color;
            color.a = next;
            holdProgressImage_.color = color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown_ = true;
            holdTriggered_ = false;
            holdTime_ = 0f;
            visualStarted_ = false;

            SetProgress(0f);
            targetAlpha_ = 0f; // 👈 no visible aún
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPointerDown_)
            {
                return;
            }

            bool shouldInvokeTap = !holdTriggered_;

            CancelHold();

            if (shouldInvokeTap)
            {
                onTap_?.Invoke();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelHold();
        }

        private void CancelHold()
        {
            isPointerDown_ = false;
            holdTriggered_ = false;
            holdTime_ = 0f;
            visualStarted_ = false;

            targetAlpha_ = 0f;
            SetProgress(0f);
        }

        private void ResetVisualsImmediate()
        {
            SetProgress(0f);

            if (holdProgressImage_ != null)
            {
                var color = holdProgressImage_.color;
                color.a = 0;
                holdProgressImage_.color = color;
            }

            targetAlpha_ = 0f;
        }

        private void SetProgress(float progress)
        {
            if (holdProgressImage_ == null)
            {
                return;
            }

            holdProgressImage_.fillAmount = Mathf.Clamp01(1f - progress);
        }
    }
}