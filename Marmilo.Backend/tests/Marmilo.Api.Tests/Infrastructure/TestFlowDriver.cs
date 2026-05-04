using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests.Infrastructure;

internal sealed class TestFlowDriver
{
    private readonly MarmiloApiFactory factory_;
    private readonly TestAuthSession session_;

    public TestFlowDriver(MarmiloApiFactory factory, TestAuthSession session)
    {
        factory_ = factory;
        session_ = session;
    }

    public async Task<JsonObject> RegisterParentAsync(string familyName)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                familyName,
                email = session_.Email,
                authUserId = session_.AuthUserId
            });

        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> GetCurrentFamilyAsync()
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.GetAsync("/families/current");
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> GetAuthMeAsync()
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.GetAsync("/auth/me");
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> CreateChildAsync(string name, string petName, int pictureId)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/children",
            new
            {
                name,
                petName,
                pictureId
            });

        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> GetChildAsync(Guid childId)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.GetAsync($"/children/{childId}");
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> UpdateChildAsync(Guid childId, string name, string petName, int pictureId, bool isActive)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/children/{childId}",
            new
            {
                name,
                petName,
                pictureId,
                isActive
            });

        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> CreateRewardRuleAsync(string title, string description, int currencyAmount)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/reward-rules",
            new
            {
                title,
                description,
                currencyAmount
            });

        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> GrantRewardAsync(Guid childId, Guid rewardRuleId)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/children/{childId}/grants",
            new
            {
                rewardRuleId
            });

        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> GetGameStateAsync(Guid childId)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.GetAsync($"/children/{childId}/game-state");
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> GetLedgerAsync(Guid childId)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.GetAsync($"/children/{childId}/ledger");
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> CreateMarketItemAsync(string title, string description, int price)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/market-items",
            new
            {
                title,
                description,
                price,
                itemType = "real_world_reward",
                payload = new { }
            });

        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> RedeemAsync(Guid childId, Guid marketItemId)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/children/{childId}/redemptions",
            new
            {
                marketItemId
            });

        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public async Task<JsonObject> GetRedemptionsAsync(Guid childId)
    {
        using HttpClient client = session_.CreateAuthenticatedClient(factory_);
        HttpResponseMessage response = await client.GetAsync($"/children/{childId}/redemptions");
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonObjectAsync();
    }

    public HttpClient CreateClient() => session_.CreateAuthenticatedClient(factory_);
}
