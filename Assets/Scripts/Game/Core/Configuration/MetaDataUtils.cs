using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Configuration
{
    /// <summary>
    /// Resolves configured colors for paint and skin item ids.
    /// </summary>
    public class MetaDataUtils
    {
        private readonly Dictionary<int, Color> paintColorsDict_;
        private readonly Dictionary<int, Color[]> skinColorsDict_;

        public MetaDataUtils(Dictionary<int, Color> paintColorsDict, Dictionary<int, Color[]> skinColorsDict)
        {
            paintColorsDict_ = paintColorsDict;
            skinColorsDict_ = skinColorsDict;
        }

        public Color GetColorForPaint(int itemId) => paintColorsDict_[itemId];

        public Color[] GetColorForSkin(int itemId) => skinColorsDict_[itemId];
    }
}
