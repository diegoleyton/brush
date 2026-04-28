using Flowbit.Utilities.Unity.UI;

using UnityEngine;
using System.Collections;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Handles the view of the inventory in the room
    /// </summary>
    public sealed class RoomInventoryView : MonoBehaviour
    {
        [SerializeField]
        private UIComponentAnimatorController animatorController_;

        [SerializeField]
        [Min(0f)]
        private float showDelaySeconds_ = 0.5f;

        private bool hidden_ = true;
        private Coroutine showCoroutine_;

        public bool IsHidden => hidden_;

        public void Toggle()
        {
            if (hidden_)
            {
                Show();
                return;
            }

            Hide();
        }

        public void Show()
        {
            CancelPendingShow();

            if (!hidden_)
            {
                return;
            }

            hidden_ = false;
            animatorController_.GoToInitialState();
        }

        public void ShowWithDelay()
        {
            CancelPendingShow();
            showCoroutine_ = StartCoroutine(ShowAfterDelay());
        }

        public void Hide()
        {
            CancelPendingShow();

            if (hidden_)
            {
                return;
            }

            hidden_ = true;
            animatorController_.GoToFinalState();
        }

        private IEnumerator ShowAfterDelay()
        {
            if (showDelaySeconds_ > 0f)
            {
                yield return new WaitForSeconds(showDelaySeconds_);
            }

            Show();
            showCoroutine_ = null;
        }

        private void CancelPendingShow()
        {
            if (showCoroutine_ == null)
            {
                return;
            }

            StopCoroutine(showCoroutine_);
            showCoroutine_ = null;
        }
    }
}
