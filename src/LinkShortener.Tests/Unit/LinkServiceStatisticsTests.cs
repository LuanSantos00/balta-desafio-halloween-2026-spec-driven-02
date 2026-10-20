using System.Security.Cryptography;
using System.Text;
using LinkShortener.Api.Application;
using LinkShortener.Api.Data;
using Microsoft.Extensions.Caching.Memory;

namespace LinkShortener.Tests.Unit;

public sealed class LinkServiceStatisticsTests
{
    private const string ManagementKey = "valid-management-key";

    [Fact]
    public async Task GetStatisticsAsync_WithValidManagementKey_ReturnsAccessCount()
    {
        var link = CreateLink();
        link.AccessCount = 12;
        using var cache = CreateCache();
        var service = CreateService(link, cache);

        var result = await service.GetStatisticsAsync(link.Code, ManagementKey);

        var statistics = Assert.IsType<LinkStatisticsResult.Authorized>(result);
        Assert.Equal(link.Code, statistics.Code);
        Assert.Equal(12, statistics.AccessCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("incorrect-management-key")]
    public async Task GetStatisticsAsync_WithInvalidManagementKey_ReturnsUnauthorized(string? managementKey)
    {
        using var cache = CreateCache();
        var service = CreateService(CreateLink(), cache);

        var result = await service.GetStatisticsAsync("TEST", managementKey);

        Assert.IsType<LinkStatisticsResult.Unauthorized>(result);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithUnknownCode_ReturnsNotFound()
    {
        using var cache = CreateCache();
        var service = CreateService(link: null, cache);

        var result = await service.GetStatisticsAsync("MISSING", ManagementKey);

        Assert.IsType<LinkStatisticsResult.NotFound>(result);
    }

    private static Link CreateLink()
    {
        return new Link
        {
            Code = "TEST",
            TargetUrl = "https://example.com",
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 0,
            ManagementKeyHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(ManagementKey)))
        };
    }

    private static MemoryCache CreateCache()
    {
        return new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
    }

    private static LinkService CreateService(Link? link, IMemoryCache cache)
    {
        return new LinkService(
            new FakeLinkRepository(link),
            new ShortCodeGenerator(),
            new TargetUrlValidator(),
            cache);
    }

    private sealed class FakeLinkRepository(Link? link) : ILinkRepository
    {
        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(link?.Code == code);
        }

        public Task<bool> AddAsync(Link newLink, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(link?.Code == code ? link : null);
        }

        public Task<bool> TryIncrementAccessCountAsync(
            string code,
            DateTime accessedAtUtc,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}
