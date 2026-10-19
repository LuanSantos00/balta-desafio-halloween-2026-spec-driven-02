using LinkShortener.Api.Data;

namespace LinkShortener.Api.Application;

public abstract record LinkCreationResult
{
    private LinkCreationResult()
    {
    }

    public sealed record Created(Link Link, string ManagementKey) : LinkCreationResult;

    public sealed record InvalidTargetUrl : LinkCreationResult;

    public sealed record InvalidCode : LinkCreationResult;

    public sealed record DuplicateCode : LinkCreationResult;
}
