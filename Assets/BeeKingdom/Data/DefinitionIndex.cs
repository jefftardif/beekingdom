using System;
using System.Collections.Generic;
using BeeKingdom.Config.Runtime;

namespace BeeKingdom.Data
{
    public sealed class DefinitionIndex
    {
        private readonly Dictionary<Type, Dictionary<ConfigurationId, IConfigurationDefinition>> definitionsByType = new Dictionary<Type, Dictionary<ConfigurationId, IConfigurationDefinition>>();
        private readonly Dictionary<Type, object> allByType = new Dictionary<Type, object>();

        public int Count { get; private set; }

        public void ReplaceAll(IReadOnlyList<IConfigurationDefinition> definitions)
        {
            definitionsByType.Clear();
            allByType.Clear();
            Count = definitions.Count;

            Dictionary<Type, List<IConfigurationDefinition>> lists = new Dictionary<Type, List<IConfigurationDefinition>>();
            for (int i = 0; i < definitions.Count; i++)
            {
                IConfigurationDefinition definition = definitions[i];
                Type type = definition.GetType();
                if (!definitionsByType.TryGetValue(type, out Dictionary<ConfigurationId, IConfigurationDefinition> typed))
                {
                    typed = new Dictionary<ConfigurationId, IConfigurationDefinition>();
                    definitionsByType[type] = typed;
                    lists[type] = new List<IConfigurationDefinition>();
                }

                typed[definition.Id] = definition;
                lists[type].Add(definition);
            }

            foreach (KeyValuePair<Type, List<IConfigurationDefinition>> pair in lists)
            {
                Array typedArray = Array.CreateInstance(pair.Key, pair.Value.Count);
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    typedArray.SetValue(pair.Value[i], i);
                }

                allByType[pair.Key] = typedArray;
            }
        }

        public bool TryGet<TDefinition>(ConfigurationId id, out TDefinition definition) where TDefinition : class, IConfigurationDefinition
        {
            if (definitionsByType.TryGetValue(typeof(TDefinition), out Dictionary<ConfigurationId, IConfigurationDefinition> typed) &&
                typed.TryGetValue(id, out IConfigurationDefinition value))
            {
                definition = value as TDefinition;
                return definition != null;
            }

            definition = null;
            return false;
        }

        public IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : class, IConfigurationDefinition
        {
            if (allByType.TryGetValue(typeof(TDefinition), out object value))
            {
                return (TDefinition[])value;
            }

            return Array.Empty<TDefinition>();
        }

        public bool ContainsAny(ConfigurationId id)
        {
            foreach (Dictionary<ConfigurationId, IConfigurationDefinition> typed in definitionsByType.Values)
            {
                if (typed.ContainsKey(id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
