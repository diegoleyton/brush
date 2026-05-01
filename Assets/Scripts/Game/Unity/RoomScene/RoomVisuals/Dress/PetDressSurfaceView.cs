using UnityEngine;
using UnityEngine.UI;

using Flowbit.Utilities.Unity.AssetLoader;

using Game.Unity.Definitions;
using Game.Unity.Settings;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for the pet dress surface.
    /// </summary>
    public sealed class PetDressSurfaceView : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

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

        public void ApplyDress(int itemId)
        {
            if (image_ == null)
            {
                return;
            }

            if (itemId <= 0)
            {
                loadVersion_++;
                ReleaseHandle(ref pendingHandle_);
                ReleaseHandle(ref activeHandle_);
                image_.enabled = false;
                image_.sprite = null;
                return;
            }

            image_.enabled = true;

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

            string assetName = AssetNameResolver.GetDressAssetName(itemId);
            pendingHandle_ = assetLoader_.LoadAssetAsync<Sprite>(assetName, sprite =>
            {
                if (requestVersion != loadVersion_)
                {
                    return;
                }

                if (sprite != null)
                {
                    image_.sprite = sprite;
                    ReleaseHandle(ref activeHandle_);
                    activeHandle_ = pendingHandle_;
                    pendingHandle_ = null;
                    return;
                }

                pendingHandle_ = null;

                if (allowFallback &&
                    roomSettings_ != null &&
                    roomSettings_.DefaultDressItemId != itemId)
                {
                    StartLoad(roomSettings_.DefaultDressItemId, requestVersion, allowFallback: false);
                }
            });
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
