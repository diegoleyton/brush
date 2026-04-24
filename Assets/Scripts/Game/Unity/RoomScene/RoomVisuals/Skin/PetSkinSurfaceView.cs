using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for the pet skin surface.
    /// </summary>
    public sealed class PetSkinSurfaceView : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        private Color defaultColor_ = Color.white;

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

        public void ApplySkinColor(Color color)
        {
            if (image_ != null)
            {
                image_.color = color;
            }
        }
    }
}
