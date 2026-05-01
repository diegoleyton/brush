using Game.Core.Data;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders a room item into a shared set of item view graphics.
    /// </summary>
    public interface IItemView
    {
        InteractionPointType ItemType { get; }

        void SetView(RoomInventoryItemData data, ItemViewRenderer renderer);
    }
}
