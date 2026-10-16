using LinkShortener.Api.Application;

namespace LinkShortener.Api.Data;

public sealed class LinkRepository(LinkDbContext dbContext) : ILinkRepository
{
    public async Task AddAsync(Link link, CancellationToken cancellationToken)
    {
        dbContext.Links.Add(link);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
