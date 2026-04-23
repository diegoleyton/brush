using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Flowbit.Utilities.Unity.DragAndDrop
{
    /// <summary>
    /// Routes a pointer drag gesture either to a ScrollRect or to a UIDraggable.
    /// </summary>
    public sealed class UIDragScrollRouter : MonoBehaviour,
        IUIDragRouter,
        IPointerDownHandler,
        IPointerUpHandler,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        ICancelHandler
    {
        private enum GestureRoute
        {
            None,
            ScrollRect,
            Draggable
        }

        [SerializeField]
        private UIDraggable draggable_;

        [SerializeField]
        private ScrollRect scrollRect_;

        [SerializeField]
        private bool resolveAncestorScrollRectIfMissing_ = true;

        [Header("Hold To Drag")]
        [SerializeField]
        private bool enableHoldToDrag_ = true;

        [SerializeField]
        [Min(0f)]
        private float holdToDragDelaySeconds_ = 0.35f;

        [SerializeField]
        [Min(0f)]
        private float holdToDragMaxDistance_ = 18f;

        private GestureRoute activeRoute_;
        private bool isPointerDown_;
        private float pointerDownTime_;
        private Vector2 pointerDownPosition_;

        public bool IsRoutingEnabled => isActiveAndEnabled;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            isPointerDown_ = true;
            pointerDownTime_ = Time.unscaledTime;
            pointerDownPosition_ = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetPointerState();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            ScrollRect scrollRect = ResolveScrollRect();
            if (scrollRect != null)
            {
                ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            activeRoute_ = ResolveRoute(eventData);

            switch (activeRoute_)
            {
                case GestureRoute.ScrollRect:
                    ScrollRect scrollRect = ResolveScrollRect();
                    if (scrollRect != null)
                    {
                        ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
                    }
                    break;

                case GestureRoute.Draggable:
                    draggable_?.BeginDragFromRouter(eventData);
                    break;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            switch (activeRoute_)
            {
                case GestureRoute.ScrollRect:
                    ScrollRect scrollRect = ResolveScrollRect();
                    if (scrollRect != null)
                    {
                        ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
                    }
                    break;

                case GestureRoute.Draggable:
                    draggable_?.DragFromRouter(eventData);
                    break;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            switch (activeRoute_)
            {
                case GestureRoute.ScrollRect:
                    ScrollRect scrollRect = ResolveScrollRect();
                    if (scrollRect != null)
                    {
                        ExecuteEvents.Execute(scrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
                    }
                    break;

                case GestureRoute.Draggable:
                    draggable_?.EndDragFromRouter(eventData);
                    break;
            }

            activeRoute_ = GestureRoute.None;
            ResetPointerState();
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (activeRoute_ == GestureRoute.Draggable)
            {
                draggable_?.CancelDragFromRouter(eventData);
            }

            activeRoute_ = GestureRoute.None;
            ResetPointerState();
        }

        private GestureRoute ResolveRoute(PointerEventData eventData)
        {
            if (draggable_ == null)
            {
                return GestureRoute.ScrollRect;
            }

            ScrollRect scrollRect = ResolveScrollRect();
            if (scrollRect == null)
            {
                return GestureRoute.Draggable;
            }

            if (ShouldPrioritizeDraggableFromHold(eventData))
            {
                return GestureRoute.Draggable;
            }

            Vector2 delta = eventData.delta;
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            if (scrollRect.horizontal && !scrollRect.vertical)
            {
                return absX >= absY ? GestureRoute.ScrollRect : GestureRoute.Draggable;
            }

            if (scrollRect.vertical && !scrollRect.horizontal)
            {
                return absY >= absX ? GestureRoute.ScrollRect : GestureRoute.Draggable;
            }

            if (scrollRect.horizontal && scrollRect.vertical)
            {
                return GestureRoute.ScrollRect;
            }

            return GestureRoute.Draggable;
        }

        private ScrollRect ResolveScrollRect()
        {
            if (scrollRect_ != null)
            {
                return scrollRect_;
            }

            if (!resolveAncestorScrollRectIfMissing_)
            {
                return null;
            }

            scrollRect_ = GetComponentInParent<ScrollRect>();
            return scrollRect_;
        }

        private bool ShouldPrioritizeDraggableFromHold(PointerEventData eventData)
        {
            if (!enableHoldToDrag_ || !isPointerDown_)
            {
                return false;
            }

            if (Time.unscaledTime - pointerDownTime_ < holdToDragDelaySeconds_)
            {
                return false;
            }

            return Vector2.Distance(pointerDownPosition_, eventData.position) <= holdToDragMaxDistance_;
        }

        private void ResetPointerState()
        {
            isPointerDown_ = false;
            pointerDownTime_ = 0f;
            pointerDownPosition_ = Vector2.zero;
        }
    }
}
