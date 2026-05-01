using Game.Core.Data;
using Game.Unity.Definitions;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders dress inventory, market, and reward visuals.
    /// </summary>
    public sealed class DressItemView : IItemView
    {
        public InteractionPointType ItemType => InteractionPointType.DRESS;

        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            renderer.LoadMainSprite(
                AssetNameResolver.GetDressAssetName(data.ItemId),
                startTransparentUntilLoaded: false,
                showFallbackOnMissing: true);
        }
    }
}
