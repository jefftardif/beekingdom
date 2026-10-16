using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class HiveStartupSynchronizationTests
    {
        [Test]
        public void WaitingForEssentialData_DoesNotBecomeReadyAfterCosmeticDelay()
        {
            double time=0;
            var barrier=new HiveStartupReadiness(new[]{new HiveStartupStep("resources",true,()=>HiveStartupStepState.Waiting)},()=>time);
            time=2.5;
            Assert.That(barrier.Poll().Ready,Is.False);
            Assert.That(barrier.Poll().Failed,Is.False);
            Assert.That(barrier.Poll().Progress,Is.LessThan(1f));
        }
        [Test]
        public void EssentialSuccessAndUnavailableOptionalService_AllowEntryWithWarning()
        {
            var barrier=new HiveStartupReadiness(new[]{
                new HiveStartupStep("resources",true,()=>HiveStartupStepState.Ready),
                new HiveStartupStep("nursery",false,()=>HiveStartupStepState.Unavailable)},()=>0);
            var state=barrier.Poll();
            Assert.That(state.Ready,Is.True);Assert.That(state.Failed,Is.False);
            CollectionAssert.AreEqual(new[]{"nursery"},state.Unavailable);
            Assert.That(state.Progress,Is.EqualTo(1f));
        }
        [Test]
        public void RequiredFailure_DoesNotShowAnEmptyHiveAsReady()
        {
            var barrier=new HiveStartupReadiness(new[]{new HiveStartupStep("buildings",true,()=>HiveStartupStepState.Unavailable)},()=>0);
            Assert.That(barrier.Poll().Ready,Is.False);Assert.That(barrier.Poll().Failed,Is.True);
        }
        [Test]
        public void RequiredTimeout_IsBoundedAndNeedsExplicitRetry()
        {
            double time=0;HiveStartupStepState read=HiveStartupStepState.Waiting;
            var barrier=new HiveStartupReadiness(new[]{new HiveStartupStep("buildings",true,()=>read)},()=>time,45);
            time=46;Assert.That(barrier.Poll().Failed,Is.True);
            read=HiveStartupStepState.Ready;Assert.That(barrier.Poll().Failed,Is.True);
            var retry=new HiveStartupReadiness(new[]{new HiveStartupStep("buildings",true,()=>read)},()=>time,45);
            Assert.That(retry.Poll().Ready,Is.True);
        }
        [Test]
        public void OptionalTimeout_DoesNotBlockKnownGoodEssentialData()
        {
            double time=0;
            var barrier=new HiveStartupReadiness(new[]{new HiveStartupStep("resources",true,()=>HiveStartupStepState.Ready),
                new HiveStartupStep("mail",false,()=>HiveStartupStepState.Waiting)},()=>time,45);
            Assert.That(barrier.Poll().Ready,Is.False);time=46;
            Assert.That(barrier.Poll().Ready,Is.True);CollectionAssert.AreEqual(new[]{"mail"},barrier.Poll().Unavailable);
        }
        [Test]
        public void ReadyBarrier_DoesNotReappearWhenAWindowRefreshesLater()
        {
            HiveStartupStepState value=HiveStartupStepState.Ready;
            var barrier=new HiveStartupReadiness(new[]{new HiveStartupStep("buildings",true,()=>value)},()=>0);
            Assert.That(barrier.Poll().Ready,Is.True);value=HiveStartupStepState.Waiting;
            Assert.That(barrier.Poll().Ready,Is.True);
        }
        [Test]
        public void ProbeExceptions_FailClosedWithoutThrowingInOnGUI()
        {
            var barrier=new HiveStartupReadiness(new[]{new HiveStartupStep("resources",true,()=>throw new InvalidOperationException())},()=>0);
            Assert.That(barrier.Poll().Failed,Is.True);
        }
        [TestCase("Loading",true,HiveStartupStepState.Waiting)]
        [TestCase("NotConfigured",false,HiveStartupStepState.Waiting)]
        [TestCase("Ready",true,HiveStartupStepState.Ready)]
        [TestCase("Ready",false,HiveStartupStepState.Unavailable)]
        [TestCase("NoAlliance",true,HiveStartupStepState.Ready)]
        [TestCase("ReadyToLaunch",true,HiveStartupStepState.Ready)]
        [TestCase("Error",true,HiveStartupStepState.Unavailable)]
        [TestCase("OfflineReadOnly",true,HiveStartupStepState.Unavailable)]
        public void StatusProjection_PreservesMeaningOfReadResults(string state,bool data,HiveStartupStepState expected)
        {Assert.That(HiveStartupReadiness.FromModel(state,data),Is.EqualTo(expected));}
        [Test]
        public void SameSourceAccountHiveAndServer_ReuseControllers()
        {
            object source=new object();Guid player=Guid.NewGuid(),hive=Guid.NewGuid();
            var identity=new HiveGameplayBindingIdentity(source,player,hive,"https://test.invalid",true);
            Assert.That(identity.Matches(source,player,hive,"https://test.invalid",true),Is.True);
        }
        [TestCase("source")]
        [TestCase("player")]
        [TestCase("hive")]
        [TestCase("server")]
        [TestCase("authority")]
        public void DifferentSessionIdentity_DoesNotReusePriorPlayersData(string difference)
        {
            object source=new object();Guid player=Guid.NewGuid(),hive=Guid.NewGuid();
            var identity=new HiveGameplayBindingIdentity(source,player,hive,"https://test.invalid",true);
            Assert.That(identity.Matches(difference=="source"?new object():source,
                difference=="player"?Guid.NewGuid():player,difference=="hive"?Guid.NewGuid():hive,
                difference=="server"?"https://other.invalid":"https://test.invalid",difference!="authority"),Is.False);
        }
        [Test]
        public void LiveEntryWaitsBeforeSettingEnteredHive_AndLoadsHeaderFirst()
        {
            string source=Read("HiveViewProductUiPresenter.cs");
            int start=source.IndexOf("private static void EnterHiveFromSplash(string route)",StringComparison.Ordinal);
            int end=source.IndexOf("private static void LoadHiveMapScene()",start,StringComparison.Ordinal);
            string method=source.Substring(start,end-start);
            Assert.That(method.IndexOf("DeferHiveEntryUntilSynchronized",StringComparison.Ordinal),Is.LessThan(method.IndexOf("SplashAuthGateState.EnteredHive",StringComparison.Ordinal)));
            Assert.That(method.IndexOf("PublishSynchronizedHeaderBeforeEntry",StringComparison.Ordinal),Is.LessThan(method.IndexOf("SplashAuthGateState.EnteredHive",StringComparison.Ordinal)));
            StringAssert.DoesNotContain("elapsed / StartupLoadingScreenDurationSeconds",source);
        }
        [Test]
        public void StartupReadSchedule_DoesNotClaimRewardsOrPerformEconomicActions()
        {
            string source=Read("MobileAccountSessionRuntimeBootstrap.Startup.cs");
            foreach(string mutation in new[]{"DepositAsync(","WithdrawAsync(",".Collect(",".Claim(",".Complete(","StartCareAsync("})
                StringAssert.DoesNotContain(mutation,source);
            StringAssert.Contains("bank.ReadAsync",source);
            StringAssert.Contains("startupLifetime?.Cancel()",source);
        }
        [Test]
        public void ReconfigurationChecksIdentityBeforeTearingDownAValidSession()
        {
            string source=Read("MobileAccountSessionRuntimeBootstrap.cs");
            int start=source.IndexOf("public static bool TryConfigureGameplayForActiveSession()",StringComparison.Ordinal);
            string method=source.Substring(start,source.IndexOf("private static void HandleGameplayNetworkSignal",start,StringComparison.Ordinal)-start);
            StringAssert.DoesNotContain("TryConfigureGameplayForActiveSession()\n        {\n            CloseGameplayForSignedOutSession",method.Replace("\r\n","\n"));
            StringAssert.Contains("CanReuseGameplayBinding",method);
            StringAssert.Contains("BeginStartupDataTracking()",method);
            StringAssert.Contains("generation != startupGeneration",source);
        }
        [Test]
        public void BankOpeningConsumesPreloadedSnapshotInsteadOfResettingTheWholeHive()
        {
            string source=Read("HiveViewProductUiPresenter.Bank.cs");
            StringAssert.Contains("TakeStartupBankSnapshot()",source);
            StringAssert.Contains("buildingUpgradeController.Model.BuildingLevels.Count == 0",source);
        }
        private static string Read(string name)=>File.ReadAllText(Path.Combine(Application.dataPath,"BeeKingdom/Playground",name));
    }
}