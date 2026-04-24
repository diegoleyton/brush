using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Services;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted face state to fixed room face surfaces.
    /// </summary>
    public sealed class PetFaceController
    {
        private readonly IReadOnlyList<PetFaceSurfaceView> faceSurfaceViews_;
        private readonly DataRepository repository_;

        public PetFaceController(
            IReadOnlyList<PetFaceSurfaceView> faceSurfaceViews,
            DataRepository repository)
        {
            faceSurfaceViews_ = faceSurfaceViews;
            repository_ = repository;
        }

        public void Refresh()
        {
            if (faceSurfaceViews_ == null)
            {
                return;
            }

            int faceItemId = repository_?.CurrentProfile?.PetData?.FaceItemId
                ?? DefaultProfileState.DefaultPetFaceItemId;

            for (int index = 0; index < faceSurfaceViews_.Count; index++)
            {
                PetFaceSurfaceView surfaceView = faceSurfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }

                surfaceView.ResetColor();
                surfaceView.ApplyFaceColor(RoomItemVisuals.GetItemColor(
                    Core.Data.InteractionPointType.FACE,
                    faceItemId));
            }
        }
    }
}
