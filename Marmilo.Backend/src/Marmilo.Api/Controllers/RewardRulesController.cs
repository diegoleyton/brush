using Marmilo.Api.Auth;
using Marmilo.Api.Contracts.RewardRules;
using Marmilo.Api.Development;
using Marmilo.Domain.Parents;
using Marmilo.Domain.Rewards;
using Marmilo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marmilo.Api.Controllers;

[ApiController]
[Route("reward-rules")]
public sealed class RewardRulesController : ControllerBase
{
    private readonly MarmiloDbContext dbContext_;
    private readonly ICurrentPrincipalContextAccessor currentPrincipalContextAccessor_;

    public RewardRulesController(
        MarmiloDbContext dbContext,
        ICurrentPrincipalContextAccessor currentPrincipalContextAccessor)
    {
        dbContext_ = dbContext;
        currentPrincipalContextAccessor_ = currentPrincipalContextAccessor;
    }

    [HttpGet]
    public async Task<ActionResult<RewardRuleListResponse>> List(CancellationToken cancellationToken)
    {
        var familyId = await ResolveCurrentFamilyIdAsync(cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var items = await dbContext_.RewardRules
            .Where(rule => rule.FamilyId == familyId.Value)
            .OrderBy(rule => rule.CreatedAt)
            .Select(rule => ToResponse(rule))
            .ToListAsync(cancellationToken);

        return Ok(new RewardRuleListResponse(items));
    }

    [HttpPost]
    public async Task<ActionResult<RewardRuleResponse>> Create(
        [FromBody] CreateRewardRuleRequest request,
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
            RewardRule rule = new(familyId.Value, request.Title, request.Description, request.CurrencyAmount);
            dbContext_.RewardRules.Add(rule);
            await dbContext_.SaveChangesAsync(cancellationToken);
            return Created($"/reward-rules/{rule.Id}", ToResponse(rule));
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new { message = argumentException.Message });
        }
    }

    [HttpPatch("{rewardRuleId:guid}")]
    public async Task<ActionResult<RewardRuleResponse>> Update(
        Guid rewardRuleId,
        [FromBody] UpdateRewardRuleRequest request,
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

        var rule = await dbContext_.RewardRules
            .FirstOrDefaultAsync(rule => rule.FamilyId == familyId.Value && rule.Id == rewardRuleId, cancellationToken);

        if (rule == null)
        {
            return NotFound(new { message = "Reward rule was not found." });
        }

        try
        {
            rule.Update(request.Title, request.Description, request.CurrencyAmount, request.IsActive);
            await dbContext_.SaveChangesAsync(cancellationToken);
            return Ok(ToResponse(rule));
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new { message = argumentException.Message });
        }
    }

    private async Task<Guid?> ResolveCurrentFamilyIdAsync(CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext?.ParentUser == null)
        {
            return null;
        }

        return await dbContext_.FamilyParents
            .Where(membership => membership.ParentUserId == principalContext.ParentUser.Id)
            .Select(membership => (Guid?)membership.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static RewardRuleResponse ToResponse(RewardRule rule) => new(
        rule.Id,
        rule.Title,
        rule.Description,
        rule.CurrencyAmount,
        rule.IsActive);
}
