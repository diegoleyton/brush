using Flowbit.Utilities.Unity.UI;

using Game.Core.Data;
using Game.Core.Events;

using UnityEngine;
using System;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Handles the view of the inventory in the room
    /// </summary>
    public sealed class RoomInventoryView : MonoBehaviour
    {
        [SerializeField]
        private UIComponentAnimatorController animatorController_;

        private bool hidden_ = true;

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
            if (!hidden_)
            {
                return;
            }

            hidden_ = false;
            animatorController_.GoToInitialState();
        }

        public void Hide()
        {
            if (hidden_)
            {
                return;
            }

            hidden_ = true;
            animatorController_.GoToFinalState();
        }
    }
}
