using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for the pet hat surface.
    /// </summary>
    public sealed class PetHatSurfaceView : MonoBehaviour
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

        public void ApplyHatColor(Color color)
        {
            if (image_ != null)
            {
                image_.color = color;
            }
        }
    }
}
