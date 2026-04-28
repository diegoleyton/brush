using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Services;
using Game.Unity.Settings;
using Flowbit.Utilities.Unity.Instantiator;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted placeable room objects to the room scene visuals.
    /// </summary>
    public sealed class RoomPlaceableObjectsController
    {
        private readonly DataRepository repository_;
        private readonly IReadOnlyList<RoomPlaceableObjectSurfaceView> surfaceViews_;
        private readonly RoomSettings roomSettings_;
        private readonly IObjectInstantiator instantiator_;

        public RoomPlaceableObjectsController(
            DataRepository repository,
            IReadOnlyList<RoomPlaceableObjectSurfaceView> surfaceViews,
            RoomSettings roomSettings,
            IObjectInstantiator instantiator)
        {
            repository_ = repository;
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

                RectTransform visualInstance = CreateVisualInstance(location.Item.ItemId);
                if (visualInstance == null)
                {
                    continue;
                }

                RoomPlaceableObjectView placeableObjectView = visualInstance.GetComponent<RoomPlaceableObjectView>();
                if (placeableObjectView != null)
                {
                    placeableObjectView.SetColor(Color.white);
                    placeableObjectView.ApplyPlaceableItem(location.Item.ItemId);
                    placeableObjectView.ConfigureTopLevel(location.LocationId, location.Item.ItemId);
                }

                surfaceView.SetPlacedVisual(visualInstance);
                ApplyChildObjects(location, placeableObjectView);
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

            RectTransform visualInstance = CreateVisualInstance(location.Item.ItemId);
            if (visualInstance == null)
            {
                return;
            }

            RoomPlaceableObjectView placeableObjectView = visualInstance.GetComponent<RoomPlaceableObjectView>();
            if (placeableObjectView != null)
            {
                placeableObjectView.SetColor(Color.white);
                placeableObjectView.ApplyPlaceableItem(location.Item.ItemId);
                placeableObjectView.ConfigureTopLevel(location.LocationId, location.Item.ItemId);
            }

            surfaceView.SetPlacedVisual(visualInstance);
            ApplyChildObjects(location, placeableObjectView);
        }

        public void RefreshChildPlaceableObject(int locationId, int slotId)
        {
            PlacedRoomObjectLocation location = FindPlacedObjectLocation(locationId);
            if (location?.Item == null)
            {
                return;
            }

            RoomPlaceableObjectSurfaceView surfaceView = FindSurfaceView(locationId);
            RoomPlaceableObjectView parentView = surfaceView?.GetPlacedVisualComponent<RoomPlaceableObjectView>();
            if (parentView == null)
            {
                return;
            }

            RoomPlaceableChildSurfaceView childSurfaceView = parentView.FindChildSurfaceView(slotId);
            if (childSurfaceView == null)
            {
                return;
            }

            childSurfaceView.ClearPlacedVisual();

            PlacedChildObjectSlot slot = FindChildSlot(location, slotId);
            if (slot?.Item == null)
            {
                return;
            }

            RectTransform childVisual = CreateVisualInstance(slot.Item.ItemId);
            if (childVisual == null)
            {
                return;
            }

            RoomPlaceableObjectView childView = childVisual.GetComponent<RoomPlaceableObjectView>();
            if (childView != null)
            {
                childView.SetColor(Color.white);
                childView.ApplyPlaceableItem(slot.Item.ItemId);
                childView.ConfigureChild(locationId, slotId, slot.Item.ItemId);
            }

            childSurfaceView.SetPlacedVisual(childVisual);
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

        private void ApplyChildObjects(PlacedRoomObjectLocation location, RoomPlaceableObjectView parentView)
        {
            if (location?.Item?.ChildObjects == null || parentView == null)
            {
                return;
            }

            for (int index = 0; index < location.Item.ChildObjects.Count; index++)
            {
                PlacedChildObjectSlot slot = location.Item.ChildObjects[index];
                if (slot?.Item == null)
                {
                    continue;
                }

                RoomPlaceableChildSurfaceView childSurfaceView = parentView.FindChildSurfaceView(slot.SlotId);
                if (childSurfaceView == null)
                {
                    continue;
                }

                RectTransform childVisual = CreateVisualInstance(slot.Item.ItemId);
                if (childVisual == null)
                {
                    continue;
                }

                RoomPlaceableObjectView childView = childVisual.GetComponent<RoomPlaceableObjectView>();
                if (childView != null)
                {
                    childView.SetColor(Color.white);
                    childView.ApplyPlaceableItem(slot.Item.ItemId);
                    childView.ConfigureChild(location.LocationId, slot.SlotId, slot.Item.ItemId);
                }
                childSurfaceView.SetPlacedVisual(childVisual);
            }
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

        private static PlacedChildObjectSlot FindChildSlot(PlacedRoomObjectLocation location, int slotId)
        {
            if (location?.Item?.ChildObjects == null)
            {
                return null;
            }

            for (int index = 0; index < location.Item.ChildObjects.Count; index++)
            {
                PlacedChildObjectSlot slot = location.Item.ChildObjects[index];
                if (slot != null && slot.SlotId == slotId)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
