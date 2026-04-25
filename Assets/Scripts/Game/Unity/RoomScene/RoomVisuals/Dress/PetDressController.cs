using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Services;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted dress state to fixed pet dress surfaces.
    /// </summary>
    public sealed class PetDressController
    {
        private readonly IReadOnlyList<PetDressSurfaceView> dressSurfaceViews_;
        private readonly DataRepository repository_;

        public PetDressController(
            IReadOnlyList<PetDressSurfaceView> dressSurfaceViews,
            DataRepository repository)
        {
            dressSurfaceViews_ = dressSurfaceViews;
            repository_ = repository;
        }

        public void Refresh()
        {
            if (dressSurfaceViews_ == null)
            {
                return;
            }

            int dressItemId = repository_?.CurrentProfile?.PetData?.DressItemId
                ?? DefaultProfileState.DefaultPetDressItemId;

            for (int index = 0; index < dressSurfaceViews_.Count; index++)
            {
                PetDressSurfaceView surfaceView = dressSurfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }

                surfaceView.ResetColor();
                surfaceView.ApplyDressColor(RoomItemVisuals.GetItemColor(
                    Core.Data.InteractionPointType.DRESS,
                    dressItemId));
            }
        }

        public sealed class Factory : PlaceholderFactory<IReadOnlyList<PetDressSurfaceView>, PetDressController>
        {
        }
    }
}
