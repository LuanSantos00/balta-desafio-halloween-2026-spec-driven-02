using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using LinkShortener.Api.Data;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Tests.Integration;

public sealed class CreateLinkTests
{
    [Fact]
    public async Task PostLinks_WithValidTarget_CreatesAndReturnsLink()
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

            var response = await client.PostAsJsonAsync(
                "/api/links",
                new { targetUrl = "https://example.com/article" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdLink = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
            Assert.NotNull(createdLink);
            Assert.Matches("^[A-HJ-NP-Z2-9]{7}$", createdLink.Code);
            Assert.Equal($"http://{new Uri(address).Authority}/{createdLink.Code}", createdLink.ShortUrl);
            Assert.False(string.IsNullOrWhiteSpace(createdLink.ManagementKey));

            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LinkDbContext>();
            var persistedLink = await dbContext.Links.SingleAsync();
            Assert.Equal(createdLink.Code, persistedLink.Code);
            Assert.Equal("https://example.com/article", persistedLink.TargetUrl);
            Assert.NotEqual(createdLink.ManagementKey, persistedLink.ManagementKeyHash);
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

    private sealed record CreateLinkResponse(
        string ShortUrl,
        string Code,
        string ManagementKey);
}
