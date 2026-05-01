using Flowbit.Utilities.Unity.Instantiator;
using Flowbit.Utilities.Unity.ScrollableList;

using Game.Core.Data;
using Game.Unity.Definitions;
using System;

using Zenject;

namespace Game.Unity.MarketScene
{
    /// <summary>
    /// Recyclable market item list that can be configured as a vertical grid in the editor.
    /// </summary>
    public sealed class MarketItemListUI : RecyclableScrollableListUI<Game.Core.Data.MarketItemDefinition, MarketItemView>
    {
        private IObjectInstantiator instantiator_;
        private Action<MarketItemDefinition> selectionHandler_;

        [Inject]
        public void Construct([Inject(Id = InstantiatorIds.Dependency)] IObjectInstantiator instantiator)
        {
            instantiator_ = instantiator;
        }

        protected override MarketItemView InstantiateElement(
            MarketItemView elementPrefab,
            UnityEngine.RectTransform elementParent)
        {
            MarketItemView itemView = instantiator_ != null
                ? instantiator_.InstantiatePrefab(elementPrefab, elementParent)
                : base.InstantiateElement(elementPrefab, elementParent);
            itemView.SetSelectionHandler(selectionHandler_);
            return itemView;
        }

        public void SetSelectionHandler(Action<MarketItemDefinition> selectionHandler)
        {
            selectionHandler_ = selectionHandler;

            MarketItemView[] itemViews = GetComponentsInChildren<MarketItemView>(true);
            for (int index = 0; index < itemViews.Length; index++)
            {
                MarketItemView itemView = itemViews[index];
                if (itemView != null)
                {
                    itemView.SetSelectionHandler(selectionHandler_);
                }
            }
        }
    }
}
