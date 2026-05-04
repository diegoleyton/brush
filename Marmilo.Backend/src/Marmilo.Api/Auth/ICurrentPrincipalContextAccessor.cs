namespace Marmilo.Api.Auth;

public interface ICurrentPrincipalContextAccessor
{
    Task<CurrentPrincipalContext?> TryGetCurrentAsync(CancellationToken cancellationToken);
}
