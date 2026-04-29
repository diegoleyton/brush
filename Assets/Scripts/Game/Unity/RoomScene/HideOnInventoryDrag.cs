using System;

using Flowbit.Utilities.Core.Events;

using Game.Unity.Definitions.Events;

using UnityEngine;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Hides its GameObject while a room inventory drag is active.
    /// </summary>
    public sealed class HideOnInventoryDrag : MonoBehaviour
    {
        private EventDispatcher dispatcher_;
        private bool subscribed_;
        private bool wasActiveBeforeDrag_;

        [Inject]
        public void Construct(EventDispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        private void Awake()
        {
            wasActiveBeforeDrag_ = gameObject.activeSelf;
            SubscribeToDispatcher();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        private void OnRoomInventoryDragStarted(RoomInventoryDragStartedEvent _)
        {
            wasActiveBeforeDrag_ = gameObject.activeSelf;
            gameObject.SetActive(false);
        }

        private void OnRoomInventoryDragEnded(RoomInventoryDragEndedEvent _)
        {
            if (wasActiveBeforeDrag_)
            {
                gameObject.SetActive(true);
            }
        }

        private void SubscribeToDispatcher()
        {
            if (subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Subscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            subscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomInventoryDragStartedEvent>(OnRoomInventoryDragStarted);
            dispatcher_.Unsubscribe<RoomInventoryDragEndedEvent>(OnRoomInventoryDragEnded);
            subscribed_ = false;
        }
    }
}
