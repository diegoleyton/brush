using Flowbit.Utilities.Navigation;
using Game.Unity.Definitions;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Navigation service tailored for this game.
    /// </summary>
    public interface IGameNavigationService
    {
        void Navigate(SceneType sceneType, NavigationParams navigationParams = null);
        void NavigateWithoutHistory(SceneType sceneType, NavigationParams navigationParams = null);
        void NavigateAsRoot(SceneType sceneType, NavigationParams navigationParams = null);
        void Back();
        void Close(SceneType sceneType);
        bool CanGoBack { get; }
    }
}
