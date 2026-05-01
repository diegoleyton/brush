using Game.Core.Data;
using Game.Unity.Settings;

using UnityEngine;

namespace Game.Unity.RoomScene
{
    /// <summary>
    /// Shared visual helpers for room inventory and room-applied visuals.
    /// </summary>
    public static class RoomItemVisuals
    {
        private const string PaintPaletteResourcePath = "PaintItemColorPalette";
        private const string SkinPaletteResourcePath = "SkinItemColorPalette";

        private static ItemColorPalette paintPalette_;
        private static ItemColorPalette skinPalette_;

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
            if (interactionPointType == InteractionPointType.PAINT)
            {
                return GetPaletteColor(ref paintPalette_, PaintPaletteResourcePath, interactionPointType, itemId);
            }

            if (interactionPointType == InteractionPointType.SKIN)
            {
                return GetPaletteColor(ref skinPalette_, SkinPaletteResourcePath, interactionPointType, itemId);
            }

            float categoryOffset = (int)interactionPointType * 0.137f;
            float hue = Mathf.Repeat((itemId * 0.173f) + categoryOffset, 1f);
            return Color.HSVToRGB(hue, 0.65f, 0.95f);
        }

        private static Color GetPaletteColor(
            ref ItemColorPalette cachedPalette,
            string resourcePath,
            InteractionPointType interactionPointType,
            int itemId)
        {
            if (cachedPalette == null)
            {
                cachedPalette = Resources.Load<ItemColorPalette>(resourcePath);
            }

            if (cachedPalette != null)
            {
                return cachedPalette.GetColor(itemId);
            }

            float categoryOffset = (int)interactionPointType * 0.137f;
            float hue = Mathf.Repeat((itemId * 0.173f) + categoryOffset, 1f);
            return Color.HSVToRGB(hue, 0.65f, 0.95f);
        }
    }
}
