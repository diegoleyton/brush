using System;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.Instantiator;

using Game.Core.Events;
using Game.Core.Services;
using Game.Unity.Definitions;
using Game.Unity.Settings;

using UnityEngine;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted room state to the scene visuals.
    /// </summary>
    public sealed class RoomViewController : MonoBehaviour
    {
        private DataRepository repository_;
        private EventDispatcher dispatcher_;
        private RoomSettings roomSettings_;
        private IObjectInstantiator instantiator_;

        private RoomObjectDropArea[] dropAreas_ = Array.Empty<RoomObjectDropArea>();

        private bool initialized_;
        private bool subscribed_;
        private PlaceableObjectRoomViewStrategy placeableObjectStrategy_;

        [Inject]
        public void Construct(
            DataRepository repository,
            EventDispatcher dispatcher,
            RoomSettings roomSettings,
            [Inject(Id = InstantiatorIds.Unity)] IObjectInstantiator instantiator)
        {
            repository_ = repository;
            dispatcher_ = dispatcher;
            roomSettings_ = roomSettings;
            instantiator_ = instantiator;
        }

        private void Awake()
        {
            SubscribeToDispatcher();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        public void Initialize()
        {
            if (initialized_)
            {
                return;
            }

            ValidateSceneReferences();
            placeableObjectStrategy_ = new PlaceableObjectRoomViewStrategy(
                repository_,
                dropAreas_,
                roomSettings_,
                instantiator_);

            RefreshFromData();
            initialized_ = true;
        }

        public void SetDropAreas(RoomObjectDropArea[] dropAreas)
        {
            dropAreas_ = dropAreas ?? Array.Empty<RoomObjectDropArea>();
        }

        private void ValidateSceneReferences()
        {
            if (dropAreas_ == null || dropAreas_.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomViewController)} requires at least one {nameof(RoomObjectDropArea)} reference.");
            }

            for (int index = 0; index < dropAreas_.Length; index++)
            {
                if (dropAreas_[index] == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(RoomViewController)} has a missing drop area reference at index {index}.");
                }
            }

            if (roomSettings_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomViewController)} requires a {nameof(RoomSettings)} binding.");
            }
        }

        private void SubscribeToDispatcher()
        {
            if (subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<LocalDataChangedEvent>(OnLocalDataChanged);
            subscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<LocalDataChangedEvent>(OnLocalDataChanged);
            subscribed_ = false;
        }

        private void OnLocalDataChanged(LocalDataChangedEvent _)
        {
            if (!initialized_)
            {
                return;
            }

            RefreshFromData();
        }

        private void RefreshFromData()
        {
            placeableObjectStrategy_?.Refresh();
        }
    }
}
