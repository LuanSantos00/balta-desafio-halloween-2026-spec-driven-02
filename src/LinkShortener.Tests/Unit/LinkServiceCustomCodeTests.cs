using LinkShortener.Api.Application;
using LinkShortener.Api.Data;

namespace LinkShortener.Tests.Unit;

public sealed class LinkServiceCustomCodeTests
{
    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    [InlineData("abcd")]
    [InlineData("ABC0")]
    [InlineData("ABC D")]
    [InlineData("ABC-")]
    public async Task CreateAsync_WithInvalidCustomCode_ReturnsInvalidCode(string code)
    {
        var repository = new FakeLinkRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync("https://example.com", code);

        Assert.IsType<LinkCreationResult.InvalidCode>(result);
        Assert.Empty(repository.Links);
    }

    [Theory]
    [InlineData("ABCD")]
    [InlineData("ABCDEFGHIJKLMNOPQRST")]
    [InlineData("A1Z9")]
    public async Task CreateAsync_WithValidCustomCode_CreatesLink(string code)
    {
        var repository = new FakeLinkRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync("https://example.com", code);

        var created = Assert.IsType<LinkCreationResult.Created>(result);
        Assert.Equal(code, created.Link.Code);
        Assert.Single(repository.Links);
    }

    [Fact]
    public async Task CreateAsync_WithExistingCustomCode_ReturnsDuplicateCode()
    {
        var repository = new FakeLinkRepository();
        repository.ExistingCodes.Add("EXISTS");
        var service = CreateService(repository);

        var result = await service.CreateAsync("https://example.com", "EXISTS");

        Assert.IsType<LinkCreationResult.DuplicateCode>(result);
        Assert.Empty(repository.Links);
    }

    private static LinkService CreateService(FakeLinkRepository repository)
    {
        return new LinkService(
            repository,
            new ShortCodeGenerator(),
            new TargetUrlValidator());
    }

    private sealed class FakeLinkRepository : ILinkRepository
    {
        public List<Link> Links { get; } = [];

        public HashSet<string> ExistingCodes { get; } = [];

        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingCodes.Contains(code));
        }

        public Task<bool> AddAsync(Link link, CancellationToken cancellationToken)
        {
            if (ExistingCodes.Contains(link.Code))
            {
                return Task.FromResult(false);
            }

            Links.Add(link);
            ExistingCodes.Add(link.Code);
            return Task.FromResult(true);
        }
    }
}
