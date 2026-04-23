using System;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Unity.Settings
{
    [Serializable]
    public sealed class PlaceableObjectPrefabDefinition
    {
        public int ItemId;
        public RectTransform Prefab;
    }

    /// <summary>
    /// Global room view settings.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomSettings", menuName = "Game/Room/Room Settings")]
    public sealed class RoomSettings : ScriptableObject
    {
        [SerializeField]
        private RectTransform defaultPlaceableObjectPrefab_;

        [SerializeField]
        private List<PlaceableObjectPrefabDefinition> placeableObjectPrefabs_ =
            new List<PlaceableObjectPrefabDefinition>();

        public RectTransform ResolvePlaceableObjectPrefab(int itemId)
        {
            for (int index = 0; index < placeableObjectPrefabs_.Count; index++)
            {
                PlaceableObjectPrefabDefinition definition = placeableObjectPrefabs_[index];
                if (definition != null && definition.ItemId == itemId && definition.Prefab != null)
                {
                    return definition.Prefab;
                }
            }

            return defaultPlaceableObjectPrefab_;
        }
    }
}
