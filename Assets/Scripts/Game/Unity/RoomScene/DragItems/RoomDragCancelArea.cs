using System;

using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.DragAndDrop;
using Flowbit.Utilities.Unity.UI;

using Game.Unity.Definitions.Events;
using Game.Unity.Settings;

using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual region that cancels room inventory drags when released over it.
    /// </summary>
    public sealed class RoomDragCancelArea : MonoBehaviour, IUIDragCancelArea
    {
        private EventDispatcher dispatcher_;
        private RoomSettings roomSettings_;
        private RoomDropVisual visibilityController_;
        private bool subscribed_;
        private bool visible_;

        [SerializeField]
        private CanvasGroup canvasGroup_;

        [Inject]
        public void Construct(EventDispatcher dispatcher, RoomSettings roomSettings)
        {
            dispatcher_ = dispatcher;
            roomSettings_ = roomSettings;
        }

        private void Awake()
        {
            if (canvasGroup_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomDragCancelArea)} requires a {nameof(CanvasGroup)} reference.");
            }

            CreateVisibilityController();
            SetVisible(false);
            SubscribeToDispatcher();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDispatcher();
        }

        public bool CanCancel(UIDraggable draggable)
        {
            return visible_ && draggable is RoomInventoryDraggable;
        }

        public void OnDragHoverEntered(UIDraggable draggable)
        {
            if (visible_)
            {
                visibilityController_?.Highlight(true);
            }
        }

        public void OnDragHoverExited(UIDraggable draggable)
        {

            if (visible_)
            {
                visibilityController_?.Highlight(false);
            }
        }

        private void OnRoomInventoryDragStarted(RoomInventoryDragStartedEvent _)
        {
            SetVisible(true);
        }

        private void OnRoomInventoryDragEnded(RoomInventoryDragEndedEvent _)
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            visible_ = visible;
            canvasGroup_.alpha = visible ? 1f : 0f;
            canvasGroup_.interactable = visible;
            canvasGroup_.blocksRaycasts = visible;

            if (visibilityController_ == null)
            {
                return;
            }

            visibilityController_.gameObject.SetActive(visible);
            if (visible)
            {
                visibilityController_?.Highlight(false);
            }
        }

        private void CreateVisibilityController()
        {
            if (roomSettings_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomDragCancelArea)} requires a {nameof(RoomSettings)} binding.");
            }

            if (roomSettings_.RoomDropAreaVisibility == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomSettings)} requires a {nameof(RoomSettings.RoomDropAreaVisibility)} prefab reference.");
            }

            visibilityController_ = Instantiate(roomSettings_.RoomDropAreaVisibility, transform, false);
            visibilityController_?.HideBackground();

            RectTransform visibilityRectTransform = visibilityController_.transform as RectTransform;
            if (visibilityRectTransform == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomDragCancelArea)} visibility prefab must use a {nameof(RectTransform)}.");
            }

            visibilityRectTransform.anchorMin = Vector2.zero;
            visibilityRectTransform.anchorMax = Vector2.one;
            visibilityRectTransform.offsetMin = Vector2.zero;
            visibilityRectTransform.offsetMax = Vector2.zero;
            visibilityRectTransform.anchoredPosition = Vector2.zero;
            visibilityRectTransform.localScale = Vector3.one;
            visibilityRectTransform.SetAsLastSibling();
            DisableVisibilityRaycasts(visibilityController_.gameObject);
            visibilityController_?.Highlight(false);
            visibilityController_.gameObject.SetActive(false);
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

        private static void DisableVisibilityRaycasts(GameObject visibilityObject)
        {
            if (visibilityObject == null)
            {
                return;
            }

            CanvasGroup visibilityCanvasGroup = visibilityObject.GetComponent<CanvasGroup>();
            if (visibilityCanvasGroup == null)
            {
                visibilityCanvasGroup = visibilityObject.AddComponent<CanvasGroup>();
            }

            visibilityCanvasGroup.blocksRaycasts = false;
            visibilityCanvasGroup.interactable = false;

            Graphic[] graphics = visibilityObject.GetComponentsInChildren<Graphic>(true);
            for (int index = 0; index < graphics.Length; index++)
            {
                if (graphics[index] != null)
                {
                    graphics[index].raycastTarget = false;
                }
            }
        }
    }
}
