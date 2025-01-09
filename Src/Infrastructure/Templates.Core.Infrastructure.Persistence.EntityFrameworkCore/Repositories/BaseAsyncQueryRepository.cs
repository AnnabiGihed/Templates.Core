using Microsoft.EntityFrameworkCore;
using Templates.Core.Domain.Primitives;
using Templates.Core.Domain.Repositories;

namespace Templates.Core.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class BaseAsyncQueryRepository<TProjection, TId> : IAsyncQueryRepository<TProjection, TId> where TProjection : ProjectionRoot<TId>
{
	protected readonly DbContext _dbContext;

	public BaseAsyncQueryRepository(DbContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async Task<TProjection> GetByPredicateAsync(Func<TProjection, bool> predicate, string includeNavigationProperty = default!)
	{
		TProjection? t;

		t = (includeNavigationProperty != default) ?
			_dbContext.Set<TProjection>().AsNoTracking().Include(includeNavigationProperty).Where(predicate).FirstOrDefault()
			:
			_dbContext.Set<TProjection>().AsNoTracking().Where(predicate).FirstOrDefault();

		return t == null ? throw new ArgumentNullException(nameof(t)) :
			await Task.FromResult(t);
	}

	public async Task<TProjection?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
	{
		TProjection? t = await _dbContext.Set<TProjection>().FindAsync(new object[] { id }, cancellationToken);
		return t;
	}

	public async Task<IReadOnlyList<TProjection>> ListAllAsync(CancellationToken cancellationToken = default)
	{
		return await _dbContext.Set<TProjection>().AsNoTracking().ToListAsync(cancellationToken);
	}

	public virtual async Task<IReadOnlyList<TProjection>> GetPagedReponseAsync(int page, int size, CancellationToken cancellationToken = default)
	{
		return await _dbContext.Set<TProjection>().AsNoTracking().Skip((page - 1) * size).Take(size).AsNoTracking().ToListAsync(cancellationToken);
	}

	public async Task<TProjection> AddAsync(TProjection entity)
	{
		await _dbContext.Set<TProjection>().AddAsync(entity);
		return entity;
	}

	public async Task<bool> UpdateAsync(TProjection entity)
	{
		await Task.Run(() =>
		{
			_dbContext.Entry(entity).State = EntityState.Modified;
		});

		return true;
	}

	public async Task DeleteAsync(TProjection entity)
	{
		await Task.Run(() =>
		{
			_dbContext.Set<TProjection>().Remove(entity);
		});
	}

	public async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
	{
		TProjection? t = await _dbContext.Set<TProjection>().FindAsync(new object[] { id }, cancellationToken);
		return t != null;
	}
}
