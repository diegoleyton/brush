using System;

using Game.Core.Data;
using Game.Unity.Settings;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders paint inventory and drag visuals.
    /// </summary>
    public sealed class PaintItemView : IItemView
    {
        public InteractionPointType ItemType => InteractionPointType.PAINT;

        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            RoomSettings roomSettings = renderer.RoomSettings;
            renderer.SetTintedMainSprite(
                roomSettings != null ? roomSettings.PaintItemSprite : null,
                data.Color,
                nameof(RoomSettings.PaintItemSprite));
        }
    }
}
