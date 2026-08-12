using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ServerHandoffFrameworks171To180Tests
    {
        [Test]
        public void ColonyCommandRouter_RoutesKnownColonyCommandsAndRejectsUnknownOrWrongScope()
        {
            ColonyCommandRouter router = ColonyCommandRouter.CreateDefault();

            Assert.AreEqual(ColonyCommandDestination.Construction, router.Route("build", RegionalCommandScopeKind.Colony).Destination);
            Assert.AreEqual(ColonyCommandDestination.Population, router.Route("population", RegionalCommandScopeKind.Colony).Destination);
            Assert.IsFalse(router.Route("missing", RegionalCommandScopeKind.Colony).Routed);
            Assert.IsFalse(router.Route("build", RegionalCommandScopeKind.Region).Routed);
            CollectionAssert.AreEqual(
                router.Routes.Select(route => route.CommandKind).OrderBy(kind => kind).ToArray(),
                router.Routes.Select(route => route.CommandKind).ToArray());
        }

        [Test]
        public void RegionalCommandScopeValidator_ValidatesKnownRegionsAndReadOnlyObservation()
        {
            var validator = new RegionalCommandScopeValidator(new[] { "region-a" });

            RegionalCommandScopeResult region = validator.Validate(new RegionalCommandScope(RegionalCommandScopeKind.Region, "region-a"), true);
            RegionalCommandScopeResult colonyOutOfRegion = validator.Validate(new RegionalCommandScope(RegionalCommandScopeKind.Colony, "missing", "colony-a"), true);
            RegionalCommandScopeResult observation = validator.Validate(new RegionalCommandScope(RegionalCommandScopeKind.ReadOnlyObservation, "region-a", readOnly: true), false);
            RegionalCommandScopeResult unknown = validator.Validate(new RegionalCommandScope(RegionalCommandScopeKind.Unknown), false);

            Assert.IsTrue(region.Accepted);
            Assert.IsFalse(colonyOutOfRegion.Accepted);
            Assert.IsTrue(observation.Accepted);
            Assert.IsFalse(unknown.Accepted);
            CollectionAssert.AreEqual(
                validator.Rules.Select(rule => rule.Kind).OrderBy(kind => kind).ToArray(),
                validator.Rules.Select(rule => rule.Kind).ToArray());
        }

        [Test]
        public void ServerSimulationTickContract_ValidatesPhaseOrderAndDigestAfterSnapshot()
        {
            var contract = new ServerSimulationTickContract();

            ServerTickDiagnostics validEmpty = contract.Validate(new ServerTickInput(1, null), new ServerTickOutput(true, true));
            ServerTickDiagnostics badOrder = contract.Validate(new ServerTickInput(1, new[] { ServerTickPhase.Snapshot, ServerTickPhase.CommandRouting }));
            ServerTickDiagnostics badOutput = contract.Validate(new ServerTickInput(1, contract.ExpectedPhases), new ServerTickOutput(false, true));

            Assert.IsTrue(validEmpty.Valid);
            Assert.IsFalse(badOrder.Valid);
            Assert.IsFalse(badOutput.Valid);
            List<ServerTickPhase> phases = contract.ExpectedPhases.ToList();
            Assert.Greater(phases.IndexOf(ServerTickPhase.Snapshot), phases.IndexOf(ServerTickPhase.Simulation));
        }

        [Test]
        public void ObservationSubscriptionRegistry_AcceptsReadOnlyAndRejectsMutationUnknownFilterOrUnauthorizedClient()
        {
            var registry = new ClientObservationSubscriptionRegistry();

            ObservationSubscriptionDiagnostics valid = registry.Add(new ClientObservationSubscription("client-a", ObservationScope.Region, new ObservationFilter("public-region"), new ObservationDeliveryPolicy(), true));
            ObservationSubscriptionDiagnostics mutation = registry.Add(new ClientObservationSubscription("client-b", ObservationScope.Region, new ObservationFilter("public-region"), new ObservationDeliveryPolicy(readOnly: false, mutationRequested: true), true));
            ObservationSubscriptionDiagnostics unknownFilter = registry.Add(new ClientObservationSubscription("client-c", ObservationScope.Region, new ObservationFilter(string.Empty), new ObservationDeliveryPolicy(), true));
            ObservationSubscriptionDiagnostics unauthorized = registry.Add(new ClientObservationSubscription("client-d", ObservationScope.Region, new ObservationFilter("public-region"), new ObservationDeliveryPolicy(), false));

            Assert.IsTrue(valid.Valid);
            Assert.IsFalse(mutation.Valid);
            Assert.IsFalse(unknownFilter.Valid);
            Assert.IsFalse(unauthorized.Valid);
            Assert.AreEqual("client-a", registry.Subscriptions[0].ClientId);
        }

        [Test]
        public void AuthoritativeEventJournal_OrdersEntriesDetectsSequenceGapsAndReplaysFromCursor()
        {
            var journal = new AuthoritativeEventJournal();
            journal.Append(new AuthoritativeEventEntry(2, 2, ServerTickPhase.Snapshot, "cmd-b", AuthoritativeEventKind.SnapshotProduced));
            AuthoritativeEventJournalDiagnostics gap = journal.Append(new AuthoritativeEventEntry(1, 1, ServerTickPhase.CommandRouting, "cmd-a", AuthoritativeEventKind.CommandAccepted));
            AuthoritativeEventJournalDiagnostics unknown = journal.Append(new AuthoritativeEventEntry(3, 3, ServerTickPhase.Diagnostics, "cmd-c", AuthoritativeEventKind.Unknown));

            IReadOnlyList<AuthoritativeEventEntry> replay = journal.Replay(new AuthoritativeEventCursor(1));

            Assert.IsTrue(gap.Valid);
            Assert.IsFalse(unknown.Valid);
            Assert.AreEqual(2, replay[0].Sequence);
            Assert.AreEqual(1, journal.Entries[0].Tick);

            var gapJournal = new AuthoritativeEventJournal();
            AuthoritativeEventJournalDiagnostics missing = gapJournal.Append(new AuthoritativeEventEntry(2, 1, ServerTickPhase.CommandRouting, "cmd", AuthoritativeEventKind.CommandAccepted));
            Assert.IsFalse(missing.Valid);
        }

        [Test]
        public void RetryIdempotencyPolicy_ClassifiesDuplicateAppliedConflictExpiredAndStableFingerprint()
        {
            var policy = new RetryIdempotencyPolicy();
            var original = new RetryCommandFingerprint("key", "build", "payload-a");
            var same = new RetryCommandFingerprint("key", "build", "payload-a");
            var changed = new RetryCommandFingerprint("key", "build", "payload-b");

            Assert.AreEqual(RetryDecision.DuplicateIgnored, policy.Evaluate(same, original, false, false).Decision);
            Assert.AreEqual(RetryDecision.ReturnExistingResult, policy.Evaluate(same, original, true, false).Decision);
            Assert.AreEqual(RetryDecision.RejectedConflict, policy.Evaluate(changed, original, false, false).Decision);
            Assert.AreEqual(RetryDecision.Expired, policy.Evaluate(same, original, false, true).Decision);
            Assert.AreEqual(original.GetHashCode(), same.GetHashCode());
        }

        [Test]
        public void ConflictResolutionDiagnostic_DetectsScopeRetryClosedSessionAndEventOrderConflicts()
        {
            var diagnostic = new ConflictResolutionDiagnostic();
            var context = new ConflictContext(
                "cmd-1",
                "session-1",
                new RegionalCommandScope(RegionalCommandScopeKind.Region, "missing"),
                new AuthoritativeEventEntry(2, 1, ServerTickPhase.CommandRouting, "cmd-1", AuthoritativeEventKind.CommandAccepted),
                "queued");

            IReadOnlyList<ConflictFinding> findings = diagnostic.Analyze(
                context,
                new RegionalCommandScopeResult(false, new RegionalCommandScopeDiagnostics(new[] { "Region is unknown" })),
                new RetryPolicyDiagnostics(RetryDecision.RejectedConflict, "payload changed"),
                AuthoritySessionState.Closed,
                new AuthoritativeEventJournalDiagnostics(new[] { "gap" }));

            Assert.IsTrue(findings.Any(finding => finding.Kind == ConflictKind.Scope));
            Assert.IsTrue(findings.Any(finding => finding.Kind == ConflictKind.Retry));
            Assert.AreEqual(ConflictKind.SessionState, findings[0].Kind);
            Assert.IsTrue(findings.Any(finding => finding.Kind == ConflictKind.EventOrder));
        }

        [Test]
        public void DisconnectRecoveryContract_ReturnsExpectedVerdictsAndOrderedSteps()
        {
            var contract = new DisconnectRecoveryContract();
            var compatible = ProtocolVersionRegistry.CreateDefault().Resolve(new ProtocolVersion(1, 0));
            var incompatible = ProtocolVersionRegistry.CreateDefault().Resolve(new ProtocolVersion(9, 9));
            var reconnecting = new AuthoritySessionContext("session", AuthoritySessionState.Reconnecting, 10, 0);

            Assert.AreEqual(RecoveryVerdict.Recoverable, contract.Evaluate(reconnecting, compatible, true, true, false, false).Verdict);
            Assert.AreEqual(RecoveryVerdict.Blocked, contract.Evaluate(reconnecting, incompatible, true, true, false, false).Verdict);
            Assert.AreEqual(RecoveryVerdict.NeedsSnapshot, contract.Evaluate(reconnecting, compatible, true, true, false, true).Verdict);
            Assert.AreEqual(RecoveryVerdict.Rejected, contract.Evaluate(reconnecting, compatible, false, true, false, false).Verdict);
            CollectionAssert.AreEqual(contract.Steps.OrderBy(step => step).ToArray(), contract.Steps.OrderBy(step => step).ToArray());
        }

        [Test]
        public void AuthorityLoadBudgetPolicy_ReportsSoftAndHardExcessWithoutWallClock()
        {
            var policy = new AuthorityLoadBudgetPolicy(new[]
            {
                new AuthorityLoadBudget(AuthorityLoadBudgetScope.CommandsPerTick, 10, 20, "defer-low-priority"),
                new AuthorityLoadBudget(AuthorityLoadBudgetScope.Snapshots, 2, 3, "snapshot-full")
            });

            AuthorityLoadBudgetDiagnostics ok = policy.Evaluate(new Dictionary<AuthorityLoadBudgetScope, int> { [AuthorityLoadBudgetScope.CommandsPerTick] = 8 });
            AuthorityLoadBudgetDiagnostics soft = policy.Evaluate(new Dictionary<AuthorityLoadBudgetScope, int> { [AuthorityLoadBudgetScope.CommandsPerTick] = 15 });
            AuthorityLoadBudgetDiagnostics hard = policy.Evaluate(new Dictionary<AuthorityLoadBudgetScope, int> { [AuthorityLoadBudgetScope.Snapshots] = 4 });

            Assert.IsFalse(ok.Findings.Any());
            Assert.AreEqual(AuthorityTelemetrySeverity.Warning, soft.Findings[0].Severity);
            Assert.IsTrue(hard.HardExceeded);
            CollectionAssert.AreEqual(policy.Budgets.Select(budget => budget.Scope).OrderBy(scope => scope).ToArray(), policy.Budgets.Select(budget => budget.Scope).ToArray());
        }

        [Test]
        public void ServerHandoffGate_ComposesCriteriaAndBudgetsIntoVerdicts()
        {
            var gate = new ServerHandoffGate();
            var readyCriteria = new[] { new ServerHandoffCriterion("routing", true, true), new ServerHandoffCriterion("recovery", true, true) };
            var warningBudget = new AuthorityLoadBudgetDiagnostics(new[] { new AuthorityLoadBudgetFinding(AuthorityLoadBudgetScope.Subscriptions, AuthorityTelemetrySeverity.Warning, "soft") });
            var hardBudget = new AuthorityLoadBudgetDiagnostics(new[] { new AuthorityLoadBudgetFinding(AuthorityLoadBudgetScope.CommandsPerTick, AuthorityTelemetrySeverity.Critical, "hard") });

            Assert.AreEqual(ServerHandoffVerdict.Ready, gate.Evaluate(readyCriteria).Verdict);
            Assert.AreEqual(ServerHandoffVerdict.ReadyWithWarnings, gate.Evaluate(readyCriteria, warningBudget).Verdict);
            Assert.AreEqual(ServerHandoffVerdict.Blocked, gate.Evaluate(readyCriteria, hardBudget).Verdict);
            Assert.AreEqual(ServerHandoffVerdict.NeedsRevision, gate.Evaluate(new[] { new ServerHandoffCriterion("recovery", false, false, "missing") }).Verdict);
        }
    }
}
