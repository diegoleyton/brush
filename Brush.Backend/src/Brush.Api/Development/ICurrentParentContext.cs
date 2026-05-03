using Brush.Domain.Parents;

namespace Brush.Api.Development;

public interface ICurrentParentContext
{
    Task<ParentUser?> TryGetCurrentParentAsync(CancellationToken cancellationToken);
}
