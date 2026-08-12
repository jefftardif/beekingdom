using BeeKingdom.Core.Attributes;
using BeeKingdom.Core.Modifiers;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class GameplayAttributesFrameworkTests
    {
        [Test]
        public void CreateSetInitializesDefaultValue()
        {
            GameplayAttributeManager manager = CreateManager();

            manager.CreateSet("queen", "QueenAttributes", "Health");

            Assert.That(manager.GetValue("queen", "QueenAttributes", "Health"), Is.EqualTo(100d));
        }

        [Test]
        public void SetBaseValueClampsToLimits()
        {
            GameplayAttributeManager manager = CreateManager();
            manager.CreateSet("queen", "QueenAttributes", "Health");

            manager.SetBaseValue("queen", "QueenAttributes", "Health", 200d);
            manager.Recalculate("queen", "QueenAttributes", "Health");

            Assert.That(manager.GetValue("queen", "QueenAttributes", "Health"), Is.EqualTo(100d));
            Assert.That(manager.Diagnostics.Clamps, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void ModifyValueChangesBaseValue()
        {
            GameplayAttributeManager manager = CreateManager();
            manager.CreateSet("hive", "HiveAttributes", "Population");

            manager.ModifyValue("hive", "HiveAttributes", "Population", 10d);
            manager.Recalculate("hive", "HiveAttributes", "Population");

            Assert.That(manager.GetValue("hive", "HiveAttributes", "Population"), Is.EqualTo(10d));
        }

        [Test]
        public void RecalculateDelegatesToModifierEngine()
        {
            GameplayModifierEngine modifiers = new GameplayModifierEngine();
            modifiers.AddModifier(new GameplayModifierDefinition("bonus", "Energy", GameplayModifierOperation.Add, 25d));
            GameplayAttributeManager manager = CreateManager(modifiers);
            manager.CreateSet("queen", "QueenAttributes", "Energy");

            double value = manager.Recalculate("queen", "QueenAttributes", "Energy");

            Assert.That(value, Is.EqualTo(75d));
        }

        [Test]
        public void SnapshotAndRestoreRoundTrip()
        {
            GameplayAttributeManager manager = CreateManager();
            manager.CreateSet("queen", "QueenAttributes", "Fertility");
            manager.SetBaseValue("queen", "QueenAttributes", "Fertility", 0.5d);
            GameplayAttributeSnapshot snapshot = manager.Snapshot("queen", "QueenAttributes");
            manager.SetBaseValue("queen", "QueenAttributes", "Fertility", 0.1d);

            manager.RestoreSnapshot(snapshot);

            Assert.That(manager.GetValue("queen", "QueenAttributes", "Fertility"), Is.EqualTo(0.5d));
        }

        [Test]
        public void EvaluationIsDeterministic()
        {
            Assert.That(EvaluateSample(), Is.EqualTo(EvaluateSample()));
        }

        [Test]
        public void HandlesLargeAttributeSet()
        {
            GameplayAttributeManager manager = new GameplayAttributeManager();
            string[] ids = new string[10000];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = "Attr" + i;
                manager.RegisterAttribute(new GameplayAttributeDefinition(ids[i], "Load", GameplayAttributeType.Float, i, 0d, 20000d));
            }

            GameplayAttributeSet set = manager.CreateSet("owner", "LoadSet", ids);

            Assert.That(set.Attributes.Count, Is.EqualTo(10000));
            Assert.That(manager.GetValue("owner", "LoadSet", "Attr9999"), Is.EqualTo(9999d));
        }

        private static double EvaluateSample()
        {
            GameplayModifierEngine modifiers = new GameplayModifierEngine();
            modifiers.AddModifier(new GameplayModifierDefinition("bonus", "Health", GameplayModifierOperation.Add, 10d));
            GameplayAttributeManager manager = CreateManager(modifiers);
            manager.CreateSet("queen", "QueenAttributes", "Health");
            return manager.Recalculate("queen", "QueenAttributes", "Health");
        }

        private static GameplayAttributeManager CreateManager(GameplayModifierEngine modifiers = null)
        {
            GameplayAttributeManager manager = new GameplayAttributeManager(modifiers);
            manager.RegisterAttribute(new GameplayAttributeDefinition("Health", "Queen", GameplayAttributeType.Integer, 100d, 0d, 100d));
            manager.RegisterAttribute(new GameplayAttributeDefinition("Energy", "Queen", GameplayAttributeType.Integer, 50d, 0d, 100d));
            manager.RegisterAttribute(new GameplayAttributeDefinition("Fertility", "Queen", GameplayAttributeType.Percentage, 1d, 0d, 1d));
            manager.RegisterAttribute(new GameplayAttributeDefinition("Population", "Colony", GameplayAttributeType.Integer, 0d, 0d, 100000d));
            return manager;
        }
    }
}
