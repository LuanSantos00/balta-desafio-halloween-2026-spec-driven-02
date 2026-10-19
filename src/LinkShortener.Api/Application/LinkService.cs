using System.Security.Cryptography;
using System.Text;
using LinkShortener.Api.Data;

namespace LinkShortener.Api.Application;

public sealed class LinkService(
    ILinkRepository linkRepository,
    ShortCodeGenerator shortCodeGenerator,
    TargetUrlValidator targetUrlValidator)
{
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

    private static bool IsValidCustomCode(string code)
    {
        return code.Length is >= 4 and <= 20
            && code.All(character =>
                character is >= 'A' and <= 'Z'
                or >= '1' and <= '9');
    }
}
