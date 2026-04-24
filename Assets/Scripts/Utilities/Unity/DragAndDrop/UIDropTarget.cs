using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Flowbit.Utilities.Unity.DragAndDrop
{
    /// <summary>
    /// Receives draggable UI elements dropped through the Unity event system.
    /// </summary>
    public abstract class UIDropTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
    {
        [SerializeField]
        private bool reparentDroppedItem_ = true;

        [SerializeField]
        private bool resetAnchoredPositionOnDrop_ = true;

        [SerializeField]
        private bool positionDroppedItemAtPointer_ = false;

        [SerializeField]
        private UnityEvent onDropAccepted_;

        /// <summary>
        /// Configures the target at runtime.
        /// </summary>
        public void Configure(
            bool reparentDroppedItem = true,
            bool resetAnchoredPositionOnDrop = true,
            bool positionDroppedItemAtPointer = false)
        {
            reparentDroppedItem_ = reparentDroppedItem;
            resetAnchoredPositionOnDrop_ = resetAnchoredPositionOnDrop;
            positionDroppedItemAtPointer_ = positionDroppedItemAtPointer;
        }

        /// <summary>
        /// Returns whether this target can currently accept the given draggable.
        /// </summary>
        public virtual bool CanAccept(UIDraggable draggable)
        {
            return draggable != null && isActiveAndEnabled;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            UIDraggable draggable = ResolveDraggable(eventData);
            draggable?.NotifyHoverEntered(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            UIDraggable draggable = ResolveDraggable(eventData);
            draggable?.NotifyHoverExited(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            UIDraggable draggable = ResolveDraggable(eventData);
            if (draggable == null || !draggable.TryAcceptDrop(this, eventData))
            {
                return;
            }

            onDropAccepted_?.Invoke();
            draggable.OnDropAccepted(this);
        }

        public virtual void AcceptDrop(UIDraggable draggable, PointerEventData eventData)
        {
            if (!reparentDroppedItem_ || draggable == null)
            {
                return;
            }

            RectTransform draggableTransform = draggable.DraggedRectTransform;
            RectTransform targetContainer = ResolveDropContainer();

            if (draggableTransform == null || targetContainer == null)
            {
                return;
            }

            draggableTransform.SetParent(targetContainer, false);
            draggableTransform.SetAsLastSibling();

            if (resetAnchoredPositionOnDrop_)
            {
                if (positionDroppedItemAtPointer_ &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        targetContainer,
                        eventData.position,
                        eventData.pressEventCamera,
                        out Vector2 localPoint))
                {
                    draggableTransform.anchoredPosition = localPoint;
                }
                else
                {
                    draggableTransform.anchoredPosition = Vector2.zero;
                }
            }
        }

        protected virtual RectTransform ResolveDropContainer()
        {
            return transform as RectTransform;
        }

        private static UIDraggable ResolveDraggable(PointerEventData eventData)
        {
            GameObject draggedObject = eventData?.pointerDrag;
            if (draggedObject == null)
            {
                return null;
            }

            return draggedObject.GetComponentInParent<UIDraggable>();
        }
    }
}
