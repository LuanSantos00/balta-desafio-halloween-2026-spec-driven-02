using LinkShortener.Api.Application;

namespace LinkShortener.Tests.Unit;

public sealed class ShortCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsSevenCharactersFromUnambiguousAlphabet()
    {
        var generator = new ShortCodeGenerator();

        var code = generator.Generate();

        Assert.Matches("^[A-HJ-NP-Z2-9]{7}$", code);
    }
}
