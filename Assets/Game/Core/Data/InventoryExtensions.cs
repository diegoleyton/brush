using System;
using System.Collections.Generic;

namespace Game.Core.Data
{
    /// <summary>
    /// Inventory helpers for resolving the dictionary backing a given interaction type.
    /// </summary>
    public static class InventoryExtensions
    {
        public static Dictionary<int, int> GetInventoryItems(this Inventory inventory, InteractionPointType pointType)
        {
            switch (pointType)
            {
                case InteractionPointType.FACE:
                    return inventory.Face;
                case InteractionPointType.FOOD:
                    return inventory.Food;
                case InteractionPointType.PAINT:
                    return inventory.Paint;
                case InteractionPointType.PLACEABLE_OBJECT:
                    return inventory.PlaceableObjects;
                case InteractionPointType.SKIN:
                    return inventory.Skin;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pointType), pointType, null);
            }
        }
    }
}
