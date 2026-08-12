using BeeKingdom.Core.Services;

namespace BeeKingdom.Services
{
    public sealed class NullAudioService : GameServiceBase, IAudioService
    {
        public override int Priority => 60;

        public void SetMuted(bool muted)
        {
        }
    }
}
