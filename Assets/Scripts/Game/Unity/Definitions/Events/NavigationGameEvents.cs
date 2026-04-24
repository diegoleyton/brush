using Flowbit.Utilities.Core.Events;

namespace Game.Unity.Definitions.Events
{
    /// <summary>
    /// Event called when we go to a new scene
    /// </summary>
    public sealed class OnNextScene : IEvent
    {
        public OnNextScene(SceneType sceneType)
        {
            SceneType = sceneType;
        }

        public SceneType SceneType { get; }
    }

    /// <summary>
    /// Event called when we go back to a previous scene
    /// </summary>
    public sealed class OnPreviousScene : IEvent
    {
    }

    /// <summary>
    /// Event called when we open a popup
    /// </summary>
    public sealed class OnPopupOpen : IEvent
    {
    }

    /// <summary>
    /// Event called when we close a popup
    /// </summary>
    public sealed class OnPopupClose : IEvent
    {
    }

    /// <summary>
    /// Event called when the first scene is loaded
    /// </summary>
    public sealed class OnFirstScene : IEvent
    {
    }
}
