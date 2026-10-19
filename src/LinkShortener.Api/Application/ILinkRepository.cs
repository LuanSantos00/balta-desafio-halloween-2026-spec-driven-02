using LinkShortener.Api.Data;

namespace LinkShortener.Api.Application;

public interface ILinkRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<bool> AddAsync(Link link, CancellationToken cancellationToken);

    Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken);
}
