using System.Collections.Generic;
using BeeKingdom.Config.Runtime;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Data
{
    public interface IDataRegistry : IGameService
    {
        RegistryDiagnostics Diagnostics { get; }
        TDefinition Get<TDefinition>(string id) where TDefinition : class, IConfigurationDefinition;
        bool TryGet<TDefinition>(string id, out TDefinition definition) where TDefinition : class, IConfigurationDefinition;
        IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : class, IConfigurationDefinition;
        bool Exists<TDefinition>(string id) where TDefinition : class, IConfigurationDefinition;
        RegistryValidationResult Reload();
        RegistryValidationResult Validate();
    }
}
