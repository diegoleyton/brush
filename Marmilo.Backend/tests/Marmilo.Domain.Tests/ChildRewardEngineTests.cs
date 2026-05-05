using System.Text.Json;

using Marmilo.Domain.GameState;

namespace Marmilo.Domain.Tests;

public sealed class ChildRewardEngineTests
{
    private const int RewardKindItem = 0;
    private const int RewardKindCurrency = 1;
    private const int InteractionPointTypeFood = 5;
    private const int InteractionPointTypeSkin = 2;
    private const int CurrencyTypeCoins = 0;

    [Fact]
    public void ApplyRewardClaim_clears_pending_reward_and_adds_currency_and_inventory_items()
    {
        ChildGameState state = new(Guid.NewGuid(), "Nube");
        state.SetCoinsBalance(10);

        IReadOnlyList<ChildRewardEngine.GeneratedReward> rewards =
        [
            new(RewardKindCurrency, RewardType: 0, CurrencyTypeCoins, Id: 0, Quantity: 5),
            new(RewardKindItem, InteractionPointTypeFood, CurrencyTypeCoins, Id: 3, Quantity: 2)
        ];

        ChildRewardEngine.ApplyRewardClaim(state, rewards);

        Assert.False(state.PendingReward);
        Assert.Equal(15, state.CoinsBalance);

        using JsonDocument inventoryDocument = JsonDocument.Parse(state.InventoryStateJson);
        JsonElement foodItems = inventoryDocument.RootElement.GetProperty("foodSerialized_");
        JsonElement grantedFood = foodItems.EnumerateArray().Single(item => item.GetProperty("ItemId").GetInt32() == 3);

        Assert.Equal(2, grantedFood.GetProperty("Amount").GetInt32());
    }

    [Fact]
    public void ApplyRewardClaim_keeps_unlocked_inventory_items_as_unlimited()
    {
        ChildGameState state = new(Guid.NewGuid(), "Nube");
        string inventoryWithUnlockedSkin = """
            {
              "placableObjectsSerialized_": [],
              "paintSerialized_": [],
              "foodSerialized_": [],
              "skinSerialized_": [{ "ItemId": 4, "Amount": -1 }],
              "hatSerialized_": [],
              "dressSerialized_": [],
              "eyesSerialized_": []
            }
            """;

        state.Update(
            brushSessionDurationMinutes: 2,
            pendingReward: true,
            muted: false,
            state.PetStateJson,
            state.RoomStateJson,
            inventoryWithUnlockedSkin);

        IReadOnlyList<ChildRewardEngine.GeneratedReward> rewards =
        [
            new(RewardKindItem, InteractionPointTypeSkin, CurrencyTypeCoins, Id: 4, Quantity: 2)
        ];

        ChildRewardEngine.ApplyRewardClaim(state, rewards);

        using JsonDocument inventoryDocument = JsonDocument.Parse(state.InventoryStateJson);
        JsonElement skinItems = inventoryDocument.RootElement.GetProperty("skinSerialized_");
        JsonElement unlockedSkin = skinItems.EnumerateArray().Single(item => item.GetProperty("ItemId").GetInt32() == 4);

        Assert.Equal(-1, unlockedSkin.GetProperty("Amount").GetInt32());
    }
}
