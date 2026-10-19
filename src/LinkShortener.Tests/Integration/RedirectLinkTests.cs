using System.Net;
using System.Net.Http.Json;
using LinkShortener.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
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

            const int concurrentRedirects = 50;
            var concurrentResponses = await Task.WhenAll(
                Enumerable.Range(0, concurrentRedirects)
                    .Select(_ => client.GetAsync("/ARTICLE")));
            Assert.All(
                concurrentResponses,
                response => Assert.Equal(HttpStatusCode.Redirect, response.StatusCode));

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LinkDbContext>();
                var accessCount = await dbContext.Links
                    .Where(link => link.Code == "ARTICLE")
                    .Select(link => link.AccessCount)
                    .SingleAsync();

                Assert.Equal(concurrentRedirects + 1, accessCount);
            }

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
