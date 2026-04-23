using Flowbit.Utilities.Unity.DragAndDrop;
using Flowbit.Utilities.Core.Events;

using Game.Core.Data;
using Game.Core.Events;

using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Generic drop area inside the room scene. Keeps a single visual item per area and
    /// controls its own visibility according to the selected inventory category.
    /// </summary>
    public sealed class RoomObjectDropArea : UIDropTarget
    {
        private EventDispatcher dispatcher_;

        [SerializeField]
        private RectTransform itemContainer_;

        [SerializeField]
        private GameObject visibilityRoot_;

        [SerializeField]
        private InteractionPointType supportedInventoryType_ = InteractionPointType.PLACEABLE_OBJECT;

        [SerializeField]
        private int targetId_;

        private bool isVisible_;
        private bool subscribed_;

        /// <summary>
        /// Inventory category supported by this drop area.
        /// </summary>
        public InteractionPointType SupportedInventoryType => supportedInventoryType_;

        /// <summary>
        /// Room target identifier associated with this drop area.
        /// </summary>
        public int TargetId => targetId_;

        [Inject]
        public void Construct(EventDispatcher dispatcher)
        {
            dispatcher_ = dispatcher;
        }

        private void Awake()
        {
            if (visibilityRoot_ == null && itemContainer_ != null)
            {
                visibilityRoot_ = itemContainer_.gameObject;
            }

            Configure(
                itemContainer_,
                reparentDroppedItem: true,
                resetAnchoredPositionOnDrop: true,
                positionDroppedItemAtPointer: false);

            SubscribeToDispatcher();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        public override bool CanAccept(UIDraggable draggable)
        {
            return isVisible_ && base.CanAccept(draggable) && itemContainer_ != null;
        }

        public override void AcceptDrop(UIDraggable draggable, PointerEventData eventData)
        {
            ClearCurrentItem();
            base.AcceptDrop(draggable, eventData);
        }

        /// <summary>
        /// Replaces the current visual content with the given item instance.
        /// </summary>
        public void SetPlacedVisual(RectTransform visual)
        {
            ClearCurrentItem();

            if (visual == null || itemContainer_ == null)
            {
                return;
            }

            visual.SetParent(itemContainer_, false);
            visual.anchorMin = new Vector2(0.5f, 0.5f);
            visual.anchorMax = new Vector2(0.5f, 0.5f);
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.anchoredPosition = Vector2.zero;
            visual.localScale = Vector3.one;
        }

        /// <summary>
        /// Clears any placed visual currently shown by this drop area.
        /// </summary>
        public void ClearPlacedVisual()
        {
            ClearCurrentItem();
        }

        private void OnShowInteractionPoints(ShowInteractionPointsEvent eventData)
        {
            SetVisible(eventData.InteractionPointType == SupportedInventoryType);
        }

        private void OnHideInteractionPoints(HideInteractionPointsEvent _)
        {
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            isVisible_ = isVisible;

            if (visibilityRoot_ != null)
            {
                visibilityRoot_.SetActive(isVisible);
            }
        }

        private void SubscribeToDispatcher()
        {
            if (subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Subscribe<ShowInteractionPointsEvent>(OnShowInteractionPoints);
            dispatcher_.Subscribe<HideInteractionPointsEvent>(OnHideInteractionPoints);
            subscribed_ = true;
        }

        private void UnsubscribeFromDispatcher()
        {
            if (!subscribed_ || dispatcher_ == null)
            {
                return;
            }

            dispatcher_.Unsubscribe<ShowInteractionPointsEvent>(OnShowInteractionPoints);
            dispatcher_.Unsubscribe<HideInteractionPointsEvent>(OnHideInteractionPoints);
            subscribed_ = false;
        }

        private void ClearCurrentItem()
        {
            if (itemContainer_ == null)
            {
                return;
            }

            for (int index = itemContainer_.childCount - 1; index >= 0; index--)
            {
                Destroy(itemContainer_.GetChild(index).gameObject);
            }
        }
    }
}
