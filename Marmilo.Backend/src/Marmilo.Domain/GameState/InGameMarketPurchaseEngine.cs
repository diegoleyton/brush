using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marmilo.Domain.GameState;

public static class InGameMarketPurchaseEngine
{
    private const int InteractionPointTypePlaceableObject = 0;
    private const int InteractionPointTypePaint = 1;
    private const int InteractionPointTypeSkin = 2;
    private const int InteractionPointTypeHat = 3;
    private const int InteractionPointTypeDress = 4;
    private const int InteractionPointTypeFood = 5;
    private const int InteractionPointTypeEyes = 6;

    public static bool IsAlreadyOwned(
        ChildGameState gameState,
        InGameMarketCatalog.InGameMarketItemDefinition item)
    {
        if (gameState == null)
        {
            throw new ArgumentNullException(nameof(gameState));
        }

        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        PetStatePayload petState = DeserializePetState(gameState.PetStateJson);
        InventoryStatePayload inventoryState = DeserializeInventoryState(gameState.InventoryStateJson);

        if (item.ItemType == InteractionPointTypeDress && petState.DressItemId == item.ItemId)
        {
            return true;
        }

        InventoryItemAmountPayload? existing = GetInventoryItems(inventoryState, item.ItemType)
            .FirstOrDefault(entry => entry.ItemId == item.ItemId);

        return existing?.Amount == -1;
    }

    public static void ApplyPurchase(
        ChildGameState gameState,
        InGameMarketCatalog.InGameMarketItemDefinition item)
    {
        if (gameState == null)
        {
            throw new ArgumentNullException(nameof(gameState));
        }

        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        InventoryStatePayload inventoryState = DeserializeInventoryState(gameState.InventoryStateJson);

        if (item.ItemType == InteractionPointTypeDress)
        {
            UnlockOwnedDress(inventoryState.DressSerialized, item.ItemId);
        }
        else
        {
            AddInventoryItem(GetInventoryItems(inventoryState, item.ItemType), item.ItemId, item.Quantity);
        }

        gameState.SetCoinsBalance(Math.Max(0, gameState.CoinsBalance - item.Price));
        gameState.Update(
            gameState.BrushSessionDurationMinutes,
            gameState.PendingRewardCount,
            gameState.Muted,
            gameState.PetStateJson,
            gameState.RoomStateJson,
            JsonSerializer.Serialize(inventoryState));
    }

    private static PetStatePayload DeserializePetState(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new PetStatePayload();
        }

        try
        {
            return JsonSerializer.Deserialize<PetStatePayload>(json) ?? new PetStatePayload();
        }
        catch (JsonException)
        {
            return new PetStatePayload();
        }
    }

    private static InventoryStatePayload DeserializeInventoryState(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new InventoryStatePayload();
        }

        try
        {
            return JsonSerializer.Deserialize<InventoryStatePayload>(json) ?? new InventoryStatePayload();
        }
        catch (JsonException)
        {
            return new InventoryStatePayload();
        }
    }

    private static List<InventoryItemAmountPayload> GetInventoryItems(InventoryStatePayload inventoryState, int itemType)
    {
        return itemType switch
        {
            InteractionPointTypePlaceableObject => inventoryState.PlacableObjectsSerialized,
            InteractionPointTypePaint => inventoryState.PaintSerialized,
            InteractionPointTypeSkin => inventoryState.SkinSerialized,
            InteractionPointTypeHat => inventoryState.HatSerialized,
            InteractionPointTypeDress => inventoryState.DressSerialized,
            InteractionPointTypeFood => inventoryState.FoodSerialized,
            InteractionPointTypeEyes => inventoryState.EyesSerialized,
            _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
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

    private static void UnlockOwnedDress(List<InventoryItemAmountPayload> items, int itemId)
    {
        InventoryItemAmountPayload? existing = items.FirstOrDefault(item => item.ItemId == itemId);
        if (existing == null)
        {
            items.Add(new InventoryItemAmountPayload
            {
                ItemId = itemId,
                Amount = -1
            });

            return;
        }

        existing.Amount = -1;
    }

    private sealed class PetStatePayload
    {
        public int DressItemId { get; set; }
    }

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
