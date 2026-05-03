namespace Brush.Api.Contracts.Families;

public sealed record CurrentFamilyResponse(
    Guid Id,
    string Name,
    IReadOnlyList<CurrentFamilyParentResponse> Parents);
