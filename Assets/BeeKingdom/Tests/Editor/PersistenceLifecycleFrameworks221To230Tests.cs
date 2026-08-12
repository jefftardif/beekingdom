using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Save;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class PersistenceLifecycleFrameworks221To230Tests
    {
        [Test]
        public void PersistentLifecycleRule_ValidatesTransitionsAndConflicts()
        {
            var rule = new PersistentLifecycleRule();

            Assert.IsTrue(rule.Validate(new PersistentLifecycleTransition(PersistentLifecycleState.Draft, PersistentLifecycleState.Active, "ready"), false).Allowed);
            Assert.IsTrue(rule.Validate(new PersistentLifecycleTransition(PersistentLifecycleState.Active, PersistentLifecycleState.Deprecated, "obsolete"), false).Allowed);
            Assert.IsFalse(rule.Validate(new PersistentLifecycleTransition(PersistentLifecycleState.Active, PersistentLifecycleState.Invalid, "bad"), true).Allowed);
            Assert.IsTrue(rule.Validate(new PersistentLifecycleTransition(PersistentLifecycleState.Deprecated, PersistentLifecycleState.ArchivedCandidate, "candidate"), false).Allowed);
            Assert.IsFalse(rule.Validate(new PersistentLifecycleTransition(PersistentLifecycleState.Invalid, PersistentLifecycleState.Active, string.Empty), false).Allowed);
        }

        [Test]
        public void RetentionScheduleResolver_ComposesFactorsWithoutApplyingRetention()
        {
            var resolver = new RetentionScheduleResolver();

            Assert.AreEqual(RetentionResolution.Keep, resolver.Resolve(PersistentDataClass.QAEvidence, PersistentLifecycleState.Active, true, false, false).Schedule.Resolution);
            Assert.AreEqual(RetentionResolution.Blocked, resolver.Resolve(PersistentDataClass.Forbidden, PersistentLifecycleState.Active, false, false, false).Schedule.Resolution);
            Assert.AreEqual(RetentionResolution.ArchiveCandidate, resolver.Resolve(PersistentDataClass.GameplayState, PersistentLifecycleState.Deprecated, false, false, false).Schedule.Resolution);
            Assert.AreEqual(RetentionResolution.ExpireCandidate, resolver.Resolve(PersistentDataClass.Telemetry, PersistentLifecycleState.Active, false, true, false).Schedule.Resolution);
            Assert.IsTrue(resolver.Resolve(PersistentDataClass.Telemetry, PersistentLifecycleState.Active, false, false, true).Diagnostics.Issues.Any());
        }

        [Test]
        public void ArchiveEligibilityPolicy_ProtectsQaAuditActiveAndInvalidData()
        {
            var policy = new ArchiveEligibilityPolicy();

            Assert.AreEqual(ArchiveEligibilityVerdict.Blocked, policy.Evaluate(true, false, false, false, false).Verdict);
            Assert.AreEqual(ArchiveEligibilityVerdict.EligibleWithWarnings, policy.Evaluate(false, true, false, false, false).Verdict);
            Assert.AreEqual(ArchiveEligibilityVerdict.NotEligible, policy.Evaluate(false, false, true, false, false).Verdict);
            Assert.AreEqual(ArchiveEligibilityVerdict.Blocked, policy.Evaluate(false, false, false, true, false).Verdict);
            Assert.IsTrue(policy.Evaluate(true, false, false, true, false).Diagnostics.Reasons.SequenceEqual(policy.Evaluate(true, false, false, true, false).Diagnostics.Reasons.OrderBy(r => r)));
        }

        [Test]
        public void RedactionRequirementRegistry_ResolvesSensitiveOutputs()
        {
            var registry = new RedactionRequirementRegistry(new[]
            {
                new RedactionRequirement(SensitiveFieldClass.Token, RedactionOutputRule.RawForbidden),
                new RedactionRequirement(SensitiveFieldClass.AccountId, RedactionOutputRule.HashedReference),
                new RedactionRequirement(SensitiveFieldClass.ServerDiagnostic, RedactionOutputRule.QAOnly),
                new RedactionRequirement(SensitiveFieldClass.CorrelationId, RedactionOutputRule.ClientSafe)
            });

            Assert.AreEqual(RedactionOutputRule.RawForbidden, registry.Resolve(SensitiveFieldClass.Token).Rule);
            Assert.AreEqual(RedactionOutputRule.HashedReference, registry.Resolve(SensitiveFieldClass.AccountId).Rule);
            Assert.AreEqual(RedactionOutputRule.QAOnly, registry.Resolve(SensitiveFieldClass.ServerDiagnostic).Rule);
            Assert.IsTrue(registry.Resolve(SensitiveFieldClass.Unknown).Diagnostics.Issues.Any());
            Assert.AreEqual(RedactionOutputRule.ClientSafe, registry.Resolve(SensitiveFieldClass.CorrelationId).Rule);
        }

        [Test]
        public void PersistenceEventTaxonomy_ClassifiesKindsAndRejectsUnknown()
        {
            var taxonomy = new PersistenceEventTaxonomy();

            Assert.AreEqual(PersistenceEventSeverity.Error, taxonomy.Classify(PersistenceEventKind.Fail));
            Assert.AreEqual(PersistenceEventSeverity.Info, taxonomy.Classify(PersistenceEventKind.Verify));
            Assert.AreEqual(PersistenceEventSeverity.Warning, taxonomy.Classify(PersistenceEventKind.RedactionRequired));
            Assert.AreEqual(PersistenceEventSeverity.Critical, taxonomy.Classify(PersistenceEventKind.Unknown));
            Assert.AreEqual("corr-1", new PersistenceEventCorrelation("corr-1").Id);
        }

        [Test]
        public void LongRunSamplingPlan_PrioritizesCriticalFailureRecoveryBudgetAndGaps()
        {
            var plan = new LongRunSamplingPlan(new[]
            {
                new LongRunSnapshotSample("low", new[] { new LongRunSamplingCriterion("normal", LongRunSamplingPriority.Low) }),
                new LongRunSnapshotSample("critical", new[] { new LongRunSamplingCriterion("failure-critical", LongRunSamplingPriority.Critical), new LongRunSamplingCriterion("qa-gap", LongRunSamplingPriority.High) })
            });

            Assert.AreEqual("critical", plan.Samples[0].SampleId);
            Assert.AreEqual(LongRunSamplingPriority.Critical, plan.Samples[0].Priority);
            Assert.IsTrue(plan.Samples[0].Criteria.Any(c => c.Name == "qa-gap"));
        }

        [Test]
        public void PersistenceDriftDetector_DetectsKnownAndUnknownDrifts()
        {
            var detector = new PersistenceDriftDetector();

            Assert.AreEqual(PersistenceDriftKind.Schema, detector.Detect("schema-1", "schema-2", PersistenceDriftKind.Schema, "scenario").Findings[0].Kind);
            Assert.AreEqual(PersistenceDriftSeverity.Critical, detector.Detect("id-a", "id-b", PersistenceDriftKind.Identity, "sample").Findings[0].Severity);
            Assert.IsFalse(detector.Detect("same", "same", PersistenceDriftKind.Retention, "sample").Findings.Any());
            Assert.AreEqual(PersistenceDriftKind.Unknown, detector.Detect(string.Empty, "x", PersistenceDriftKind.Unknown, "sample").Findings[0].Kind);
        }

        [Test]
        public void DataGovernanceExportReport_SurfacesCriticalFindingsRedactionAndMissingEvidence()
        {
            var report = new DataGovernanceExportReport(new[]
            {
                new DataGovernanceReportSection("drift", new[] { new DataGovernanceReportFinding("sample", PersistenceDriftSeverity.Critical, "integrity drift") }, false),
                new DataGovernanceReportSection("sensitive", new[] { new DataGovernanceReportFinding("audit", PersistenceDriftSeverity.Warning, "redaction needed") }, true),
                new DataGovernanceReportSection("empty", null, false)
            });

            Assert.AreEqual(DataGovernanceReportVerdict.Blocked, report.Verdict);
            Assert.IsTrue(report.Diagnostics.Issues.Any(issue => issue.Contains("Redaction")));
            Assert.IsTrue(report.Diagnostics.Issues.Any(issue => issue.Contains("Evidence missing")));
            Assert.AreEqual("drift", report.Sections[0].Name);
        }

        [Test]
        public void PersistenceServerHandoffChecklist_RequiresBeeSourcesAndServerAnalysis()
        {
            var checklist = new PersistenceServerHandoffChecklist(new[]
            {
                new PersistenceServerHandoffRequirement("BEE-211", "classification contract", PersistenceServerHandoffStatus.AnalysisRequired),
                new PersistenceServerHandoffRequirement(string.Empty, "future SQL", PersistenceServerHandoffStatus.Blocked)
            });

            Assert.AreEqual("BEE-211", checklist.Requirements[0].BeeSource);
            Assert.IsTrue(checklist.Diagnostics.Gaps.Any(g => g.Reason.Contains("Analysis required")));
            Assert.IsTrue(checklist.Diagnostics.Gaps.Any(g => g.Reason.Contains("without BEE")));
        }

        [Test]
        public void PersistenceLifecycleGate_BlocksServerProgressMissingAndBee231()
        {
            var gate = new PersistenceLifecycleGate();
            var ready = new[] { new PersistenceLifecycleCriterion("lifecycle", true, false, true), new PersistenceLifecycleCriterion("handoff", true, false, true) };

            Assert.AreEqual(PersistenceLifecycleVerdict.Ready, gate.Evaluate(ready, true, false).Verdict);
            Assert.AreEqual(PersistenceLifecycleVerdict.NeedsRevision, gate.Evaluate(new[] { new PersistenceLifecycleCriterion("redaction", false, false, false) }, true, false).Verdict);
            Assert.AreEqual(PersistenceLifecycleVerdict.Blocked, gate.Evaluate(new[] { new PersistenceLifecycleCriterion("destructive-action", false, false, true) }, true, false).Verdict);
            Assert.AreEqual(PersistenceLifecycleVerdict.Blocked, gate.Evaluate(ready, false, false).Verdict);
            Assert.AreEqual(PersistenceLifecycleVerdict.Blocked, gate.Evaluate(ready, true, true).Verdict);
        }
    }
}
