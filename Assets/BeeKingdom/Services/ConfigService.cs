using System.Collections.Generic;
using BeeKingdom.Core.Config;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Services
{
    public sealed class ConfigService : GameServiceBase, IConfigService
    {
        private readonly Dictionary<System.Type, GameConfigAsset> configsByType = new Dictionary<System.Type, GameConfigAsset>();

        public override int Priority => 20;

        public ConfigService(IEnumerable<GameConfigAsset> configs)
        {
            foreach (GameConfigAsset config in configs)
            {
                if (config != null)
                {
                    configsByType[config.GetType()] = config;
                }
            }
        }

        public bool TryGetConfig<TConfig>(out TConfig config) where TConfig : class
        {
            if (configsByType.TryGetValue(typeof(TConfig), out GameConfigAsset value))
            {
                config = value as TConfig;
                return config != null;
            }

            config = null;
            return false;
        }
    }
}
