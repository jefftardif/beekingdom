namespace BeeKingdom.Persistence.Abstractions;

public interface IRepository<TAggregate, in TKey>
{
    Task<TAggregate?> FindAsync(TKey key, CancellationToken cancellationToken = default);
    Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
