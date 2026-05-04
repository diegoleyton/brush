using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Marmilo.Api.Tests.Infrastructure;

internal static class HttpResponseMessageExtensions
{
    public static async Task<JsonObject> ReadJsonObjectAsync(this HttpResponseMessage response)
    {
        JsonObject? json = await response.Content.ReadFromJsonAsync<JsonObject>();
        return json ?? throw new InvalidOperationException("Expected a JSON object response body.");
    }
}
