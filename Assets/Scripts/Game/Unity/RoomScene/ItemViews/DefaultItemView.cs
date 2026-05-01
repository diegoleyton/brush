namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Renders item types that only need the default tinted main sprite.
    /// </summary>
    public sealed class DefaultItemView
    {
        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            renderer.SetDefaultTint(data.Color);
        }
    }
}
