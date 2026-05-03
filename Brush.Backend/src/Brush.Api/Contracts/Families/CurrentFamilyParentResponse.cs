namespace Brush.Api.Contracts.Families;

public sealed record CurrentFamilyParentResponse(
    Guid Id,
    Guid AuthUserId,
    string Email,
    string Role);
