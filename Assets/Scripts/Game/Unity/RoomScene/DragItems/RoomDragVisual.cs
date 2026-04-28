using UnityEngine;
using UnityEngine.UI;

using Flowbit.Utilities.Unity.AssetLoader;

using Game.Unity.Definitions;
using Game.Unity.Settings;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Simple visual component used by spawned room drag visuals.
    /// </summary>
    public sealed class RoomDragVisual : MonoBehaviour
    {
        private Sprite defaultSprite_;

        [SerializeField]
        private Image image_;

        [SerializeField]
        private Image extraImage1_;

        [SerializeField]
        private Image extraImage2_;

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

        private void Awake()
        {
            if (image_ != null)
            {
                defaultSprite_ = image_.sprite;
            }
        }

        private void OnDestroy()
        {
            ReleaseHandle(ref pendingHandle_);
            ReleaseHandle(ref activeHandle_);
        }

        /// <summary>
        /// Applies the given color to the configured graphic.
        /// </summary>
        public void SetColor(Color color)
        {
            if (image_ != null)
            {
                SetExtraImagesAlpha(0f);
                image_.enabled = true;
                SetImageAlpha(1f);
                image_.color = color;
            }
        }

        public void SetSprite(Sprite sprite)
        {
            if (image_ != null)
            {
                SetExtraImagesAlpha(0f);
                image_.sprite = sprite;
                image_.enabled = sprite != null;
                SetImageAlpha(sprite != null ? 1f : 0f);
            }
        }

        public void SetEyesSprite(Sprite sprite)
        {
            if (extraImage1_ != null)
            {
                extraImage1_.sprite = sprite;
            }

            if (extraImage2_ != null)
            {
                extraImage2_.sprite = sprite;
            }

            SetExtraImagesAlpha(sprite != null ? 1f : 0f);

            if (image_ != null)
            {
                image_.enabled = true;
                SetImageAlpha(0f);
                image_.sprite = defaultSprite_;
            }
        }

        public void ApplyPlaceableItem(int itemId)
        {
            if (assetLoader_ == null || image_ == null)
            {
                return;
            }

            int requestVersion = ++loadVersion_;
            ReleaseHandle(ref pendingHandle_);
            SetExtraImagesAlpha(0f);
            image_.sprite = defaultSprite_;
            image_.enabled = true;
            image_.color = Color.white;
            SetImageAlpha(1f);

            string assetName = AssetNameResolver.GetPlaceableItemAssetName(itemId);
            pendingHandle_ = assetLoader_.LoadAssetAsync<Sprite>(assetName, sprite =>
            {
                if (requestVersion != loadVersion_)
                {
                    return;
                }

                if (sprite == null)
                {
                    pendingHandle_ = null;
                    return;
                }

                image_.sprite = sprite;
                image_.enabled = true;
                ReleaseHandle(ref activeHandle_);
                activeHandle_ = pendingHandle_;
                pendingHandle_ = null;
            });
        }

        public void ApplyEyes(int itemId)
        {
            if (assetLoader_ == null)
            {
                return;
            }

            SetExtraImagesAlpha(0f);
            if (image_ != null)
            {
                image_.sprite = defaultSprite_;
                image_.color = Color.white;
                image_.enabled = true;
                SetImageAlpha(1f);
            }

            int requestVersion = ++loadVersion_;
            StartEyesLoad(itemId, requestVersion, allowFallback: true);
        }

        private void StartEyesLoad(int itemId, int requestVersion, bool allowFallback)
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
                    StartEyesLoad(roomSettings_.DefaultEyesItemId, requestVersion, allowFallback: false);
                }
            });
        }

        private void SetExtraImagesAlpha(float alpha)
        {
            SetGraphicAlpha(extraImage1_, alpha);
            SetGraphicAlpha(extraImage2_, alpha);
        }

        private void SetImageAlpha(float alpha)
        {
            SetGraphicAlpha(image_, alpha);
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
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
