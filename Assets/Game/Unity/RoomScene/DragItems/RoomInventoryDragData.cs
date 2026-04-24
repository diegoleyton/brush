using Game.Core.Data;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Stores the inventory metadata associated with a draggable room inventory item.
    /// </summary>
    public sealed class RoomInventoryDragData : MonoBehaviour
    {
        public int ItemId { get; private set; }

        public InteractionPointType InteractionPointType { get; private set; }

        public void SetData(int itemId, InteractionPointType interactionPointType)
        {
            ItemId = itemId;
            InteractionPointType = interactionPointType;
        }
    }
}
