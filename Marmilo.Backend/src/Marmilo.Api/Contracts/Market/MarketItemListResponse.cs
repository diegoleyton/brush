namespace Marmilo.Api.Contracts.Market;

public sealed record MarketItemListResponse(
    IReadOnlyList<MarketItemResponse> Items);
