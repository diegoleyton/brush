namespace Marmilo.Api.Contracts.Children;

public sealed record RedemptionResponse(
    Guid Id,
    Guid MarketItemId,
    int Cost,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ResolvedAt,
    Guid? ResolvedByParentUserId);
