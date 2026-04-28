using UnityEngine;
using UnityEngine.UI;

using Game.Core.Configuration;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component for the pet skin surface.
    /// </summary>
    public sealed class PetSkinSurfaceView : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        public void ResetColor()
        {
            if (image_ != null)
            {
                image_.color = DefaultProfileState.DefaultRoomSurfaceColor;
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
