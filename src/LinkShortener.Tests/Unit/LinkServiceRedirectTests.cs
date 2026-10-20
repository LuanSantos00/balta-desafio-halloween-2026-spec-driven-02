using LinkShortener.Api.Application;
using LinkShortener.Api.Data;
using Microsoft.Extensions.Caching.Memory;

namespace LinkShortener.Tests.Unit;

public sealed class LinkServiceRedirectTests
{
    [Fact]
    public async Task GetRedirectAsync_WithExistingUnexpiredLink_ReturnsRedirect()
    {
        var link = CreateLink(expiresAtUtc: DateTime.UtcNow.AddMinutes(1));
        using var cache = CreateCache();
        var service = CreateService(link, cache);

        var result = await service.GetRedirectAsync(link.Code);

        var redirect = Assert.IsType<LinkRedirectResult.Redirect>(result);
        Assert.Equal(link.TargetUrl, redirect.TargetUrl);
        Assert.Equal(1, link.AccessCount);
    }

    [Fact]
    public async Task GetRedirectAsync_WithCachedLink_SkipsLookupAndStillIncrementsAccessCount()
    {
        var link = CreateLink(expiresAtUtc: DateTime.UtcNow.AddMinutes(1));
        using var cache = CreateCache();
        var repository = new FakeLinkRepository(link);
        var service = CreateService(repository, cache);

        await service.GetRedirectAsync(link.Code);
        var result = await service.GetRedirectAsync(link.Code);

        var redirect = Assert.IsType<LinkRedirectResult.Redirect>(result);
        Assert.Equal(link.TargetUrl, redirect.TargetUrl);
        Assert.Equal(1, repository.FindByCodeCallCount);
        Assert.Equal(2, link.AccessCount);
    }

    [Fact]
    public async Task GetRedirectAsync_WithMissingCode_ReturnsNotFound()
    {
        using var cache = CreateCache();
        var service = CreateService(link: null, cache);

        var result = await service.GetRedirectAsync("MISSING");

        Assert.IsType<LinkRedirectResult.NotFound>(result);
    }

    [Fact]
    public async Task GetRedirectAsync_WithExpiredLink_ReturnsExpired()
    {
        var link = CreateLink(expiresAtUtc: DateTime.UtcNow.AddMinutes(-1));
        using var cache = CreateCache();
        var service = CreateService(link, cache);

        var result = await service.GetRedirectAsync(link.Code);

        Assert.IsType<LinkRedirectResult.Expired>(result);
        Assert.Equal(0, link.AccessCount);
    }

    private static Link CreateLink(DateTime? expiresAtUtc)
    {
        return new Link
        {
            Code = "TEST",
            TargetUrl = "https://example.com",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
            AccessCount = 0,
            ManagementKeyHash = "hash"
        };
    }

    private static MemoryCache CreateCache()
    {
        return new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000 });
    }

    private static LinkService CreateService(Link? link, IMemoryCache cache)
    {
        return CreateService(new FakeLinkRepository(link), cache);
    }

    private static LinkService CreateService(FakeLinkRepository repository, IMemoryCache cache)
    {
        return new LinkService(
            repository,
            new ShortCodeGenerator(),
            new TargetUrlValidator(),
            cache);
    }

    private sealed class FakeLinkRepository(Link? link) : ILinkRepository
    {
        public int FindByCodeCallCount { get; private set; }

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
            FindByCodeCallCount++;
            return Task.FromResult(link?.Code == code ? link : null);
        }

        public Task<bool> TryIncrementAccessCountAsync(
            string code,
            DateTime accessedAtUtc,
            CancellationToken cancellationToken)
        {
            if (link?.Code != code
                || (link.ExpiresAtUtc is not null && link.ExpiresAtUtc <= accessedAtUtc))
            {
                return Task.FromResult(false);
            }

            link.AccessCount++;
            return Task.FromResult(true);
        }
    }
}
