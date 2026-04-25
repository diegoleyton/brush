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
                case InteractionPointType.EYES:
                    return inventory.Eyes;
                case InteractionPointType.FOOD:
                    return inventory.Food;
                case InteractionPointType.HAT:
                    return inventory.Hat;
                case InteractionPointType.PAINT:
                    return inventory.Paint;
                case InteractionPointType.PLACEABLE_OBJECT:
                    return inventory.PlaceableObjects;
                case InteractionPointType.DRESS:
                    return inventory.Dress;
                case InteractionPointType.SKIN:
                    return inventory.Skin;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pointType), pointType, null);
            }
        }
    }
}
