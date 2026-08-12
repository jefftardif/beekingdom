namespace BeeKingdom.Core.Services
{
    public interface IConfigService : IGameService
    {
        bool TryGetConfig<TConfig>(out TConfig config) where TConfig : class;
    }
}
