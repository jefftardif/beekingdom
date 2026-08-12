using System.Collections.Generic;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Data
{
    public sealed class ConfigurationDataProvider : IDataProvider
    {
        private readonly IConfigurationService configurationService;

        public ConfigurationDataProvider(IConfigurationService configurationService)
        {
            this.configurationService = configurationService;
        }

        public IReadOnlyList<IConfigurationDefinition> LoadDefinitions()
        {
            return configurationService.Reload().Definitions;
        }
    }
}
