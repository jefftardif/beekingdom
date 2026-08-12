using BeeKingdom.Persistence.Abstractions;

namespace BeeKingdom.Persistence.Transactions;

public sealed class NoopUnitOfWorkFactory : IUnitOfWorkFactory
{
    public Task<IUnitOfWork> BeginAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IUnitOfWork>(new NoopUnitOfWork());
    }

    private sealed class NoopUnitOfWork : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
