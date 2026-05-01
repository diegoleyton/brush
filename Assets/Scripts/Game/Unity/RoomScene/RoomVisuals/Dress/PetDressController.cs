using System.Collections.Generic;

using Game.Core.Services;
using Game.Unity.Settings;

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
        private readonly RoomSettings roomSettings_;

        public PetDressController(
            IReadOnlyList<PetDressSurfaceView> dressSurfaceViews,
            DataRepository repository,
            RoomSettings roomSettings)
        {
            dressSurfaceViews_ = dressSurfaceViews;
            repository_ = repository;
            roomSettings_ = roomSettings;
        }

        public void Refresh()
        {
            if (dressSurfaceViews_ == null)
            {
                return;
            }

            int dressItemId = repository_?.CurrentProfile?.PetData?.DressItemId
                ?? (roomSettings_ != null ? roomSettings_.DefaultDressItemId : 1);

            for (int index = 0; index < dressSurfaceViews_.Count; index++)
            {
                PetDressSurfaceView surfaceView = dressSurfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }

                surfaceView.ApplyDress(dressItemId);
            }
        }

        public sealed class Factory : PlaceholderFactory<IReadOnlyList<PetDressSurfaceView>, PetDressController>
        {
        }
    }
}
