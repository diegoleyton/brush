using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

using Flowbit.Utilities.Unity.AssetLoader;

using Game.Unity.Definitions;
using Game.Unity.Settings;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for the pet eyes surface.
    /// </summary>
    public sealed class PetEyesSurfaceView : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("eye_l")]
        private Image leftEye_;

        [SerializeField]
        [FormerlySerializedAs("eye_r")]
        private Image rightEye_;

        private IAssetLoader assetLoader_;
        private RoomSettings roomSettings_;
        private IAssetLoadHandle<Sprite> activeHandle_;
        private IAssetLoadHandle<Sprite> pendingHandle_;
        private int loadVersion_;

        [Inject]
        public void Construct(IAssetLoader assetLoader, RoomSettings roomSettings)
        {
            assetLoader_ = assetLoader;
            roomSettings_ = roomSettings;
        }

        private void OnDestroy()
        {
            ReleaseHandle(ref pendingHandle_);
            ReleaseHandle(ref activeHandle_);
        }

        public void ApplyEyes(int itemId)
        {
            if (assetLoader_ == null)
            {
                return;
            }

            int requestVersion = ++loadVersion_;
            StartLoad(itemId, requestVersion, allowFallback: true);
        }

        private void StartLoad(int itemId, int requestVersion, bool allowFallback)
        {
            ReleaseHandle(ref pendingHandle_);

            string assetName = AssetNameResolver.GetEyeAssetName(itemId);
            pendingHandle_ = assetLoader_.LoadAssetAsync<Sprite>(assetName, sprite =>
            {
                if (requestVersion != loadVersion_)
                {
                    return;
                }

                if (sprite != null)
                {
                    SetEyesSprite(sprite);
                    ReleaseHandle(ref activeHandle_);
                    activeHandle_ = pendingHandle_;
                    pendingHandle_ = null;
                    return;
                }

                pendingHandle_ = null;

                if (allowFallback && roomSettings_ != null && roomSettings_.DefaultEyesItemId != itemId)
                {
                    StartLoad(roomSettings_.DefaultEyesItemId, requestVersion, allowFallback: false);
                }
            });
        }

        private void SetEyesSprite(Sprite sprite)
        {
            if (leftEye_ != null)
            {
                leftEye_.sprite = sprite;
            }

            if (rightEye_ != null)
            {
                rightEye_.sprite = sprite;
            }
        }

        private static void ReleaseHandle(ref IAssetLoadHandle<Sprite> handle)
        {
            if (handle == null)
            {
                return;
            }

            handle.Release();
            handle = null;
        }
    }
}
