using NUnit.Framework;
using BeeKingdom.Playground;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldTargetPanelPolicyTests
    {
        [Test]
        public void PanelKeepsExactlyOneTargetAndCanBeDismissed()
        {
            var selection=new WorldTargetPanelSelection();
            Assert.That(selection.Visible,Is.False);
            selection.Open(WorldTargetPanelMode.Resource,"res_pollen_core");
            Assert.That(selection.IsTarget(WorldTargetPanelMode.Resource,"res_pollen_core"),Is.True);
            selection.Open(WorldTargetPanelMode.Enemy,"spider_next_to_hive");
            Assert.That(selection.IsTarget(WorldTargetPanelMode.Resource,"res_pollen_core"),Is.False);
            Assert.That(selection.IsTarget(WorldTargetPanelMode.Enemy,"spider_next_to_hive"),Is.True);
            selection.Close();
            Assert.That(selection.Visible,Is.False);
        }

        [Test]
        public void EmptyTargetCannotOpenOverlay()
        {
            var selection=new WorldTargetPanelSelection();
            selection.Open(WorldTargetPanelMode.Enemy," ");
            Assert.That(selection.Visible,Is.False);
            selection.Open(WorldTargetPanelMode.None,"ignored");
            Assert.That(selection.Visible,Is.False);
        }

        [Test]
        public void EnemyWithoutSquadOpensActualBarracksFlow()
        {
            var action=WorldTargetPanelPolicy.Enemy(true,false,true,false,true);
            Assert.That(action.Action,Is.EqualTo(WorldTargetPrimaryAction.PrepareArmy));
            Assert.That(action.Enabled,Is.True);
        }

        [Test]
        public void PreparedSquadRoutesToOfficialPatrolOverlay()
        {
            var action=WorldTargetPanelPolicy.Enemy(true,true,true,false,true);
            Assert.That(action.Action,Is.EqualTo(WorldTargetPrimaryAction.PlanAttack));
        }

        [Test]
        public void EnemyPatrolUnavailableOrAtCapacityCannotLaunch()
        {
            Assert.That(WorldTargetPanelPolicy.Enemy(true,true,false,false,true).Enabled,Is.False);
            Assert.That(WorldTargetPanelPolicy.Enemy(true,true,true,true,true).Enabled,Is.False);
            Assert.That(WorldTargetPanelPolicy.Enemy(true,true,true,false,false).Enabled,Is.False);
        }

        [Test]
        public void MissingEnemyDoesNotOfferCombat()
        {
            Assert.That(WorldTargetPanelPolicy.Enemy(false,true,true,false,true).Enabled,Is.False);
        }

        private static WorldTargetActionDecision Gather(
            bool official=true,bool active=false,bool claim=false,
            bool elsewhere=false,bool placement=true,bool roster=true,
            bool slot=true,bool ready=true,bool busy=false,bool hasHive=true,
            bool server=true,bool hasNode=true,bool exists=true)=>
            WorldTargetPanelPolicy.Resource(exists,hasHive,official,server,
                busy,claim,active,elsewhere,hasNode,placement,roster,slot,ready);

        [Test]
        public void NonOfficialResourcesCannotCreateFakePersistentRewards()
        {
            var action=Gather(official:false);
            Assert.That(action.Enabled,Is.True);
            Assert.That(action.Action,Is.EqualTo(WorldTargetPrimaryAction.PreviewGather));
            Assert.That(action.ReasonKey,Is.EqualTo("target.reason.preview_only"));
            Assert.That(Gather(official:false,ready:false).Enabled,Is.False);
            Assert.That(Gather(official:false,hasHive:false).Enabled,Is.False);
        }

        [Test]
        public void OfficialResourceOpensServerSquadComposition()
        {
            Assert.That(Gather().Action,Is.EqualTo(WorldTargetPrimaryAction.PrepareGather));
        }

        [Test]
        public void ActiveFlightOffersRecallNotSecondLaunch()
        {
            Assert.That(Gather(active:true).Action,
                Is.EqualTo(WorldTargetPrimaryAction.RecallGatherers));
        }

        [Test]
        public void CompletedFlightOffersOfficialClaim()
        {
            Assert.That(Gather(claim:true).Action,
                Is.EqualTo(WorldTargetPrimaryAction.ClaimGathering));
        }

        [Test]
        public void FlightOnOtherResourceCannotLaunch()
        {
            var action=Gather(elsewhere:true);
            Assert.That(action.Enabled,Is.False);
            Assert.That(action.ReasonKey,Is.EqualTo("target.reason.other_flight"));
        }

        [Test]
        public void MissingTerrainRosterOrMarchCapacityPreventsLaunch()
        {
            Assert.That(Gather(placement:false).Enabled,Is.False);
            Assert.That(Gather(roster:false).Enabled,Is.False);
            Assert.That(Gather(slot:false).Enabled,Is.False);
        }

        [Test]
        public void ServerStateMustConfirmReadiness()
        {
            Assert.That(Gather(ready:false).Enabled,Is.False);
            Assert.That(Gather(server:false).Enabled,Is.False);
            Assert.That(Gather(hasNode:false).Enabled,Is.False);
            Assert.That(Gather(busy:true).Enabled,Is.False);
        }

        [Test]
        public void MissingHiveAndTargetCannotLaunch()
        {
            Assert.That(Gather(hasHive:false).Enabled,Is.False);
            Assert.That(Gather(exists:false).Enabled,Is.False);
        }

        [Test]
        public void BusyServerBlocksClaimUntilValidated()
        {
            Assert.That(Gather(claim:true,busy:true).Enabled,Is.False);
        }
    }
}
