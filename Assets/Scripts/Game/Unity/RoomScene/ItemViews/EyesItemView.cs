using Game.Core.Data;
using Game.Unity.Definitions;
using Game.Unity.Settings;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders eyes inventory and drag visuals.
    /// </summary>
    public sealed class EyesItemView : IItemView
    {
        public InteractionPointType ItemType => InteractionPointType.EYES;

        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            renderer.SetExtraSprites(null, null);
            renderer.SetExtraImagesAlpha(0f);
            renderer.ShowMainImageFallback();

            LoadEyes(renderer, data.ItemId, allowFallback: true);
        }

        private static void LoadEyes(ItemViewRenderer renderer, int itemId, bool allowFallback)
        {
            renderer.LoadSprite(AssetNameResolver.GetEyeAssetName(itemId), sprite =>
            {
                if (sprite != null)
                {
                    renderer.SetExtraSprites(sprite, sprite);
                    renderer.SetExtraImagesAlpha(1f);
                    renderer.SetMainAlpha(0f);
                    return;
                }

                RoomSettings roomSettings = renderer.RoomSettings;
                if (allowFallback &&
                    roomSettings != null &&
                    roomSettings.DefaultEyesItemId != itemId)
                {
                    LoadEyes(renderer, roomSettings.DefaultEyesItemId, allowFallback: false);
                    return;
                }

                renderer.ShowMainImageFallback();
            });
        }
    }
}
