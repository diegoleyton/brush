using Brush.Api.Contracts.Children;
using Brush.Api.Development;
using Brush.Domain.Families;
using Brush.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Brush.Api.Controllers;

[ApiController]
[Route("children")]
public sealed class ChildrenController : ControllerBase
{
    private readonly BrushDbContext dbContext_;
    private readonly ICurrentParentContext currentParentContext_;

    public ChildrenController(
        BrushDbContext dbContext,
        ICurrentParentContext currentParentContext)
    {
        dbContext_ = dbContext;
        currentParentContext_ = currentParentContext;
    }

    [HttpGet]
    public async Task<ActionResult<ChildProfileListResponse>> List(CancellationToken cancellationToken)
    {
        var familyId = await ResolveCurrentFamilyIdAsync(cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var children = await dbContext_.ChildProfiles
            .Where(childProfile => childProfile.FamilyId == familyId.Value)
            .OrderBy(childProfile => childProfile.CreatedAt)
            .Select(childProfile => new ChildProfileListItemResponse(
                childProfile.Id,
                childProfile.Name,
                childProfile.PetName,
                childProfile.PictureId,
                childProfile.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(new ChildProfileListResponse(children));
    }

    [HttpPost]
    public async Task<ActionResult<ChildProfileResponse>> Create(
        [FromBody] CreateChildProfileRequest request,
        CancellationToken cancellationToken)
    {
        var familyId = await ResolveCurrentFamilyIdAsync(cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        try
        {
            ChildProfile childProfile = new(familyId.Value, request.Name, request.PetName, request.PictureId);
            dbContext_.ChildProfiles.Add(childProfile);
            await dbContext_.SaveChangesAsync(cancellationToken);

            ChildProfileResponse response = new(
                childProfile.Id,
                childProfile.FamilyId,
                childProfile.Name,
                childProfile.PetName,
                childProfile.PictureId,
                childProfile.IsActive);

            return Created($"/children/{childProfile.Id}", response);
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new
            {
                message = argumentException.Message
            });
        }
    }

    private async Task<Guid?> ResolveCurrentFamilyIdAsync(CancellationToken cancellationToken)
    {
        var currentParent = await currentParentContext_.TryGetCurrentParentAsync(cancellationToken);
        if (currentParent == null)
        {
            return null;
        }

        return await dbContext_.FamilyParents
            .Where(membership => membership.ParentUserId == currentParent.Id)
            .Select(membership => (Guid?)membership.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
