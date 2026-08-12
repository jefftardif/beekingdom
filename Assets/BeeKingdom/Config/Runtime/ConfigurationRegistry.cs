using System.Collections.Generic;
using BeeKingdom.Config.Loaders;

namespace BeeKingdom.Config.Runtime
{
    public sealed class ConfigurationRegistry
    {
        private readonly List<IConfigurationLoader> loaders = new List<IConfigurationLoader>();

        public void AddLoader(IConfigurationLoader loader)
        {
            loaders.Add(loader);
        }

        public ConfigurationLoader CreateLoader()
        {
            return new ConfigurationLoader(loaders);
        }
    }
}
