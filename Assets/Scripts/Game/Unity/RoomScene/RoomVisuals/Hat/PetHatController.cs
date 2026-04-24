using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Services;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted hat state to fixed pet hat surfaces.
    /// </summary>
    public sealed class PetHatController
    {
        private readonly IReadOnlyList<PetHatSurfaceView> hatSurfaceViews_;
        private readonly DataRepository repository_;

        public PetHatController(
            IReadOnlyList<PetHatSurfaceView> hatSurfaceViews,
            DataRepository repository)
        {
            hatSurfaceViews_ = hatSurfaceViews;
            repository_ = repository;
        }

        public void Refresh()
        {
            if (hatSurfaceViews_ == null)
            {
                return;
            }

            int hatItemId = repository_?.CurrentProfile?.PetData?.HatItemId
                ?? DefaultProfileState.DefaultPetHatItemId;

            for (int index = 0; index < hatSurfaceViews_.Count; index++)
            {
                PetHatSurfaceView surfaceView = hatSurfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }

                surfaceView.ResetColor();
                surfaceView.ApplyHatColor(RoomItemVisuals.GetItemColor(
                    Core.Data.InteractionPointType.HAT,
                    hatItemId));
            }
        }
    }
}
