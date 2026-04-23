using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual component used by placed room object instances.
    /// </summary>
    public sealed class RoomPlaceableObjectView : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        public void SetColor(Color color)
        {
            if (image_ != null)
            {
                image_.color = color;
            }
        }
    }
}
