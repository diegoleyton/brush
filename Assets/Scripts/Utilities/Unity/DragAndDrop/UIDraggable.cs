using System;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Flowbit.Utilities.Unity.DragAndDrop
{
    /// <summary>
    /// Makes a UI element draggable through the Unity event system.
    /// Supports cancellation and optional event dispatching.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIDraggable : MonoBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        ICancelHandler
    {
        [Header("Dragging")]
        [SerializeField]
        private RectTransform dragRoot_;

        [SerializeField]
        private bool reparentToDragRootWhileDragging_ = true;

        [SerializeField]
        private bool cancelWithEscapeKey_ = true;

        [SerializeField]
        private Vector2 dragPointerOffset_;

        [Header("Behaviour")]
        [SerializeField]
        private bool returnToOriginOnCancel_ = true;

        [SerializeField]
        private bool returnToOriginOnFailedDrop_ = true;

        [SerializeField]
        private bool returnToOriginOnSuccessfulDrop_ = false;

        [SerializeField]
        private CanvasGroup canvasGroup_;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onDragStarted_;

        [SerializeField]
        private UnityEvent onDragCompleted_;

        [SerializeField]
        private UnityEvent onDragCancelled_;

        private RectTransform rectTransform_;
        private RectTransform activeDragRoot_;
        private RectTransform activeDraggedRectTransform_;
        private GameObject activeDragVisualInstance_;
        private Func<RectTransform> dragVisualFactory_;
        private IUIDragRouter dragRouter_;

        private Transform originalParent_;
        private int originalSiblingIndex_;
        private Vector3 originalWorldPosition_;
        private Vector3 originalLocalScale_;
        private Quaternion originalLocalRotation_;
        private Vector2 originalAnchoredPosition_;
        private Vector2 originalSizeDelta_;
        private Vector2 originalAnchorMin_;
        private Vector2 originalAnchorMax_;
        private Vector2 originalPivot_;
        private bool originalBlocksRaycasts_;

        private UIDropTarget hoveredTarget_;
        private bool dropAccepted_;
        private bool isDragging_;
        private bool usedDragVisual_;
        private bool wasCancelledByCancelArea_;

        /// <summary>
        /// Gets whether this element is currently being dragged.
        /// </summary>
        public bool IsDragging => isDragging_;

        /// <summary>
        /// Gets the currently hovered drop target, if any.
        /// </summary>
        public UIDropTarget HoveredTarget => hoveredTarget_;

        /// <summary>
        /// Gets whether the active drag was released over a cancel area.
        /// </summary>
        protected bool WasCancelledByCancelArea => wasCancelledByCancelArea_;

        internal RectTransform DraggedRectTransform =>
            activeDraggedRectTransform_ != null ? activeDraggedRectTransform_ : rectTransform_;

        private void Awake()
        {
            rectTransform_ = GetComponent<RectTransform>();
            dragRouter_ = ResolveDragRouter();

            if (canvasGroup_ == null)
            {
                canvasGroup_ = GetComponent<CanvasGroup>();
            }

            if (canvasGroup_ == null)
            {
                canvasGroup_ = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Update()
        {
            if (isDragging_ && cancelWithEscapeKey_ && Input.GetKeyDown(KeyCode.Escape))
            {
                CancelDrag();
            }
        }

        private void OnDisable()
        {
            if (isDragging_)
            {
                if (dropAccepted_)
                {
                    FinishSuccessfulDrop();
                    return;
                }

                CancelDrag(DragCancelReason.Disabled);
            }
        }

        /// <summary>
        /// Sets the drag root used while dragging.
        /// </summary>
        public void SetDragRoot(RectTransform dragRoot)
        {
            dragRoot_ = dragRoot;
        }

        /// <summary>
        /// Sets a screen-space offset applied while following the pointer during drag.
        /// Positive Y moves the dragged visual upward on screen.
        /// </summary>
        public void SetDragPointerOffset(Vector2 dragPointerOffset)
        {
            dragPointerOffset_ = dragPointerOffset;
        }

        /// <summary>
        /// Sets the drag router used to delegate gesture routing for this draggable.
        /// </summary>
        public void SetDragRouter(IUIDragRouter dragRouter)
        {
            dragRouter_ = dragRouter;
        }

        /// <summary>
        /// Sets the optional factory used to create the dragged visual instead of moving the source object.
        /// </summary>
        public void SetDragVisualFactory(Func<RectTransform> dragVisualFactory)
        {
            dragVisualFactory_ = dragVisualFactory;
        }

        /// <summary>
        /// Sets whether a successful drop should still restore the original transform.
        /// Useful for list items that should stay in place while using a separate drag visual.
        /// </summary>
        public void SetReturnToOriginOnSuccessfulDrop(bool returnToOriginOnSuccessfulDrop)
        {
            returnToOriginOnSuccessfulDrop_ = returnToOriginOnSuccessfulDrop;
        }

        /// <summary>
        /// Cleans up any transient drag presentation before the source view is recycled.
        /// Does not dispatch drag lifecycle events.
        /// </summary>
        public void CleanupDragPresentation()
        {
            if (activeDragVisualInstance_ != null)
            {
                DestroyActiveDragVisual();
            }

            if (!isDragging_ && originalParent_ == null)
            {
                return;
            }

            if (!usedDragVisual_)
            {
                RestoreOriginalTransform();
            }
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (ShouldUseExternalGestureRouter())
            {
                return;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (ShouldUseExternalGestureRouter())
            {
                return;
            }

            BeginDragFromRouter(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ShouldUseExternalGestureRouter())
            {
                return;
            }

            DragFromRouter(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ShouldUseExternalGestureRouter())
            {
                return;
            }

            EndDragFromRouter(eventData);
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (ShouldUseExternalGestureRouter())
            {
                return;
            }

            CancelDragFromRouter(eventData);
        }

        /// <summary>
        /// Starts a drag gesture delegated by an external router.
        /// </summary>
        public void BeginDragFromRouter(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (isDragging_)
            {
                return;
            }

            BeginDrag(eventData);
        }

        /// <summary>
        /// Continues a drag gesture delegated by an external router.
        /// </summary>
        public void DragFromRouter(PointerEventData eventData)
        {
            if (!isDragging_)
            {
                return;
            }

            MoveToPointer(eventData);
        }

        /// <summary>
        /// Ends a drag gesture delegated by an external router.
        /// </summary>
        public void EndDragFromRouter(PointerEventData eventData)
        {
            if (!isDragging_)
            {
                return;
            }

            if (dropAccepted_)
            {
                FinishSuccessfulDrop();
                return;
            }

            if (wasCancelledByCancelArea_)
            {
                CancelDrag(DragCancelReason.CancelArea);
                return;
            }

            if (activeDragVisualInstance_ != null)
            {
                CancelDrag(DragCancelReason.FailedDrop);
                return;
            }

            if (returnToOriginOnFailedDrop_)
            {
                CancelDrag(DragCancelReason.FailedDrop);
                return;
            }

            FinishDragState();
        }

        /// <summary>
        /// Cancels a drag gesture delegated by an external router.
        /// </summary>
        public void CancelDragFromRouter(BaseEventData eventData)
        {
            if (isDragging_)
            {
                CancelDrag(DragCancelReason.EventSystemCancel);
            }
        }

        /// <summary>
        /// Cancels the active drag, restoring the original position when configured.
        /// </summary>
        public void CancelDrag()
        {
            CancelDrag(DragCancelReason.Manual);
        }

        internal void NotifyHoverEntered(UIDropTarget target)
        {
            if (!isDragging_ || target == hoveredTarget_)
            {
                return;
            }

            hoveredTarget_ = target;
        }

        internal void NotifyHoverExited(UIDropTarget target)
        {
            if (!isDragging_ || hoveredTarget_ != target)
            {
                return;
            }

            hoveredTarget_ = null;
        }

        internal bool TryAcceptDrop(UIDropTarget target, PointerEventData eventData)
        {
            if (!isDragging_ || target == null || !target.CanAccept(this))
            {
                return false;
            }

            hoveredTarget_ = target;
            dropAccepted_ = true;
            target.AcceptDrop(this, eventData);
            return true;
        }

        internal void NotifyCancelledByCancelArea()
        {
            if (!isDragging_)
            {
                return;
            }

            wasCancelledByCancelArea_ = true;
            OnDragCancelledByCancelArea();
        }

        /// <summary>
        /// Called after this draggable has been accepted by a drop target.
        /// </summary>
        public virtual void OnDropAccepted(UIDropTarget target, PointerEventData eventData)
        {
        }

        /// <summary>
        /// Called when dragging starts.
        /// </summary>
        public virtual void OnDragStarted()
        {
        }

        /// <summary>
        /// Called when dragging ends, either successfully or unsuccessfully.
        /// </summary>
        public virtual void OnDragEnded(bool dropAccepted)
        {
        }

        /// <summary>
        /// Called when the active drag is released over a cancel area.
        /// </summary>
        public virtual void OnDragCancelledByCancelArea()
        {
        }

        private void BeginDrag(PointerEventData eventData)
        {
            if (isDragging_)
            {
                return;
            }

            activeDragRoot_ = ResolveDragRoot();
            originalParent_ = rectTransform_.parent;
            originalSiblingIndex_ = rectTransform_.GetSiblingIndex();
            originalWorldPosition_ = rectTransform_.position;
            originalLocalScale_ = rectTransform_.localScale;
            originalLocalRotation_ = rectTransform_.localRotation;
            originalAnchoredPosition_ = rectTransform_.anchoredPosition;
            originalSizeDelta_ = rectTransform_.sizeDelta;
            originalAnchorMin_ = rectTransform_.anchorMin;
            originalAnchorMax_ = rectTransform_.anchorMax;
            originalPivot_ = rectTransform_.pivot;
            originalBlocksRaycasts_ = canvasGroup_ != null && canvasGroup_.blocksRaycasts;

            hoveredTarget_ = null;
            dropAccepted_ = false;
            isDragging_ = true;
            usedDragVisual_ = ShouldUseDragVisualFactory();

            if (usedDragVisual_)
            {
                activeDraggedRectTransform_ = CreateDragVisualInstance();
            }
            else
            {
                activeDraggedRectTransform_ = rectTransform_;
            }

            if (!ShouldUseDragVisualFactory() && canvasGroup_ != null)
            {
                canvasGroup_.blocksRaycasts = false;
            }

            if (!ShouldUseDragVisualFactory() && reparentToDragRootWhileDragging_ && activeDragRoot_ != null)
            {
                rectTransform_.SetParent(activeDragRoot_, true);
                rectTransform_.SetAsLastSibling();
            }

            MoveToPointer(eventData);
            OnDragStarted();
            onDragStarted_?.Invoke();
        }

        private void CancelDrag(DragCancelReason reason)
        {
            if (!isDragging_)
            {
                return;
            }

            if (usedDragVisual_)
            {
                if (activeDragVisualInstance_ != null)
                {
                    DestroyActiveDragVisual();
                }
            }
            else if (ShouldRestoreOriginalTransformOnCancel(reason))
            {
                RestoreOriginalTransform();
            }

            OnDragEnded(false);
            FinishDragState();
            onDragCancelled_?.Invoke();
        }

        private void FinishSuccessfulDrop()
        {
            if (usedDragVisual_)
            {
                if (activeDragVisualInstance_ != null)
                {
                    DestroyActiveDragVisual();
                }
            }
            else if (returnToOriginOnSuccessfulDrop_)
            {
                RestoreOriginalTransform();
            }

            OnDragEnded(true);
            FinishDragState();
            onDragCompleted_?.Invoke();
        }

        private void FinishDragState()
        {
            isDragging_ = false;
            dropAccepted_ = false;
            hoveredTarget_ = null;
            activeDragRoot_ = null;
            activeDraggedRectTransform_ = null;
            activeDragVisualInstance_ = null;
            usedDragVisual_ = false;
            wasCancelledByCancelArea_ = false;

            if (canvasGroup_ != null)
            {
                canvasGroup_.blocksRaycasts = originalBlocksRaycasts_;
            }
        }

        private bool ShouldRestoreOriginalTransformOnCancel(DragCancelReason reason)
        {
            switch (reason)
            {
                case DragCancelReason.FailedDrop:
                    return returnToOriginOnFailedDrop_;
                case DragCancelReason.CancelArea:
                    return returnToOriginOnCancel_;
                default:
                    return returnToOriginOnCancel_;
            }
        }

        private void RestoreOriginalTransform()
        {
            rectTransform_.SetParent(originalParent_, false);
            rectTransform_.anchorMin = originalAnchorMin_;
            rectTransform_.anchorMax = originalAnchorMax_;
            rectTransform_.pivot = originalPivot_;
            rectTransform_.sizeDelta = originalSizeDelta_;
            rectTransform_.anchoredPosition = originalAnchoredPosition_;
            rectTransform_.SetSiblingIndex(originalSiblingIndex_);
            rectTransform_.localScale = originalLocalScale_;
            rectTransform_.localRotation = originalLocalRotation_;
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            RectTransform draggedRectTransform = DraggedRectTransform;
            RectTransform dragRoot = activeDragRoot_ != null ? activeDragRoot_ : draggedRectTransform.parent as RectTransform;
            if (dragRoot == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    dragRoot,
                    eventData.position + dragPointerOffset_,
                    eventData.pressEventCamera,
                    out Vector3 worldPoint))
            {
                draggedRectTransform.position = worldPoint;
            }
        }

        private RectTransform ResolveDragRoot()
        {
            if (dragRoot_ != null && dragRoot_ != rectTransform_)
            {
                return dragRoot_;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
            {
                return canvas.rootCanvas.transform as RectTransform;
            }

            return rectTransform_.parent as RectTransform;
        }

        private bool ShouldUseDragVisualFactory()
        {
            return dragVisualFactory_ != null;
        }

        private bool ShouldUseExternalGestureRouter()
        {
            return dragRouter_ != null && dragRouter_.IsRoutingEnabled;
        }

        private IUIDragRouter ResolveDragRouter()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                MonoBehaviour behaviour = behaviours[index];
                if (behaviour is IUIDragRouter dragRouter)
                {
                    return dragRouter;
                }
            }

            return null;
        }

        private RectTransform CreateDragVisualInstance()
        {
            if (!ShouldUseDragVisualFactory())
            {
                return rectTransform_;
            }

            RectTransform draggedRectTransform = dragVisualFactory_();

            if (draggedRectTransform == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(UIDraggable)} drag visual factory returned null.");
            }

            activeDragVisualInstance_ = draggedRectTransform.gameObject;
            activeDragVisualInstance_.SetActive(true);

            CanvasGroup dragCanvasGroup = activeDragVisualInstance_.GetComponent<CanvasGroup>();
            if (dragCanvasGroup == null)
            {
                dragCanvasGroup = activeDragVisualInstance_.AddComponent<CanvasGroup>();
            }

            dragCanvasGroup.blocksRaycasts = false;
            draggedRectTransform.SetAsLastSibling();

            return draggedRectTransform;
        }

        private void DestroyActiveDragVisual()
        {
            if (activeDragVisualInstance_ == null)
            {
                return;
            }

            Destroy(activeDragVisualInstance_);
            activeDragVisualInstance_ = null;
        }
    }
}
