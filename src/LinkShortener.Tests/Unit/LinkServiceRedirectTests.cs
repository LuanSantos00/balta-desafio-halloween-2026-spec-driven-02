using LinkShortener.Api.Application;
using LinkShortener.Api.Data;

namespace LinkShortener.Tests.Unit;

public sealed class LinkServiceRedirectTests
{
    [Fact]
    public async Task GetRedirectAsync_WithExistingUnexpiredLink_ReturnsRedirect()
    {
        var link = CreateLink(expiresAtUtc: DateTime.UtcNow.AddMinutes(1));
        var service = CreateService(link);

        var result = await service.GetRedirectAsync(link.Code);

        var redirect = Assert.IsType<LinkRedirectResult.Redirect>(result);
        Assert.Same(link, redirect.Link);
    }

    [Fact]
    public async Task GetRedirectAsync_WithMissingCode_ReturnsNotFound()
    {
        var service = CreateService(link: null);

        var result = await service.GetRedirectAsync("MISSING");

        Assert.IsType<LinkRedirectResult.NotFound>(result);
    }

    [Fact]
    public async Task GetRedirectAsync_WithExpiredLink_ReturnsExpired()
    {
        var link = CreateLink(expiresAtUtc: DateTime.UtcNow.AddMinutes(-1));
        var service = CreateService(link);

        var result = await service.GetRedirectAsync(link.Code);

        Assert.IsType<LinkRedirectResult.Expired>(result);
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

    private static LinkService CreateService(Link? link)
    {
        return new LinkService(
            new FakeLinkRepository(link),
            new ShortCodeGenerator(),
            new TargetUrlValidator());
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
    }
}
