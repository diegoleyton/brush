using Game.Unity.Definitions;
using UnityEngine;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Base class for popup navigation nodes.
    /// </summary>
    public abstract class PopupBase : SceneBase
    {
        [field: SerializeField]
        public RectTransform Root { get; private set; }

        [field: SerializeField]
        public Canvas Canvas { get; private set; }

        public void Close()
        {
            NavigationService.Close(sceneType_);
        }
    }
}
