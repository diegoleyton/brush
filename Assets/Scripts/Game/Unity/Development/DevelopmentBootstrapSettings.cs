using UnityEngine;
using UnityEngine.Serialization;

using Game.Core.Data;

namespace Game.Unity.Development
{
    [System.Serializable]
    public sealed class DevelopmentBootstrapInventoryEntry
    {
        [field: SerializeField]
        public int ItemId { get; private set; } = 1;

        [field: SerializeField]
        public int Quantity { get; private set; } = 1;
    }

    [System.Serializable]
    public sealed class DevelopmentBootstrapInventoryCategorySettings
    {
        [field: SerializeField]
        public bool ResetPetTimesOnStartup { get; private set; } = false;

        [field: SerializeField]
        public int FirstItemCount { get; private set; } = 0;

        [field: SerializeField]
        public DevelopmentBootstrapInventoryEntry[] AdditionalItems { get; private set; } =
            System.Array.Empty<DevelopmentBootstrapInventoryEntry>();
    }

    /// <summary>
    /// Development-only settings used to seed local bootstrap state for quick iteration.
    /// </summary>
    [CreateAssetMenu(fileName = "DevelopmentBootstrapSettings", menuName = "Game/Development/Development Bootstrap Settings")]
    public sealed class DevelopmentBootstrapSettings : ScriptableObject
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
        public bool ForcePlaceableObjectRewardsOnly { get; private set; } = false;

        [field: SerializeField]
        public int TestProfileCount { get; private set; } = 1;

        [field: SerializeField]
        [field: FormerlySerializedAs("Objects")]
        public DevelopmentBootstrapInventoryCategorySettings PlaceableObjects { get; private set; } = new DevelopmentBootstrapInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentBootstrapInventoryCategorySettings Paint { get; private set; } = new DevelopmentBootstrapInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentBootstrapInventoryCategorySettings Food { get; private set; } = new DevelopmentBootstrapInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentBootstrapInventoryCategorySettings Skin { get; private set; } = new DevelopmentBootstrapInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentBootstrapInventoryCategorySettings Hat { get; private set; } = new DevelopmentBootstrapInventoryCategorySettings();

        [field: SerializeField]
        public DevelopmentBootstrapInventoryCategorySettings Dress { get; private set; } = new DevelopmentBootstrapInventoryCategorySettings();

        [field: SerializeField]
        [field: FormerlySerializedAs("Face")]
        public DevelopmentBootstrapInventoryCategorySettings Eyes { get; private set; } = new DevelopmentBootstrapInventoryCategorySettings();

        public DevelopmentBootstrapInventoryCategorySettings GetCategorySettings(InteractionPointType interactionPointType)
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
                case InteractionPointType.CURRENCY:
                    return null;
                default:
                    return null;
            }
        }
    }
}
