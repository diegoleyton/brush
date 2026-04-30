using System.Collections.Generic;

using Game.Core.Data;
using Game.Core.Services;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted paint state to fixed room surfaces.
    /// </summary>
    public sealed class RoomPaintController
    {
        private readonly DataRepository repository_;
        private readonly IReadOnlyList<RoomPaintSurfaceView> paintSurfaceViews_;

        public RoomPaintController(
            DataRepository repository,
            IReadOnlyList<RoomPaintSurfaceView> paintSurfaceViews)
        {
            repository_ = repository;
            paintSurfaceViews_ = paintSurfaceViews;
        }

        public void Refresh()
        {
            RefreshPaintSurfaces();
        }

        public void RefreshPaintSurface(int surfaceId, Vector2? dropScreenPosition = null)
        {
            RoomPaintSurfaceView surfaceView = FindPaintSurfaceView(surfaceId);
            if (surfaceView == null)
            {
                return;
            }

            RoomPaintSurfaceState surface = FindPaintSurfaceState(surfaceId);
            if (surface == null)
            {
                surfaceView.ResetColor();
                return;
            }

            surfaceView.ApplyPaintColor(
                RoomItemVisuals.GetItemColor(InteractionPointType.PAINT, surface.PaintId),
                animate: true,
                dropScreenPosition: dropScreenPosition);
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
                    RoomItemVisuals.GetItemColor(InteractionPointType.PAINT, surface.PaintId),
                    animate: false);
            }
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
    }
}
