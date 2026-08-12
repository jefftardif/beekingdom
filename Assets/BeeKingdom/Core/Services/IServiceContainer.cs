namespace BeeKingdom.Core.Services
{
    /// <summary>
    /// Mutable service container owned by the composition root.
    /// Runtime systems should receive IServiceRegistry instead of this interface.
    /// </summary>
    public interface IServiceContainer : IServiceRegistry
    {
        void Register<TService>(TService service) where TService : class;
        void Clear();
    }
}
