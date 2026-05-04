using Marmilo.Domain.Common;
using Marmilo.Domain.Families;

namespace Marmilo.Domain.Market;

public sealed class MarketItem : Entity
{
    private MarketItem()
    {
    }

    public MarketItem(
        Guid familyId,
        string title,
        string description,
        int price,
        MarketItemType itemType,
        string payloadJson)
    {
        FamilyId = familyId;
        Update(title, description, price, itemType, payloadJson, isActive: true);
    }

    public Guid FamilyId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public int Price { get; private set; }

    public MarketItemType ItemType { get; private set; }

    public string PayloadJson { get; private set; } = "{}";

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Family Family { get; private set; } = null!;

    public void Update(
        string title,
        string description,
        int price,
        MarketItemType itemType,
        string payloadJson,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Market item title is required.", nameof(title));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        }

        Title = title.Trim();
        Description = (description ?? string.Empty).Trim();
        Price = price;
        ItemType = itemType;
        PayloadJson = ValidateJson(payloadJson);
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string ValidateJson(string json)
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
            throw new ArgumentException("Invalid JSON payload.", nameof(json), exception);
        }
    }
}
