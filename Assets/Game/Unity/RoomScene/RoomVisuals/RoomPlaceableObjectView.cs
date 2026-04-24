using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component used by placed room object instances.
    /// </summary>
    public sealed class RoomPlaceableObjectView : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        private RoomDropArea[] dropAreas_;
        private RoomPlaceableChildSurfaceView[] childSurfaceViews_;

        private void Awake()
        {
            ResolveReferences();
        }

        public void SetColor(Color color)
        {
            if (image_ != null)
            {
                image_.color = color;
            }
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

    }
}
