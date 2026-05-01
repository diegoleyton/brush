using Game.Core.Data;
using Game.Unity.Definitions;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders food inventory and drag visuals.
    /// </summary>
    public sealed class FoodItemView : IItemView
    {
        public InteractionPointType ItemType => InteractionPointType.FOOD;

        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            renderer.LoadMainSprite(
                AssetNameResolver.GetFoodAssetName(data.ItemId),
                startTransparentUntilLoaded: false,
                showFallbackOnMissing: true);
        }
    }
}
