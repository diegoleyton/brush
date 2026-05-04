namespace Marmilo.Api.Contracts.Children;

public sealed record ChildProfileListResponse(
    IReadOnlyList<ChildProfileListItemResponse> Children);
