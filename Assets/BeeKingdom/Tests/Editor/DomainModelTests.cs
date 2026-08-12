using System;
using BeeKingdom.Gameplay.Domain.Collections;
using BeeKingdom.Gameplay.Domain.Entities;
using BeeKingdom.Gameplay.Domain.Enums;
using BeeKingdom.Gameplay.Domain.Identifiers;
using BeeKingdom.Gameplay.Domain.ValueObjects;
using NUnit.Framework;
using GameplayHive = BeeKingdom.Gameplay.Domain.Entities.Hive;

namespace BeeKingdom.Tests.Editor
{
    public sealed class DomainModelTests
    {
        [Test]
        public void TypedIdentifiersAreUnique()
        {
            PlayerId first = PlayerId.New();
            PlayerId second = PlayerId.New();

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void HiveRequiresExactlyOneQueen()
        {
            DateTime now = DateTime.UtcNow;
            HiveId hiveId = HiveId.New();
            Queen queen = new Queen(BeeId.New(), hiveId, new Health(10, 10), new Energy(5, 5), now);

            GameplayHive hive = new GameplayHive(hiveId, PlayerId.New(), queen, now);

            Assert.That(hive.Queen, Is.SameAs(queen));
            Assert.Throws<ArgumentNullException>(() => new GameplayHive(HiveId.New(), PlayerId.New(), null, now));
        }

        [Test]
        public void BeeAndBuildingAlwaysReferenceHive()
        {
            DateTime now = DateTime.UtcNow;
            HiveId hiveId = HiveId.New();

            Bee bee = new Bee(BeeId.New(), hiveId, BeeRole.Worker, BeeState.Idle, new Health(8, 10), new Energy(4, 10), now);
            Building building = new Building(BuildingId.New(), hiveId, BuildingType.FlowerGarden, new Position2D(1, 2), now);

            Assert.That(bee.HiveId, Is.EqualTo(hiveId));
            Assert.That(building.HiveId, Is.EqualTo(hiveId));
        }

        [Test]
        public void ValueObjectsAreImmutable()
        {
            ResourceAmount amount = new ResourceAmount(ResourceType.Honey, 25);
            Health health = new Health(3, 10);
            Energy energy = new Energy(4, 12);
            Position2D position = new Position2D(2, 5);

            Assert.That(amount.Amount, Is.EqualTo(25));
            Assert.That(health.Maximum, Is.EqualTo(10));
            Assert.That(energy.Current, Is.EqualTo(4));
            Assert.That(position.X, Is.EqualTo(2));
        }

        [Test]
        public void CollectionsExposePreparedApi()
        {
            DateTime now = DateTime.UtcNow;
            HiveId hiveId = HiveId.New();
            Bee bee = new Bee(BeeId.New(), hiveId, BeeRole.Builder, BeeState.Idle, new Health(5, 5), new Energy(5, 5), now);
            BeeCollection bees = new BeeCollection();

            bees.Add(bee);

            Assert.That(bees.Count, Is.EqualTo(1));
            Assert.That(bees.TryGet(bee.Id, out Bee resolved), Is.True);
            Assert.That(resolved, Is.SameAs(bee));
        }
    }
}
