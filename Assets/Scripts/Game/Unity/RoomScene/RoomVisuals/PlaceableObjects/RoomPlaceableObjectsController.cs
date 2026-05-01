using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Settings;
using Flowbit.Utilities.Core.Events;
using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.DragAndDrop;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted placeable room objects to the room scene visuals.
    /// </summary>
    public sealed class RoomPlaceableObjectsController
    {
        private readonly DataRepository repository_;
        private readonly EventDispatcher dispatcher_;
        private readonly IReadOnlyList<RoomPlaceableObjectSurfaceView> surfaceViews_;
        private readonly RoomSettings roomSettings_;
        private readonly IObjectInstantiator instantiator_;

        public RoomPlaceableObjectsController(
            DataRepository repository,
            EventDispatcher dispatcher,
            IReadOnlyList<RoomPlaceableObjectSurfaceView> surfaceViews,
            RoomSettings roomSettings,
            IObjectInstantiator instantiator)
        {
            repository_ = repository;
            dispatcher_ = dispatcher;
            surfaceViews_ = surfaceViews;
            roomSettings_ = roomSettings;
            instantiator_ = instantiator;
        }

        public void Refresh()
        {
            ClearAllPlaceableObjectAreas();

            List<PlacedRoomObjectLocation> placeableObjects = repository_?.CurrentProfile?.RoomData?.PlaceableObjects;
            if (placeableObjects == null)
            {
                return;
            }

            for (int index = 0; index < placeableObjects.Count; index++)
            {
                PlacedRoomObjectLocation location = placeableObjects[index];
                if (location?.Item == null)
                {
                    continue;
                }

                RoomPlaceableObjectSurfaceView surfaceView = FindSurfaceView(location.LocationId);
                if (surfaceView == null)
                {
                    continue;
                }

                RectTransform visualInstance = CreateConfiguredVisualInstance(
                    location.LocationId,
                    location.Item.ItemId,
                    enableRoomDrag: true);
                if (visualInstance == null)
                {
                    continue;
                }

                surfaceView.SetPlacedVisual(visualInstance);
            }
        }

        public void RefreshPlaceableObject(int locationId)
        {
            RoomPlaceableObjectSurfaceView surfaceView = FindSurfaceView(locationId);
            if (surfaceView == null)
            {
                return;
            }

            surfaceView.ClearPlacedVisual();

            PlacedRoomObjectLocation location = FindPlacedObjectLocation(locationId);
            if (location?.Item == null)
            {
                return;
            }

            RectTransform visualInstance = CreateConfiguredVisualInstance(
                location.LocationId,
                location.Item.ItemId,
                enableRoomDrag: true);
            if (visualInstance == null)
            {
                return;
            }

            surfaceView.SetPlacedVisual(visualInstance);
        }

        private void ClearAllPlaceableObjectAreas()
        {
            if (surfaceViews_ == null)
            {
                return;
            }

            for (int index = 0; index < surfaceViews_.Count; index++)
            {
                RoomPlaceableObjectSurfaceView surfaceView = surfaceViews_[index];
                if (surfaceView != null)
                {
                    surfaceView.ClearPlacedVisual();
                }
            }
        }

        private RoomPlaceableObjectSurfaceView FindSurfaceView(int targetId)
        {
            if (surfaceViews_ == null)
            {
                return null;
            }

            for (int index = 0; index < surfaceViews_.Count; index++)
            {
                RoomPlaceableObjectSurfaceView surfaceView = surfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }

                if (surfaceView.LocationId == targetId)
                {
                    return surfaceView;
                }
            }

            return null;
        }

        private RectTransform CreateVisualInstance(int itemId)
        {
            RectTransform prefab = roomSettings_?.ResolvePlaceableObjectPrefab(itemId);
            if (prefab == null)
            {
                return null;
            }

            RectTransform instance = instantiator_ != null
                ? instantiator_.InstantiatePrefab(prefab)
                : UnityEngine.Object.Instantiate(prefab);
            return instance;
        }

        private RectTransform CreateConfiguredVisualInstance(
            int locationId,
            int itemId,
            bool enableRoomDrag,
            Transform parentOverride = null,
            Vector2? sizeOverride = null)
        {
            RectTransform instance = CreateVisualInstance(itemId);
            if (instance == null)
            {
                return null;
            }

            if (parentOverride != null)
            {
                instance.SetParent(parentOverride, false);
                instance.SetAsLastSibling();
                instance.anchorMin = new Vector2(0.5f, 0.5f);
                instance.anchorMax = new Vector2(0.5f, 0.5f);
                instance.pivot = new Vector2(0.5f, 0.5f);
                instance.sizeDelta = sizeOverride ?? instance.rect.size;
                instance.anchoredPosition = Vector2.zero;
                instance.localScale = Vector3.one;
            }

            RoomPlaceableObjectView placeableObjectView = instance.GetComponent<RoomPlaceableObjectView>();
            if (placeableObjectView != null)
            {
                placeableObjectView.SetColor(Color.white);
                placeableObjectView.ApplyPlaceableItem(itemId);
                placeableObjectView.ConfigureTopLevel(locationId, itemId);

                if (enableRoomDrag)
                {
                    ConfigureRoomDrag(instance, placeableObjectView, locationId, itemId);
                }
            }

            return instance;
        }

        private void ConfigureRoomDrag(
            RectTransform visualInstance,
            RoomPlaceableObjectView placeableObjectView,
            int locationId,
            int itemId)
        {
            if (dispatcher_ == null || visualInstance == null || placeableObjectView == null)
            {
                return;
            }

            UIDragScrollRouter dragRouter = visualInstance.GetComponent<UIDragScrollRouter>();
            if (dragRouter == null)
            {
                dragRouter = visualInstance.gameObject.AddComponent<UIDragScrollRouter>();
            }

            CanvasGroup canvasGroup = visualInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = visualInstance.gameObject.AddComponent<CanvasGroup>();
            }

            RoomPlacedObjectDraggable draggable = visualInstance.GetComponent<RoomPlacedObjectDraggable>();
            if (draggable == null)
            {
                draggable = visualInstance.gameObject.AddComponent<RoomPlacedObjectDraggable>();
            }

            dragRouter.SetDraggable(draggable);
            dragRouter.SetScrollRect(null);
            draggable.SetDragRouter(dragRouter);
            draggable.Configure(
                dispatcher_,
                placeableObjectView,
                CreateDragData(itemId),
                locationId);
            draggable.SetDragRoot(ResolveDragVisualParent(visualInstance) as RectTransform);
            Vector2 dragVisualSize = ResolveRoomDragVisualSize(visualInstance);
            draggable.SetDragPointerOffset(ResolveRoomDragPointerOffset(dragVisualSize));
            draggable.SetDragVisualFactory(() =>
                CreateConfiguredVisualInstance(
                    locationId,
                    itemId,
                    enableRoomDrag: false,
                    parentOverride: ResolveDragVisualParent(visualInstance),
                    sizeOverride: dragVisualSize));
        }

        private static Transform ResolveDragVisualParent(RectTransform visualInstance)
        {
            if (visualInstance == null)
            {
                return null;
            }

            Canvas canvas = visualInstance.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
            {
                return canvas.rootCanvas.transform;
            }

            if (canvas != null)
            {
                return canvas.transform;
            }

            return visualInstance.parent;
        }

        private Vector2 ResolveRoomDragVisualSize(RectTransform visualInstance)
        {
            if (roomSettings_ != null)
            {
                Vector2 configuredSize = roomSettings_.PlaceableObjectDragVisualSize;
                if (configuredSize.x > 0f && configuredSize.y > 0f)
                {
                    return configuredSize;
                }
            }

            if (visualInstance == null)
            {
                return new Vector2(180f, 180f);
            }

            Vector2 fallbackSize = visualInstance.rect.size;
            if (fallbackSize.x <= 0f || fallbackSize.y <= 0f)
            {
                fallbackSize = visualInstance.sizeDelta;
            }

            if (fallbackSize.x <= 0f || fallbackSize.y <= 0f)
            {
                fallbackSize = new Vector2(180f, 180f);
            }

            return fallbackSize;
        }

        private Vector2 ResolveRoomDragPointerOffset(Vector2 dragVisualSize)
        {
            if (roomSettings_ != null)
            {
                Vector2 configuredOffset = roomSettings_.PlaceableObjectDragPointerOffset;
                if (configuredOffset != Vector2.zero)
                {
                    return configuredOffset;
                }
            }

            if (dragVisualSize.y <= 0f)
            {
                return new Vector2(0f, 120f);
            }

            float upwardOffset = Mathf.Max(120f, dragVisualSize.y * 0.35f);
            return new Vector2(0f, upwardOffset);
        }

        private static RoomInventoryItemData CreateDragData(int itemId)
        {
            return new RoomInventoryItemData
            {
                ItemId = itemId,
                InteractionPointType = InteractionPointType.PLACEABLE_OBJECT,
                Name = $"{RoomItemVisuals.GetCategoryDisplayName(InteractionPointType.PLACEABLE_OBJECT)} {itemId}",
                Color = RoomItemVisuals.GetItemColor(InteractionPointType.PLACEABLE_OBJECT, itemId),
                Quantity = 1
            };
        }

        private PlacedRoomObjectLocation FindPlacedObjectLocation(int locationId)
        {
            List<PlacedRoomObjectLocation> placeableObjects = repository_?.CurrentProfile?.RoomData?.PlaceableObjects;
            if (placeableObjects == null)
            {
                return null;
            }

            for (int index = 0; index < placeableObjects.Count; index++)
            {
                PlacedRoomObjectLocation location = placeableObjects[index];
                if (location != null && location.LocationId == locationId)
                {
                    return location;
                }
            }

            return null;
        }
    }
}
