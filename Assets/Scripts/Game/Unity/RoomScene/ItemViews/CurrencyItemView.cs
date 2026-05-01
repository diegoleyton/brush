using Game.Core.Data;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders currency rewards using the configured currency sprite.
    /// </summary>
    public sealed class CurrencyItemView : IItemView
    {
        public InteractionPointType ItemType => InteractionPointType.CURRENCY;

        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            renderer.SetMainSprite(
                renderer.RoomSettings != null ? renderer.RoomSettings.CurrencyRewardSprite : null,
                UnityEngine.Color.white);
        }
    }
}
