using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Simple visual component used by spawned room drag visuals.
    /// </summary>
    public sealed class RoomDragVisual : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        /// <summary>
        /// Applies the given color to the configured graphic.
        /// </summary>
        public void SetColor(Color color)
        {
            if (image_ != null)
            {
                image_.color = color;
            }
        }
    }
}
