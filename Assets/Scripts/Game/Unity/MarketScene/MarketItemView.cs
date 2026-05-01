using System;

using Flowbit.Utilities.Unity.AssetLoader;
using Flowbit.Utilities.Unity.ScrollableList;

using Game.Core.Data;
using Game.Unity.RoomScene;
using Game.Unity.Settings;

using UnityEngine;
using UnityEngine.UI;

using Zenject;

namespace Game.Unity.MarketScene
{
    /// <summary>
    /// Renders a single market entry and forwards selection input to the scene controller.
    /// </summary>
    public sealed class MarketItemView : BaseScrollableListElement<MarketItemDefinition>
    {
        [SerializeField]
        private Image mainImage_;

        [SerializeField]
        private Image extraImage1_;

        [SerializeField]
        private Image extraImage2_;

        [SerializeField]
        private Text priceText_;

        [SerializeField]
        private Button button_;

        private ItemViewRegistry itemViewRegistry_;
        private ItemViewRenderer itemViewRenderer_;
        private Action<MarketItemDefinition> selectionHandler_;
        private MarketItemDefinition currentDefinition_;

        [Inject]
        public void Construct(
            IAssetLoader assetLoader,
            RoomSettings roomSettings,
            ItemViewRegistry itemViewRegistry)
        {
            itemViewRegistry_ = itemViewRegistry;
            itemViewRenderer_ = new ItemViewRenderer(
                mainImage_,
                extraImage1_,
                extraImage2_,
                assetLoader,
                roomSettings,
                nameof(MarketItemView));
        }

        private void OnDestroy()
        {
            itemViewRenderer_?.Dispose();
        }

        public void Validate()
        {
            if (mainImage_ == null ||
                extraImage1_ == null ||
                extraImage2_ == null ||
                priceText_ == null ||
                button_ == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(MarketItemView)} is missing one or more required serialized references.");
            }
        }

        public override void Show(MarketItemDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            Validate();
            currentDefinition_ = definition;
            itemViewRenderer_.Reset();
            itemViewRegistry_.SetView(CreateItemData(definition), itemViewRenderer_);
            priceText_.text = definition.Price.ToString();
            button_.interactable = true;
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            currentDefinition_ = null;
            if (itemViewRenderer_ != null)
            {
                itemViewRenderer_.Reset();
            }

            if (priceText_ != null)
            {
                priceText_.text = string.Empty;
            }

            if (button_ != null)
            {
                button_.interactable = false;
            }

            gameObject.SetActive(false);
        }

        public void SetSelectionHandler(Action<MarketItemDefinition> selectionHandler)
        {
            selectionHandler_ = selectionHandler;
        }

        public void Select()
        {
            if (currentDefinition_ == null)
            {
                return;
            }

            selectionHandler_?.Invoke(currentDefinition_);
        }

        private static RoomInventoryItemData CreateItemData(MarketItemDefinition definition)
        {
            Color color =
                definition.ItemType == InteractionPointType.PAINT ||
                definition.ItemType == InteractionPointType.SKIN
                    ? RoomItemVisuals.GetItemColor(definition.ItemType, definition.ItemId)
                    : Color.white;

            return new RoomInventoryItemData
            {
                ItemId = definition.ItemId,
                InteractionPointType = definition.ItemType,
                Quantity = -1,
                Color = color
            };
        }
    }
}
