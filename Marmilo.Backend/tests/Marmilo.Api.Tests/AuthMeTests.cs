using Marmilo.Api.Tests.Infrastructure;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests;

public sealed class AuthMeTests : IClassFixture<MarmiloApiFactory>
{
    private readonly MarmiloApiFactory factory_;

    public AuthMeTests(MarmiloApiFactory factory)
    {
        factory_ = factory;
    }

    [Fact]
    public async Task Auth_me_uses_development_header_and_returns_registered_parent()
    {
        await factory_.ResetDatabaseAsync();

        TestAuthSession session = new(Guid.NewGuid(), "authme@marmilo.test");
        TestFlowDriver flow = new(factory_, session);

        JsonObject registerResponse = await flow.RegisterParentAsync("Marmilo Auth Family");
        Guid parentUserId = registerResponse["parentUser"]?["id"]?.GetValue<Guid>()
            ?? throw new InvalidOperationException("Expected parent user id.");

        JsonObject me = await flow.GetAuthMeAsync();

        Assert.Equal(session.AuthUserId, me["authUserId"]?.GetValue<Guid>());
        Assert.Equal(session.Email, me["email"]?.GetValue<string>());
        Assert.True(me["isDevelopmentFallback"]?.GetValue<bool>());
        Assert.Equal(parentUserId, me["parentUserId"]?.GetValue<Guid>());
    }
}
