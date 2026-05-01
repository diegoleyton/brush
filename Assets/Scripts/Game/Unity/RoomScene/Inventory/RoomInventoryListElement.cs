using Flowbit.Utilities.Unity.ScrollableList;
using Flowbit.Utilities.Unity.Instantiator;

using Game.Core.Data;

using UnityEngine;
using UnityEngine.UI;

using System;
using Zenject;
using Game.Unity.Definitions;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Visual-only room inventory item used in the scene prototype.
    /// </summary>
    public sealed class RoomInventoryItemData
    {
        public int ItemId;
        public InteractionPointType InteractionPointType;
        public string Name;
        public Color Color;
        public int Quantity;
    }

    /// <summary>
    /// Recycled list element used by the room inventory list.
    /// </summary>
    public sealed class RoomInventoryListElement : BaseScrollableListElement<RoomInventoryItemData>
    {
        private RoomInventoryItemData data_;
        private IObjectInstantiator instantiator_;
        private ItemViewRegistry itemViewRegistry_;
        private ItemViewRenderer itemViewRenderer_;

        [SerializeField]
        private Image image_;

        [SerializeField]
        private Image extraImage1_;

        [SerializeField]
        private Image extraImage2_;

        [SerializeField]
        private RoomInventoryDraggable draggable_;

        [SerializeField]
        private RoomDragVisual dragVisualPrefab_;

        [SerializeField]
        private Text quantityText_;

        [SerializeField]
        private GameObject quantityTextContainer_;

        [Inject]
        public void Construct(
            Flowbit.Utilities.Unity.AssetLoader.IAssetLoader assetLoader,
            Game.Unity.Settings.RoomSettings roomSettings,
            ItemViewRegistry itemViewRegistry,
            [Inject(Id = InstantiatorIds.Dependency)] IObjectInstantiator instantiator)
        {
            instantiator_ = instantiator;
            itemViewRegistry_ = itemViewRegistry;
            itemViewRenderer_ = new ItemViewRenderer(
                image_,
                extraImage1_,
                extraImage2_,
                assetLoader,
                roomSettings,
                nameof(RoomInventoryListElement));
        }

        private void OnDestroy()
        {
            itemViewRenderer_?.Dispose();
        }

        public override void Show(RoomInventoryItemData data)
        {
            data_ = data;
            gameObject.SetActive(true);
            itemViewRenderer_.Reset();
            itemViewRegistry_.SetView(data, itemViewRenderer_);

            if (quantityText_ != null)
            {
                bool shouldShowQuantity = data.Quantity >= 0;
                quantityTextContainer_.SetActive(shouldShowQuantity);
                if (shouldShowQuantity)
                {
                    quantityText_.text = data.Quantity.ToString();
                }
            }

            if (draggable_ == null)
            {
                throw new Exception("Missing draggable");
            }

            draggable_.SetData(data);
            draggable_.SetReturnToOriginOnSuccessfulDrop(true);

            if (dragVisualPrefab_ != null)
            {
                draggable_.SetDragVisualFactory(CreateDragVisualInstance);
            }
            else
            {
                draggable_.SetDragVisualFactory(null);
            }
        }

        public override void Hide()
        {
            data_ = null;

            if (draggable_ != null)
            {
                if (!draggable_.IsDragging)
                {
                    draggable_.CleanupDragPresentation();
                }

                draggable_.ClearData();
                draggable_.SetDragVisualFactory(null);
            }

            itemViewRenderer_.Reset();

            if (quantityText_ != null)
            {
                quantityTextContainer_.SetActive(false);
                quantityText_.text = string.Empty;
            }

            gameObject.SetActive(false);
        }

        private void ConfigureDragVisualInstance(RoomDragVisual dragVisualInstance)
        {
            if (dragVisualInstance == null || data_ == null)
            {
                return;
            }

            dragVisualInstance.Show(data_);
        }

        private RectTransform CreateDragVisualInstance()
        {
            if (dragVisualPrefab_ == null)
            {
                return null;
            }

            Transform parent = ResolveDragVisualParent();
            RoomDragVisual dragVisualInstance = instantiator_ != null
                ? instantiator_.InstantiatePrefab(dragVisualPrefab_, parent)
                : Instantiate(dragVisualPrefab_, parent, false);
            ConfigureDragVisualInstance(dragVisualInstance);
            return dragVisualInstance.transform as RectTransform;
        }

        private Transform ResolveDragVisualParent()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
            {
                return canvas.rootCanvas.transform;
            }

            if (canvas != null)
            {
                return canvas.transform;
            }

            return transform.parent;
        }
    }
}
