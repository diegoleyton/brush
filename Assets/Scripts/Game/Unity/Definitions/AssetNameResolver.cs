namespace Game.Unity.Definitions
{
    /// <summary>
    /// Reolve asset names for different types.
    /// </summary>
    public static class AssetNameResolver
    {
        private const string EYES_ASSET_PREFIX = "eyes_";
        private const string PLACEABLE_ITEM_ASSET_PREFIX = "item_";

        /// <summary>
        /// Get the eyes asset name for an id
        /// </summary>
        public static string GetEyeAssetName(int id)
        {
            return EYES_ASSET_PREFIX + id;
        }

        /// <summary>
        /// Get the placeable item asset name for an id.
        /// </summary>
        public static string GetPlaceableItemAssetName(int id)
        {
            return PLACEABLE_ITEM_ASSET_PREFIX + id;
        }
    }
}
