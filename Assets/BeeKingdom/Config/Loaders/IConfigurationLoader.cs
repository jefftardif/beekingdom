using System.Collections.Generic;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Config.Loaders
{
    public interface IConfigurationLoader
    {
        IReadOnlyList<IConfigurationDefinition> LoadDefinitions();
    }
}
