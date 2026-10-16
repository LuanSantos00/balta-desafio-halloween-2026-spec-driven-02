using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public sealed class TargetUrlValidatorTests
{
    private readonly TargetUrlValidator _validator = new();

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path?query=value")]
    public void IsValid_AcceptsHttpAndHttpsUrls(string targetUrl)
    {
        Assert.True(_validator.IsValid(targetUrl));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("https://")]
    [InlineData("https://example.com/path with spaces")]
    public void IsValid_RejectsInvalidUrls(string? targetUrl)
    {
        Assert.False(_validator.IsValid(targetUrl));
    }

    [Fact]
    public void IsValid_AcceptsUrlAtMaximumLength()
    {
        var targetUrl = $"https://example.com/{new string('a', TargetUrlValidator.MaximumLength - 20)}";

        Assert.Equal(TargetUrlValidator.MaximumLength, targetUrl.Length);
        Assert.True(_validator.IsValid(targetUrl));
    }

    [Fact]
    public void IsValid_RejectsUrlLongerThanMaximumLength()
    {
        var targetUrl = $"https://example.com/{new string('a', TargetUrlValidator.MaximumLength - 19)}";

        Assert.Equal(TargetUrlValidator.MaximumLength + 1, targetUrl.Length);
        Assert.False(_validator.IsValid(targetUrl));
    }
}
