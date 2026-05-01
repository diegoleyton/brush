using Game.Core.Data;
using Game.Unity.Settings;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders skin inventory and drag visuals.
    /// </summary>
    public sealed class SkinItemView : IItemView
    {
        public InteractionPointType ItemType => InteractionPointType.SKIN;

        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            RoomSettings roomSettings = renderer.RoomSettings;
            renderer.SetTintedMainSprite(
                roomSettings != null ? roomSettings.SkinItemSprite : null,
                data.Color,
                nameof(RoomSettings.SkinItemSprite));
        }
    }
}
