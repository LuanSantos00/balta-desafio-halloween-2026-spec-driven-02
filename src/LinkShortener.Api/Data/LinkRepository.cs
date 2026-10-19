using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LinkShortener.Api.Application;

namespace LinkShortener.Api.Data;

public sealed class LinkRepository(LinkDbContext dbContext) : ILinkRepository
{
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return dbContext.Links.AnyAsync(entity => entity.Code == code, cancellationToken);
    }

    public Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return dbContext.Links.SingleOrDefaultAsync(
            entity => entity.Code == code,
            cancellationToken);
    }

    public async Task<bool> AddAsync(Link link, CancellationToken cancellationToken)
    {
        dbContext.Links.Add(link);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException
            {
                SqliteErrorCode: 19,
                SqliteExtendedErrorCode: 2067
            })
        {
            dbContext.Entry(link).State = EntityState.Detached;
            return false;
        }
    }
}
