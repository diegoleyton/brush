using System;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Unity.Settings
{
    /// <summary>
    /// Explicit color palette for a room item category, keyed by item id.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemColorPalette", menuName = "Game/Room/Item Color Palette")]
    public sealed class ItemColorPalette : ScriptableObject
    {
        [SerializeField]
        private Color fallbackColor_ = Color.white;

        [SerializeField]
        private List<ItemColorEntry> entries_ = new List<ItemColorEntry>();

        public Color GetColor(int itemId)
        {
            for (int index = 0; index < entries_.Count; index++)
            {
                ItemColorEntry entry = entries_[index];
                if (entry != null && entry.ItemId == itemId)
                {
                    return entry.Color;
                }
            }

            return fallbackColor_;
        }
    }

    /// <summary>
    /// Serializable mapping from item id to display color.
    /// </summary>
    [Serializable]
    public sealed class ItemColorEntry
    {
        public int ItemId;
        public Color Color = Color.white;
    }
}
