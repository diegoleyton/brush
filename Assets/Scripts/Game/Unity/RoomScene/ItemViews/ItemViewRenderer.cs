using System;

using Flowbit.Utilities.Unity.AssetLoader;

using Game.Unity.Settings;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Owns the shared graphics and asset-loading state used by item view strategies.
    /// </summary>
    public sealed class ItemViewRenderer : IDisposable
    {
        private readonly Image mainImage_;
        private readonly Image extraImage1_;
        private readonly Image extraImage2_;
        private readonly IAssetLoader assetLoader_;
        private readonly RoomSettings roomSettings_;
        private readonly string ownerName_;
        private readonly Sprite defaultSprite_;

        private IAssetLoadHandle<Sprite> activeHandle_;
        private IAssetLoadHandle<Sprite> pendingHandle_;
        private int loadVersion_;

        public ItemViewRenderer(
            Image mainImage,
            Image extraImage1,
            Image extraImage2,
            IAssetLoader assetLoader,
            RoomSettings roomSettings,
            string ownerName)
        {
            mainImage_ = mainImage;
            extraImage1_ = extraImage1;
            extraImage2_ = extraImage2;
            assetLoader_ = assetLoader;
            roomSettings_ = roomSettings;
            ownerName_ = ownerName;
            defaultSprite_ = mainImage != null ? mainImage.sprite : null;
        }

        public RoomSettings RoomSettings => roomSettings_;

        public void Dispose()
        {
            ReleaseHandles();
        }

        public void Reset()
        {
            loadVersion_++;
            ReleaseHandles();

            if (mainImage_ != null)
            {
                mainImage_.sprite = defaultSprite_;
                mainImage_.enabled = true;
                mainImage_.color = Color.white;
                SetMainAlpha(1f);
            }

            if (extraImage1_ != null)
            {
                extraImage1_.sprite = null;
            }

            if (extraImage2_ != null)
            {
                extraImage2_.sprite = null;
            }

            SetExtraImagesAlpha(0f);
        }

        public void SetDefaultTint(Color color)
        {
            SetExtraImagesAlpha(0f);

            if (mainImage_ == null)
            {
                return;
            }

            mainImage_.sprite = defaultSprite_;
            mainImage_.enabled = true;
            mainImage_.color = color;
            SetMainAlpha(1f);
        }

        public void SetTintedMainSprite(Sprite sprite, Color color, string settingName)
        {
            if (mainImage_ == null)
            {
                return;
            }

            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"{ownerName_} requires {nameof(RoomSettings)}.{settingName} to render this item.");
            }

            SetExtraImagesAlpha(0f);
            mainImage_.sprite = sprite;
            mainImage_.enabled = true;
            mainImage_.color = color;
            SetMainAlpha(1f);
        }

        public void SetMainSprite(Sprite sprite, Color color)
        {
            if (mainImage_ == null)
            {
                return;
            }

            mainImage_.sprite = sprite != null ? sprite : defaultSprite_;
            mainImage_.enabled = true;
            mainImage_.color = color;
            SetMainAlpha(1f);
            SetExtraImagesAlpha(0f);
        }

        public void LoadMainSprite(
            string assetName,
            bool startTransparentUntilLoaded,
            bool showFallbackOnMissing)
        {
            if (assetLoader_ == null || mainImage_ == null)
            {
                return;
            }

            int requestVersion = ++loadVersion_;
            ReleaseHandle(ref pendingHandle_);
            mainImage_.color = Color.white;
            mainImage_.sprite = defaultSprite_;
            mainImage_.enabled = true;
            SetMainAlpha(startTransparentUntilLoaded ? 0f : 1f);
            SetExtraImagesAlpha(0f);

            pendingHandle_ = assetLoader_.LoadAssetAsync<Sprite>(assetName, sprite =>
            {
                if (requestVersion != loadVersion_)
                {
                    return;
                }

                if (sprite == null)
                {
                    pendingHandle_ = null;
                    if (showFallbackOnMissing)
                    {
                        ShowMainImageFallback();
                    }

                    return;
                }

                mainImage_.sprite = sprite;
                mainImage_.enabled = true;
                mainImage_.color = Color.white;
                SetMainAlpha(1f);
                ReleaseHandle(ref activeHandle_);
                activeHandle_ = pendingHandle_;
                pendingHandle_ = null;
            });
        }

        public void LoadSprite(string assetName, Action<Sprite> onLoaded)
        {
            if (assetLoader_ == null)
            {
                onLoaded?.Invoke(null);
                return;
            }

            int requestVersion = ++loadVersion_;
            ReleaseHandle(ref pendingHandle_);
            pendingHandle_ = assetLoader_.LoadAssetAsync<Sprite>(assetName, sprite =>
            {
                if (requestVersion != loadVersion_)
                {
                    return;
                }

                if (sprite != null)
                {
                    ReleaseHandle(ref activeHandle_);
                    activeHandle_ = pendingHandle_;
                }

                pendingHandle_ = null;
                onLoaded?.Invoke(sprite);
            });
        }

        public void ShowMainImageFallback()
        {
            if (mainImage_ == null)
            {
                return;
            }

            mainImage_.sprite = defaultSprite_;
            mainImage_.enabled = true;
            mainImage_.color = Color.white;
            SetMainAlpha(1f);
            SetExtraImagesAlpha(0f);
        }

        public void SetExtraSprites(Sprite extraSprite1, Sprite extraSprite2)
        {
            if (extraImage1_ != null)
            {
                extraImage1_.sprite = extraSprite1;
            }

            if (extraImage2_ != null)
            {
                extraImage2_.sprite = extraSprite2;
            }
        }

        public void SetExtraImagesAlpha(float alpha)
        {
            SetGraphicAlpha(extraImage1_, alpha);
            SetGraphicAlpha(extraImage2_, alpha);
        }

        public void SetMainAlpha(float alpha)
        {
            SetGraphicAlpha(mainImage_, alpha);
        }

        private void ReleaseHandles()
        {
            ReleaseHandle(ref pendingHandle_);
            ReleaseHandle(ref activeHandle_);
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
