using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Tests.Integration;

public sealed class RedirectLinkTests
{
    [Fact]
    public async Task GetCode_WithExistingLink_ReturnsRedirectAndUnknownCodeReturnsNotFound()
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
            using var client = new HttpClient(
                new HttpClientHandler { AllowAutoRedirect = false })
            {
                BaseAddress = new Uri(address)
            };

            var createResponse = await client.PostAsJsonAsync(
                "/api/links",
                new { targetUrl = "https://example.com/article", code = "ARTICLE" });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var redirectResponse = await client.GetAsync("/ARTICLE");
            Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
            Assert.Equal(
                new Uri("https://example.com/article"),
                redirectResponse.Headers.Location);

            var missingResponse = await client.GetAsync("/MISSING");
            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
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
}
