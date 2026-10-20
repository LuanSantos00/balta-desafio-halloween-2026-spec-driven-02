namespace LinkShortener.Api.Application;

public abstract record LinkStatisticsResult
{
    private LinkStatisticsResult()
    {
    }

    public sealed record Authorized(string Code, long AccessCount) : LinkStatisticsResult;

    public sealed record Unauthorized : LinkStatisticsResult;

    public sealed record NotFound : LinkStatisticsResult;
}
