using Marmilo.Api.Development;
using System.Net.Http.Headers;

namespace Marmilo.Api.Tests.Infrastructure;

internal sealed record TestAuthSession(Guid AuthUserId, string Email)
{
    public HttpClient CreateAuthenticatedClient(MarmiloApiFactory factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            DevelopmentAuthDefaults.ParentAuthUserIdHeaderName,
            AuthUserId.ToString());

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }
}
