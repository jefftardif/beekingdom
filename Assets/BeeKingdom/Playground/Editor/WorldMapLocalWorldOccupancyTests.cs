using System;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldMapLocalWorldOccupancyTests
    {
        private static WorldMapLocalWorldOccupancy.Claim Claim(string id,float x,float y,
            float w=240,float h=150) => new WorldMapLocalWorldOccupancy.Claim {
                Id=id,WorldPoint=new Vector2(x,y),Size=new Vector2(w,h),
                Offset=new Vector2(0,20f)
            };

        [Test]
        public void ScreenshotSpiderReservesDifferentWorldPositionFromHiveAndYellowFlowers()
        {
            var planner=new WorldMapLocalWorldOccupancy(14);
            Assert.That(planner.TryPin(Claim("hive:JEFF",1000,1000,270,186)),Is.True);
            var yellow=planner.Reserve(Claim("resource:pollen",1030,1000,222,154),
                new [] {new Vector2(1030,1000),new Vector2(1370,1040)},_=>true);
            Assert.That(yellow.Accepted,Is.True);
            var spider=planner.Reserve(Claim("bestiary:T3",1030,980),
                new [] {new Vector2(1030,980),new Vector2(1300,800),new Vector2(1600,1200)},_=>true);
            Assert.That(spider.Accepted,Is.True);
            Assert.That(spider.Position,Is.Not.EqualTo(new Vector2(1030,980)));
            Assert.That(planner.IsClear(Claim("bestiary:other",spider.Position.x,spider.Position.y,
                240,150)),Is.False);
            Assert.That(planner.Count,Is.EqualTo(3));
        }

        [Test]
        public void RejectedMovementPreservesPositionAndBlocksNewClaim()
        {
            var ledger=new WorldMapLocalWorldOccupancy();
            Assert.That(ledger.TryPin(Claim("hive:old",1000,1000)),Is.True);
            var rejected=ledger.Reserve(Claim("bestiary:T3",1000,1000),
                new [] {new Vector2(1000,1000),new Vector2(1600,1000)},
                point=>point.x<=1000);
            Assert.That(rejected.Accepted,Is.False);
            Assert.That(rejected.Position,Is.EqualTo(new Vector2(1000,1000)));
            Assert.That(ledger.ObserveUnresolved(Claim("bestiary:T3",1000,1000)),Is.True);
            Assert.That(ledger.Reserve(Claim("resource:later",1000,1000),
                new [] {new Vector2(1000,1000) },_=>true).Accepted,Is.False);
        }

        [Test]
        public void AlreadyPinnedWaterCannotBeRelocatedByAnotherClaim()
        {
            var ledger=new WorldMapLocalWorldOccupancy();
            var water=Claim("resource:water",1200,900,180,80);
            Assert.That(ledger.TryPin(water),Is.True);
            var attempt=ledger.Reserve(Claim("bestiary:near-water",1200,900),
                new [] {new Vector2(1200,900),new Vector2(1550,900)},_=>true);
            Assert.That(attempt.Position,Is.EqualTo(new Vector2(1550,900)));
            Assert.That(ledger.TryGet("resource:water",out var still),Is.True);
            Assert.That(still.WorldPoint,Is.EqualTo(water.WorldPoint));
        }

        [Test]
        public void DeterministicAndStableWithFixedCandidatesAcrossZoomChanges()
        {
            var candidates=new []{new Vector2(1000,1000),new Vector2(1400,1000),new Vector2(1400,1400)};
            Vector2 accepted=Vector2.zero;
            foreach(float zoom in new [] {.30f,.55f,1.0f,1.5f,2.65f})
            {
                var ledger=new WorldMapLocalWorldOccupancy();
                ledger.TryPin(Claim("hive:JEFF",1000,1000));
                var decision=ledger.Reserve(Claim("bestiary:T3",1000,1000),candidates,_=>true);
                Assert.That(decision.Accepted,Is.True);
                if(accepted==Vector2.zero)accepted=decision.Position;
                Assert.That(decision.Position,Is.EqualTo(accepted),
                    "World placement must be independent of rendering zoom="+zoom);
            }
        }
    }
}
