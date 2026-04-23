using UnityEngine;

namespace Game.Unity.Settings
{
    /// <summary>
    /// Global room view settings.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomSettings", menuName = "Game/Room/Room Settings")]
    public sealed class RoomSettings : ScriptableObject
    {
        [SerializeField]
        private RectTransform placeableObjectPrefab_;

        public RectTransform PlaceableObjectPrefab => placeableObjectPrefab_;
    }
}
