using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marmilo.Domain.GameState;

public static class ChildRewardEngine
{
    private const int RewardKindItem = 0;
    private const int RewardKindCurrency = 1;

    private const int InteractionPointTypePlaceableObject = 0;
    private const int InteractionPointTypeSkin = 2;
    private const int InteractionPointTypeFood = 5;
    private const int InteractionPointTypeEyes = 6;

    private const int CurrencyTypeCoins = 0;
    private const int CurrencyRewardAmount = 5;

    public static IReadOnlyList<GeneratedReward> GenerateRewards()
    {
        return
        [
            new GeneratedReward(
                RewardKindItem,
                InteractionPointTypeFood,
                CurrencyTypeCoins,
                Id: Random.Shared.Next(1, 17),
                Quantity: Random.Shared.Next(2, 5)),
            GenerateRandomReward()
        ];
    }

    public static void ApplyRewardClaim(ChildGameState gameState, IReadOnlyList<GeneratedReward> rewards)
    {
        if (gameState == null)
        {
            throw new ArgumentNullException(nameof(gameState));
        }

        if (rewards == null)
        {
            throw new ArgumentNullException(nameof(rewards));
        }

        InventoryStatePayload inventoryState = DeserializeInventoryState(gameState.InventoryStateJson);
        int coinsBalance = gameState.CoinsBalance;

        for (int index = 0; index < rewards.Count; index++)
        {
            GeneratedReward reward = rewards[index];
            if (reward.Kind == RewardKindCurrency)
            {
                coinsBalance += reward.Quantity;
                continue;
            }

            AddInventoryItem(GetInventoryItems(inventoryState, reward.RewardType), reward.Id, reward.Quantity);
        }

        gameState.SetCoinsBalance(coinsBalance);
        gameState.Update(
            gameState.BrushSessionDurationMinutes,
            pendingReward: false,
            gameState.Muted,
            gameState.PetStateJson,
            gameState.RoomStateJson,
            JsonSerializer.Serialize(inventoryState));
    }

    private static GeneratedReward GenerateRandomReward()
    {
        int roll = Random.Shared.Next(0, 100);

        if (roll < 70)
        {
            return new GeneratedReward(
                RewardKindCurrency,
                RewardType: 0,
                CurrencyTypeCoins,
                Id: 0,
                Quantity: CurrencyRewardAmount);
        }

        if (roll < 80)
        {
            return new GeneratedReward(
                RewardKindItem,
                InteractionPointTypeEyes,
                CurrencyTypeCoins,
                Id: Random.Shared.Next(1, 5),
                Quantity: 2);
        }

        if (roll < 90)
        {
            return new GeneratedReward(
                RewardKindItem,
                InteractionPointTypePlaceableObject,
                CurrencyTypeCoins,
                Id: Random.Shared.Next(1, 17),
                Quantity: 1);
        }

        return new GeneratedReward(
            RewardKindItem,
            InteractionPointTypeSkin,
            CurrencyTypeCoins,
            Id: Random.Shared.Next(2, 27),
            Quantity: 2);
    }

    private static InventoryStatePayload DeserializeInventoryState(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new InventoryStatePayload();
        }

        try
        {
            InventoryStatePayload? payload = JsonSerializer.Deserialize<InventoryStatePayload>(json);
            return payload ?? new InventoryStatePayload();
        }
        catch (JsonException)
        {
            return new InventoryStatePayload();
        }
    }

    private static List<InventoryItemAmountPayload> GetInventoryItems(InventoryStatePayload inventoryState, int rewardType)
    {
        return rewardType switch
        {
            InteractionPointTypePlaceableObject => inventoryState.PlacableObjectsSerialized,
            InteractionPointTypeFood => inventoryState.FoodSerialized,
            InteractionPointTypeSkin => inventoryState.SkinSerialized,
            InteractionPointTypeEyes => inventoryState.EyesSerialized,
            _ => throw new ArgumentOutOfRangeException(nameof(rewardType), rewardType, null)
        };
    }

    private static void AddInventoryItem(List<InventoryItemAmountPayload> items, int itemId, int quantity)
    {
        InventoryItemAmountPayload? existing = items.FirstOrDefault(item => item.ItemId == itemId);
        if (existing == null)
        {
            items.Add(new InventoryItemAmountPayload
            {
                ItemId = itemId,
                Amount = quantity
            });

            return;
        }

        if (existing.Amount == -1)
        {
            return;
        }

        existing.Amount += quantity;
    }

    public sealed record GeneratedReward(
        int Kind,
        int RewardType,
        int CurrencyType,
        int Id,
        int Quantity);

    private sealed class InventoryStatePayload
    {
        [JsonPropertyName("placableObjectsSerialized_")]
        public List<InventoryItemAmountPayload> PlacableObjectsSerialized { get; set; } = [];

        [JsonPropertyName("paintSerialized_")]
        public List<InventoryItemAmountPayload> PaintSerialized { get; set; } = [];

        [JsonPropertyName("foodSerialized_")]
        public List<InventoryItemAmountPayload> FoodSerialized { get; set; } = [];

        [JsonPropertyName("skinSerialized_")]
        public List<InventoryItemAmountPayload> SkinSerialized { get; set; } = [];

        [JsonPropertyName("hatSerialized_")]
        public List<InventoryItemAmountPayload> HatSerialized { get; set; } = [];

        [JsonPropertyName("dressSerialized_")]
        public List<InventoryItemAmountPayload> DressSerialized { get; set; } = [];

        [JsonPropertyName("eyesSerialized_")]
        public List<InventoryItemAmountPayload> EyesSerialized { get; set; } = [];
    }

    private sealed class InventoryItemAmountPayload
    {
        public int ItemId { get; set; }

        public int Amount { get; set; }
    }
}
