using System;
using Game.Core.Data;

namespace Game.Core.Services
{
    /// <summary>
    /// Client-side representation of a child's remotely persisted game state.
    /// </summary>
    [Serializable]
    public sealed class ChildGameStateSnapshot
    {
        public string childId;
        public int coinsBalance;
        public int brushSessionDurationMinutes;
        public bool pendingReward;
        public bool muted;
        public Pet petState = new Pet();
        public Room roomState = new Room();
        public Inventory inventoryState = new Inventory();

        public string ChildId
        {
            get => childId;
            set => childId = value;
        }

        public int CoinsBalance
        {
            get => coinsBalance;
            set => coinsBalance = value;
        }

        public int BrushSessionDurationMinutes
        {
            get => brushSessionDurationMinutes;
            set => brushSessionDurationMinutes = value;
        }

        public bool PendingReward
        {
            get => pendingReward;
            set => pendingReward = value;
        }

        public bool Muted
        {
            get => muted;
            set => muted = value;
        }

        public Pet PetState
        {
            get => petState;
            set => petState = value;
        }

        public Room RoomState
        {
            get => roomState;
            set => roomState = value;
        }

        public Inventory InventoryState
        {
            get => inventoryState;
            set => inventoryState = value;
        }
    }
}
