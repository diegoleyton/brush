using UnityEngine;
using UnityEngine.Serialization;

using Game.Core.Data;

namespace Game.Unity.Development
{
    [System.Serializable]
    public sealed class DevelopmentInventoryEntry
    {
        [field: SerializeField]
        public int ItemId { get; private set; } = 1;

        [field: SerializeField]
        public int Quantity { get; private set; } = 1;
    }

    [System.Serializable]
    public sealed class DevelopmentInventoryCategorySettings
    {
        [field: SerializeField]
        public bool ResetPetTimesOnStartup { get; private set; } = false;

        [field: SerializeField]
        public int FirstItemCount { get; private set; } = 0;

        [field: SerializeField]
        public DevelopmentInventoryEntry[] AdditionalItems { get; private set; } =
            System.Array.Empty<DevelopmentInventoryEntry>();
    }

    /// <summary>
    /// Development-only settings used to bootstrap a test profile.
    /// </summary>
    [CreateAssetMenu(fileName = "DevelopmentProfileSettings", menuName = "Game/Development/Development Profile Settings")]
    public sealed class DevelopmentProfileSettings : ScriptableObject
    {
        [field: SerializeField]
        public bool Enabled { get; private set; } = true;

        [field: SerializeField]
        public bool OnlyInEditorOrDevelopmentBuild { get; private set; } = true;

        [field: SerializeField]
        public string ProfileName { get; private set; } = "Dev Kid";

        [field: SerializeField]
        public string PetName { get; private set; } = "Brushy";

        [field: SerializeField]
        public int ProfilePictureId { get; private set; } = 1;

        [field: SerializeField]
        public int MinimumCoins { get; private set; } = 250;

        [field: SerializeField]
        [field: FormerlySerializedAs("Objects")]
        public DevelopmentInventoryCategorySettings PlaceableObjects { get; private set; } = new DevelopmentInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentInventoryCategorySettings Paint { get; private set; } = new DevelopmentInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentInventoryCategorySettings Food { get; private set; } = new DevelopmentInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentInventoryCategorySettings Skin { get; private set; } = new DevelopmentInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentInventoryCategorySettings Hat { get; private set; } = new DevelopmentInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentInventoryCategorySettings Dress { get; private set; } = new DevelopmentInventoryCategorySettings();

        [field: SerializeField]
        [field: FormerlySerializedAs("Face")]
        public DevelopmentInventoryCategorySettings Eyes { get; private set; } = new DevelopmentInventoryCategorySettings();

        public DevelopmentInventoryCategorySettings GetCategorySettings(InteractionPointType interactionPointType)
        {
            switch (interactionPointType)
            {
                case InteractionPointType.PLACEABLE_OBJECT:
                    return PlaceableObjects;
                case InteractionPointType.PAINT:
                    return Paint;
                case InteractionPointType.FOOD:
                    return Food;
                case InteractionPointType.HAT:
                    return Hat;
                case InteractionPointType.SKIN:
                    return Skin;
                case InteractionPointType.DRESS:
                    return Dress;
                case InteractionPointType.EYES:
                    return Eyes;
                default:
                    return null;
            }
        }
    }
}
