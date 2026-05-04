using Marmilo.Domain.Families;

namespace Marmilo.Domain.GameState;

public sealed class ChildGameState
{
    public const int DefaultBrushSessionDurationMinutes = 2;

    private ChildGameState()
    {
    }

    public ChildGameState(Guid childProfileId)
    {
        ChildProfileId = childProfileId;
    }

    public Guid ChildProfileId { get; private set; }

    public int CoinsBalance { get; private set; }

    public int BrushSessionDurationMinutes { get; private set; } = DefaultBrushSessionDurationMinutes;

    public bool PendingReward { get; private set; }

    public bool Muted { get; private set; }

    public string PetStateJson { get; private set; } = "{}";

    public string RoomStateJson { get; private set; } = "{}";

    public string InventoryStateJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public ChildProfile ChildProfile { get; private set; } = null!;

    public void Update(
        int brushSessionDurationMinutes,
        bool pendingReward,
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
        PendingReward = pendingReward;
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
