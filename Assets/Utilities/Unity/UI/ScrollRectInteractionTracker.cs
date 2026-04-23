using UnityEngine;
using UnityEngine.EventSystems;

namespace Flowbit.Utilities.Unity.UI
{
    /// <summary>
    /// Tracks user interaction with a ScrollRect (dragging and scrolling).
    /// This can be used to temporarily disable automatic behaviors (such as auto-scroll)
    /// while the user is actively interacting with the scroll view.
    /// </summary>
    public class ScrollRectInteractionTracker : MonoBehaviour,
        IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        /// <summary>
        /// Gets a value indicating whether the user is currently dragging the ScrollRect.
        /// </summary>
        public bool IsDragging { get; private set; }

        /// <summary>
        /// Gets the last time (in seconds since startup) when the user interacted with the ScrollRect.
        /// This is updated on drag end and scroll events.
        /// </summary>
        public float LastInteractionTime { get; private set; }

        /// <summary>
        /// Called when the user starts dragging the ScrollRect.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragging = true;
        }

        /// <summary>
        /// Called when the user stops dragging the ScrollRect.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
            LastInteractionTime = Time.time;
        }

        /// <summary>
        /// Called when the user scrolls using the mouse wheel or similar input.
        /// </summary>
        public void OnScroll(PointerEventData eventData)
        {
            LastInteractionTime = Time.time;
        }
    }
}