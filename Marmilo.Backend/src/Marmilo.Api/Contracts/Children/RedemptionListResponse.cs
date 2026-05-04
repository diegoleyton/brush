namespace Marmilo.Api.Contracts.Children;

public sealed record RedemptionListResponse(
    IReadOnlyList<RedemptionResponse> Items);
