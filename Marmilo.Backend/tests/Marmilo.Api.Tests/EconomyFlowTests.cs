using Marmilo.Api.Tests.Infrastructure;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests;

public sealed class EconomyFlowTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public EconomyFlowTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Reward_grant_and_redemption_update_balance_and_ledger()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "economy@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Economy Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        JsonObject rewardRule = await flow.CreateRewardRuleAsync(
            "Compartir con su hermano",
            "Ayudar y compartir",
            5);

        Guid rewardRuleId = rewardRule["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected reward rule id.");

        JsonObject grant = await flow.GrantRewardAsync(childId, rewardRuleId);
        Assert.Equal(5, grant["newBalance"]?.GetValue<int>());

        JsonObject gameStateAfterGrant = await flow.GetGameStateAsync(childId);
        Assert.Equal(5, gameStateAfterGrant["coinsBalance"]?.GetValue<int>());

        JsonObject marketItem = await flow.CreateMarketItemAsync(
            "Ir a tomar helado",
            "Salida especial",
            5);

        Guid marketItemId = marketItem["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected market item id.");

        JsonObject redemption = await flow.RedeemAsync(childId, marketItemId);
        Assert.Equal("requested", redemption["status"]?.GetValue<string>());
        Assert.Equal(5, redemption["cost"]?.GetValue<int>());

        JsonObject gameStateAfterRedeem = await flow.GetGameStateAsync(childId);
        Assert.Equal(0, gameStateAfterRedeem["coinsBalance"]?.GetValue<int>());

        JsonObject ledger = await flow.GetLedgerAsync(childId);
        JsonArray ledgerItems = ledger["items"]?.AsArray()
            ?? throw new InvalidOperationException("Expected ledger items.");

        Assert.Equal(2, ledgerItems.Count);
        Assert.Equal("redeem", ledgerItems[0]?["entryType"]?.GetValue<string>());
        Assert.Equal("grant", ledgerItems[1]?["entryType"]?.GetValue<string>());

        JsonObject redemptions = await flow.GetRedemptionsAsync(childId);
        JsonArray redemptionItems = redemptions["items"]?.AsArray()
            ?? throw new InvalidOperationException("Expected redemptions array.");

        Assert.Single(redemptionItems);
        Assert.Equal(marketItemId, redemptionItems[0]?["marketItemId"]?.GetValue<Guid>());
    }
}
