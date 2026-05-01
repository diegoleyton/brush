using System;
using System.Collections.Generic;

using Game.Core.Data;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Resolves the item view strategy used to render each inventory item type.
    /// </summary>
    public sealed class ItemViewRegistry
    {
        private readonly Dictionary<InteractionPointType, IItemView> itemViewsByType_;
        private readonly DefaultItemView defaultItemView_;

        public ItemViewRegistry(List<IItemView> itemViews, DefaultItemView defaultItemView)
        {
            if (itemViews == null)
            {
                throw new ArgumentNullException(nameof(itemViews));
            }

            defaultItemView_ = defaultItemView ?? throw new ArgumentNullException(nameof(defaultItemView));
            itemViewsByType_ = new Dictionary<InteractionPointType, IItemView>();

            foreach (IItemView itemView in itemViews)
            {
                if (itemViewsByType_.ContainsKey(itemView.ItemType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate {nameof(IItemView)} registered for {itemView.ItemType}.");
                }

                itemViewsByType_.Add(itemView.ItemType, itemView);
            }
        }

        public void SetView(RoomInventoryItemData data, ItemViewRenderer renderer)
        {
            if (itemViewsByType_.TryGetValue(data.InteractionPointType, out IItemView itemView))
            {
                itemView.SetView(data, renderer);
                return;
            }

            defaultItemView_.SetView(data, renderer);
        }
    }
}
