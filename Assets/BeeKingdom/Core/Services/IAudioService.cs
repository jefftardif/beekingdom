namespace BeeKingdom.Core.Services
{
    public interface IAudioService : IGameService
    {
        void SetMuted(bool muted);
    }
}
