using System.Collections.Generic;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Defines the type of the scenes.
    /// </summary>
    public enum SceneType
    {
        RoomScene,
        BrushScene,
        ConfirmPopup,
        PasswordPopup,
        RewardsScene,
        ProfileSelectionScene,
        ProfileManagementScene,
        MarketScene
    }

    public static class SceneTypeExtension
    {
        private static Dictionary<SceneType, string> typeToId_ = new()
        {
            {SceneType.RoomScene, "RoomScene"},
            {SceneType.BrushScene, "BrushScene"},
            {SceneType.ConfirmPopup, "ConfirmPopup"},
            {SceneType.PasswordPopup, "PasswordPopup"},
            {SceneType.RewardsScene, "RewardsScene"},
            {SceneType.ProfileSelectionScene, "ProfileSelectionScene"},
            {SceneType.ProfileManagementScene, "ProfileManagementScene"},
            {SceneType.MarketScene, "MarketScene"}
        };

        public static string GeTInstruction(this SceneType sceneType)
        {
            return typeToId_[sceneType];
        }
    }
}
