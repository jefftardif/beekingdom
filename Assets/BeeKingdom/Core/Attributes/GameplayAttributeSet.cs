using System.Collections.Generic;

namespace BeeKingdom.Core.Attributes
{
    public sealed class GameplayAttributeSet
    {
        private readonly Dictionary<string, GameplayAttributeInstance> attributes = new Dictionary<string, GameplayAttributeInstance>();

        public string OwnerId { get; }
        public string SetId { get; }
        public IReadOnlyDictionary<string, GameplayAttributeInstance> Attributes => attributes;

        public GameplayAttributeSet(string ownerId, string setId)
        {
            OwnerId = string.IsNullOrWhiteSpace(ownerId) ? "owner" : ownerId;
            SetId = string.IsNullOrWhiteSpace(setId) ? "attributes" : setId;
        }

        public void Add(GameplayAttributeInstance instance)
        {
            attributes[instance.Definition.AttributeId] = instance;
        }

        public bool TryGet(string attributeId, out GameplayAttributeInstance instance)
        {
            return attributes.TryGetValue(attributeId, out instance);
        }
    }
}
