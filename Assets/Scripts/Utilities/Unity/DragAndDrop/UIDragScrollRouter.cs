using System.Collections.Generic;

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
        IPointerMoveHandler,
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
        private bool holdDragActivated_;
        private float pointerDownTime_;
        private Vector2 pointerDownPosition_;
        private Vector2 lastPointerPosition_;
        private Camera pressEventCamera_;
        private UIDropTarget hoveredDropTarget_;
        private IUIDragCancelArea hoveredCancelArea_;

        public bool IsRoutingEnabled => isActiveAndEnabled;

        private void OnDisable()
        {
            activeRoute_ = GestureRoute.None;
            hoveredDropTarget_ = null;
            ResetPointerState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            isPointerDown_ = true;
            pointerDownTime_ = Time.unscaledTime;
            pointerDownPosition_ = eventData.position;
            lastPointerPosition_ = eventData.position;
            pressEventCamera_ = eventData.pressEventCamera;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (holdDragActivated_ && activeRoute_ == GestureRoute.Draggable)
            {
                EndHoldDrag(eventData);
                return;
            }

            ResetPointerState();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            lastPointerPosition_ = eventData.position;
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
            if (holdDragActivated_)
            {
                return;
            }

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
            if (holdDragActivated_)
            {
                lastPointerPosition_ = eventData.position;
                return;
            }

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
            if (holdDragActivated_)
            {
                return;
            }

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
                    FinalizeDraggableRoute(eventData);
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

        private void Update()
        {
            UpdatePointerPositionFromMouse();

            if (!holdDragActivated_ && ShouldStartHoldDrag())
            {
                BeginHoldDrag();
            }

            if (activeRoute_ != GestureRoute.Draggable || draggable_ == null || !draggable_.IsDragging)
            {
                return;
            }

            PointerEventData eventData = CreatePointerEventData();
            draggable_.DragFromRouter(eventData);
            UpdateHoveredDropTarget(eventData);

            if (!Input.GetMouseButton(0))
            {
                FinalizeDraggableRoute(eventData);
                activeRoute_ = GestureRoute.None;
                ResetPointerState();
            }
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

        private bool ShouldStartHoldDrag()
        {
            if (!enableHoldToDrag_ || !isPointerDown_ || holdDragActivated_ || draggable_ == null)
            {
                return false;
            }

            if (Time.unscaledTime - pointerDownTime_ < holdToDragDelaySeconds_)
            {
                return false;
            }

            return Vector2.Distance(pointerDownPosition_, lastPointerPosition_) <= holdToDragMaxDistance_;
        }

        private void BeginHoldDrag()
        {
            if (draggable_ == null || draggable_.IsDragging)
            {
                return;
            }

            holdDragActivated_ = true;
            activeRoute_ = GestureRoute.Draggable;
            draggable_.BeginDragFromRouter(CreatePointerEventData());
            UpdateHoveredDropTarget(CreatePointerEventData());
        }

        private void EndHoldDrag(PointerEventData eventData)
        {
            FinalizeDraggableRoute(eventData);
            activeRoute_ = GestureRoute.None;
            ResetPointerState();
        }

        private void FinalizeDraggableRoute(PointerEventData eventData)
        {
            PointerEventData finalEventData = CreatePointerEventData(eventData);
            ExecuteDropIfAvailable(finalEventData);
            draggable_?.EndDragFromRouter(finalEventData);
        }

        private void ExecuteDropIfAvailable(PointerEventData eventData)
        {
            UIDropTarget dropTarget = FindDropTarget(eventData);

            if (dropTarget == null)
            {
                return;
            }

            ExecuteEvents.Execute(dropTarget.gameObject, eventData, ExecuteEvents.dropHandler);
        }

        private void UpdateHoveredDropTarget(PointerEventData eventData)
        {
            IUIDragCancelArea cancelArea = FindCancelArea(eventData);
            if (!ReferenceEquals(cancelArea, hoveredCancelArea_))
            {
                if (hoveredCancelArea_ != null)
                {
                    hoveredCancelArea_.OnDragHoverExited(draggable_);
                }

                hoveredCancelArea_ = cancelArea;

                if (hoveredCancelArea_ != null)
                {
                    hoveredCancelArea_.OnDragHoverEntered(draggable_);
                }
            }

            UIDropTarget dropTarget = FindDropTarget(eventData);
            if (dropTarget == hoveredDropTarget_)
            {
                return;
            }

            if (hoveredDropTarget_ != null)
            {
                draggable_?.NotifyHoverExited(hoveredDropTarget_);
                hoveredDropTarget_.OnDragHoverExited(draggable_);
            }

            hoveredDropTarget_ = dropTarget;

            if (hoveredDropTarget_ != null)
            {
                draggable_?.NotifyHoverEntered(hoveredDropTarget_);
                hoveredDropTarget_.OnDragHoverEntered(draggable_);
            }
        }

        private UIDropTarget FindDropTarget(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            for (int index = 0; index < raycastResults.Count; index++)
            {
                IUIDragCancelArea cancelArea =
                    raycastResults[index].gameObject.GetComponentInParent<IUIDragCancelArea>();
                if (cancelArea != null && draggable_ != null && cancelArea.CanCancel(draggable_))
                {
                    return null;
                }

                UIDropTarget dropTarget =
                    raycastResults[index].gameObject.GetComponentInParent<UIDropTarget>();
                if (dropTarget != null)
                {
                    if (draggable_ != null && dropTarget.CanAccept(draggable_))
                    {
                        return dropTarget;
                    }
                }
            }

            return null;
        }

        private IUIDragCancelArea FindCancelArea(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            for (int index = 0; index < raycastResults.Count; index++)
            {
                IUIDragCancelArea cancelArea =
                    raycastResults[index].gameObject.GetComponentInParent<IUIDragCancelArea>();
                if (cancelArea != null && draggable_ != null && cancelArea.CanCancel(draggable_))
                {
                    return cancelArea;
                }
            }

            return null;
        }

        private PointerEventData CreatePointerEventData(PointerEventData source = null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = source != null ? source.position : lastPointerPosition_,
                pressPosition = pointerDownPosition_,
                pointerDrag = draggable_ != null ? draggable_.gameObject : null,
                pointerCurrentRaycast = source != null ? source.pointerCurrentRaycast : default,
                pointerPressRaycast = source != null ? source.pointerPressRaycast : default,
                eligibleForClick = false,
                dragging = true,
                useDragThreshold = false
            };

            return eventData;
        }

        private void UpdatePointerPositionFromMouse()
        {
            if (!Input.mousePresent)
            {
                return;
            }

            if (!isPointerDown_ && activeRoute_ != GestureRoute.Draggable)
            {
                return;
            }

            lastPointerPosition_ = Input.mousePosition;
        }

        private void ResetPointerState()
        {
            if (hoveredDropTarget_ != null)
            {
                draggable_?.NotifyHoverExited(hoveredDropTarget_);
                hoveredDropTarget_.OnDragHoverExited(draggable_);
            }

            if (hoveredCancelArea_ != null)
            {
                hoveredCancelArea_.OnDragHoverExited(draggable_);
            }

            isPointerDown_ = false;
            holdDragActivated_ = false;
            pointerDownTime_ = 0f;
            pointerDownPosition_ = Vector2.zero;
            lastPointerPosition_ = Vector2.zero;
            pressEventCamera_ = null;
            hoveredDropTarget_ = null;
            hoveredCancelArea_ = null;
        }
    }
}
