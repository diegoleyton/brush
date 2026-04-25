using Game.Core.Data;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Shared visual helpers for room inventory and room-applied visuals.
    /// </summary>
    public static class RoomItemVisuals
    {
        public static string GetCategoryDisplayName(InteractionPointType interactionPointType)
        {
            switch (interactionPointType)
            {
                case InteractionPointType.PLACEABLE_OBJECT:
                    return "Placeable Object";
                case InteractionPointType.PAINT:
                    return "Paint";
                case InteractionPointType.FOOD:
                    return "Food";
                case InteractionPointType.HAT:
                    return "Hat";
                case InteractionPointType.SKIN:
                    return "Skin";
                case InteractionPointType.DRESS:
                    return "Dress";
                case InteractionPointType.EYES:
                    return "Eyes";
                default:
                    return "Item";
            }
        }

        public static Color GetItemColor(InteractionPointType interactionPointType, int itemId)
        {
            float categoryOffset = (int)interactionPointType * 0.137f;
            float hue = Mathf.Repeat((itemId * 0.173f) + categoryOffset, 1f);
            return Color.HSVToRGB(hue, 0.65f, 0.95f);
        }
    }
}
