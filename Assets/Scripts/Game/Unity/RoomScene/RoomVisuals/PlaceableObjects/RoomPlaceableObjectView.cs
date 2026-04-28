using UnityEngine;
using UnityEngine.UI;

using Flowbit.Utilities.Unity.AssetLoader;

using Game.Unity.Definitions;

using Zenject;

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

        private RoomDropArea[] dropAreas_;
        private RoomPlaceableChildSurfaceView[] childSurfaceViews_;

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

        public void ConfigureTopLevel(int locationId, int itemId)
        {
            for (int index = 0; index < dropAreas_.Length; index++)
            {
                RoomDropArea dropArea = dropAreas_[index];
                if (dropArea == null)
                {
                    continue;
                }

                if (dropArea.RoomTargetKind == Definitions.RoomTargetKind.PLACEABLE_OBJECT &&
                    dropArea.SupportedInventoryType == Core.Data.InteractionPointType.PLACEABLE_OBJECT)
                {
                    dropArea.SetParentTargetId(locationId);
                    dropArea.SetDropEnabled(HasChildSurfaceView(dropArea.TargetId));
                    continue;
                }

                if (dropArea.RoomTargetKind == Definitions.RoomTargetKind.PLACEABLE_OBJECT &&
                    dropArea.SupportedInventoryType == Core.Data.InteractionPointType.PAINT)
                {
                    dropArea.SetTargetIds(locationId, -1);
                    dropArea.SetDropEnabled(true);
                }
            }

            for (int index = 0; index < childSurfaceViews_.Length; index++)
            {
                RoomPlaceableChildSurfaceView surfaceView = childSurfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }
            }
        }

        public void ConfigureChild(int parentLocationId, int parentSlotId, int itemId)
        {
            for (int index = 0; index < dropAreas_.Length; index++)
            {
                RoomDropArea dropArea = dropAreas_[index];
                if (dropArea == null)
                {
                    continue;
                }

                if (dropArea.RoomTargetKind == Definitions.RoomTargetKind.PLACEABLE_OBJECT &&
                    dropArea.SupportedInventoryType == Core.Data.InteractionPointType.PLACEABLE_OBJECT)
                {
                    dropArea.SetParentTargetId(parentLocationId);
                    dropArea.SetDropEnabled(false);
                    continue;
                }

                if (dropArea.RoomTargetKind == Definitions.RoomTargetKind.PLACEABLE_OBJECT &&
                    dropArea.SupportedInventoryType == Core.Data.InteractionPointType.PAINT)
                {
                    dropArea.SetTargetIds(parentSlotId, parentLocationId);
                    dropArea.SetDropEnabled(true);
                }
            }

            for (int index = 0; index < childSurfaceViews_.Length; index++)
            {
                RoomPlaceableChildSurfaceView surfaceView = childSurfaceViews_[index];
                if (surfaceView == null)
                {
                    continue;
                }

                surfaceView.ClearPlacedVisual();
            }

        }

        public RoomPlaceableChildSurfaceView FindChildSurfaceView(int slotId)
        {
            for (int index = 0; index < childSurfaceViews_.Length; index++)
            {
                RoomPlaceableChildSurfaceView surfaceView = childSurfaceViews_[index];
                if (surfaceView != null && surfaceView.SlotId == slotId)
                {
                    return surfaceView;
                }
            }

            return null;
        }

        private bool HasChildSurfaceView(int slotId)
        {
            return FindChildSurfaceView(slotId) != null;
        }

        private void ResolveReferences()
        {
            dropAreas_ = GetComponentsInChildren<RoomDropArea>(true);
            childSurfaceViews_ = GetComponentsInChildren<RoomPlaceableChildSurfaceView>(true);
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
