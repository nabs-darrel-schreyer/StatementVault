using System.Net;
using System.Net.Http.Json;

namespace StatementVault.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Healthy", payload?.Status);
    }

    private sealed record HealthResponse(string Status);
}
