using Game.Unity.Definitions;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Base class for popup navigation nodes.
    /// </summary>
    public abstract class PopupBase : SceneBase
    {
        public void Close()
        {
            NavigationService.Close(sceneType_);
        }
    }
}
