namespace LinkShortener.Api.Application;

public sealed class TargetUrlValidator
{
    public const int MaximumLength = 2048;

    public bool IsValid(string? targetUrl)
    {
        if (string.IsNullOrWhiteSpace(targetUrl) || targetUrl.Length > MaximumLength)
        {
            return false;
        }

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host)
            && uri.IsWellFormedOriginalString();
    }
}
