using Marmilo.Api.Tests.Infrastructure;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests;

public sealed class RewardClaimFlowTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public RewardClaimFlowTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Claim_rewards_returns_two_rewards_and_clears_pending_reward()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "rewards@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Rewards Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        JsonObject claim = await flow.ClaimRewardsAsync(childId);
        JsonArray rewards = claim["rewards"]?.AsArray()
            ?? throw new InvalidOperationException("Expected rewards array.");

        JsonObject refreshedState = await flow.GetGameStateAsync(childId);

        Assert.Equal(2, rewards.Count);
        Assert.Equal(0, refreshedState["pendingRewardCount"]?.GetValue<int>());
    }

    [Fact]
    public async Task Claim_rewards_fails_when_no_pending_reward_exists()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "rewards-empty@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Rewards Empty Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        await flow.ClaimRewardsAsync(childId);

        using HttpClient client = flow.CreateClient();
        HttpResponseMessage secondResponse = await client.PostAsync($"/children/{childId}/claim-rewards", content: null);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, secondResponse.StatusCode);

        JsonObject error = await secondResponse.ReadJsonObjectAsync();
        Assert.Equal("No pending reward is available for this child profile.", error["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task Claim_rewards_consumes_only_one_pending_reward_when_multiple_are_available()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "rewards-multi@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        await flow.RegisterParentAsync("Marmilo Rewards Multi Family");

        JsonObject child = await flow.CreateChildAsync("Sofia", "Nube", 1);
        Guid childId = child["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected child id.");

        await flow.CompleteBrushSessionAsync(childId);

        JsonObject claim = await flow.ClaimRewardsAsync(childId);
        JsonArray rewards = claim["rewards"]?.AsArray()
            ?? throw new InvalidOperationException("Expected rewards array.");
        JsonObject refreshedState = await flow.GetGameStateAsync(childId);

        Assert.Equal(2, rewards.Count);
        Assert.Equal(1, refreshedState["pendingRewardCount"]?.GetValue<int>());
    }
}
