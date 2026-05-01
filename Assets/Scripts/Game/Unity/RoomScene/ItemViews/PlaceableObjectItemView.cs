using Game.Core.Data;
using Game.Unity.Definitions;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders placeable object inventory and drag visuals.
    /// </summary>
    public sealed class PlaceableObjectItemView : IItemView
    {
        public InteractionPointType ItemType => InteractionPointType.PLACEABLE_OBJECT;

        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            renderer.LoadMainSprite(
                AssetNameResolver.GetPlaceableItemAssetName(data.ItemId),
                startTransparentUntilLoaded: true,
                showFallbackOnMissing: true);
        }
    }
}
