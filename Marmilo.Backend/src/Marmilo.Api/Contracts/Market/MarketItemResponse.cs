using System.Text.Json;

namespace Marmilo.Api.Contracts.Market;

public sealed record MarketItemResponse(
    Guid Id,
    string Title,
    string Description,
    int Price,
    string ItemType,
    JsonElement Payload,
    bool IsActive);
