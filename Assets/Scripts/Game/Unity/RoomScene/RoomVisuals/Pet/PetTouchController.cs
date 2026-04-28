using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Handles touch and hold input for a pet view.
    /// </summary>
    public sealed class PetTouchController : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField]
        private PetView petView_;

        [SerializeField]
        private float holdDelaySeconds_ = 0.45f;

        private bool pointerDown_;
        private bool holdStarted_;
        private bool suppressClick_;

        private float pointerDownTime_;

        private void Awake()
        {
            if (petView_ == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(PetTouchController)} requires a {nameof(PetView)} reference.");
            }
        }

        private void Update()
        {
            if (!pointerDown_ || holdStarted_)
            {
                return;
            }

            if (Time.unscaledTime - pointerDownTime_ < holdDelaySeconds_)
            {
                return;
            }

            holdStarted_ = true;
            suppressClick_ = true;
            petView_?.StartTouching();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            pointerDown_ = true;
            holdStarted_ = false;
            suppressClick_ = false;
            pointerDownTime_ = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !pointerDown_)
            {
                return;
            }

            if (holdStarted_)
            {
                petView_?.StopTouching();
            }

            ResetPointerState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!pointerDown_)
            {
                return;
            }

            if (holdStarted_)
            {
                petView_?.StopTouching();
            }

            ResetPointerState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || suppressClick_)
            {
                return;
            }

            petView_?.Touch();
        }

        private void ResetPointerState()
        {
            pointerDown_ = false;
            holdStarted_ = false;
            pointerDownTime_ = 0f;
        }
    }
}
