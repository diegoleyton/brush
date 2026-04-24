using UnityEngine;
using UnityEngine.UI;

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

        private Color defaultColor_ = Color.white;

        public int SurfaceId => surfaceId_;

        private void Awake()
        {
            if (image_ != null)
            {
                defaultColor_ = image_.color;
            }
        }

        public void ResetColor()
        {
            if (image_ != null)
            {
                image_.color = defaultColor_;
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
