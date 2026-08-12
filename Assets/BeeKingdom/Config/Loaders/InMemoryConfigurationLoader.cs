using System.Collections.Generic;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Config.Loaders
{
    public sealed class InMemoryConfigurationLoader : IConfigurationLoader
    {
        private readonly IReadOnlyList<IConfigurationDefinition> definitions;

        public InMemoryConfigurationLoader(IReadOnlyList<IConfigurationDefinition> definitions)
        {
            this.definitions = definitions;
        }

        public IReadOnlyList<IConfigurationDefinition> LoadDefinitions()
        {
            return definitions;
        }
    }
}
