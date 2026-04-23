using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.DragAndDrop;

using Game.Core.Data;
using Game.Core.Events;

using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Shared base behavior for room drop areas that react to the selected inventory category.
    /// </summary>
    public abstract class RoomDropAreaBase : UIDropTarget
    {
        protected EventDispatcher dispatcher_;
        protected IRoomInventorySelectionState selectionState_;

        [SerializeField]
        private RectTransform itemContainer_;

        [SerializeField]
        private GameObject visibilityRoot_;

        private bool interactionVisible_;
        private bool subscribed_;

        [Inject]
        public void Construct(EventDispatcher dispatcher, IRoomInventorySelectionState selectionState)
        {
            dispatcher_ = dispatcher;
            selectionState_ = selectionState;
        }

        protected virtual void Awake()
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
            interactionVisible_ = ShouldShowForInteractionType(selectionState_?.CurrentInteractionPointType);
            UpdateVisibility();
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        public override bool CanAccept(UIDraggable draggable)
        {
            return interactionVisible_ && IsAdditionalVisibilitySatisfied() && base.CanAccept(draggable) && itemContainer_ != null;
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

        protected void RefreshVisibility()
        {
            UpdateVisibility();
        }

        protected abstract bool ShouldShowForInteractionType(InteractionPointType? interactionPointType);

        protected virtual bool IsAdditionalVisibilitySatisfied()
        {
            return true;
        }

        private void OnShowInteractionPoints(ShowInteractionPointsEvent eventData)
        {
            interactionVisible_ = ShouldShowForInteractionType(eventData.InteractionPointType);
            UpdateVisibility();
        }

        private void OnHideInteractionPoints(HideInteractionPointsEvent _)
        {
            interactionVisible_ = false;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (visibilityRoot_ != null)
            {
                visibilityRoot_.SetActive(interactionVisible_ && IsAdditionalVisibilitySatisfied());
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
                Object.Destroy(itemContainer_.GetChild(index).gameObject);
            }
        }
    }
}
