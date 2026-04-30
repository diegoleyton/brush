using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.DragAndDrop;
using Flowbit.Utilities.Unity.UI;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Definitions.Events;
using Game.Unity.Settings;

using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Generic room drop area that reacts to the selected inventory category and reports
    /// a logical room target when a drop is accepted.
    /// </summary>
    public sealed class RoomDropArea : UIDropTarget
    {
        private EventDispatcher dispatcher_;
        private DataRepository repository_;
        private RoomSettings roomSettings_;

        [SerializeField]
        private InteractionPointType supportedInventoryType_ = InteractionPointType.PLACEABLE_OBJECT;

        [SerializeField]
        private int targetId_;

        [SerializeField]
        private bool useVisibility_;

        private RoomDropVisual visibilityController_;

        private bool dropEnabled_ = true;
        private bool interactionVisible_;
        private bool isPointerOver_;
        private bool subscribed_;

        public InteractionPointType SupportedInventoryType => supportedInventoryType_;
        public int TargetId => targetId_;

        [Inject]
        public void Construct(EventDispatcher dispatcher, DataRepository repository, RoomSettings roomSettings)
        {
            dispatcher_ = dispatcher;
            repository_ = repository;
            roomSettings_ = roomSettings;
        }

        private void Awake()
        {
            Configure(
                reparentDroppedItem: false,
                resetAnchoredPositionOnDrop: true,
                positionDroppedItemAtPointer: false);

            CreateVisibilityControllerIfNeeded();
            SubscribeToDispatcher();
            interactionVisible_ = false;
            UpdateVisibility();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        public override bool CanAccept(UIDraggable draggable)
        {
            return interactionVisible_ && dropEnabled_ && HasValidTargetConfiguration() && base.CanAccept(draggable);
        }

        public override void OnDragHoverEntered(UIDraggable draggable)
        {
            isPointerOver_ = true;

            if (CanShowVisibilityFeedback())
            {
                visibilityController_?.Highlight(true);
            }
        }

        public override void OnDragHoverExited(UIDraggable draggable)
        {
            isPointerOver_ = false;

            if (CanShowVisibilityFeedback())
            {
                visibilityController_?.Highlight(false);
            }
        }

        public void SetTargetId(int targetId)
        {
            targetId_ = targetId;
            RefreshVisibility();
        }

        public void SetDropEnabled(bool enabled)
        {
            dropEnabled_ = enabled;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            UpdateVisibility();
        }

        private bool ShouldShowForInteractionType(InteractionPointType? interactionPointType)
        {
            return interactionPointType == SupportedInventoryType;
        }

        private bool HasValidTargetConfiguration()
        {
            return true;
        }

        private void OnRoomInventoryDragStarted(RoomInventoryDragStartedEvent eventData)
        {
            interactionVisible_ = eventData?.Data != null &&
                ShouldShowForInteractionType(eventData.Data.InteractionPointType) &&
                CanShowForDraggedItem(eventData.Data);
            UpdateVisibility();
        }

        private bool CanShowForDraggedItem(RoomInventoryItemData data)
        {
            if (data == null)
            {
                return false;
            }

            if (!CanApplyDraggedItem(data))
            {
                return false;
            }

            return true;
        }

        private bool CanApplyDraggedItem(RoomInventoryItemData data)
        {
            if (repository_ == null)
            {
                return true;
            }

            switch (supportedInventoryType_)
            {
                case InteractionPointType.PLACEABLE_OBJECT:
                    return CanApplyPlaceableObject(data.ItemId);
                case InteractionPointType.PAINT:
                    return CanApplyPaint(data.ItemId);
                case InteractionPointType.EYES:
                case InteractionPointType.SKIN:
                case InteractionPointType.HAT:
                case InteractionPointType.DRESS:
                    return repository_.GetAppliedPetItemId(supportedInventoryType_) != data.ItemId;
                case InteractionPointType.FOOD:
                    return repository_.CanPetEat == PetEatStatus.OK;
                default:
                    return true;
            }
        }

        private bool CanApplyPlaceableObject(int itemId)
        {
            return !repository_.RoomHasObject(targetId_, itemId);
        }

        private bool CanApplyPaint(int itemId)
        {
            return !repository_.RoomSurfaceHasPaint(targetId_, itemId);
        }

        private void OnRoomInventoryDragEnded(RoomInventoryDragEndedEvent _)
        {
            interactionVisible_ = false;
            isPointerOver_ = false;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (visibilityController_ == null)
            {
                return;
            }

            bool shouldShow = CanShowVisibilityFeedback();
            visibilityController_?.gameObject.SetActive(shouldShow);

            if (!shouldShow)
            {
                return;
            }

            visibilityController_?.Highlight(isPointerOver_);
        }

        private bool CanShowVisibilityFeedback()
        {
            return interactionVisible_ && dropEnabled_ && HasValidTargetConfiguration();
        }

        private void CreateVisibilityControllerIfNeeded()
        {
            if (!useVisibility_)
            {
                return;
            }

            if (roomSettings_ == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(RoomDropArea)} requires a {nameof(RoomSettings)} binding.");
            }

            if (roomSettings_.RoomDropAreaVisibility == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(RoomSettings)} requires a {nameof(RoomSettings.RoomDropAreaVisibility)} prefab reference.");
            }

            visibilityController_ = Instantiate(roomSettings_.RoomDropAreaVisibility, transform, false);

            RectTransform visibilityRectTransform = visibilityController_.transform as RectTransform;
            if (visibilityRectTransform == null)
            {
                throw new System.InvalidOperationException(
                    $"{nameof(RoomDropArea)} visibility prefab must use a {nameof(RectTransform)}.");
            }

            visibilityRectTransform.anchorMin = Vector2.zero;
            visibilityRectTransform.anchorMax = Vector2.one;
            visibilityRectTransform.offsetMin = Vector2.zero;
            visibilityRectTransform.offsetMax = Vector2.zero;
            visibilityRectTransform.anchoredPosition = Vector2.zero;
            visibilityRectTransform.localScale = Vector3.one;
            visibilityRectTransform.SetAsLastSibling();
            DisableVisibilityRaycasts(visibilityController_.gameObject);
            visibilityController_.Highlight(false);
            visibilityController_.gameObject.SetActive(false);
        }

        private static void DisableVisibilityRaycasts(GameObject visibilityObject)
        {
            if (visibilityObject == null)
            {
                return;
            }

            CanvasGroup canvasGroup = visibilityObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = visibilityObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Graphic[] graphics = visibilityObject.GetComponentsInChildren<Graphic>(true);
            for (int index = 0; index < graphics.Length; index++)
            {
                if (graphics[index] != null)
                {
                    graphics[index].raycastTarget = false;
                }
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
