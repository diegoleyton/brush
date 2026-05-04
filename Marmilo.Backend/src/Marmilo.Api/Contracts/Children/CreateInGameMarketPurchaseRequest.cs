namespace Marmilo.Api.Contracts.Children;

public sealed record CreateInGameMarketPurchaseRequest(
    int ItemType,
    int ItemId);
