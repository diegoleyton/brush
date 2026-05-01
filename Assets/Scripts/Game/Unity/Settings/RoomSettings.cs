using System;
using System.Collections.Generic;
using Game.Unity.RoomScene;
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
        private int defaultDressItemId_ = 1;

        [SerializeField]
        private Sprite paintItemSprite_;

        [SerializeField]
        private Sprite skinItemSprite_;

        [SerializeField]
        private Sprite currencyRewardSprite_;

        [SerializeField]
        private RectTransform paintSurfaceEffectPrefab_;

        [SerializeField]
        private RoomDropVisual roomDropAreaVisibility_;

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

        [SerializeField]
        private Vector2 placeableObjectDragVisualSize_ = new Vector2(180f, 180f);

        [SerializeField]
        private Vector2 placeableObjectDragPointerOffset_ = new Vector2(0f, 120f);

        public int DefaultEyesItemId => defaultEyesItemId_;
        public int DefaultDressItemId => defaultDressItemId_;
        public Sprite PaintItemSprite => paintItemSprite_;
        public Sprite SkinItemSprite => skinItemSprite_;
        public Sprite CurrencyRewardSprite => currencyRewardSprite_;
        public RectTransform PaintSurfaceEffectPrefab => paintSurfaceEffectPrefab_;
        public RoomDropVisual RoomDropAreaVisibility => roomDropAreaVisibility_;
        public float PaintSurfaceColorTransitionDuration => paintSurfaceColorTransitionDuration_;
        public float PaintSurfaceEffectLifetimeSeconds => paintSurfaceEffectLifetimeSeconds_;
        public Vector2 PlaceableObjectDragVisualSize => placeableObjectDragVisualSize_;
        public Vector2 PlaceableObjectDragPointerOffset => placeableObjectDragPointerOffset_;

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
