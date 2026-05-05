using Marmilo.Api.Auth;
using Marmilo.Api.Contracts.Children;
using Marmilo.Api.Contracts.RewardRules;
using Marmilo.Api.Development;
using Marmilo.Domain.Currency;
using Marmilo.Domain.Families;
using Marmilo.Domain.GameState;
using Marmilo.Domain.Redemptions;
using Marmilo.Domain.Rewards;
using Marmilo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Marmilo.Api.Controllers;

[ApiController]
[Route("children")]
public sealed class ChildrenController : ControllerBase
{
    private readonly MarmiloDbContext dbContext_;
    private readonly ICurrentPrincipalContextAccessor currentPrincipalContextAccessor_;

    public ChildrenController(
        MarmiloDbContext dbContext,
        ICurrentPrincipalContextAccessor currentPrincipalContextAccessor)
    {
        dbContext_ = dbContext;
        currentPrincipalContextAccessor_ = currentPrincipalContextAccessor;
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

    [HttpGet("{childId:guid}")]
    public async Task<ActionResult<ChildProfileResponse>> GetById(
        Guid childId,
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

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        return Ok(ToResponse(childProfile));
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
            ChildGameState childGameState = new(childProfile.Id, request.PetName);
            dbContext_.ChildProfiles.Add(childProfile);
            dbContext_.ChildGameStates.Add(childGameState);
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

    [HttpPatch("{childId:guid}")]
    public async Task<ActionResult<ChildProfileResponse>> Update(
        Guid childId,
        [FromBody] UpdateChildProfileRequest request,
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

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        try
        {
            childProfile.Rename(request.Name);
            childProfile.RenamePet(request.PetName);
            childProfile.SetPicture(request.PictureId);
            childProfile.SetActive(request.IsActive);

            await dbContext_.SaveChangesAsync(cancellationToken);
            return Ok(ToResponse(childProfile));
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new
            {
                message = argumentException.Message
            });
        }
    }

    [HttpDelete("{childId:guid}")]
    public async Task<IActionResult> Delete(
        Guid childId,
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

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        int familyChildCount = await dbContext_.ChildProfiles
            .CountAsync(profile => profile.FamilyId == familyId.Value, cancellationToken);

        if (familyChildCount <= 1)
        {
            return BadRequest(new
            {
                message = "At least one child profile must remain."
            });
        }

        dbContext_.ChildProfiles.Remove(childProfile);
        await dbContext_.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{childId:guid}/game-state")]
    public async Task<ActionResult<ChildGameStateResponse>> GetGameState(
        Guid childId,
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

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var gameState = await FindChildGameStateAsync(childId, cancellationToken);
        if (gameState == null)
        {
            return NotFound(new
            {
                message = "Child game state was not found."
            });
        }

        if (gameState.EnsureDefaults(childProfile.PetName))
        {
            await dbContext_.SaveChangesAsync(cancellationToken);
        }

        return Ok(ToGameStateResponse(gameState));
    }

    [HttpGet("{childId:guid}/ledger")]
    public async Task<ActionResult<CurrencyLedgerResponse>> GetLedger(
        Guid childId,
        CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext?.ParentUser == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var familyId = await ResolveCurrentFamilyIdAsync(principalContext.ParentUser.Id, cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var items = await dbContext_.CurrencyLedgerEntries
            .Where(entry => entry.ChildProfileId == childId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Select(entry => new CurrencyLedgerEntryResponse(
                entry.Id,
                entry.EntryType.ToString().ToLowerInvariant(),
                entry.Amount,
                entry.CreatedByParentUserId,
                entry.RewardRuleId,
                ParseJson(entry.MetadataJson),
                entry.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(new CurrencyLedgerResponse(items));
    }

    [HttpGet("{childId:guid}/redemptions")]
    public async Task<ActionResult<RedemptionListResponse>> GetRedemptions(
        Guid childId,
        CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext?.ParentUser == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var familyId = await ResolveCurrentFamilyIdAsync(principalContext.ParentUser.Id, cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var items = await dbContext_.Redemptions
            .Where(redemption => redemption.ChildProfileId == childId)
            .OrderByDescending(redemption => redemption.RequestedAt)
            .Select(redemption => ToRedemptionResponse(redemption))
            .ToListAsync(cancellationToken);

        return Ok(new RedemptionListResponse(items));
    }

    [HttpPost("{childId:guid}/grants")]
    public async Task<ActionResult<GrantCurrencyResponse>> GrantCurrency(
        Guid childId,
        [FromBody] GrantCurrencyRequest request,
        CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext?.ParentUser == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var familyId = await ResolveCurrentFamilyIdAsync(principalContext.ParentUser.Id, cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var gameState = await FindChildGameStateAsync(childId, cancellationToken);
        if (gameState == null)
        {
            return NotFound(new
            {
                message = "Child game state was not found."
            });
        }

        try
        {
            RewardRule? rewardRule = null;
            if (request.RewardRuleId.HasValue)
            {
                rewardRule = await dbContext_.RewardRules
                    .FirstOrDefaultAsync(
                        rule => rule.FamilyId == familyId.Value && rule.Id == request.RewardRuleId.Value,
                        cancellationToken);

                if (rewardRule == null)
                {
                    return NotFound(new
                    {
                        message = "Reward rule was not found."
                    });
                }

                if (!rewardRule.IsActive)
                {
                    return BadRequest(new
                    {
                        message = "Reward rule is not active."
                    });
                }
            }

            int grantAmount = rewardRule?.CurrencyAmount ?? request.Amount ?? 0;
            string metadataJson = BuildGrantMetadataJson(request, rewardRule);
            CurrencyLedgerEntry ledgerEntry = new(
                familyId.Value,
                childId,
                CurrencyLedgerEntryType.Grant,
                grantAmount,
                principalContext.ParentUser.Id,
                rewardRule?.Id,
                metadataJson);

            await using var transaction = await dbContext_.Database.BeginTransactionAsync(cancellationToken);

            dbContext_.CurrencyLedgerEntries.Add(ledgerEntry);
            gameState.SetCoinsBalance(gameState.CoinsBalance + grantAmount);
            await dbContext_.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Ok(new GrantCurrencyResponse(ledgerEntry.Id, gameState.CoinsBalance));
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new
            {
                message = argumentException.Message
            });
        }
    }

    [HttpPost("{childId:guid}/claim-rewards")]
    public async Task<ActionResult<RewardClaimResponse>> ClaimRewards(
        Guid childId,
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

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var gameState = await FindChildGameStateAsync(childId, cancellationToken);
        if (gameState == null)
        {
            return NotFound(new
            {
                message = "Child game state was not found."
            });
        }

        gameState.EnsureDefaults(childProfile.PetName);

        if (gameState.PendingRewardCount <= 0)
        {
            return BadRequest(new
            {
                message = "No pending reward is available for this child profile."
            });
        }

        var rewards = ChildRewardEngine.GenerateRewards();
        ChildRewardEngine.ApplyRewardClaim(gameState, rewards);
        await dbContext_.SaveChangesAsync(cancellationToken);

        return Ok(new RewardClaimResponse(
            rewards.Select(reward => new RewardClaimItemResponse(
                reward.Kind,
                reward.RewardType,
                reward.CurrencyType,
                reward.Id,
                reward.Quantity)).ToList()));
    }

    [HttpPost("{childId:guid}/brush-completions")]
    public async Task<ActionResult<ChildGameStateResponse>> CompleteBrushSession(
        Guid childId,
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

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var gameState = await FindChildGameStateAsync(childId, cancellationToken);
        if (gameState == null)
        {
            return NotFound(new
            {
                message = "Child game state was not found."
            });
        }

        gameState.EnsureDefaults(childProfile.PetName);

        if (!gameState.TryCompleteBrushSession(
                childProfile.PetName,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
        {
            return BadRequest(new
            {
                message = "Brush session is still on cooldown."
            });
        }

        await dbContext_.SaveChangesAsync(cancellationToken);
        return Ok(ToGameStateResponse(gameState));
    }

    [HttpPost("{childId:guid}/market-purchases")]
    public async Task<ActionResult> CreateInGameMarketPurchase(
        Guid childId,
        [FromBody] CreateInGameMarketPurchaseRequest request,
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

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var gameState = await FindChildGameStateAsync(childId, cancellationToken);
        if (gameState == null)
        {
            return NotFound(new
            {
                message = "Child game state was not found."
            });
        }

        gameState.EnsureDefaults(childProfile.PetName);

        if (!InGameMarketCatalog.TryGet(request.ItemType, request.ItemId, out var itemDefinition))
        {
            return NotFound(new
            {
                message = "Market item was not found."
            });
        }

        if (InGameMarketPurchaseEngine.IsAlreadyOwned(gameState, itemDefinition))
        {
            return BadRequest(new
            {
                message = "Item is already owned."
            });
        }

        if (gameState.CoinsBalance < itemDefinition.Price)
        {
            return BadRequest(new
            {
                message = "Not enough coins to buy this item."
            });
        }

        InGameMarketPurchaseEngine.ApplyPurchase(gameState, itemDefinition);
        await dbContext_.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{childId:guid}/redemptions")]
    public async Task<ActionResult<RedemptionResponse>> CreateRedemption(
        Guid childId,
        [FromBody] CreateRedemptionRequest request,
        CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext?.ParentUser == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var familyId = await ResolveCurrentFamilyIdAsync(principalContext.ParentUser.Id, cancellationToken);
        if (familyId == null)
        {
            return Unauthorized(new
            {
                message = $"Missing or invalid {DevelopmentAuthDefaults.ParentAuthUserIdHeaderName} header, or no family exists for the parent."
            });
        }

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var gameState = await FindChildGameStateAsync(childId, cancellationToken);
        if (gameState == null)
        {
            return NotFound(new
            {
                message = "Child game state was not found."
            });
        }

        var marketItem = await dbContext_.MarketItems
            .FirstOrDefaultAsync(
                item => item.FamilyId == familyId.Value && item.Id == request.MarketItemId,
                cancellationToken);

        if (marketItem == null)
        {
            return NotFound(new
            {
                message = "Market item was not found."
            });
        }

        if (!marketItem.IsActive)
        {
            return BadRequest(new
            {
                message = "Market item is not active."
            });
        }

        if (gameState.CoinsBalance < marketItem.Price)
        {
            return BadRequest(new
            {
                message = "Not enough coins to redeem this item."
            });
        }

        try
        {
            Redemption redemption = new(familyId.Value, childId, marketItem.Id, marketItem.Price);
            CurrencyLedgerEntry ledgerEntry = new(
                familyId.Value,
                childId,
                CurrencyLedgerEntryType.Redeem,
                marketItem.Price,
                principalContext.ParentUser.Id,
                rewardRuleId: null,
                metadataJson: BuildRedemptionMetadataJson(marketItem));

            await using var transaction = await dbContext_.Database.BeginTransactionAsync(cancellationToken);

            dbContext_.Redemptions.Add(redemption);
            dbContext_.CurrencyLedgerEntries.Add(ledgerEntry);
            gameState.SetCoinsBalance(gameState.CoinsBalance - marketItem.Price);
            await dbContext_.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Created($"/children/{childId}/redemptions/{redemption.Id}", ToRedemptionResponse(redemption));
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new
            {
                message = argumentException.Message
            });
        }
    }

    [HttpPut("{childId:guid}/game-state")]
    public async Task<ActionResult<ChildGameStateResponse>> UpdateGameState(
        Guid childId,
        [FromBody] UpdateChildGameStateRequest request,
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

        var childProfile = await FindChildProfileAsync(familyId.Value, childId, cancellationToken);
        if (childProfile == null)
        {
            return NotFound(new
            {
                message = "Child profile was not found."
            });
        }

        var gameState = await FindChildGameStateAsync(childId, cancellationToken);
        if (gameState == null)
        {
            return NotFound(new
            {
                message = "Child game state was not found."
            });
        }

        try
        {
            string currentRevision = GetGameStateRevision(gameState);
            if (!string.IsNullOrWhiteSpace(request.BaseRevision) &&
                !string.Equals(request.BaseRevision, currentRevision, StringComparison.Ordinal))
            {
                return Conflict(new
                {
                    message = "Child game state changed on the server.",
                    revision = currentRevision
                });
            }

            gameState.Update(
                request.BrushSessionDurationMinutes,
                request.PendingRewardCount,
                request.Muted,
                SerializeJson(request.PetState),
                SerializeJson(request.RoomState),
                SerializeJson(request.InventoryState));

            await dbContext_.SaveChangesAsync(cancellationToken);
            return Ok(ToGameStateResponse(gameState));
        }
        catch (ArgumentException argumentException)
        {
            return BadRequest(new
            {
                message = argumentException.Message
            });
        }
    }

    private async Task<ChildProfile?> FindChildProfileAsync(
        Guid familyId,
        Guid childId,
        CancellationToken cancellationToken)
    {
        return await dbContext_.ChildProfiles
            .FirstOrDefaultAsync(
                childProfile => childProfile.FamilyId == familyId && childProfile.Id == childId,
                cancellationToken);
    }

    private async Task<ChildGameState?> FindChildGameStateAsync(
        Guid childId,
        CancellationToken cancellationToken)
    {
        return await dbContext_.ChildGameStates
            .FirstOrDefaultAsync(gameState => gameState.ChildProfileId == childId, cancellationToken);
    }

    private async Task<Guid?> ResolveCurrentFamilyIdAsync(CancellationToken cancellationToken)
    {
        CurrentPrincipalContext? principalContext =
            await currentPrincipalContextAccessor_.TryGetCurrentAsync(cancellationToken);

        if (principalContext?.ParentUser == null)
        {
            return null;
        }

        return await ResolveCurrentFamilyIdAsync(principalContext.ParentUser.Id, cancellationToken);
    }

    private async Task<Guid?> ResolveCurrentFamilyIdAsync(
        Guid parentUserId,
        CancellationToken cancellationToken)
    {
        return await dbContext_.FamilyParents
            .Where(membership => membership.ParentUserId == parentUserId)
            .Select(membership => (Guid?)membership.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static ChildProfileResponse ToResponse(ChildProfile childProfile) => new(
        childProfile.Id,
        childProfile.FamilyId,
        childProfile.Name,
        childProfile.PetName,
        childProfile.PictureId,
        childProfile.IsActive);

    private static ChildGameStateResponse ToGameStateResponse(ChildGameState gameState) => new(
        gameState.ChildProfileId,
        GetGameStateRevision(gameState),
        gameState.CoinsBalance,
        gameState.BrushSessionDurationMinutes,
        gameState.PendingRewardCount,
        gameState.Muted,
        ParseJson(gameState.PetStateJson),
        ParseJson(gameState.RoomStateJson),
        ParseJson(gameState.InventoryStateJson));

    private static string GetGameStateRevision(ChildGameState gameState) =>
        gameState.UpdatedAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

    private static JsonElement ParseJson(string json)
    {
        using JsonDocument document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        return document.RootElement.Clone();
    }

    private static string SerializeJson(JsonElement jsonElement) => jsonElement.GetRawText();

    private static string BuildGrantMetadataJson(GrantCurrencyRequest request, RewardRule? rewardRule)
    {
        var payload = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            payload["reason"] = request.Reason.Trim();
        }

        if (rewardRule != null)
        {
            payload["rewardRule"] = new RewardRuleResponse(
                rewardRule.Id,
                rewardRule.Title,
                rewardRule.Description,
                rewardRule.CurrencyAmount,
                rewardRule.IsActive);
        }

        if (request.Metadata is JsonElement metadata)
        {
            payload["payload"] = JsonSerializer.Deserialize<object>(metadata.GetRawText());
        }

        return JsonSerializer.Serialize(payload);
    }

    private static string BuildRedemptionMetadataJson(Marmilo.Domain.Market.MarketItem marketItem)
    {
        var payload = new Dictionary<string, object?>
        {
            ["marketItem"] = new
            {
                marketItem.Id,
                marketItem.Title,
                marketItem.Description,
                marketItem.Price,
                ItemType = marketItem.ItemType.ToString(),
                Payload = JsonSerializer.Deserialize<object>(marketItem.PayloadJson)
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static RedemptionResponse ToRedemptionResponse(Redemption redemption) => new(
        redemption.Id,
        redemption.MarketItemId,
        redemption.Cost,
        redemption.Status.ToString().ToLowerInvariant(),
        redemption.RequestedAt,
        redemption.ResolvedAt,
        redemption.ResolvedByParentUserId);
}
