using Game.Core.Data;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Drop area contained inside a placed room object for second-level objects.
    /// </summary>
    public sealed class RoomChildDropArea : RoomDropAreaBase
    {
        [SerializeField]
        private int slotId_;

        private bool slotEnabled_;
        private int parentLocationId_ = -1;

        public int SlotId => slotId_;

        public int ParentLocationId => parentLocationId_;

        public void SetParentLocationId(int parentLocationId)
        {
            parentLocationId_ = parentLocationId;
        }

        public void SetSlotEnabled(bool enabled)
        {
            slotEnabled_ = enabled;
            RefreshVisibility();
        }

        protected override bool ShouldShowForInteractionType(InteractionPointType? interactionPointType)
        {
            return interactionPointType == InteractionPointType.PLACEABLE_OBJECT;
        }

        protected override bool IsAdditionalVisibilitySatisfied()
        {
            return slotEnabled_;
        }
    }
}
