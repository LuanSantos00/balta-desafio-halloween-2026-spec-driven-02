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
        CancellationToken cancellationToken = default)
    {
        if (targetUrl is null || !targetUrlValidator.IsValid(targetUrl))
        {
            return new LinkCreationResult.InvalidTargetUrl();
        }

        var managementKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var managementKeyHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(managementKey)));

        var link = new Link
        {
            Code = shortCodeGenerator.Generate(),
            TargetUrl = targetUrl,
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 0,
            ManagementKeyHash = managementKeyHash
        };

        await linkRepository.AddAsync(link, cancellationToken);

        return new LinkCreationResult.Created(link, managementKey);
    }
}
