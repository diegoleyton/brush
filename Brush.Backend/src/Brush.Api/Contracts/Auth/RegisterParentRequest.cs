namespace Brush.Api.Contracts.Auth;

public sealed record RegisterParentRequest(
    string Email,
    string FamilyName,
    Guid? AuthUserId = null);
