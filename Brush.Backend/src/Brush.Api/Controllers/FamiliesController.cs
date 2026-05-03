using Brush.Api.Contracts.Families;
using Brush.Api.Development;
using Brush.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brush.Api.Controllers;

[ApiController]
[Route("families")]
public sealed class FamiliesController : ControllerBase
{
    private readonly BrushDbContext dbContext_;
    private readonly ICurrentParentContext currentParentContext_;

    public FamiliesController(
        BrushDbContext dbContext,
        ICurrentParentContext currentParentContext)
    {
        dbContext_ = dbContext;
        currentParentContext_ = currentParentContext;
    }

    [HttpGet("current")]
    public async Task<ActionResult<CurrentFamilyResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var currentParent = await currentParentContext_.TryGetCurrentParentAsync(cancellationToken);
        if (currentParent == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header."
            });
        }

        var family = await dbContext_.Families
            .Where(family => family.ParentMemberships.Any(membership => membership.ParentUserId == currentParent.Id))
            .Include(currentFamily => currentFamily.ParentMemberships)
                .ThenInclude(membership => membership.ParentUser)
            .FirstOrDefaultAsync(cancellationToken);

        if (family == null)
        {
            return NotFound(new
            {
                message = "No family was found for the current parent."
            });
        }

        CurrentFamilyResponse response = new(
            family.Id,
            family.Name,
            family.ParentMemberships
                .OrderBy(membership => membership.CreatedAt)
                .Select(membership => new CurrentFamilyParentResponse(
                    membership.ParentUser.Id,
                    membership.ParentUser.AuthUserId,
                    membership.ParentUser.Email,
                    membership.Role.ToString().ToLowerInvariant()))
                .ToList());

        return Ok(response);
    }
}
