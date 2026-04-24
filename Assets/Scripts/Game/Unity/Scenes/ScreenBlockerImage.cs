using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Scenes
{
    /// <summary>
    /// Holds the image used by the global screen blocker service.
    /// </summary>
    public class ScreenBlockerImage : MonoBehaviour
    {
        [field: SerializeField]
        public Image Image { get; private set; }
    }
}
