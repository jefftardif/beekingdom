using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    // Editor-only fixtures cannot ship in a player. Every operation below stays local.
public sealed class AllianceRecoveryLocalFixture : IAllianceCenterPanelController
    {
        private readonly AllianceCenterScreenModel model = AllianceCenterPresentation.NotConfigured();
        public AllianceRecoveryLocalFixture(AllianceCenterScreenModel value) { model = value; }

        public AllianceCenterScreenModel Model => model;
        public bool IsConfigured => true;
        public bool IsBusy => false;
        public IReadOnlyList<PlayerSearchResultModel> InvitePlayerSearchResults => Array.Empty<PlayerSearchResultModel>();
        public InvitePlayerSearchStatus InviteSearchStatus => InvitePlayerSearchStatus.Idle;
        public void Refresh() { }
        public void RefreshQuietly() { }
        public void Search(string nameOrTag, string language, RemoteAllianceJoinMode? joinMode) { }
        public void Create(string name, string tag, string description, string language, RemoteAllianceJoinMode joinMode) { }
        public void JoinOpen(Guid allianceId) { }
        public void SubmitApplication(Guid allianceId, string message) { }
        public void AcceptApplication(Guid applicationId) { }
        public void RejectApplication(Guid applicationId) { }
        public void AcceptInvitation(Guid invitationId) { }
        public void DeclineInvitation(Guid invitationId) { }
        public void SearchPlayersForInvite(string query) { }
        public void InvitePlayer(Guid playerId) { }
        public void Leave() { }
        public void Kick(Guid targetPlayerId) { }
        public void Promote(Guid targetPlayerId) { }
        public void Demote(Guid targetPlayerId) { }
        public void TransferLeadership(Guid targetPlayerId) { }
        public void Dissolve() { }
        public void UpdateProfile(string description, string language, string emblemKey, RemoteAllianceJoinMode? joinMode) { }

        public IReadOnlyList<AllianceHelpRowModel> HelpRequests => Array.Empty<AllianceHelpRowModel>();
        public void RefreshHelp() { }
        public void ContributeHelp(Guid helpRequestId) { }
        public void ContributeHelpAll() { }

        private static readonly AllianceHelpOperationState UnknownHelpState = new AllianceHelpOperationState();
        public AllianceHelpOperationState GetHelpOperationState(string operationCategory, string operationTargetId) => UnknownHelpState;
        public void RefreshHelpOperationState(string operationCategory, string operationTargetId) { }
        public void RequestHelp(Guid hiveId, string operationCategory, string operationTargetId) { }

        private static readonly AllianceResearchScreenModel UnknownResearchModel = new AllianceResearchScreenModel();
        public AllianceResearchScreenModel ResearchModel => UnknownResearchModel;
        public void RefreshResearch() { }
        public void SelectResearchFundingTarget(string technologyId) { }
        public void DonateToResearch(Guid hiveId, string technologyId, string resourceKey, long amount) { }
        public void LaunchResearch(string technologyId) { }
        public void ApplyResearchSpeedUp(Guid hiveId, string technologyId, string itemId) { }
        public void SetResearchFundingTargetPickerOpen(bool isMajor, bool open) { }
    }

    public sealed class AllianceActivitiesRecoveryTests
    {
        private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        private static Type Presenter => typeof(HiveViewProductUiPresenter);
        private static IAllianceCenterPanelController previousController;
        private static bool previewActive;
        private static readonly string[] AlphaTabs = { "overview", "members", "chat", "journal", "help", "donations", "research" };

        private static string MethodSource(string name)
        {
            string text = File.ReadAllText(Application.dataPath + "/BeeKingdom/Playground/HiveViewProductUiPresenter.cs");
            var declarations = Regex.Matches(text, @"(?m)^[ \t]*(?:public|private|internal) static [^\r\n]+\([^\r\n]*\)\r?$");
            for (int i = 0; i < declarations.Count; i++)
            {
                var match = declarations[i];
                if (!Regex.IsMatch(match.Value, @"\b" + Regex.Escape(name) + @"\(")) continue;
                int end = i + 1 < declarations.Count ? declarations[i + 1].Index : text.Length;
                return text.Substring(match.Index, end - match.Index);
            }
            Assert.Fail("Missing presentation method: " + name);
            return string.Empty;
        }

        [Test]
        public void AllianceShell_UsesRecoveredActivitiesHeaderNotLegacyThreeColumns()
        {
            string source = MethodSource("DrawAllianceHeadquartersScreen");
            StringAssert.Contains("DrawAllianceActivitiesButton", source);
            StringAssert.Contains("DrawAllianceTopNavigation", source);
            StringAssert.Contains("DrawUnderOwnOverlayGate", source);
            StringAssert.DoesNotContain("DrawAllianceRightColumn(", source);
            StringAssert.DoesNotContain("DrawAllianceNavigation(left", source);
        }

        [Test]
        public void AlphaNavigation_ExposesExactlySevenFunctionalTabs()
        {
            string source = MethodSource("DrawAllianceTopNavigation");
            var ids = Regex.Match(source, @"string\[\] ids\s*=\s*\{([^}]+)\}");
            Assert.That(ids.Success, Is.True);
            string[] actual = Regex.Matches(ids.Groups[1].Value, "\"([^\"]+)\"")
                .Cast<Match>().Select(x => x.Groups[1].Value).ToArray();
            CollectionAssert.AreEqual(AlphaTabs, actual);
            string routes = MethodSource("DrawAllianceTabContent");
            foreach (string tab in AlphaTabs) StringAssert.Contains("\"" + tab + "\"", routes);
        }

        [Test]
        public void MemberAndActionOverlays_KeepInputGateAndPriorityBackNavigation()
        {
            string shell = MethodSource("DrawAllianceHeadquartersScreen");
            StringAssert.Contains("allianceMemberProfileOpen", shell);
            StringAssert.Contains("DrawUnderOwnOverlayGate", shell);
            StringAssert.Contains("ClosePremiumScreensInPriorityOrderCore", shell);
            string profile = MethodSource("DrawAllianceProfileTopBar");
            StringAssert.Contains("DrawActivitiesButton", profile);
        }

        [TestCase("message", true)]
        [TestCase("friend", false)]
        [TestCase("report", false)]
        [TestCase("invite", false)]
        public void ProfileActions_DoNotReturnToSimulatedSocialData(string action, bool expected)
        {
            var method = Presenter.GetMethod("AllianceMemberActionAllowed", Flags);
            Assert.That((bool)method.Invoke(null, new object[] { action }), Is.EqualTo(expected));
        }

        [TestCase("AllianceNumberLabel")]
        [TestCase("AllianceFullNumberLabel")]
        public void UnknownMetrics_AreNotInvented(string name)
        {
            var method = Presenter.GetMethod(name, Flags);
            Assert.That((string)method.Invoke(null, new object[] { -1L }), Is.EqualTo("\u2014"));
        }

        [TestCase(RemoteAllianceRole.Member, false, false)]
        [TestCase(RemoteAllianceRole.Officer, false, true)]
        [TestCase(RemoteAllianceRole.Leader, true, true)]
        public void Roles_PreserveAuthoritativePermissionProjection(RemoteAllianceRole role, bool leader, bool officer)
        {
            var model = LocalModel(role);
            Assert.That(model.Overview.IsLeader, Is.EqualTo(leader));
            Assert.That(model.Overview.IsOfficerOrLeader, Is.EqualTo(officer));
        }

        [Test]
        public void NoAllianceAndLoading_DoNotFabricateMembership()
        {
            var noAlliance = AllianceCenterPresentation.NoAlliance(null, null);
            Assert.That(noAlliance.HasAlliance, Is.False);
            Assert.That(AllianceCenterPresentation.Loading(noAlliance).HasAlliance, Is.False);
            Assert.That(AllianceCenterPresentation.Error(noAlliance, "test.offline").HasAlliance, Is.False);
        }

        [Test]
        public void Announcement_RestoresExistingClientOperationWithoutAutomaticPublish()
        {
            Assert.That(typeof(IAllianceCenterPanelController).GetMethod("UpdateProfile"), Is.Not.Null);
            string source = MethodSource("DrawAllianceQuickActionOverlay");
            StringAssert.Contains("IsOfficerOrLeader", source);
            StringAssert.Contains("allianceCenterController.UpdateProfile(", source);
            StringAssert.DoesNotContain("UpdateProfile(", MethodSource("DrawAllianceHeadquartersScreen"));
        }

        [TestCase(320)]
        [TestCase(390)]
        [TestCase(844)]
        [TestCase(1920)]
        public void ProfileHeader_DoesNotOverlapBackButtonOrBody(int width)
        {
            object layout = Presenter.GetMethod("CalculateAllianceProfileHeaderLayout", Flags).Invoke(null, new object[] { (float)width });
            Type type = layout.GetType();
            Rect back = (Rect)type.GetField("Back").GetValue(layout);
            Rect title = (Rect)type.GetField("Title").GetValue(layout);
            Rect header = (Rect)type.GetField("Header").GetValue(layout);
            float top = (float)type.GetField("BodyTop").GetValue(layout);
            Assert.That(back.Overlaps(title), Is.False);
            Assert.That(back.height, Is.GreaterThanOrEqualTo(48f));
            Assert.That(title.xMax, Is.LessThanOrEqualTo(width));
            Assert.That(top, Is.GreaterThan(header.yMax));
            Assert.That(header.Contains(back.min) && header.Contains(back.max), Is.True);
        }

        [TestCase("DrawAllianceMemberProfilePortrait")]
        [TestCase("DrawAllianceMemberProfileLandscape")]
        [TestCase("DrawAllianceProfileInfoSection")]
        public void ProfileMetrics_DoNotDisplayNegativeSentinelNumbers(string method)
        {
            string source = MethodSource(method);
            StringAssert.Contains("AllianceCountLabel(member.", source);
            Assert.That(Regex.IsMatch(source, @"member\.(Level|Buildings|Researches|Helps|Donations)\.ToString\(\)"), Is.False);
        }

        [Test]
        public void BuildingUpgradeChrome_DoesNotDrawOverMemberOrActionModal()
        {
            string source = MethodSource("DrawAllianceUpgradeButtonForExternalHost");
            StringAssert.Contains("allianceMemberProfileOpen", source);
            StringAssert.Contains("allianceActionPanelOpen", source);
            Assert.That(source.IndexOf("allianceMemberProfileOpen", StringComparison.Ordinal),
                Is.LessThan(source.IndexOf("DrawPreviewActionButton", StringComparison.Ordinal)));
        }

        [Test]
        public void UnknownIntegerMetric_UsesPlaceholderButRealZeroIsPreserved()
        {
            var method = Presenter.GetMethod("AllianceCountLabel", Flags);
            Assert.That((string)method.Invoke(null, new object[] { -1 }), Is.EqualTo("\u2014"));
            Assert.That((string)method.Invoke(null, new object[] { 0 }), Is.EqualTo("0"));
        }
        private static AllianceCenterScreenModel LocalModel(RemoteAllianceRole role)
        {
            Guid allianceId = Guid.Parse("00000000-0000-4000-a000-000000000016");
            Guid playerId = Guid.Parse("00000000-0000-4000-b000-000000000016");
            var members = new[] {
                new RemoteAllianceMemberSummary { PlayerId = playerId, DisplayName = "Membre QA local", Role = role, JoinedAtUtc = DateTimeOffset.UtcNow },
                new RemoteAllianceMemberSummary { PlayerId = Guid.Parse("00000000-0000-4000-b000-000000000017"), DisplayName = "Autre membre QA", Role = RemoteAllianceRole.Member, JoinedAtUtc = DateTimeOffset.UtcNow }
            };
            return AllianceCenterPresentation.Ready(
                new RemoteAllianceEntity { AllianceId = allianceId, Name = "TEST LOCAL - restauration UI", Tag = "QA", Description = "Donnees fictives de verification visuelle. Aucune action serveur.", Language = "fr-CA", MemberCount = members.Length, MaxMembers = 100, LeaderPlayerId = playerId, Revision = 1, JoinMode = RemoteAllianceJoinMode.InviteOnly },
                new RemoteAllianceMembership { AllianceId = allianceId, PlayerId = playerId, Role = role, JoinedAtUtc = DateTimeOffset.UtcNow }, members, null, null);
        }

        // Explicit diagnostic entry point only; never called automatically from a runtime scene.
        public static string BeginLocalPreview()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode required");
            if (previewActive) return "Local fixture already active";
            previousController = (IAllianceCenterPanelController)Presenter.GetField("allianceCenterController", Flags).GetValue(null);
            if (previousController != null && previousController.IsConfigured)
                throw new InvalidOperationException("Refusing to replace a configured real controller");
            HiveViewProductUiPresenter.UseAllianceCenterControllerForProof(new AllianceRecoveryLocalFixture(LocalModel(RemoteAllianceRole.Leader)));
            foreach (var ftue in UnityEngine.Object.FindObjectsByType<BeeKingdom.Tutorial.FtueTutorialBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) ftue.DismissPresentation();
            var activate = Presenter.GetMethod("ActivateHiveMenu", Flags);
            object alliance = Enum.Parse(activate.GetParameters()[0].ParameterType, "Alliance");
            activate.Invoke(null, new[] { alliance, (object)"QA locale" });
            previewActive = true;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            return "Local Alliance preview: explicitly fictitious data, no network controller.";
        }

        public static string SelectLocalPreviewTab(string tab)
        {
            if (!previewActive || !AlphaTabs.Contains(tab)) throw new InvalidOperationException("Invalid local preview tab");
            Presenter.GetField("allianceOverviewTab", Flags).SetValue(null, tab);
            Presenter.GetField("allianceTabChangedAt", Flags).SetValue(null, -10f);
            return tab;
        }

        public static void EndLocalPreview()
        {
            if (!previewActive) return;
            HiveViewProductUiPresenter.CloseAllianceOverlayForExternalHost();
            HiveViewProductUiPresenter.UseAllianceCenterControllerForProof(previousController);
            Presenter.GetMethod("SyncAllianceRuntimeStateFromController", Flags).Invoke(null, null);
            previousController = null;
            previewActive = false;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) EndLocalPreview();
        }
    }
}