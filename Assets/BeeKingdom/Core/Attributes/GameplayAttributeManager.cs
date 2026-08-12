using System.Collections.Generic;
using BeeKingdom.Core.Modifiers;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Attributes
{
    public sealed class GameplayAttributeManager
    {
        private readonly GameplayAttributeRegistry registry = new GameplayAttributeRegistry();
        private readonly Dictionary<string, GameplayAttributeSet> sets = new Dictionary<string, GameplayAttributeSet>();
        private readonly GameplayModifierEngine modifierEngine;
        private readonly IEventBus eventBus;
        private const int SnapshotVersion = 1;

        public GameplayAttributeDiagnostics Diagnostics { get; } = new GameplayAttributeDiagnostics();

        public GameplayAttributeManager(GameplayModifierEngine modifierEngine = null, IEventBus eventBus = null)
        {
            this.modifierEngine = modifierEngine ?? new GameplayModifierEngine(eventBus);
            this.eventBus = eventBus;
        }

        public bool RegisterAttribute(GameplayAttributeDefinition definition)
        {
            bool registered = registry.RegisterAttribute(definition);
            if (registered)
            {
                Diagnostics.RecordRegistered(registry.Count);
                eventBus?.Publish(new AttributeRegistered(definition.AttributeId));
            }
            return registered;
        }

        public GameplayAttributeSet CreateSet(string ownerId, string setId, params string[] attributeIds)
        {
            GameplayAttributeSet set = new GameplayAttributeSet(ownerId, setId);
            for (int i = 0; i < attributeIds.Length; i++)
            {
                if (registry.TryGet(attributeIds[i], out GameplayAttributeDefinition definition))
                {
                    set.Add(new GameplayAttributeInstance(definition));
                }
            }

            sets[Key(ownerId, setId)] = set;
            Diagnostics.RecordSets(sets.Count);
            return set;
        }

        public double GetValue(string ownerId, string setId, string attributeId)
        {
            return GetInstance(ownerId, setId, attributeId).FinalValue;
        }

        public bool SetBaseValue(string ownerId, string setId, string attributeId, double value)
        {
            GameplayAttributeInstance instance = GetInstance(ownerId, setId, attributeId);
            bool changed = instance.SetBaseValue(value, out bool clamped);
            if (clamped)
            {
                Diagnostics.RecordClamp();
                eventBus?.Publish(new AttributeClamped(ownerId, attributeId));
            }
            if (changed)
            {
                Diagnostics.RecordChange();
                eventBus?.Publish(new AttributeChanged(ownerId, attributeId, instance.BaseValue));
            }
            return changed;
        }

        public bool ModifyValue(string ownerId, string setId, string attributeId, double delta)
        {
            GameplayAttributeInstance instance = GetInstance(ownerId, setId, attributeId);
            return SetBaseValue(ownerId, setId, attributeId, instance.BaseValue + delta);
        }

        public double Recalculate(string ownerId, string setId, string attributeId, ModifierEvaluationContext context = null)
        {
            GameplayAttributeInstance instance = GetInstance(ownerId, setId, attributeId);
            double value = modifierEngine.Evaluate(attributeId, instance.BaseValue, context ?? new ModifierEvaluationContext());
            instance.SetFinalValue(value, out bool clamped);
            if (clamped)
            {
                Diagnostics.RecordClamp();
                eventBus?.Publish(new AttributeClamped(ownerId, attributeId));
            }
            Diagnostics.RecordRecalculation();
            eventBus?.Publish(new AttributeRecalculated(ownerId, attributeId, instance.FinalValue));
            return instance.FinalValue;
        }

        public GameplayAttributeSnapshot Snapshot(string ownerId, string setId)
        {
            GameplayAttributeSet set = sets[Key(ownerId, setId)];
            List<GameplayAttributeSnapshotEntry> entries = new List<GameplayAttributeSnapshotEntry>();
            foreach (GameplayAttributeInstance instance in set.Attributes.Values)
            {
                entries.Add(new GameplayAttributeSnapshotEntry(instance.Definition.AttributeId, instance.BaseValue, instance.FinalValue));
            }
            Diagnostics.RecordSnapshot();
            eventBus?.Publish(new AttributeSnapshotCreated(ownerId));
            return new GameplayAttributeSnapshot(SnapshotVersion, ownerId, setId, entries);
        }

        public void RestoreSnapshot(GameplayAttributeSnapshot snapshot)
        {
            GameplayAttributeSet set = sets[Key(snapshot.OwnerId, snapshot.SetId)];
            for (int i = 0; i < snapshot.Entries.Count; i++)
            {
                GameplayAttributeSnapshotEntry entry = snapshot.Entries[i];
                if (set.TryGet(entry.AttributeId, out GameplayAttributeInstance instance))
                {
                    instance.Restore(entry.BaseValue, entry.FinalValue);
                }
            }
            Diagnostics.RecordRestore();
            eventBus?.Publish(new AttributeRestored(snapshot.OwnerId));
        }

        private GameplayAttributeInstance GetInstance(string ownerId, string setId, string attributeId)
        {
            return sets[Key(ownerId, setId)].Attributes[attributeId];
        }

        private static string Key(string ownerId, string setId)
        {
            return ownerId + "::" + setId;
        }
    }
}
