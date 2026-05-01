using UnityEngine;
using UnityEngine.UI;

using Flowbit.Utilities.Unity.AssetLoader;

using Game.Unity.Definitions;
using Game.Unity.Settings;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Simple visual component used by drop area visuals.
    /// </summary>
    public sealed class RoomDropVisual : MonoBehaviour
    {
        [SerializeField]
        private Image image_;

        [SerializeField]
        private Image background_;

        [SerializeField]
        private Color regularColor_;

        [SerializeField]
        private Color highlightColor_;

        private void Awake()
        {
            Highlight(false);
        }

        public void Highlight(bool isHighligted)
        {
            image_.color = isHighligted ? highlightColor_ : regularColor_;
        }

        public void HideBackground()
        {
            background_.enabled = false;
        }
    }
}
