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
        [SerializeField]
        private GameObject SurfaceViewConatiner_;

        private DataRepository repository_;
        private EventDispatcher dispatcher_;
        private RoomSettings roomSettings_;
        private IObjectInstantiator instantiator_;

        private RoomPlaceableObjectSurfaceView[] placeableObjectSurfaceViews_;
        private RoomPaintSurfaceView[] paintSurfaceViews_;
        private PetFaceSurfaceView[] faceSurfaceViews_;

        private bool initialized_;
        private bool subscribed_;
        private RoomPlaceableObjectsController placeableObjectsController_;
        private RoomPaintController paintController_;
        private RoomFaceController faceController_;

        [Inject]
        public void Construct(
            DataRepository repository,
            EventDispatcher dispatcher,
            RoomSettings roomSettings,
            [Inject(Id = InstantiatorIds.Dependency)] IObjectInstantiator instantiator)
        {
            repository_ = repository;
            dispatcher_ = dispatcher;
            roomSettings_ = roomSettings;
            instantiator_ = instantiator;
        }

        private void Awake()
        {
            SetSurfaceViews();
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
            placeableObjectsController_ = new RoomPlaceableObjectsController(
                repository_,
                placeableObjectSurfaceViews_,
                roomSettings_,
                instantiator_);
            paintController_ = new RoomPaintController(
                repository_,
                placeableObjectSurfaceViews_,
                paintSurfaceViews_);
            faceController_ = new RoomFaceController(faceSurfaceViews_, repository_);

            RefreshFromData();
            initialized_ = true;
        }

        private void ValidateSceneReferences()
        {
            if (placeableObjectSurfaceViews_ == null || placeableObjectSurfaceViews_.Length == 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomViewController)} requires at least one {nameof(RoomPlaceableObjectSurfaceView)} reference.");
            }

            for (int index = 0; index < placeableObjectSurfaceViews_.Length; index++)
            {
                if (placeableObjectSurfaceViews_[index] == null)
                {
                    throw new InvalidOperationException(
                        $"{nameof(RoomViewController)} has a missing surface view reference at index {index}.");
                }
            }

            if (roomSettings_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomViewController)} requires a {nameof(RoomSettings)} binding.");
            }
        }

        private void SetSurfaceViews()
        {
            placeableObjectSurfaceViews_ = SurfaceViewConatiner_.GetComponentsInChildren<RoomPlaceableObjectSurfaceView>(true);
            paintSurfaceViews_ = SurfaceViewConatiner_.GetComponentsInChildren<RoomPaintSurfaceView>(true);
            faceSurfaceViews_ = SurfaceViewConatiner_.GetComponentsInChildren<PetFaceSurfaceView>(true);
        }

        private void SubscribeToDispatcher()
        {
            if (subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<RoomDataItemAppliedEvent>(OnRoomDataItemApplied);
            dispatcher_.Subscribe<PetDataAppliedEvent>(OnPetDataApplied);
            subscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<RoomDataItemAppliedEvent>(OnRoomDataItemApplied);
            dispatcher_.Unsubscribe<PetDataAppliedEvent>(OnPetDataApplied);
            subscribed_ = false;
        }

        private void OnRoomDataItemApplied(RoomDataItemAppliedEvent eventData)
        {
            if (!initialized_)
            {
                return;
            }

            if (eventData.ItemType == Core.Data.InteractionPointType.PLACEABLE_OBJECT)
            {
                if (eventData.ParentTargetId >= 0)
                {
                    placeableObjectsController_?.RefreshChildPlaceableObject(
                        eventData.ParentTargetId,
                        eventData.TargetId);
                    paintController_?.RefreshPaintedChildObject(
                        eventData.ParentTargetId,
                        eventData.TargetId);
                    return;
                }

                placeableObjectsController_?.RefreshPlaceableObject(eventData.TargetId);
                paintController_?.RefreshPaintedObject(eventData.TargetId);
                return;
            }

            if (eventData.ItemType == Core.Data.InteractionPointType.PAINT)
            {
                if (eventData.ParentTargetId >= 0)
                {
                    paintController_?.RefreshPaintedChildObject(
                        eventData.ParentTargetId,
                        eventData.TargetId);
                    return;
                }

                if (Game.Core.Configuration.GameIds.IsRoomSurfaceId(eventData.TargetId))
                {
                    paintController_?.RefreshPaintSurface(eventData.TargetId);
                    return;
                }

                paintController_?.RefreshPaintedObject(eventData.TargetId);
                return;
            }

        }

        private void OnPetDataApplied(PetDataAppliedEvent eventData)
        {
            if (!initialized_)
            {
                return;
            }

            if (eventData.ItemType == Core.Data.InteractionPointType.FACE)
            {
                faceController_?.Refresh();
            }
        }

        private void RefreshFromData()
        {
            placeableObjectsController_?.Refresh();
            paintController_?.Refresh();
            faceController_?.Refresh();
        }
    }
}
