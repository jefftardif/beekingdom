using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Config.Runtime
{
    public interface IConfigurationService : IGameService
    {
        ConfigurationLoadResult LastLoadResult { get; }
        TDefinition GetById<TDefinition>(ConfigurationId id) where TDefinition : class, IConfigurationDefinition;
        bool TryGet<TDefinition>(ConfigurationId id, out TDefinition definition) where TDefinition : class, IConfigurationDefinition;
        IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : class, IConfigurationDefinition;
        ConfigurationLoadResult Reload();
    }
}
