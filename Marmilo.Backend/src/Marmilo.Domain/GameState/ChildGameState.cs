using Marmilo.Domain.Families;

namespace Marmilo.Domain.GameState;

public sealed class ChildGameState
{
    public const int DefaultBrushSessionDurationMinutes = 2;
    public const long BrushCooldownSeconds = 5 * 60 * 60;

    private ChildGameState()
    {
    }

    public ChildGameState(Guid childProfileId, string petName = "")
    {
        ChildProfileId = childProfileId;
        PendingRewardCount = 1;
        PetStateJson = ChildGameStateDefaults.CreatePetStateJson(petName);
        RoomStateJson = ChildGameStateDefaults.CreateRoomStateJson();
        InventoryStateJson = ChildGameStateDefaults.CreateInventoryStateJson();
    }

    public Guid ChildProfileId { get; private set; }

    public int CoinsBalance { get; private set; }

    public int BrushSessionDurationMinutes { get; private set; } = DefaultBrushSessionDurationMinutes;

    public int PendingRewardCount { get; private set; }

    public bool Muted { get; private set; }

    public string PetStateJson { get; private set; } = "{}";

    public string RoomStateJson { get; private set; } = "{}";

    public string InventoryStateJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public ChildProfile ChildProfile { get; private set; } = null!;

    public void Update(
        int brushSessionDurationMinutes,
        int pendingRewardCount,
        bool muted,
        string petStateJson,
        string roomStateJson,
        string inventoryStateJson)
    {
        if (brushSessionDurationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(brushSessionDurationMinutes),
                "Brush session duration must be positive.");
        }

        BrushSessionDurationMinutes = brushSessionDurationMinutes;
        PendingRewardCount = Math.Max(0, pendingRewardCount);
        Muted = muted;
        PetStateJson = ValidateJson(petStateJson, nameof(petStateJson));
        RoomStateJson = ValidateJson(roomStateJson, nameof(roomStateJson));
        InventoryStateJson = ValidateJson(inventoryStateJson, nameof(inventoryStateJson));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCoinsBalance(int coinsBalance)
    {
        if (coinsBalance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(coinsBalance), "Coins balance cannot be negative.");
        }

        CoinsBalance = coinsBalance;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool EnsureDefaults(string petName)
    {
        string normalizedPetStateJson = ChildGameStateDefaults.NormalizePetStateJson(PetStateJson, petName);
        string normalizedRoomStateJson = ChildGameStateDefaults.NormalizeRoomStateJson(RoomStateJson);
        string normalizedInventoryStateJson = ChildGameStateDefaults.NormalizeInventoryStateJson(InventoryStateJson);

        bool changed = false;

        if (!string.Equals(PetStateJson, normalizedPetStateJson, StringComparison.Ordinal))
        {
            PetStateJson = normalizedPetStateJson;
            changed = true;
        }

        if (!string.Equals(RoomStateJson, normalizedRoomStateJson, StringComparison.Ordinal))
        {
            RoomStateJson = normalizedRoomStateJson;
            changed = true;
        }

        if (!string.Equals(InventoryStateJson, normalizedInventoryStateJson, StringComparison.Ordinal))
        {
            InventoryStateJson = normalizedInventoryStateJson;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        return changed;
    }

    public bool TryCompleteBrushSession(string petName, long nowUnixSeconds)
    {
        string normalizedPetStateJson = ChildGameStateDefaults.NormalizePetStateJson(PetStateJson, petName);
        long lastBrushTime = ChildGameStateDefaults.GetLastBrushTime(normalizedPetStateJson, petName);

        if (lastBrushTime > 0 && (nowUnixSeconds - lastBrushTime) <= BrushCooldownSeconds)
        {
            return false;
        }

        PetStateJson = ChildGameStateDefaults.SetLastBrushTime(normalizedPetStateJson, petName, nowUnixSeconds);
        PendingRewardCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    private static string ValidateJson(string json, string paramName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(json);
            return json;
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new ArgumentException("Invalid JSON payload.", paramName, exception);
        }
    }
}
