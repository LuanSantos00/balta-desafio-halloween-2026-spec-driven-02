using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Tests.Integration;

public sealed class LinkStatisticsTests
{
    [Fact]
    public async Task GetStatistics_WithValidManagementKeyReturnsAccessCount_AndInvalidKeyReturnsUnauthorized()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"linkshortener-{Guid.NewGuid():N}.db");
        var app = Program.CreateApp(
            [],
            builder =>
            {
                builder.Configuration["ConnectionStrings:Links"] = $"Data Source={databasePath};Pooling=False";
                builder.WebHost.UseUrls("http://127.0.0.1:0");
            });

        try
        {
            await app.StartAsync();

            var server = app.Services.GetRequiredService<IServer>();
            var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            var createResponse = await client.PostAsJsonAsync(
                "/api/links",
                new { targetUrl = "https://example.com/article", code = "ARTICLE" });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createdLink = await createResponse.Content.ReadFromJsonAsync<CreateLinkResponse>();
            Assert.NotNull(createdLink);

            client.DefaultRequestHeaders.Add("X-Management-Key", createdLink.ManagementKey);
            var statisticsResponse = await client.GetAsync("/api/links/ARTICLE/stats");
            Assert.Equal(HttpStatusCode.OK, statisticsResponse.StatusCode);
            var statistics = await statisticsResponse.Content.ReadFromJsonAsync<LinkStatisticsResponse>();
            Assert.NotNull(statistics);
            Assert.Equal("ARTICLE", statistics.Code);
            Assert.Equal(0, statistics.AccessCount);

            client.DefaultRequestHeaders.Remove("X-Management-Key");
            client.DefaultRequestHeaders.Add("X-Management-Key", "incorrect-key");
            var unauthorizedResponse = await client.GetAsync("/api/links/ARTICLE/stats");
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

            client.DefaultRequestHeaders.Remove("X-Management-Key");
            var missingKeyResponse = await client.GetAsync("/api/links/ARTICLE/stats");
            Assert.Equal(HttpStatusCode.Unauthorized, missingKeyResponse.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private sealed record CreateLinkResponse(string ShortUrl, string Code, string ManagementKey);

    private sealed record LinkStatisticsResponse(string Code, long AccessCount);
}
