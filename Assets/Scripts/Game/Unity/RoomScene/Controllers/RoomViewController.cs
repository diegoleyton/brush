using System;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Core.Logger;
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
        private IGameLogger logger_;
        private RoomSettings roomSettings_;
        private IObjectInstantiator instantiator_;
        private PetEyesController.Factory eyesControllerFactory_;
        private PetHatController.Factory hatControllerFactory_;
        private PetSkinController.Factory skinControllerFactory_;
        private PetDressController.Factory dressControllerFactory_;

        private RoomPlaceableObjectSurfaceView[] placeableObjectSurfaceViews_;
        private RoomPaintSurfaceView[] paintSurfaceViews_;
        private PetEyesSurfaceView[] eyesSurfaceViews_;
        private PetHatSurfaceView[] hatSurfaceViews_;
        private PetSkinSurfaceView[] skinSurfaceViews_;
        private PetDressSurfaceView[] dressSurfaceViews_;

        private bool initialized_;
        private bool subscribed_;
        private RoomPlaceableObjectsController placeableObjectsController_;
        private RoomPaintController paintController_;
        private PetEyesController eyesController_;
        private PetHatController hatController_;
        private PetSkinController skinController_;
        private PetDressController dressController_;

        [Inject]
        public void Construct(
            DataRepository repository,
            EventDispatcher dispatcher,
            IGameLogger logger,
            RoomSettings roomSettings,
            PetEyesController.Factory eyesControllerFactory,
            PetHatController.Factory hatControllerFactory,
            PetSkinController.Factory skinControllerFactory,
            PetDressController.Factory dressControllerFactory,
            [Inject(Id = InstantiatorIds.Dependency)] IObjectInstantiator instantiator)
        {
            repository_ = repository;
            dispatcher_ = dispatcher;
            logger_ = logger;
            roomSettings_ = roomSettings;
            instantiator_ = instantiator;
            eyesControllerFactory_ = eyesControllerFactory;
            hatControllerFactory_ = hatControllerFactory;
            skinControllerFactory_ = skinControllerFactory;
            dressControllerFactory_ = dressControllerFactory;
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
            eyesController_ = eyesControllerFactory_?.Create(eyesSurfaceViews_);
            hatController_ = hatControllerFactory_?.Create(hatSurfaceViews_);
            skinController_ = skinControllerFactory_?.Create(skinSurfaceViews_);
            dressController_ = dressControllerFactory_?.Create(dressSurfaceViews_);

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
            eyesSurfaceViews_ = SurfaceViewConatiner_.GetComponentsInChildren<PetEyesSurfaceView>(true);
            hatSurfaceViews_ = SurfaceViewConatiner_.GetComponentsInChildren<PetHatSurfaceView>(true);
            skinSurfaceViews_ = SurfaceViewConatiner_.GetComponentsInChildren<PetSkinSurfaceView>(true);
            dressSurfaceViews_ = SurfaceViewConatiner_.GetComponentsInChildren<PetDressSurfaceView>(true);
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
                    logger_?.Log($"[RoomView] Refresh placeable child {eventData.ParentTargetId}/{eventData.TargetId}.");
                    placeableObjectsController_?.RefreshChildPlaceableObject(
                        eventData.ParentTargetId,
                        eventData.TargetId);
                    paintController_?.RefreshPaintedChildObject(
                        eventData.ParentTargetId,
                        eventData.TargetId);
                    return;
                }

                logger_?.Log($"[RoomView] Refresh placeable object {eventData.TargetId}.");
                placeableObjectsController_?.RefreshPlaceableObject(eventData.TargetId);
                paintController_?.RefreshPaintedObject(eventData.TargetId);
                return;
            }

            if (eventData.ItemType == Core.Data.InteractionPointType.PAINT)
            {
                if (eventData.ParentTargetId >= 0)
                {
                    logger_?.Log($"[RoomView] Refresh painted child {eventData.ParentTargetId}/{eventData.TargetId}.");
                    paintController_?.RefreshPaintedChildObject(
                        eventData.ParentTargetId,
                        eventData.TargetId);
                    return;
                }

                if (Game.Core.Configuration.GameIds.IsRoomSurfaceId(eventData.TargetId))
                {
                    logger_?.Log($"[RoomView] Refresh painted surface {eventData.TargetId}.");
                    paintController_?.RefreshPaintSurface(eventData.TargetId);
                    return;
                }

                logger_?.Log($"[RoomView] Refresh painted object {eventData.TargetId}.");
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

            if (eventData.ItemType == Core.Data.InteractionPointType.EYES)
            {
                logger_?.Log("[RoomView] Refresh pet eyes.");
                eyesController_?.Refresh();
                return;
            }

            if (eventData.ItemType == Core.Data.InteractionPointType.HAT)
            {
                logger_?.Log("[RoomView] Refresh pet hat.");
                hatController_?.Refresh();
                return;
            }

            if (eventData.ItemType == Core.Data.InteractionPointType.SKIN)
            {
                logger_?.Log("[RoomView] Refresh pet skin.");
                skinController_?.Refresh();
                return;
            }

            if (eventData.ItemType == Core.Data.InteractionPointType.DRESS)
            {
                logger_?.Log("[RoomView] Refresh pet dress.");
                dressController_?.Refresh();
            }
        }

        private void RefreshFromData()
        {
            placeableObjectsController_?.Refresh();
            paintController_?.Refresh();
            eyesController_?.Refresh();
            hatController_?.Refresh();
            skinController_?.Refresh();
            dressController_?.Refresh();
        }
    }
}
