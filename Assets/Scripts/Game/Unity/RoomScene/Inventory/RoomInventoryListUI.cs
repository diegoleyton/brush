using Flowbit.Utilities.Unity.ScrollableList;
using Flowbit.Utilities.Unity.Instantiator;

using Game.Unity.Definitions;

using Zenject;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Horizontal recyclable inventory list used by the room prototype.
    /// Configure the inherited serialized fields directly in the editor.
    /// </summary>
    public sealed class RoomInventoryListUI : RecyclableScrollableListUI<RoomInventoryItemData, RoomInventoryListElement>
    {
        private IObjectInstantiator instantiator_;

        [Inject]
        public void Construct([Inject(Id = InstantiatorIds.Dependency)] IObjectInstantiator instantiator)
        {
            instantiator_ = instantiator;
        }

        protected override RoomInventoryListElement InstantiateElement(
            RoomInventoryListElement elementPrefab,
            UnityEngine.RectTransform elementParent)
        {
            return instantiator_ != null
                ? instantiator_.InstantiatePrefab(elementPrefab, elementParent)
                : base.InstantiateElement(elementPrefab, elementParent);
        }
    }
}
