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

        public void Toggle()
        {
            hidden_ = !hidden_;

            if (hidden_)
            {
                animatorController_.GoToFinalState();
            }
            else
            {
                animatorController_.GoToInitialState();
            }
        }
    }
}
