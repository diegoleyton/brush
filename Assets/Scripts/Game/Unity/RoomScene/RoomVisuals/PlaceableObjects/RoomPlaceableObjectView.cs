using UnityEngine;
using UnityEngine.UI;

using Flowbit.Utilities.Unity.AssetLoader;
using Zenject;
using Game.Unity.Definitions;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component used by placed room object instances.
    /// </summary>
    public sealed class RoomPlaceableObjectView : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        private IAssetLoader assetLoader_;
        private IAssetLoadHandle<Sprite> activeHandle_;
        private IAssetLoadHandle<Sprite> pendingHandle_;
        private int loadVersion_;
        private CanvasGroup canvasGroup_;

        private RoomDropArea[] dropAreas_;
        [Inject]
        public void Construct(IAssetLoader assetLoader)
        {
            assetLoader_ = assetLoader;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDestroy()
        {
            ReleaseHandle(ref pendingHandle_);
            ReleaseHandle(ref activeHandle_);
        }

        public void SetColor(Color color)
        {
            if (image_ != null)
            {
                image_.enabled = true;
                image_.color = color;
                image_.raycastTarget = true;
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
            image_.sprite = null;
            image_.enabled = false;
            image_.raycastTarget = true;

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
                image_.raycastTarget = true;
                ReleaseHandle(ref activeHandle_);
                activeHandle_ = pendingHandle_;
                pendingHandle_ = null;
            });
        }

        public void ConfigureTopLevel(int locationId, int itemId)
        {
            for (int index = 0; index < dropAreas_.Length; index++)
            {
                RoomDropArea dropArea = dropAreas_[index];
                if (dropArea == null)
                {
                    continue;
                }

                if (dropArea.SupportedInventoryType == Core.Data.InteractionPointType.PLACEABLE_OBJECT ||
                    dropArea.SupportedInventoryType == Core.Data.InteractionPointType.PAINT)
                {
                    dropArea.SetDropEnabled(false);
                }
            }
        }

        public void SetVisualVisible(bool visible)
        {
            EnsureCanvasGroup();
            canvasGroup_.alpha = visible ? 1f : 0f;
            canvasGroup_.blocksRaycasts = visible;
            canvasGroup_.interactable = visible;
        }

        private void ResolveReferences()
        {
            dropAreas_ = GetComponentsInChildren<RoomDropArea>(true);
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

        private void EnsureCanvasGroup()
        {
            if (canvasGroup_ != null)
            {
                return;
            }

            canvasGroup_ = GetComponent<CanvasGroup>();
            if (canvasGroup_ == null)
            {
                canvasGroup_ = gameObject.AddComponent<CanvasGroup>();
            }
        }

    }
}
