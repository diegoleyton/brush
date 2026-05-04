using System.Text.Json;

namespace Marmilo.Api.Contracts.Market;

public sealed record CreateMarketItemRequest(
    string Title,
    string Description,
    int Price,
    string ItemType,
    JsonElement? Payload);
