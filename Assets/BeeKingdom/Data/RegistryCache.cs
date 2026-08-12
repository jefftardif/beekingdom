using System.Collections.Generic;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Data
{
    public sealed class RegistryCache
    {
        private readonly DefinitionIndex index = new DefinitionIndex();

        public DefinitionIndex Index => index;

        public void ReplaceAll(IReadOnlyList<IConfigurationDefinition> definitions)
        {
            index.ReplaceAll(definitions);
        }
    }
}
