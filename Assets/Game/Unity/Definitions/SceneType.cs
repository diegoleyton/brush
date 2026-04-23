using System.Collections.Generic;

namespace Game.Unity.Definitions
{
    /// <summary>
    /// Defines the type of the scenes.
    /// </summary>
    public enum SceneType
    {
        RoomScene
    }

    public static class SceneTypeExtension
    {
        private static Dictionary<SceneType, string> typeToId_ = new()
        {
            {SceneType.RoomScene, "RoomScene"}
        };

        public static string GeTInstruction(this SceneType sceneType)
        {
            return typeToId_[sceneType];
        }
    }
}
