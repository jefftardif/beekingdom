using System.Collections.Generic;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tests.Editor.Interaction
{
    public class BuildingInteractionRegistryTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null) Object.DestroyImmediate(_created[i]);
            }
            _created.Clear();
        }

        private GameObject MakeGo(string name, string buildingType)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<BuildingInteractionComponent>().Configure(buildingType);
            _created.Add(go);
            return go;
        }

        [Test]
        public void RegisterAndGetByGameObject()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            GameObject go = MakeGo("GO", BuildingTypes.Nursery);
            registry.Register(go, BuildingTypes.Nursery);

            BuildingDefinition definition = registry.GetBuilding(go);
            Assert.That(definition.BuildingType, Is.EqualTo(BuildingTypes.Nursery));
            Assert.That(definition.LegacyKey, Is.EqualTo(BuildingLegacyKeys.NurseryCluster));
        }

        [Test]
        public void TryGetBuildingHitAndMiss()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            GameObject go = MakeGo("GO", BuildingTypes.Bank);
            GameObject other = MakeGo("Other", BuildingTypes.Infirmary);
            registry.Register(go, BuildingTypes.Bank);

            BuildingDefinition definition;
            Assert.That(registry.TryGetBuilding(go, out definition), Is.True);
            Assert.That(definition.BuildingType, Is.EqualTo(BuildingTypes.Bank));

            Assert.That(registry.TryGetBuilding(other, out definition), Is.False);
            Assert.That(registry.TryGetBuilding(null, out definition), Is.False);
        }

        [Test]
        public void GetByBuildingTypeAndLegacyKey()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            GameObject go = MakeGo("GO", BuildingTypes.Warehouse);
            registry.Register(go, BuildingTypes.Warehouse);

            Assert.That(registry.GetByBuildingType(BuildingTypes.Warehouse).BuildingType, Is.EqualTo(BuildingTypes.Warehouse));
            Assert.That(registry.GetByLegacyKey(BuildingLegacyKeys.WarehouseCells).LegacyKey, Is.EqualTo(BuildingLegacyKeys.WarehouseCells));
        }

        [Test]
        public void UnregisterRemoves()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            GameObject go = MakeGo("GO", BuildingTypes.Nursery);
            registry.Register(go, BuildingTypes.Nursery);
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Unregister(go), Is.True);
            Assert.That(registry.Count, Is.EqualTo(0));

            BuildingDefinition definition;
            Assert.That(registry.TryGetBuilding(go, out definition), Is.False);
        }

        [Test]
        public void UnregisterUnknownReturnsFalse()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            GameObject go = MakeGo("GO", BuildingTypes.Nursery);
            Assert.That(registry.Unregister(go), Is.False);
        }

        [Test]
        public void RegisterThrowsForUnknownType()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            GameObject go = MakeGo("GO", BuildingTypes.Nursery);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => registry.Register(go, "INCONNU"));
        }

        [Test]
        public void CountTracksRegistrations()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            GameObject a = MakeGo("A", BuildingTypes.Nursery);
            GameObject b = MakeGo("B", BuildingTypes.HoneyReserve);
            registry.Register(a, BuildingTypes.Nursery);
            registry.Register(b, BuildingTypes.HoneyReserve);
            Assert.That(registry.Count, Is.EqualTo(2));
            registry.Clear();
            Assert.That(registry.Count, Is.EqualTo(0));
        }

        [Test]
        public void ComponentResolvesDefinition()
        {
            GameObject go = MakeGo("GO", BuildingTypes.RoyalPalace);
            BuildingInteractionComponent component = go.GetComponent<BuildingInteractionComponent>();
            Assert.That(component, Is.Not.Null);
            Assert.That(component.BuildingType, Is.EqualTo(BuildingTypes.RoyalPalace));
            Assert.That(component.Definition, Is.Not.Null);
            Assert.That(component.Definition.LegacyKey, Is.EqualTo(BuildingLegacyKeys.AdministrationCore));
        }
    }
}