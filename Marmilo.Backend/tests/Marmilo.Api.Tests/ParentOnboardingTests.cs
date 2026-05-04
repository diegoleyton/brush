using Marmilo.Api.Tests.Infrastructure;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests;

public sealed class ParentOnboardingTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public ParentOnboardingTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Register_then_get_current_family_returns_owner_membership()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "parent@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        JsonObject registerResponse = await flow.RegisterParentAsync("Marmilo Test Family");
        JsonObject familyResponse = await flow.GetCurrentFamilyAsync();

        Assert.Equal(session.Email, registerResponse["parentUser"]?["email"]?.GetValue<string>());
        Assert.Equal("Marmilo Test Family", registerResponse["family"]?["name"]?.GetValue<string>());
        Assert.Equal("Marmilo Test Family", familyResponse["name"]?.GetValue<string>());

        JsonArray parents = familyResponse["parents"]?.AsArray()
            ?? throw new InvalidOperationException("Expected parents array.");

        Assert.Single(parents);
        Assert.Equal(session.Email, parents[0]?["email"]?.GetValue<string>());
        Assert.Equal("owner", parents[0]?["role"]?.GetValue<string>());
    }
}
