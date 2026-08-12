using System.Collections.Generic;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Config.Loaders
{
    public sealed class ConfigurationLoader : IConfigurationLoader
    {
        private readonly IReadOnlyList<IConfigurationLoader> loaders;

        public ConfigurationLoader(IReadOnlyList<IConfigurationLoader> loaders)
        {
            this.loaders = loaders;
        }

        public IReadOnlyList<IConfigurationDefinition> LoadDefinitions()
        {
            List<IConfigurationDefinition> definitions = new List<IConfigurationDefinition>();

            foreach (IConfigurationLoader loader in loaders)
            {
                definitions.AddRange(loader.LoadDefinitions());
            }

            return definitions;
        }
    }
}
