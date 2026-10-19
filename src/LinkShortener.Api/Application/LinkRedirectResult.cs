using LinkShortener.Api.Data;

namespace LinkShortener.Api.Application;

public abstract record LinkRedirectResult
{
    private LinkRedirectResult()
    {
    }

    public sealed record Redirect(Link Link) : LinkRedirectResult;

    public sealed record NotFound : LinkRedirectResult;

    public sealed record Expired : LinkRedirectResult;
}
