using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

public class ApiGatewayRateLimiterTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["CorsOrigins:0"] = "http://localhost",
                        ["ReverseProxy:Routes:r1:ClusterId"] = "c1",
                        ["ReverseProxy:Routes:r1:Match:Path"] = "{**catch-all}",
                        ["ReverseProxy:Clusters:c1:Destinations:d1:Address"] = "http://localhost:9999"
                    });
                });
            });

        _client = _factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task RateLimiter_ReturnsTooManyRequests_WhenLimitExceeded()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var tasks = Enumerable.Range(0, 120)
            .Select(_ => _client.GetAsync("/api/v1/sessions", cts.Token))
            .ToList();

        while (tasks.Count > 0)
        {
            var finished = await Task.WhenAny(tasks);
            tasks.Remove(finished);
            try
            {
                var response = await finished;
                if (response.StatusCode == HttpStatusCode.TooManyRequests) //test passed
                {
                    cts.Cancel(); 
                    return;
                }
            }
            catch (OperationCanceledException) { }
        }

        Assert.Fail("Expected at least one 429 TooManyRequests response");
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
