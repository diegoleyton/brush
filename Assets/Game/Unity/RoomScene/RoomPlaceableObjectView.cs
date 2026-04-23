using System;

using Game.Core.Configuration;

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

        [SerializeField]
        private RoomChildDropArea[] childDropAreas_ = Array.Empty<RoomChildDropArea>();

        public void SetColor(Color color)
        {
            if (image_ != null)
            {
                image_.color = color;
            }
        }

        public void Configure(int locationId, int itemId, bool allowChildPlacement)
        {
            ResolveChildDropAreas();

            for (int index = 0; index < childDropAreas_.Length; index++)
            {
                RoomChildDropArea dropArea = childDropAreas_[index];
                if (dropArea == null)
                {
                    continue;
                }

                dropArea.SetParentLocationId(locationId);
                dropArea.SetSlotEnabled(
                    allowChildPlacement && RoomObjectCatalog.SupportsChildSlot(itemId, dropArea.SlotId));
            }
        }

        public RoomChildDropArea FindChildDropArea(int slotId)
        {
            ResolveChildDropAreas();

            for (int index = 0; index < childDropAreas_.Length; index++)
            {
                RoomChildDropArea dropArea = childDropAreas_[index];
                if (dropArea != null && dropArea.SlotId == slotId)
                {
                    return dropArea;
                }
            }

            return null;
        }

        private void ResolveChildDropAreas()
        {
            if (childDropAreas_ == null || childDropAreas_.Length == 0)
            {
                childDropAreas_ = GetComponentsInChildren<RoomChildDropArea>(true);
            }
        }
    }
}
