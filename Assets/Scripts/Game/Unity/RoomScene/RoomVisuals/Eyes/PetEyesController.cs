using System.Collections.Generic;

using Game.Core.Configuration;
using Game.Core.Services;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Applies persisted eyes state to pet eyes surfaces.
    /// </summary>
    public sealed class PetEyesController
    {
        private readonly IReadOnlyList<PetEyesSurfaceView> eyesSurfaceViews_;
        private readonly DataRepository repository_;

        public PetEyesController(
            IReadOnlyList<PetEyesSurfaceView> eyesSurfaceViews,
            DataRepository repository)
        {
            eyesSurfaceViews_ = eyesSurfaceViews;
            repository_ = repository;
        }

        public void Refresh()
        {
            if (eyesSurfaceViews_ == null)
            {
                return;
            }

            int eyesItemId = repository_?.CurrentProfile?.PetData?.EyesItemId
                ?? DefaultProfileState.DefaultPetEyesItemId;

            for (int index = 0; index < eyesSurfaceViews_.Count; index++)
            {
                PetEyesSurfaceView surfaceView = eyesSurfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }

                surfaceView.ApplyEyes(eyesItemId);
            }
        }

        public sealed class Factory : PlaceholderFactory<IReadOnlyList<PetEyesSurfaceView>, PetEyesController>
        {
        }
    }
}
