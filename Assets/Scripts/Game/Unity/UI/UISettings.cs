using UnityEngine;

namespace Game.Unity.UI
{
    /// <summary>
    /// Global settings for synchronized animated UI components.
    /// </summary>
    [CreateAssetMenu(fileName = "UISettings", menuName = "Game/UI/UI Settings")]
    public sealed class UISettings : ScriptableObject
    {
        [SerializeField]
        private UIAnimatedComponentController animatedComponentControllerPrefab_;

        public UIAnimatedComponentController AnimatedComponentControllerPrefab => animatedComponentControllerPrefab_;
    }
}
