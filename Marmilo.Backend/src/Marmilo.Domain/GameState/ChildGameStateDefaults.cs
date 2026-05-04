using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marmilo.Domain.GameState;

public static class ChildGameStateDefaults
{
    public const int DefaultPetEyesItemId = 1;
    public const int DefaultPetSkinItemId = 1;
    public const int DefaultPetHatItemId = 1;
    public const int DefaultPetDressItemId = 0;

    public static string CreatePetStateJson(string petName)
    {
        return JsonSerializer.Serialize(new PetStatePayload
        {
            Name = petName?.Trim() ?? string.Empty,
            LastEatTime = -1,
            EatCount = 0,
            LastBrushTime = -1,
            EyesItemId = DefaultPetEyesItemId,
            SkinItemId = DefaultPetSkinItemId,
            HatItemId = DefaultPetHatItemId,
            DressItemId = DefaultPetDressItemId
        });
    }

    public static string CreateRoomStateJson()
    {
        return JsonSerializer.Serialize(new RoomStatePayload());
    }

    public static string CreateInventoryStateJson()
    {
        InventoryStatePayload payload = new();
        EnsureDefaultUnlockedItems(payload);
        return JsonSerializer.Serialize(payload);
    }

    public static string NormalizePetStateJson(string json, string petName)
    {
        PetStatePayload payload = TryDeserialize(json, new PetStatePayload());

        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            payload.Name = petName?.Trim() ?? string.Empty;
        }

        if (payload.EyesItemId <= 0)
        {
            payload.EyesItemId = DefaultPetEyesItemId;
        }

        if (payload.SkinItemId <= 0)
        {
            payload.SkinItemId = DefaultPetSkinItemId;
        }

        if (payload.HatItemId <= 0)
        {
            payload.HatItemId = DefaultPetHatItemId;
        }

        return JsonSerializer.Serialize(payload);
    }

    public static string NormalizeRoomStateJson(string json)
    {
        RoomStatePayload payload = TryDeserialize(json, new RoomStatePayload());
        payload.PlaceableObjects ??= [];
        payload.PaintedSurfaces ??= [];
        return JsonSerializer.Serialize(payload);
    }

    public static string NormalizeInventoryStateJson(string json)
    {
        InventoryStatePayload payload = TryDeserialize(json, new InventoryStatePayload());
        EnsureDefaultUnlockedItems(payload);
        return JsonSerializer.Serialize(payload);
    }

    private static T TryDeserialize<T>(string json, T fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    private static void EnsureDefaultUnlockedItems(InventoryStatePayload payload)
    {
        payload.PlacableObjectsSerialized ??= [];
        payload.PaintSerialized ??= [];
        payload.FoodSerialized ??= [];
        payload.SkinSerialized ??= [];
        payload.HatSerialized ??= [];
        payload.DressSerialized ??= [];
        payload.EyesSerialized ??= [];

        EnsureDefaultUnlocked(payload.PaintSerialized, 1, 3);
        EnsureDefaultUnlocked(payload.SkinSerialized, 1, 1);
        EnsureDefaultUnlocked(payload.HatSerialized, 1, 1);
        EnsureDefaultUnlocked(payload.EyesSerialized, 1, 1);
        EnsureDefaultUnlocked(payload.DressSerialized, 0, 1);
    }

    private static void EnsureDefaultUnlocked(List<InventoryItemAmountPayload> items, int startingItemId, int count)
    {
        if (items.Count > 0)
        {
            return;
        }

        for (int index = 0; index < count; index++)
        {
            items.Add(new InventoryItemAmountPayload
            {
                ItemId = startingItemId + index,
                Amount = -1
            });
        }
    }

    private sealed class PetStatePayload
    {
        public string Name { get; set; } = string.Empty;

        public long LastEatTime { get; set; }

        public int EatCount { get; set; }

        public long LastBrushTime { get; set; }

        public int EyesItemId { get; set; }

        public int SkinItemId { get; set; }

        public int HatItemId { get; set; }

        public int DressItemId { get; set; }
    }

    private sealed class RoomStatePayload
    {
        public List<object> PlaceableObjects { get; set; } = [];

        public List<object> PaintedSurfaces { get; set; } = [];
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
