using System;
using System.Collections.Generic;
using BeeKingdom.Buildings.Interaction;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor.Interaction
{
    public class BuildingMappingTableTests
    {
        [Test]
        public void HasExactlyFourteenEntries()
        {
            Assert.That(BuildingMappingTable.Count, Is.EqualTo(14));
        }

        [Test]
        public void AllMappingsAreResolved()
        {
            Assert.That(BuildingMappingTable.All.Count, Is.EqualTo(14));
            for (int i = 0; i < BuildingMappingTable.All.Count; i++)
            {
                Assert.That(BuildingMappingTable.All[i], Is.Not.Null);
                Assert.That(BuildingMappingTable.All[i].BuildingType, Is.Not.Null.Or.Empty);
                Assert.That(BuildingMappingTable.All[i].LegacyKey, Is.Not.Null.Or.Empty);
            }
        }

        [Test]
        public void NoDuplicateBuildingTypes()
        {
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < BuildingMappingTable.All.Count; i++)
            {
                Assert.That(seen.Add(BuildingMappingTable.All[i].BuildingType), Is.True,
                    "buildingType dupliqué : " + BuildingMappingTable.All[i].BuildingType);
            }
        }

        [Test]
        public void NoDuplicateLegacyKeys()
        {
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < BuildingMappingTable.All.Count; i++)
            {
                Assert.That(seen.Add(BuildingMappingTable.All[i].LegacyKey), Is.True,
                    "legacyKey dupliquée : " + BuildingMappingTable.All[i].LegacyKey);
            }
        }

        [Test]
        public void EveryBuildingTypeHasMapping()
        {
            for (int i = 0; i < BuildingTypes.All.Length; i++)
            {
                Assert.DoesNotThrow(() => BuildingMappingTable.GetByBuildingType(BuildingTypes.All[i]),
                    "Missing mapping à buildingType " + BuildingTypes.All[i]);
            }
        }

        [Test]
        public void EveryLegacyKeyHasMapping()
        {
            for (int i = 0; i < BuildingLegacyKeys.All.Length; i++)
            {
                Assert.DoesNotThrow(() => BuildingMappingTable.GetByLegacyKey(BuildingLegacyKeys.All[i]),
                    "Missing mapping à legacyKey " + BuildingLegacyKeys.All[i]);
            }
        }

        [Test]
        public void LookupByBuildingType()
        {
            LegacyMappingEntry entry = BuildingMappingTable.GetByBuildingType(BuildingTypes.HoneyReserve);
            Assert.That(entry.LegacyKey, Is.EqualTo(BuildingLegacyKeys.HoneyStorage));
        }

        [Test]
        public void LookupByLegacyKey()
        {
            LegacyMappingEntry entry = BuildingMappingTable.GetByLegacyKey(BuildingLegacyKeys.HoneyStorage);
            Assert.That(entry.BuildingType, Is.EqualTo(BuildingTypes.HoneyReserve));
        }

        [Test]
        public void TryLookupByBuildingType()
        {
            LegacyMappingEntry entry;
            Assert.That(BuildingMappingTable.TryGetByBuildingType(BuildingTypes.Barrack, out entry), Is.True);
            Assert.That(entry.LegacyKey, Is.EqualTo(BuildingLegacyKeys.GuardPost));
            Assert.That(BuildingMappingTable.TryGetByBuildingType("INCONNU", out entry), Is.False);
        }

        [Test]
        public void TryLookupByLegacyKey()
        {
            LegacyMappingEntry entry;
            Assert.That(BuildingMappingTable.TryGetByLegacyKey(BuildingLegacyKeys.GuardPost, out entry), Is.True);
            Assert.That(entry.BuildingType, Is.EqualTo(BuildingTypes.Barrack));
            Assert.That(BuildingMappingTable.TryGetByLegacyKey("inconnu", out entry), Is.False);
        }

        [Test]
        public void ToLegacyKeyConversion()
        {
            Assert.That(BuildingMappingTable.ToLegacyKey(BuildingTypes.Warehouse), Is.EqualTo(BuildingLegacyKeys.WarehouseCells));
            Assert.That(BuildingMappingTable.ToLegacyKey(BuildingTypes.Transformation), Is.EqualTo(BuildingLegacyKeys.WaxWorkshop));
            Assert.That(BuildingMappingTable.ToLegacyKey(BuildingTypes.RoyalPalace), Is.EqualTo(BuildingLegacyKeys.AdministrationCore));
        }

        [Test]
        public void ToBuildingTypeConversion()
        {
            Assert.That(BuildingMappingTable.ToBuildingType(BuildingLegacyKeys.ResearchNode), Is.EqualTo(BuildingTypes.Research));
            Assert.That(BuildingMappingTable.ToBuildingType(BuildingLegacyKeys.AdministrationCore), Is.EqualTo(BuildingTypes.RoyalPalace));
        }

        [Test]
        public void ChampionHallIsArchivesHoneyfall()
        {
            LegacyMappingEntry entry = BuildingMappingTable.GetByBuildingType(BuildingTypes.ChampionHall);
            Assert.That(entry.LegacyKey, Is.EqualTo(BuildingLegacyKeys.ArchivesHoneyfall));
        }

        [Test]
        public void ValidateRejectsWrongCount()
        {
            List<LegacyMappingEntry> wrong = new List<LegacyMappingEntry>
            {
                new LegacyMappingEntry(BuildingTypes.Nursery, BuildingLegacyKeys.NurseryCluster)
            };
            Assert.Throws<InvalidOperationException>(() => BuildingMappingTable.Validate(wrong));
        }

        [Test]
        public void ValidateRejectsDuplicateBuildingType()
        {
            List<LegacyMappingEntry> dup = new List<LegacyMappingEntry>();
            for (int i = 0; i < BuildingMappingTable.All.Count; i++)
                dup.Add(BuildingMappingTable.All[i]);
            dup[0] = new LegacyMappingEntry(dup[1].BuildingType, dup[1].LegacyKey);
            Assert.Throws<InvalidOperationException>(() => BuildingMappingTable.Validate(dup));
        }

        [Test]
        public void ValidateRejectsDuplicateLegacyKey()
        {
            List<LegacyMappingEntry> dup = new List<LegacyMappingEntry>();
            for (int i = 0; i < BuildingMappingTable.All.Count; i++)
                dup.Add(BuildingMappingTable.All[i]);
            dup[0] = new LegacyMappingEntry(dup[0].BuildingType, dup[1].LegacyKey);
            Assert.Throws<InvalidOperationException>(() => BuildingMappingTable.Validate(dup));
        }

        [Test]
        public void ValidateRejectsMissingBuildingType()
        {
            List<LegacyMappingEntry> missing = new List<LegacyMappingEntry>();
            for (int i = 0; i < BuildingMappingTable.All.Count; i++)
                missing.Add(BuildingMappingTable.All[i]);
            missing.RemoveAt(0);
            Assert.Throws<InvalidOperationException>(() => BuildingMappingTable.Validate(missing));
        }
    }
}