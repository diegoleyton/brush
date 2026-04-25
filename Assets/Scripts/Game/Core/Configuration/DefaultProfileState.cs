using System.Collections.Generic;

using Game.Core.Data;

namespace Game.Core.Configuration
{
    /// <summary>
    /// Default values used when creating a new player profile.
    /// </summary>
    public static class DefaultProfileState
    {
        public const int NoPaintItemId = 0;
        public const int InitialCoins = 0;
        private const int WhiteItemId = 1;
        private const int BlueItemId = 2;
        private const int YellowItemId = 3;
        private const int RedItemId = 4;
        public const int DefaultPetEyesItemId = 1;
        public const int DefaultPetSkinItemId = 1;
        public const int DefaultPetHatItemId = 1;
        public const int DefaultPetDressItemId = 1;

        public const bool InitialPendingReward = true;

        public static readonly IReadOnlyDictionary<int, int> DefaultRoomSurfacePaint =
            new Dictionary<int, int>
            {
                { GameIds.FrameSurfaceId, YellowItemId },
                { GameIds.RugSurfaceId, BlueItemId },
                { GameIds.ChairSurfaceId, YellowItemId },
                { GameIds.StoolSurfaceId, RedItemId },
                { GameIds.WallSurfaceId, WhiteItemId },
                { GameIds.BedSurfaceId, BlueItemId },
            };

        public static int GetDefaultPaintForSurface(int surfaceId) => DefaultRoomSurfacePaint[surfaceId];

        public static Room CreateRoom()
        {
            Room room = new Room();

            foreach (KeyValuePair<int, int> entry in DefaultRoomSurfacePaint)
            {
                room.PaintedSurfaces.Add(new RoomPaintSurfaceState
                {
                    SurfaceId = entry.Key,
                    PaintId = entry.Value
                });
            }

            return room;
        }

        public static Inventory CreateInventory()
        {
            Inventory inventory = new Inventory();

            foreach (KeyValuePair<InteractionPointType, ItemCategoryConfig> entry in ItemCatalog.All)
            {
                Dictionary<int, int> items = inventory.GetInventoryItems(entry.Key);
                ItemCategoryConfig itemConfig = entry.Value;

                for (int itemId = 1; itemId <= itemConfig.DefaultUnlockedCount; itemId++)
                {
                    items.Add(itemId, -1);
                }
            }

            return inventory;
        }
    }
}
