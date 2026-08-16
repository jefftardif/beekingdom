using System.Collections.Generic;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tests.Editor.Interaction
{
    public class BuildingInteractionBootstrapTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.SceneManagement.Scene scene in UnityEngine.SceneManagement.SceneManager.GetAllScenes())
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root == null) continue;
                    if (root.name.StartsWith("RuntimeHit_"))
                        Object.DestroyImmediate(root);
                    else
                        DestroyRuntimeHitChildren(root);
                }
            }
        }

        private static void DestroyRuntimeHitChildren(GameObject go)
        {
            if (go == null) return;
            for (int i = go.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = go.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("RuntimeHit_")) Object.DestroyImmediate(child);
            }
        }

        [Test]
        public void MaterializeFromRealSidecarRegistersFourteen()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            int materialized = BuildingInteractionBootstrap.MaterializeRuntimeHitZones(registry);
            Assert.That(materialized, Is.EqualTo(14),
                "Le sidecar réel doit matérialiser 14 zones de clic.");
            Assert.That(registry.Count, Is.EqualTo(14));
        }

        [Test]
        public void MaterializedZonesAreIdentifiableByBuildingType()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            BuildingInteractionBootstrap.MaterializeRuntimeHitZones(registry);

            for (int i = 0; i < BuildingTypes.All.Length; i++)
            {
                Assert.DoesNotThrow(() =>
                {
                    BuildingDefinition d = registry.GetByBuildingType(BuildingTypes.All[i]);
                    Assert.That(d, Is.Not.Null);
                }, "Bâtiment non identifiable : " + BuildingTypes.All[i]);

                GameObject go = registry.GetGameObjectByBuildingType(BuildingTypes.All[i]);
                Assert.That(go, Is.Not.Null);
                BuildingDefinition definition;
                Assert.That(registry.TryGetBuilding(go, out definition), Is.True);
                Assert.That(definition.BuildingType, Is.EqualTo(BuildingTypes.All[i]));
            }
        }

        [Test]
        public void MaterializedZonesMapLegacyKeys()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            BuildingInteractionBootstrap.MaterializeRuntimeHitZones(registry);

            for (int i = 0; i < BuildingMappingTable.All.Count; i++)
            {
                LegacyMappingEntry mapping = BuildingMappingTable.All[i];
                GameObject legacyGo = registry.GetGameObjectByLegacyKey(mapping.LegacyKey);
                Assert.That(legacyGo, Is.Not.Null);
                Assert.That(registry.GetBuilding(legacyGo).BuildingType, Is.EqualTo(mapping.BuildingType));
            }
        }

        [Test]
        public void FutureBuildingsPreserveFutureStateFromSidecar()
        {
            BuildingInteractionRegistry registry = new BuildingInteractionRegistry();
            BuildingInteractionBootstrap.MaterializeRuntimeHitZones(registry);

            string[] future = { BuildingTypes.ChampionHall, BuildingTypes.Bank, BuildingTypes.Academy };
            for (int i = 0; i < future.Length; i++)
            {
                BuildingDefinition definition = registry.GetByBuildingType(future[i]);
                Assert.That(definition.StateIsFuture, Is.True, future[i] + " doit rester Future");
            }
        }
    }
}