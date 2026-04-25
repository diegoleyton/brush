namespace Game.Core.Data
{
    /// <summary>
    /// Supported interaction categories for inventory items and room actions.
    /// </summary>
    public enum InteractionPointType
    {
        PLACEABLE_OBJECT,
        PAINT,
        SKIN,
        HAT,
        DRESS,
        FOOD,
        EYES
    }

    /// <summary>
    /// Result of a save-related operation.
    /// </summary>
    public enum DataSaveStatus
    {
        OK,
        NAME_EXIST,
        ITEM_ALREADY_GIVEN,
        NO_CURRENT_PROFILE,
        OTHER_ERROR
    }

    /// <summary>
    /// Supported currencies for the in-game market.
    /// </summary>
    public enum CurrencyType
    {
        Coins
    }

    /// <summary>
    /// Result of trying to buy an item from the market.
    /// </summary>
    public enum MarketPurchaseStatus
    {
        OK,
        ITEM_NOT_FOUND,
        NOT_ENOUGH_CURRENCY,
        ALREADY_OWNED,
        NO_CURRENT_PROFILE
    }

    /// <summary>
    /// Pet feeding status based on current cooldown rules.
    /// </summary>
    public enum PetEatStatus
    {
        NO_MORE,
        NO_AFTER_BRUSHING,
        OK
    }
}
