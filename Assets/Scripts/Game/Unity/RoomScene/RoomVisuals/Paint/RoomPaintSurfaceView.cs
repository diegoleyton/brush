using UnityEngine;
using UnityEngine.UI;

using Game.Core.Configuration;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for a fixed paintable room surface.
    /// </summary>
    public sealed class RoomPaintSurfaceView : MonoBehaviour
    {
        [SerializeField]
        private int surfaceId_;

        [SerializeField]
        private Image image_;

        public int SurfaceId => surfaceId_;

        public void ResetColor()
        {
            if (image_ != null)
            {
                image_.color = DefaultProfileState.DefaultRoomSurfaceColor;
            }
        }

        public void ApplyPaintColor(Color color)
        {
            if (image_ != null)
            {
                image_.color = color;
            }
        }
    }
}
