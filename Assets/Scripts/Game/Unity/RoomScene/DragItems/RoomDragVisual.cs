using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Simple visual component used by spawned room drag visuals.
    /// </summary>
    public sealed class RoomDragVisual : MonoBehaviour
    {
        private ItemViewRenderer itemViewRenderer_;
        private ItemViewRegistry itemViewRegistry_;

        [SerializeField]
        private Image image_;

        [SerializeField]
        private Image extraImage1_;

        [SerializeField]
        private Image extraImage2_;

        [Inject]
        public void Construct(
            Flowbit.Utilities.Unity.AssetLoader.IAssetLoader assetLoader,
            Game.Unity.Settings.RoomSettings roomSettings,
            ItemViewRegistry itemViewRegistry)
        {
            itemViewRegistry_ = itemViewRegistry;
            itemViewRenderer_ = new ItemViewRenderer(
                image_,
                extraImage1_,
                extraImage2_,
                assetLoader,
                roomSettings,
                nameof(RoomDragVisual));
        }

        private void OnDestroy()
        {
            itemViewRenderer_?.Dispose();
        }

        /// <summary>
        /// Applies the provided item data to the drag visual.
        /// </summary>
        public void Show(RoomInventoryItemData data)
        {
            if (data == null)
            {
                return;
            }
            
            itemViewRenderer_.Reset();
            itemViewRegistry_.SetView(data, itemViewRenderer_);
        }
    }
}
