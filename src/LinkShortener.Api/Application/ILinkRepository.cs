using LinkShortener.Api.Data;

namespace LinkShortener.Api.Application;

public interface ILinkRepository
{
    Task AddAsync(Link link, CancellationToken cancellationToken);
}
