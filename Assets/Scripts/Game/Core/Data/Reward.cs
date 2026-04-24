namespace Game.Core.Data
{
    /// <summary>
    /// Reward granted to the player after completing an action.
    /// </summary>
    public class Reward
    {
        public InteractionPointType RewardType;
        public int Id;
        public int Quantity;
    }
}
