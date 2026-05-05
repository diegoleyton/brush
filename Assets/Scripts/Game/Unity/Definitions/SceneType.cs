using System.Collections.Generic;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Defines the type of the scenes.
    /// </summary>
    public enum SceneType
    {
        AuthScene,
        RoomScene,
        BrushScene,
        ConfirmPopup,
        PasswordPopup,
        RewardsScene,
        ProfileSelectionScene,
        ProfileManagementScene,
        MarketScene,
        ErrorPopup,
        RetryPopup
    }

    public static class SceneTypeExtension
    {
        private static Dictionary<SceneType, string> typeToId_ = new()
        {
            {SceneType.AuthScene, "AuthScene"},
            {SceneType.RoomScene, "RoomScene"},
            {SceneType.BrushScene, "BrushScene"},
            {SceneType.ConfirmPopup, "ConfirmPopup"},
            {SceneType.PasswordPopup, "PasswordPopup"},
            {SceneType.RewardsScene, "RewardsScene"},
            {SceneType.ProfileSelectionScene, "ProfileSelectionScene"},
            {SceneType.ProfileManagementScene, "ProfileManagementScene"},
            {SceneType.MarketScene, "MarketScene"},
            {SceneType.ErrorPopup, "ErrorPopup"},
            {SceneType.RetryPopup, "RetryPopup"}
        };

        public static string GeTInstruction(this SceneType sceneType)
        {
            return typeToId_[sceneType];
        }
    }
}
