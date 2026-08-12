namespace BeeKingdom.Core.Services
{
    /// <summary>
    /// Read-only service access passed during initialization to avoid direct object creation.
    /// </summary>
    public interface IServiceRegistry
    {
        bool TryGet<TService>(out TService service) where TService : class;
        TService Get<TService>() where TService : class;
    }
}
