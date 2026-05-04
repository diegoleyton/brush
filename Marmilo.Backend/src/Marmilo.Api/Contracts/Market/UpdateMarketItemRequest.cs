using System.Text.Json;

namespace Marmilo.Api.Contracts.Market;

public sealed record UpdateMarketItemRequest(
    string Title,
    string Description,
    int Price,
    string ItemType,
    bool IsActive,
    JsonElement? Payload);
