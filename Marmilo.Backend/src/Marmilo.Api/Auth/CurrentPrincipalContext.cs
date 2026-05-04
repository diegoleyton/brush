using Marmilo.Domain.Parents;

namespace Marmilo.Api.Auth;

public sealed record CurrentPrincipalContext(
    Guid AuthUserId,
    string? Email,
    ParentUser? ParentUser,
    bool IsDevelopmentFallback);
