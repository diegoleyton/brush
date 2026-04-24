using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Data;
using Game.Core.Services;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted paint state to fixed room surfaces and paintable room objects.
    /// </summary>
    public sealed class RoomPaintController
    {
        private readonly DataRepository repository_;
        private readonly IReadOnlyList<RoomPlaceableObjectSurfaceView> surfaceViews_;
        private readonly IReadOnlyList<RoomPaintSurfaceView> paintSurfaceViews_;

        public RoomPaintController(
            DataRepository repository,
            IReadOnlyList<RoomPlaceableObjectSurfaceView> surfaceViews,
            IReadOnlyList<RoomPaintSurfaceView> paintSurfaceViews)
        {
            repository_ = repository;
            surfaceViews_ = surfaceViews;
            paintSurfaceViews_ = paintSurfaceViews;
        }

        public void Refresh()
        {
            RefreshPaintSurfaces();
            RefreshPaintableObjects();
        }

        public void RefreshPaintSurface(int surfaceId)
        {
            RoomPaintSurfaceView surfaceView = FindPaintSurfaceView(surfaceId);
            if (surfaceView == null)
            {
                return;
            }

            surfaceView.ResetColor();

            RoomPaintSurfaceState surface = FindPaintSurfaceState(surfaceId);
            if (surface == null)
            {
                return;
            }

            surfaceView.ApplyPaintColor(
                RoomItemVisuals.GetItemColor(InteractionPointType.PAINT, surface.PaintId));
        }

        public void RefreshPaintedObject(int locationId)
        {
            PlacedRoomObjectLocation location = FindPlacedObjectLocation(locationId);
            if (location?.Item == null)
            {
                return;
            }

            RoomPlaceableObjectSurfaceView surfaceView = FindPlaceableObjectSurfaceView(locationId);
            RoomPlaceableObjectView parentView = surfaceView?.GetPlacedVisualComponent<RoomPlaceableObjectView>();
            if (parentView == null)
            {
                return;
            }

            ApplyPaintToView(parentView, location.Item.PaintId);
        }

        public void RefreshPaintedChildObject(int locationId, int slotId)
        {
            PlacedRoomObjectLocation location = FindPlacedObjectLocation(locationId);
            if (location?.Item == null)
            {
                return;
            }

            RoomPlaceableObjectSurfaceView surfaceView = FindPlaceableObjectSurfaceView(locationId);
            RoomPlaceableObjectView parentView = surfaceView?.GetPlacedVisualComponent<RoomPlaceableObjectView>();
            if (parentView == null)
            {
                return;
            }

            RoomPlaceableChildSurfaceView childSurfaceView = parentView.FindChildSurfaceView(slotId);
            RoomPlaceableObjectView childView = childSurfaceView?.GetPlacedVisualComponent<RoomPlaceableObjectView>();
            if (childView == null)
            {
                return;
            }

            PlacedChildObjectSlot slot = FindChildSlot(location, slotId);
            if (slot?.Item == null)
            {
                return;
            }

            ApplyPaintToView(childView, slot.Item.PaintId);
        }

        private void RefreshPaintSurfaces()
        {
            if (paintSurfaceViews_ == null)
            {
                return;
            }

            for (int index = 0; index < paintSurfaceViews_.Count; index++)
            {
                paintSurfaceViews_[index]?.ResetColor();
            }

            List<RoomPaintSurfaceState> paintedSurfaces = repository_?.CurrentProfile?.RoomData?.PaintedSurfaces;
            if (paintedSurfaces == null)
            {
                return;
            }

            for (int index = 0; index < paintedSurfaces.Count; index++)
            {
                RoomPaintSurfaceState surface = paintedSurfaces[index];
                if (surface == null)
                {
                    continue;
                }

                RoomPaintSurfaceView surfaceView = FindPaintSurfaceView(surface.SurfaceId);
                if (surfaceView == null)
                {
                    continue;
                }

                surfaceView.ApplyPaintColor(
                    RoomItemVisuals.GetItemColor(InteractionPointType.PAINT, surface.PaintId));
            }
        }

        private void RefreshPaintableObjects()
        {
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

                RoomPlaceableObjectSurfaceView surfaceView = FindPlaceableObjectSurfaceView(location.LocationId);
                RoomPlaceableObjectView parentView = surfaceView?.GetPlacedVisualComponent<RoomPlaceableObjectView>();
                if (parentView == null)
                {
                    continue;
                }

                ApplyPaintToView(parentView, location.Item.PaintId);
                ApplyChildObjectPaint(location, parentView);
            }
        }

        private void ApplyChildObjectPaint(PlacedRoomObjectLocation location, RoomPlaceableObjectView parentView)
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
                RoomPlaceableObjectView childView = childSurfaceView?.GetPlacedVisualComponent<RoomPlaceableObjectView>();
                if (childView == null)
                {
                    continue;
                }

                ApplyPaintToView(childView, slot.Item.PaintId);
            }
        }

        private static void ApplyPaintToView(RoomPlaceableObjectView view, int paintItemId)
        {
            if (view == null || paintItemId == DefaultProfileState.NoPaintItemId)
            {
                return;
            }

            view.SetColor(RoomItemVisuals.GetItemColor(InteractionPointType.PAINT, paintItemId));
        }

        private RoomPlaceableObjectSurfaceView FindPlaceableObjectSurfaceView(int targetId)
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

        private RoomPaintSurfaceView FindPaintSurfaceView(int surfaceId)
        {
            if (paintSurfaceViews_ == null)
            {
                return null;
            }

            for (int index = 0; index < paintSurfaceViews_.Count; index++)
            {
                RoomPaintSurfaceView surfaceView = paintSurfaceViews_[index];
                if (surfaceView != null && surfaceView.SurfaceId == surfaceId)
                {
                    return surfaceView;
                }
            }

            return null;
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

        private RoomPaintSurfaceState FindPaintSurfaceState(int surfaceId)
        {
            List<RoomPaintSurfaceState> paintedSurfaces = repository_?.CurrentProfile?.RoomData?.PaintedSurfaces;
            if (paintedSurfaces == null)
            {
                return null;
            }

            for (int index = 0; index < paintedSurfaces.Count; index++)
            {
                RoomPaintSurfaceState surface = paintedSurfaces[index];
                if (surface != null && surface.SurfaceId == surfaceId)
                {
                    return surface;
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
