using System.Collections.Generic;

namespace BeeKingdom.Core.Attributes
{
    public sealed class GameplayAttributeRegistry
    {
        private readonly Dictionary<string, GameplayAttributeDefinition> definitions = new Dictionary<string, GameplayAttributeDefinition>();

        public int Count => definitions.Count;

        public bool RegisterAttribute(GameplayAttributeDefinition definition)
        {
            if (definition == null || definitions.ContainsKey(definition.AttributeId)) return false;
            definitions.Add(definition.AttributeId, definition);
            return true;
        }

        public bool TryGet(string attributeId, out GameplayAttributeDefinition definition)
        {
            return definitions.TryGetValue(attributeId, out definition);
        }
    }
}
