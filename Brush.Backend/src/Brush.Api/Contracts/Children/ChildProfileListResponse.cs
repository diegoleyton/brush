namespace Brush.Api.Contracts.Children;

public sealed record ChildProfileListResponse(
    IReadOnlyList<ChildProfileListItemResponse> Children);
