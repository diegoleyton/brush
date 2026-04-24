using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Services;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted skin state to fixed pet skin surfaces.
    /// </summary>
    public sealed class PetSkinController
    {
        private readonly IReadOnlyList<PetSkinSurfaceView> skinSurfaceViews_;
        private readonly DataRepository repository_;

        public PetSkinController(
            IReadOnlyList<PetSkinSurfaceView> skinSurfaceViews,
            DataRepository repository)
        {
            skinSurfaceViews_ = skinSurfaceViews;
            repository_ = repository;
        }

        public void Refresh()
        {
            if (skinSurfaceViews_ == null)
            {
                return;
            }

            int skinItemId = repository_?.CurrentProfile?.PetData?.SkinItemId
                ?? DefaultProfileState.DefaultPetSkinItemId;

            for (int index = 0; index < skinSurfaceViews_.Count; index++)
            {
                PetSkinSurfaceView surfaceView = skinSurfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }

                surfaceView.ResetColor();
                surfaceView.ApplySkinColor(RoomItemVisuals.GetItemColor(
                    Core.Data.InteractionPointType.SKIN,
                    skinItemId));
            }
        }
    }
}
