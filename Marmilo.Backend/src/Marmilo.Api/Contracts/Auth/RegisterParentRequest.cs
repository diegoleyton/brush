namespace Marmilo.Api.Contracts.Auth;

public sealed record RegisterParentRequest(
    string FamilyName,
    string? Email = null,
    Guid? AuthUserId = null);
