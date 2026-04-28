using Flowbit.Utilities.Unity.ScrollableList;
using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.AssetLoader;

using Game.Core.Data;
using Game.Unity.Definitions;
using Game.Unity.Settings;

using UnityEngine;
using UnityEngine.UI;

using System;
using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual-only room inventory item used in the scene prototype.
    /// </summary>
    public sealed class RoomInventoryItemData
    {
        public int ItemId;
        public InteractionPointType InteractionPointType;
        public string Name;
        public Color Color;
        public int Quantity;
    }

    /// <summary>
    /// Recycled list element used by the room inventory list.
    /// </summary>
    public sealed class RoomInventoryListElement : BaseScrollableListElement<RoomInventoryItemData>
    {
        private RoomInventoryItemData data_;
        private IObjectInstantiator instantiator_;
        private IAssetLoader assetLoader_;
        private RoomSettings roomSettings_;
        private IAssetLoadHandle<Sprite> activeHandle_;
        private IAssetLoadHandle<Sprite> pendingHandle_;
        private int loadVersion_;
        private Sprite defaultSprite_;

        [SerializeField]
        private Image image_;

        [SerializeField]
        private Image extraImage1_;

        [SerializeField]
        private Image extraImage2_;

        [SerializeField]
        private RoomInventoryDraggable draggable_;

        [SerializeField]
        private RoomDragVisual dragVisualPrefab_;

        [SerializeField]
        private Text quantityText_;

        [SerializeField]
        private GameObject quantityTextContainer_;

        [Inject]
        public void Construct(
            IAssetLoader assetLoader,
            RoomSettings roomSettings,
            [Inject(Id = InstantiatorIds.Dependency)] IObjectInstantiator instantiator)
        {
            assetLoader_ = assetLoader;
            roomSettings_ = roomSettings;
            instantiator_ = instantiator;
        }

        private void OnDestroy()
        {
            ReleaseHandle(ref pendingHandle_);
            ReleaseHandle(ref activeHandle_);
        }

        private void Awake()
        {
            if (image_ != null)
            {
                defaultSprite_ = image_.sprite;
            }
        }

        public override void Show(RoomInventoryItemData data)
        {
            data_ = data;
            gameObject.SetActive(true);
            InvalidatePendingVisualLoads();
            ResetVisualState();

            if (image_ != null)
            {
                if (data.InteractionPointType == InteractionPointType.PLACEABLE_OBJECT)
                {
                    ApplyPlaceableItem(data.ItemId);
                }
                else if (data.InteractionPointType == InteractionPointType.PAINT)
                {
                    ApplyPaintItem(data.Color);
                }
                else if (data.InteractionPointType == InteractionPointType.SKIN)
                {
                    ApplySkinItem(data.Color);
                }
                else if (data.InteractionPointType == InteractionPointType.EYES)
                {
                    ApplyEyes(data.ItemId);
                }
                else
                {
                    image_.enabled = true;
                    image_.sprite = defaultSprite_;
                    SetImageAlpha(1f);
                    image_.color = data.Color;
                }
            }

            if (quantityText_ != null)
            {
                bool shouldShowQuantity = data.Quantity >= 0;
                quantityTextContainer_.SetActive(shouldShowQuantity);
                if (shouldShowQuantity)
                {
                    quantityText_.text = data.Quantity.ToString();
                }
            }

            if (draggable_ == null)
            {
                throw new Exception("Missing draggable");
            }

            draggable_.SetData(data);
            draggable_.SetReturnToOriginOnSuccessfulDrop(true);

            if (dragVisualPrefab_ != null)
            {
                draggable_.SetDragVisualFactory(CreateDragVisualInstance);
            }
            else
            {
                draggable_.SetDragVisualFactory(null);
            }
        }

        public override void Hide()
        {
            data_ = null;

            if (draggable_ != null)
            {
                if (!draggable_.IsDragging)
                {
                    draggable_.CleanupDragPresentation();
                }

                draggable_.ClearData();
                draggable_.SetDragVisualFactory(null);
            }

            InvalidatePendingVisualLoads();
            ResetVisualState();

            if (quantityText_ != null)
            {
                quantityTextContainer_.SetActive(false);
                quantityText_.text = string.Empty;
            }

            gameObject.SetActive(false);
        }

        private void ConfigureDragVisualInstance(RoomDragVisual dragVisualInstance)
        {
            if (dragVisualInstance == null || data_ == null)
            {
                return;
            }

            if (data_.InteractionPointType == InteractionPointType.PLACEABLE_OBJECT)
            {
                dragVisualInstance.SetColor(Color.white);

                if (image_ != null && image_.sprite != null)
                {
                    dragVisualInstance.SetSprite(image_.sprite);
                    return;
                }

                dragVisualInstance.ApplyPlaceableItem(data_.ItemId);
                return;
            }

            if (data_.InteractionPointType == InteractionPointType.EYES)
            {
                if (extraImage1_ != null && extraImage1_.sprite != null)
                {
                    dragVisualInstance.SetEyesSprite(extraImage1_.sprite);
                    return;
                }

                dragVisualInstance.ApplyEyes(data_.ItemId);
                return;
            }

            if (data_.InteractionPointType == InteractionPointType.PAINT)
            {
                dragVisualInstance.ApplyPaintItem(data_.Color);
                return;
            }

            if (data_.InteractionPointType == InteractionPointType.SKIN)
            {
                dragVisualInstance.ApplySkinItem(data_.Color);
                return;
            }

            dragVisualInstance.SetSprite(null);
            dragVisualInstance.SetColor(data_.Color);
        }

        private RectTransform CreateDragVisualInstance()
        {
            if (dragVisualPrefab_ == null)
            {
                return null;
            }

            Transform parent = ResolveDragVisualParent();
            RoomDragVisual dragVisualInstance = instantiator_ != null
                ? instantiator_.InstantiatePrefab(dragVisualPrefab_, parent)
                : Instantiate(dragVisualPrefab_, parent, false);
            ConfigureDragVisualInstance(dragVisualInstance);
            return dragVisualInstance.transform as RectTransform;
        }

        private Transform ResolveDragVisualParent()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
            {
                return canvas.rootCanvas.transform;
            }

            if (canvas != null)
            {
                return canvas.transform;
            }

            return transform.parent;
        }

        private void ApplyPlaceableItem(int itemId)
        {
            if (assetLoader_ == null || image_ == null)
            {
                return;
            }

            int requestVersion = ++loadVersion_;
            ReleaseHandle(ref pendingHandle_);
            image_.color = Color.white;
            image_.sprite = defaultSprite_;
            image_.enabled = true;
            SetImageAlpha(0f);

            string assetName = AssetNameResolver.GetPlaceableItemAssetName(itemId);
            pendingHandle_ = assetLoader_.LoadAssetAsync<Sprite>(assetName, sprite =>
            {
                if (requestVersion != loadVersion_)
                {
                    return;
                }

                if (sprite == null)
                {
                    ShowMainImageFallback();
                    pendingHandle_ = null;
                    return;
                }

                image_.sprite = sprite;
                image_.enabled = true;
                SetImageAlpha(1f);
                ReleaseHandle(ref activeHandle_);
                activeHandle_ = pendingHandle_;
                pendingHandle_ = null;
            });
        }

        private void ApplyPaintItem(Color color)
        {
            if (image_ == null)
            {
                return;
            }

            if (roomSettings_ == null || roomSettings_.PaintItemSprite == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomInventoryListElement)} requires {nameof(RoomSettings)}.{nameof(RoomSettings.PaintItemSprite)} to render paint items.");
            }

            ApplyTintedMainImage(roomSettings_.PaintItemSprite, color);
        }

        private void ApplySkinItem(Color color)
        {
            if (image_ == null)
            {
                return;
            }

            if (roomSettings_ == null || roomSettings_.SkinItemSprite == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(RoomInventoryListElement)} requires {nameof(RoomSettings)}.{nameof(RoomSettings.SkinItemSprite)} to render skin items.");
            }

            ApplyTintedMainImage(roomSettings_.SkinItemSprite, color);
        }

        private void ApplyTintedMainImage(Sprite sprite, Color color)
        {
            SetExtraImagesAlpha(0f);
            image_.sprite = sprite;
            image_.enabled = true;
            image_.color = color;
            SetImageAlpha(1f);
        }

        private void ApplyEyes(int itemId)
        {
            if (assetLoader_ == null)
            {
                return;
            }

            SetExtraImagesAlpha(0f);
            if (image_ != null)
            {
                image_.sprite = defaultSprite_;
                image_.enabled = true;
                image_.color = Color.white;
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
                    return;
                }

                ShowMainImageFallback();
            });
        }

        private void SetEyesSprite(Sprite sprite)
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
                SetImageAlpha(0f);
            }
        }

        private void ResetVisualState()
        {
            if (image_ != null)
            {
                image_.sprite = defaultSprite_;
                image_.enabled = true;
                image_.color = Color.white;
                SetImageAlpha(1f);
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

        private void ShowMainImageFallback()
        {
            if (image_ == null)
            {
                return;
            }

            image_.sprite = defaultSprite_;
            image_.enabled = true;
            image_.color = Color.white;
            SetImageAlpha(1f);
            SetExtraImagesAlpha(0f);
        }

        private void InvalidatePendingVisualLoads()
        {
            loadVersion_++;
            ReleaseHandle(ref pendingHandle_);
            ReleaseHandle(ref activeHandle_);
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
