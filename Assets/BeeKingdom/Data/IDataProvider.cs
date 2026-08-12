using System.Collections.Generic;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Data
{
    public interface IDataProvider
    {
        IReadOnlyList<IConfigurationDefinition> LoadDefinitions();
    }
}
