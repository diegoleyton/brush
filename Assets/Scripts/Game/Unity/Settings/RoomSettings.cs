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
        private int defaultEyesItemId_ = 1;

        [SerializeField]
        private Sprite paintItemSprite_;

        [SerializeField]
        private Sprite skinItemSprite_;

        [SerializeField]
        private RectTransform paintSurfaceEffectPrefab_;

        [SerializeField]
        [Min(0f)]
        private float paintSurfaceColorTransitionDuration_ = 0.25f;

        [SerializeField]
        [Min(0f)]
        private float paintSurfaceEffectLifetimeSeconds_ = 1f;

        [SerializeField]
        private RectTransform defaultPlaceableObjectPrefab_;

        [SerializeField]
        private List<PlaceableObjectPrefabDefinition> placeableObjectPrefabs_ =
            new List<PlaceableObjectPrefabDefinition>();

        private readonly Dictionary<int, bool> supportsChildPlaceablesCache_ = new Dictionary<int, bool>();

        public int DefaultEyesItemId => defaultEyesItemId_;
        public Sprite PaintItemSprite => paintItemSprite_;
        public Sprite SkinItemSprite => skinItemSprite_;
        public RectTransform PaintSurfaceEffectPrefab => paintSurfaceEffectPrefab_;
        public float PaintSurfaceColorTransitionDuration => paintSurfaceColorTransitionDuration_;
        public float PaintSurfaceEffectLifetimeSeconds => paintSurfaceEffectLifetimeSeconds_;

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

        public bool SupportsChildPlaceables(int itemId)
        {
            if (supportsChildPlaceablesCache_.TryGetValue(itemId, out bool cachedValue))
            {
                return cachedValue;
            }

            RectTransform prefab = ResolvePlaceableObjectPrefab(itemId);
            bool supportsChildPlaceables =
                prefab != null && prefab.GetComponentInChildren<Game.Unity.RoomScene.RoomPlaceableChildSurfaceView>(true) != null;

            supportsChildPlaceablesCache_[itemId] = supportsChildPlaceables;
            return supportsChildPlaceables;
        }
    }
}
