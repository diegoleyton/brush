using Game.Core.Data;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Generic drop area inside the room scene. Keeps a single visual item per area and
    /// controls its own visibility according to the selected inventory category.
    /// </summary>
    public sealed class RoomObjectDropArea : RoomDropAreaBase
    {
        [SerializeField]
        private InteractionPointType supportedInventoryType_ = InteractionPointType.PLACEABLE_OBJECT;

        [SerializeField]
        private int targetId_;

        /// <summary>
        /// Inventory category supported by this drop area.
        /// </summary>
        public InteractionPointType SupportedInventoryType => supportedInventoryType_;

        /// <summary>
        /// Room target identifier associated with this drop area.
        /// </summary>
        public int TargetId => targetId_;

        protected override bool ShouldShowForInteractionType(InteractionPointType? interactionPointType)
        {
            return interactionPointType == SupportedInventoryType;
        }
    }
}
