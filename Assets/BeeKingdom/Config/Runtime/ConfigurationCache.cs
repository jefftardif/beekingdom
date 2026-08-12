using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Config.Runtime
{
    public sealed class ConfigurationCache
    {
        private readonly Dictionary<Type, Dictionary<ConfigurationId, IConfigurationDefinition>> definitionsByType = new Dictionary<Type, Dictionary<ConfigurationId, IConfigurationDefinition>>();

        public void ReplaceAll(IEnumerable<IConfigurationDefinition> definitions)
        {
            definitionsByType.Clear();

            foreach (IConfigurationDefinition definition in definitions)
            {
                Type type = definition.GetType();
                if (!definitionsByType.TryGetValue(type, out Dictionary<ConfigurationId, IConfigurationDefinition> typedDefinitions))
                {
                    typedDefinitions = new Dictionary<ConfigurationId, IConfigurationDefinition>();
                    definitionsByType[type] = typedDefinitions;
                }

                typedDefinitions[definition.Id] = definition;
            }
        }

        public bool TryGet<TDefinition>(ConfigurationId id, out TDefinition definition) where TDefinition : class, IConfigurationDefinition
        {
            definition = null;
            if (!definitionsByType.TryGetValue(typeof(TDefinition), out Dictionary<ConfigurationId, IConfigurationDefinition> typedDefinitions))
            {
                return false;
            }

            if (!typedDefinitions.TryGetValue(id, out IConfigurationDefinition value))
            {
                return false;
            }

            definition = value as TDefinition;
            return definition != null;
        }

        public TDefinition GetById<TDefinition>(ConfigurationId id) where TDefinition : class, IConfigurationDefinition
        {
            if (TryGet(id, out TDefinition definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Configuration {typeof(TDefinition).Name}/{id} was not found.");
        }

        public IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : class, IConfigurationDefinition
        {
            if (!definitionsByType.TryGetValue(typeof(TDefinition), out Dictionary<ConfigurationId, IConfigurationDefinition> typedDefinitions))
            {
                return Array.Empty<TDefinition>();
            }

            return typedDefinitions.Values.Cast<TDefinition>().ToList();
        }
    }
}
