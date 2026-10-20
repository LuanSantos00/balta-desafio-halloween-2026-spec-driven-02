using System.Security.Cryptography;
using System.Text;
using LinkShortener.Api.Data;
using Microsoft.Extensions.Caching.Memory;

namespace LinkShortener.Api.Application;

public sealed class LinkService(
    ILinkRepository linkRepository,
    ShortCodeGenerator shortCodeGenerator,
    TargetUrlValidator targetUrlValidator,
    IMemoryCache redirectCache)
{
    private static readonly TimeSpan RedirectCacheSlidingExpiration = TimeSpan.FromMinutes(5);

    public async Task<LinkCreationResult> CreateAsync(
        string? targetUrl,
        string? customCode = null,
        CancellationToken cancellationToken = default)
    {
        if (targetUrl is null || !targetUrlValidator.IsValid(targetUrl))
        {
            return new LinkCreationResult.InvalidTargetUrl();
        }

        if (customCode is not null && !IsValidCustomCode(customCode))
        {
            return new LinkCreationResult.InvalidCode();
        }

        if (customCode is not null
            && await linkRepository.CodeExistsAsync(customCode, cancellationToken))
        {
            return new LinkCreationResult.DuplicateCode();
        }

        var managementKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var managementKeyHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(managementKey)));

        var link = new Link
        {
            Code = customCode ?? shortCodeGenerator.Generate(),
            TargetUrl = targetUrl,
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 0,
            ManagementKeyHash = managementKeyHash
        };

        if (!await linkRepository.AddAsync(link, cancellationToken))
        {
            return new LinkCreationResult.DuplicateCode();
        }

        return new LinkCreationResult.Created(link, managementKey);
    }

    public async Task<LinkRedirectResult> GetRedirectAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!redirectCache.TryGetValue<CachedRedirect>(code, out var cachedRedirect))
        {
            var link = await linkRepository.FindByCodeAsync(code, cancellationToken);
            if (link is null)
            {
                return new LinkRedirectResult.NotFound();
            }

            cachedRedirect = new CachedRedirect(link.TargetUrl, link.ExpiresAtUtc);
        }

        var accessedAtUtc = DateTime.UtcNow;
        if (cachedRedirect!.ExpiresAtUtc is not null
            && cachedRedirect.ExpiresAtUtc <= accessedAtUtc)
        {
            redirectCache.Remove(code);
            return new LinkRedirectResult.Expired();
        }

        if (!await linkRepository.TryIncrementAccessCountAsync(
                code,
                accessedAtUtc,
                cancellationToken))
        {
            redirectCache.Remove(code);
            return new LinkRedirectResult.Expired();
        }

        redirectCache.Set(
            code,
            cachedRedirect,
            new MemoryCacheEntryOptions
            {
                Size = 1,
                SlidingExpiration = RedirectCacheSlidingExpiration
            });

        return new LinkRedirectResult.Redirect(cachedRedirect.TargetUrl);
    }

    public async Task<LinkStatisticsResult> GetStatisticsAsync(
        string code,
        string? managementKey,
        CancellationToken cancellationToken = default)
    {
        var link = await linkRepository.FindByCodeAsync(code, cancellationToken);
        if (link is null)
        {
            return new LinkStatisticsResult.NotFound();
        }

        if (string.IsNullOrEmpty(managementKey))
        {
            return new LinkStatisticsResult.Unauthorized();
        }

        var suppliedKeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(managementKey));
        var storedKeyHash = Convert.FromHexString(link.ManagementKeyHash);
        if (!CryptographicOperations.FixedTimeEquals(suppliedKeyHash, storedKeyHash))
        {
            return new LinkStatisticsResult.Unauthorized();
        }

        return new LinkStatisticsResult.Authorized(link.Code, link.AccessCount);
    }

    private static bool IsValidCustomCode(string code)
    {
        return code.Length is >= 4 and <= 20
            && code.All(character =>
                character is >= 'A' and <= 'Z'
                or >= '1' and <= '9');
    }

    private sealed record CachedRedirect(string TargetUrl, DateTime? ExpiresAtUtc);
}
