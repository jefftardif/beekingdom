using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Save
{
    public enum SaveLoadRuntimeIntent { SavePreview, LoadPreview, CompatibilityCheck, ReplayLinkCheck, QaEvidenceLinkCheck, ServerHandoffCheck }
    public enum SaveLoadRuntimeGap { RuntimeNotImplemented, StorageFinalOutOfScope, MigrationExecutionBlocked, ServerAnalysisRequired, QaEvidenceMissing }
    public sealed class SaveLoadRuntimeInputPort { public SaveLoadRuntimeInputPort(string source, SnapshotSchemaVersion schemaVersion, string identityScope) { Source = source ?? string.Empty; SchemaVersion = schemaVersion; IdentityScope = identityScope ?? string.Empty; } public string Source { get; } public SnapshotSchemaVersion SchemaVersion { get; } public string IdentityScope { get; } }
    public sealed class SaveLoadRuntimeOutputPort { public SaveLoadRuntimeOutputPort(string consumer, string domain) { Consumer = consumer ?? string.Empty; Domain = domain ?? string.Empty; } public string Consumer { get; } public string Domain { get; } }
    public sealed class SaveLoadRuntimeBoundaryDiagnostics { public SaveLoadRuntimeBoundaryDiagnostics(IReadOnlyList<SaveLoadRuntimeGap> gaps) { Gaps = gaps ?? Array.Empty<SaveLoadRuntimeGap>(); } public IReadOnlyList<SaveLoadRuntimeGap> Gaps { get; } public bool Accepted => Gaps.Count == 0; }
    public sealed class SaveLoadRuntimeBoundary
    {
        public SaveLoadRuntimeBoundaryDiagnostics Validate(SaveLoadRuntimeIntent intent, SaveLoadRuntimeInputPort input, bool writesFinalStorage, bool executesMigration, bool directServerOperation, bool qaEvidenceLinked)
        {
            var gaps = new List<SaveLoadRuntimeGap>();
            if (input == null || input.SchemaVersion.Value <= 0) gaps.Add(intent == SaveLoadRuntimeIntent.LoadPreview ? SaveLoadRuntimeGap.RuntimeNotImplemented : SaveLoadRuntimeGap.QaEvidenceMissing);
            if (writesFinalStorage) gaps.Add(SaveLoadRuntimeGap.StorageFinalOutOfScope);
            if (executesMigration) gaps.Add(SaveLoadRuntimeGap.MigrationExecutionBlocked);
            if (directServerOperation) gaps.Add(SaveLoadRuntimeGap.ServerAnalysisRequired);
            if (!qaEvidenceLinked) gaps.Add(SaveLoadRuntimeGap.QaEvidenceMissing);
            return new SaveLoadRuntimeBoundaryDiagnostics(gaps.Distinct().OrderBy(g => g).ToList());
        }
    }

    public enum PersistenceFixtureDomain { HiveSnapshot, QueenSnapshot, BeeLifecycleSnapshot, WorldSnapshot, RegionSnapshot, AuthorityReadModel, ServerColonySnapshot, QaEvidenceBundle, Unknown }
    public enum PersistenceFixtureRisk { Low, Medium, High, Critical }
    public enum PersistenceFixtureStatus { Available, Missing, Obsolete, Blocked, Forbidden }
    public enum PersistenceFixtureGap { FixtureSourceMissing, FixtureSchemaUnknown, FixtureIdentityScopeAmbiguous, FixtureEvidenceInvented, FixtureForbiddenForRuntime }
    public sealed class PersistenceFixtureDescriptor { public PersistenceFixtureDescriptor(string name, PersistenceFixtureDomain domain, SnapshotSchemaVersion schemaVersion, string identityScope, string sourceEvidence, PersistenceFixtureStatus status) { Name = name ?? string.Empty; Domain = domain; SchemaVersion = schemaVersion; IdentityScope = identityScope ?? string.Empty; SourceEvidence = sourceEvidence ?? string.Empty; Status = status; } public string Name { get; } public PersistenceFixtureDomain Domain { get; } public SnapshotSchemaVersion SchemaVersion { get; } public string IdentityScope { get; } public string SourceEvidence { get; } public PersistenceFixtureStatus Status { get; } }
    public sealed class PersistenceFixtureCoverageLink { public PersistenceFixtureCoverageLink(string fixtureName, string coverageAxis) { FixtureName = fixtureName ?? string.Empty; CoverageAxis = coverageAxis ?? string.Empty; } public string FixtureName { get; } public string CoverageAxis { get; } }
    public sealed class PersistenceFixtureCatalog
    {
        public PersistenceFixtureCatalog(IEnumerable<PersistenceFixtureDescriptor> fixtures)
        {
            Fixtures = (fixtures ?? Array.Empty<PersistenceFixtureDescriptor>()).OrderBy(f => f.Domain).ThenBy(f => f.SchemaVersion.Value).ThenBy(f => f.IdentityScope, StringComparer.Ordinal).ThenBy(f => f.Name, StringComparer.Ordinal).ToList();
            Gaps = Detect(Fixtures);
        }
        public IReadOnlyList<PersistenceFixtureDescriptor> Fixtures { get; }
        public IReadOnlyList<PersistenceFixtureGap> Gaps { get; }
        private static IReadOnlyList<PersistenceFixtureGap> Detect(IEnumerable<PersistenceFixtureDescriptor> fixtures)
        {
            var gaps = new List<PersistenceFixtureGap>();
            foreach (PersistenceFixtureDescriptor fixture in fixtures)
            {
                if (string.IsNullOrWhiteSpace(fixture.SourceEvidence)) gaps.Add(PersistenceFixtureGap.FixtureSourceMissing);
                if (fixture.Domain == PersistenceFixtureDomain.Unknown || fixture.SchemaVersion.Value <= 0) gaps.Add(PersistenceFixtureGap.FixtureSchemaUnknown);
                if (string.IsNullOrWhiteSpace(fixture.IdentityScope)) gaps.Add(PersistenceFixtureGap.FixtureIdentityScopeAmbiguous);
                if (fixture.Status == PersistenceFixtureStatus.Available && string.IsNullOrWhiteSpace(fixture.SourceEvidence)) gaps.Add(PersistenceFixtureGap.FixtureEvidenceInvented);
                if (fixture.Status == PersistenceFixtureStatus.Forbidden) gaps.Add(PersistenceFixtureGap.FixtureForbiddenForRuntime);
            }
            return gaps.Distinct().OrderBy(g => g).ToList();
        }
    }

    public enum MigrationDryRunVerdict { WouldPass, WouldPassWithWarnings, BlockedByPrecondition, BlockedByUnknownVersion, BlockedByCycle, BlockedByMissingFixture, ForbiddenRuntimeExecution }
    public enum MigrationDryRunRisk { Low, Medium, High, Critical }
    public sealed class MigrationDryRunPrecondition { public MigrationDryRunPrecondition(string name, bool satisfied) { Name = name ?? string.Empty; Satisfied = satisfied; } public string Name { get; } public bool Satisfied { get; } }
    public sealed class MigrationDryRunStep { public MigrationDryRunStep(SaveMigrationVersion source, SaveMigrationVersion target) { Source = source; Target = target; } public SaveMigrationVersion Source { get; } public SaveMigrationVersion Target { get; } }
    public sealed class MigrationDryRunDiagnostics { public MigrationDryRunDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class MigrationDryRunScenario
    {
        public MigrationDryRunVerdict Evaluate(SaveMigrationVersion source, SaveMigrationVersion target, IEnumerable<MigrationDryRunStep> steps, IEnumerable<MigrationDryRunPrecondition> preconditions, bool fixturePresent, bool cycleDetected, bool writeRequested)
        {
            if (writeRequested) return MigrationDryRunVerdict.ForbiddenRuntimeExecution;
            if (cycleDetected) return MigrationDryRunVerdict.BlockedByCycle;
            var stepList = (steps ?? Array.Empty<MigrationDryRunStep>()).ToList();
            if (stepList.All(s => s.Source.Value != source.Value)) return MigrationDryRunVerdict.BlockedByUnknownVersion;
            if (stepList.All(s => s.Target.Value != target.Value)) return MigrationDryRunVerdict.BlockedByUnknownVersion;
            if (!fixturePresent) return MigrationDryRunVerdict.BlockedByMissingFixture;
            return (preconditions ?? Array.Empty<MigrationDryRunPrecondition>()).Any(p => !p.Satisfied) ? MigrationDryRunVerdict.BlockedByPrecondition : MigrationDryRunVerdict.WouldPass;
        }
    }

    public enum SnapshotVerificationCheck { SchemaPass, IdentityPass, ChecksumPass, RegistryLinkPass, RedactionRequirementPass, LifecycleStatePass, FixtureCoveragePass }
    public enum SnapshotVerificationFinding { SchemaMismatch, IdentityCollision, ChecksumMismatch, RegistryReferenceMissing, RedactionRequirementMissing, LifecycleStateInvalid, FixtureGap, AutoRepairBlocked }
    public enum SnapshotVerificationVerdict { Verified, VerifiedWithWarnings, Invalid, Blocked, NotEnoughEvidence }
    public sealed class SnapshotVerificationPlan { public SnapshotVerificationPlan(IEnumerable<SnapshotVerificationCheck> checks) { Checks = (checks ?? Array.Empty<SnapshotVerificationCheck>()).OrderBy(c => c).ToList(); } public IReadOnlyList<SnapshotVerificationCheck> Checks { get; } }
    public sealed class SnapshotVerificationDiagnostics { public SnapshotVerificationDiagnostics(IReadOnlyList<SnapshotVerificationFinding> findings) { Findings = findings ?? Array.Empty<SnapshotVerificationFinding>(); } public IReadOnlyList<SnapshotVerificationFinding> Findings { get; } }
    public sealed class SnapshotVerificationHarness
    {
        public (SnapshotVerificationVerdict Verdict, SnapshotVerificationDiagnostics Diagnostics) Verify(bool schemaOk, bool identityOk, bool checksumOk, bool registryOk, bool redactionOk, bool enoughEvidence, bool autoRepairRequested)
        {
            var findings = new List<SnapshotVerificationFinding>();
            if (!schemaOk) findings.Add(SnapshotVerificationFinding.SchemaMismatch);
            if (!identityOk) findings.Add(SnapshotVerificationFinding.IdentityCollision);
            if (!checksumOk) findings.Add(SnapshotVerificationFinding.ChecksumMismatch);
            if (!registryOk) findings.Add(SnapshotVerificationFinding.RegistryReferenceMissing);
            if (!redactionOk) findings.Add(SnapshotVerificationFinding.RedactionRequirementMissing);
            if (autoRepairRequested) findings.Add(SnapshotVerificationFinding.AutoRepairBlocked);
            SnapshotVerificationVerdict verdict = autoRepairRequested ? SnapshotVerificationVerdict.Blocked : !enoughEvidence ? SnapshotVerificationVerdict.NotEnoughEvidence : findings.Any(f => f == SnapshotVerificationFinding.SchemaMismatch || f == SnapshotVerificationFinding.ChecksumMismatch || f == SnapshotVerificationFinding.IdentityCollision) ? SnapshotVerificationVerdict.Invalid : findings.Any() ? SnapshotVerificationVerdict.VerifiedWithWarnings : SnapshotVerificationVerdict.Verified;
            return (verdict, new SnapshotVerificationDiagnostics(findings.OrderBy(f => f).ToList()));
        }
    }

    public enum RedactionPreviewVerdict { PreviewAllowed, PreviewAllowedWithWarnings, BlockedByUnclassifiedSecret, BlockedByForbiddenChannel }
    public enum RedactionPreviewChannel { ClientSafe, QaOnly, ServerInternal, Blocked }
    public enum RedactionPreviewDiagnostics { RedactionRuleMissing, SecretUnclassified, ChannelNotAllowed, SourceMutationRequested, HashingOutOfScope }
    public sealed class RedactionPreviewRequest { public RedactionPreviewRequest(SensitiveFieldClass fieldClass, RedactionPreviewChannel channel, bool mutationRequested, bool finalHashRequested) { FieldClass = fieldClass; Channel = channel; MutationRequested = mutationRequested; FinalHashRequested = finalHashRequested; } public SensitiveFieldClass FieldClass { get; } public RedactionPreviewChannel Channel { get; } public bool MutationRequested { get; } public bool FinalHashRequested { get; } }
    public sealed class RedactionPreviewRuleMatch { public RedactionPreviewRuleMatch(RedactionOutputRule outputRule) { OutputRule = outputRule; } public RedactionOutputRule OutputRule { get; } }
    public sealed class RedactionPreviewItem { public RedactionPreviewItem(string symbolicMask, RedactionPreviewVerdict verdict) { SymbolicMask = symbolicMask ?? string.Empty; Verdict = verdict; } public string SymbolicMask { get; } public RedactionPreviewVerdict Verdict { get; } }
    public sealed class RedactionPreviewContract
    {
        public (RedactionPreviewItem Item, IReadOnlyList<RedactionPreviewDiagnostics> Diagnostics) Preview(RedactionPreviewRequest request, RedactionRequirementRegistry registry)
        {
            var diagnostics = new List<RedactionPreviewDiagnostics>();
            if (request == null) return (new RedactionPreviewItem("[blocked]", RedactionPreviewVerdict.BlockedByUnclassifiedSecret), new[] { RedactionPreviewDiagnostics.SecretUnclassified });
            if (request.MutationRequested) diagnostics.Add(RedactionPreviewDiagnostics.SourceMutationRequested);
            if (request.FinalHashRequested) diagnostics.Add(RedactionPreviewDiagnostics.HashingOutOfScope);
            var resolved = registry.Resolve(request.FieldClass);
            if (resolved.Diagnostics.Issues.Any()) diagnostics.Add(RedactionPreviewDiagnostics.SecretUnclassified);
            if (request.Channel == RedactionPreviewChannel.ClientSafe && (resolved.Rule == RedactionOutputRule.QAOnly || resolved.Rule == RedactionOutputRule.RawForbidden)) diagnostics.Add(RedactionPreviewDiagnostics.ChannelNotAllowed);
            RedactionPreviewVerdict verdict = diagnostics.Contains(RedactionPreviewDiagnostics.SecretUnclassified) ? RedactionPreviewVerdict.BlockedByUnclassifiedSecret : diagnostics.Contains(RedactionPreviewDiagnostics.ChannelNotAllowed) ? RedactionPreviewVerdict.BlockedByForbiddenChannel : diagnostics.Any() ? RedactionPreviewVerdict.PreviewAllowedWithWarnings : RedactionPreviewVerdict.PreviewAllowed;
            return (new RedactionPreviewItem(verdict == RedactionPreviewVerdict.PreviewAllowed ? "[preview-mask]" : "[blocked]", verdict), diagnostics.OrderBy(d => d).ToList());
        }
    }

    public enum PersistenceObservationSeverity { Info, Warning, Error, Critical }
    public enum PersistenceObservabilityHook { SaveLoadBoundaryObserved, FixtureCatalogObserved, MigrationDryRunObserved, SnapshotHarnessObserved, RedactionPreviewObserved, PersistenceDriftObserved, ReportExportObserved, BackendReadinessObserved }
    public sealed class PersistenceCorrelationId { public PersistenceCorrelationId(string value) { Value = value ?? string.Empty; } public string Value { get; } }
    public sealed class PersistenceObservationPayload { public PersistenceObservationPayload(PersistenceCorrelationId correlationId, PersistenceObservabilityHook hook, string sourceBee, string domain, PersistenceObservationSeverity severity, string code, string evidenceLink, long logicalTick, string identity) { CorrelationId = correlationId; Hook = hook; SourceBee = sourceBee ?? string.Empty; Domain = domain ?? string.Empty; Severity = severity; Code = code ?? string.Empty; EvidenceLink = evidenceLink ?? string.Empty; LogicalTick = logicalTick; Identity = identity ?? string.Empty; } public PersistenceCorrelationId CorrelationId { get; } public PersistenceObservabilityHook Hook { get; } public string SourceBee { get; } public string Domain { get; } public PersistenceObservationSeverity Severity { get; } public string Code { get; } public string EvidenceLink { get; } public long LogicalTick { get; } public string Identity { get; } }
    public enum PersistenceObservationDiagnostics { MissingCorrelationId, MutableEventRequested, UnregisteredHook, PayloadEvidenceMissing, ObservationOrderUnstable }
    public sealed class PersistenceObservationTrail
    {
        public PersistenceObservationTrail(IEnumerable<PersistenceObservationPayload> payloads) { Payloads = (payloads ?? Array.Empty<PersistenceObservationPayload>()).OrderBy(p => p.LogicalTick).ThenBy(p => p.Domain, StringComparer.Ordinal).ThenByDescending(p => p.Severity).ThenBy(p => p.Code, StringComparer.Ordinal).ThenBy(p => p.Identity, StringComparer.Ordinal).ToList(); }
        public IReadOnlyList<PersistenceObservationPayload> Payloads { get; }
    }
    public sealed class PersistenceObservationHookContract
    {
        public IReadOnlyList<PersistenceObservationDiagnostics> Validate(PersistenceObservationPayload payload, bool mutablePublishRequested)
        {
            var issues = new List<PersistenceObservationDiagnostics>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.CorrelationId?.Value)) issues.Add(PersistenceObservationDiagnostics.MissingCorrelationId);
            if (payload != null && string.IsNullOrWhiteSpace(payload.EvidenceLink)) issues.Add(PersistenceObservationDiagnostics.PayloadEvidenceMissing);
            if (mutablePublishRequested) issues.Add(PersistenceObservationDiagnostics.MutableEventRequested);
            return issues.OrderBy(i => i).ToList();
        }
    }

    public enum SaveLoadDemoReadinessBadge { ReadyForPreview, ReadyWithWarnings, Blocked, NotImplemented, ServerAnalysisRequired }
    public enum SaveLoadDemoSection { RuntimeBoundary, FixtureCatalog, MigrationDryRun, SnapshotHarness, RedactionPreview, ObservabilityHooks, DriftSummary, BackendReadiness, OpenRisks }
    public enum SaveLoadDemoWarning { DemoReadModelSourceMissing, DemoMutationRequested, GameplayLogicDetected, RuntimeActionRequested, SecretChannelBlocked }
    public sealed class SaveLoadDemoReadModel { public SaveLoadDemoReadModel(SaveLoadDemoReadinessBadge badge, IEnumerable<SaveLoadDemoSection> sections, IEnumerable<SaveLoadDemoWarning> warnings) { Badge = badge; Sections = (sections ?? Array.Empty<SaveLoadDemoSection>()).OrderBy(s => s).ToList(); Warnings = (warnings ?? Array.Empty<SaveLoadDemoWarning>()).OrderBy(w => w).ToList(); } public SaveLoadDemoReadinessBadge Badge { get; } public IReadOnlyList<SaveLoadDemoSection> Sections { get; } public IReadOnlyList<SaveLoadDemoWarning> Warnings { get; } public int GapCount => Warnings.Count; }
    public sealed class SaveLoadDemoReadModelBuilder
    {
        public SaveLoadDemoReadModel Build(bool allSourcesValid, bool serverAnalyzed, bool runtimeActionRequested, bool secretBlocked)
        {
            var warnings = new List<SaveLoadDemoWarning>();
            if (!allSourcesValid) warnings.Add(SaveLoadDemoWarning.DemoReadModelSourceMissing);
            if (runtimeActionRequested) warnings.Add(SaveLoadDemoWarning.RuntimeActionRequested);
            if (secretBlocked) warnings.Add(SaveLoadDemoWarning.SecretChannelBlocked);
            SaveLoadDemoReadinessBadge badge = !serverAnalyzed ? SaveLoadDemoReadinessBadge.ServerAnalysisRequired : runtimeActionRequested || secretBlocked ? SaveLoadDemoReadinessBadge.Blocked : !allSourcesValid ? SaveLoadDemoReadinessBadge.ReadyWithWarnings : SaveLoadDemoReadinessBadge.ReadyForPreview;
            return new SaveLoadDemoReadModel(badge, Enum.GetValues(typeof(SaveLoadDemoSection)).Cast<SaveLoadDemoSection>(), warnings);
        }
    }

    public enum PersistenceRegressionCategory { Boundary, FixtureCatalog, MigrationDryRun, SnapshotHarness, RedactionPreview, ObservabilityHook, DriftDetection, DemoReadModel, ReportExport, BackendReadiness }
    public enum PersistenceRegressionExpectedVerdict { Positive, Warning, Blocking }
    public enum PersistenceRegressionStatus { ContractReady, UnitTestExpected, IntegrationFuture, RuntimeBlocked, EvidenceMissing, InvalidScenario }
    public sealed class PersistenceRegressionEvidenceLink { public PersistenceRegressionEvidenceLink(string id) { Id = id ?? string.Empty; } public string Id { get; } }
    public sealed class PersistenceRegressionScenario { public PersistenceRegressionScenario(string id, PersistenceRegressionCategory category, PersistenceRegressionExpectedVerdict? expectedVerdict, PersistenceRegressionStatus status, PersistenceRegressionEvidenceLink evidenceLink) { Id = id ?? string.Empty; Category = category; ExpectedVerdict = expectedVerdict; Status = status; EvidenceLink = evidenceLink; } public string Id { get; } public PersistenceRegressionCategory Category { get; } public PersistenceRegressionExpectedVerdict? ExpectedVerdict { get; } public PersistenceRegressionStatus Status { get; } public PersistenceRegressionEvidenceLink EvidenceLink { get; } }
    public enum PersistenceRegressionDiagnostics { ScenarioSourceMissing, ExpectedVerdictMissing, EvidenceLinkMissing, RuntimeClaimInvented, CategoryCoverageGap }
    public sealed class PersistenceRegressionSuite
    {
        public PersistenceRegressionSuite(IEnumerable<PersistenceRegressionScenario> scenarios) { Scenarios = (scenarios ?? Array.Empty<PersistenceRegressionScenario>()).OrderBy(s => s.Category).ThenBy(s => s.Id, StringComparer.Ordinal).ToList(); Diagnostics = Detect(Scenarios); }
        public IReadOnlyList<PersistenceRegressionScenario> Scenarios { get; }
        public IReadOnlyList<PersistenceRegressionDiagnostics> Diagnostics { get; }
        private static IReadOnlyList<PersistenceRegressionDiagnostics> Detect(IReadOnlyList<PersistenceRegressionScenario> scenarios)
        {
            var issues = new List<PersistenceRegressionDiagnostics>();
            if (scenarios.Any(s => !s.ExpectedVerdict.HasValue)) issues.Add(PersistenceRegressionDiagnostics.ExpectedVerdictMissing);
            if (scenarios.Any(s => s.EvidenceLink == null || string.IsNullOrWhiteSpace(s.EvidenceLink.Id))) issues.Add(PersistenceRegressionDiagnostics.EvidenceLinkMissing);
            if (scenarios.Any(s => s.Status == PersistenceRegressionStatus.ContractReady && s.EvidenceLink == null)) issues.Add(PersistenceRegressionDiagnostics.RuntimeClaimInvented);
            foreach (PersistenceRegressionCategory category in Enum.GetValues(typeof(PersistenceRegressionCategory))) if (!scenarios.Any(s => s.Category == category)) issues.Add(PersistenceRegressionDiagnostics.CategoryCoverageGap);
            return issues.Distinct().OrderBy(i => i).ToList();
        }
    }

    public enum BackendPersistenceReadinessStatus { ReadyForServerAnalysis, NeedsBeeRevision, BlockedByMissingEvidence, BlockedByRuntimeScope, BlockedBySqlScope, NotApplicable }
    public enum BackendPersistenceServerAnalysisNeed { None, Required, Stale }
    public sealed class BackendPersistenceRisk { public BackendPersistenceRisk(string reason) { Reason = reason ?? string.Empty; } public string Reason { get; } }
    public sealed class BackendPersistenceRequirementRow { public BackendPersistenceRequirementRow(string beeSource, bool sharedContractImpact, bool serverContractImpact, bool storageFutureImpact, bool sqlOutOfScope, bool qaEvidenceRequired, BackendPersistenceReadinessStatus status, string blockingReason) { BeeSource = beeSource ?? string.Empty; SharedContractImpact = sharedContractImpact; ServerContractImpact = serverContractImpact; StorageFutureImpact = storageFutureImpact; SqlOutOfScope = sqlOutOfScope; QaEvidenceRequired = qaEvidenceRequired; Status = status; BlockingReason = blockingReason ?? string.Empty; } public string BeeSource { get; } public bool SharedContractImpact { get; } public bool ServerContractImpact { get; } public bool StorageFutureImpact { get; } public bool SqlOutOfScope { get; } public bool QaEvidenceRequired { get; } public BackendPersistenceReadinessStatus Status { get; } public string BlockingReason { get; } }
    public enum BackendPersistenceDiagnostics { ServerAnalysisMissing, SqlImplementationRequested, ServiceCreationRequested, RequirementSourceMissing, RuntimePersistencePremature }
    public sealed class BackendPersistenceReadinessMatrix
    {
        public BackendPersistenceReadinessMatrix(IEnumerable<BackendPersistenceRequirementRow> rows, int serverProgressBee)
        {
            Rows = (rows ?? Array.Empty<BackendPersistenceRequirementRow>()).OrderBy(r => r.BeeSource, StringComparer.Ordinal).ToList();
            Diagnostics = Detect(Rows, serverProgressBee);
        }
        public IReadOnlyList<BackendPersistenceRequirementRow> Rows { get; }
        public IReadOnlyList<BackendPersistenceDiagnostics> Diagnostics { get; }
        private static IReadOnlyList<BackendPersistenceDiagnostics> Detect(IReadOnlyList<BackendPersistenceRequirementRow> rows, int serverProgressBee)
        {
            var issues = new List<BackendPersistenceDiagnostics>();
            if (serverProgressBee <= 230 && rows.Any(r => string.CompareOrdinal(r.BeeSource, "BEE-231") >= 0)) issues.Add(BackendPersistenceDiagnostics.ServerAnalysisMissing);
            if (rows.Any(r => string.IsNullOrWhiteSpace(r.BeeSource))) issues.Add(BackendPersistenceDiagnostics.RequirementSourceMissing);
            if (rows.Any(r => r.Status == BackendPersistenceReadinessStatus.BlockedBySqlScope)) issues.Add(BackendPersistenceDiagnostics.SqlImplementationRequested);
            if (rows.Any(r => r.Status == BackendPersistenceReadinessStatus.BlockedByRuntimeScope)) issues.Add(BackendPersistenceDiagnostics.ServiceCreationRequested);
            return issues.Distinct().OrderBy(i => i).ToList();
        }
    }

    public enum PersistenceRuntimeReadinessVerdict { ReadyForDesignReview, ReadyWithWarnings, NeedsBeeRevision, BlockedByServerAnalysis, BlockedByRuntimeScope, BlockedByBee241Premature }
    public sealed class PersistenceRuntimeReadinessCriterion { public PersistenceRuntimeReadinessCriterion(string family, bool passed, bool warning, bool runtimeBlocked) { Family = family ?? string.Empty; Passed = passed; Warning = warning; RuntimeBlocked = runtimeBlocked; } public string Family { get; } public bool Passed { get; } public bool Warning { get; } public bool RuntimeBlocked { get; } }
    public sealed class PersistenceRuntimeReadinessRisk { public PersistenceRuntimeReadinessRisk(string reason) { Reason = reason ?? string.Empty; } public string Reason { get; } }
    public sealed class PersistenceRuntimeReadinessDiagnostics { public PersistenceRuntimeReadinessDiagnostics(IReadOnlyList<string> issues) { Issues = issues ?? Array.Empty<string>(); } public IReadOnlyList<string> Issues { get; } }
    public sealed class PersistenceRuntimeReadinessReport { public PersistenceRuntimeReadinessReport(PersistenceRuntimeReadinessVerdict verdict, IEnumerable<PersistenceRuntimeReadinessCriterion> criteria) { Verdict = verdict; Criteria = (criteria ?? Array.Empty<PersistenceRuntimeReadinessCriterion>()).OrderBy(c => c.Family, StringComparer.Ordinal).ToList(); Diagnostics = new PersistenceRuntimeReadinessDiagnostics(Criteria.Where(c => !c.Passed).Select(c => c.Family).ToList()); } public PersistenceRuntimeReadinessVerdict Verdict { get; } public IReadOnlyList<PersistenceRuntimeReadinessCriterion> Criteria { get; } public PersistenceRuntimeReadinessDiagnostics Diagnostics { get; } }
    public sealed class PersistenceRuntimeReadinessGate
    {
        public PersistenceRuntimeReadinessReport Evaluate(IEnumerable<PersistenceRuntimeReadinessCriterion> criteria, bool serverProgressBeyond230, bool bee241Referenced, bool sqlOrServerRequested, bool runtimeEvidenceInvented)
        {
            var list = (criteria ?? Array.Empty<PersistenceRuntimeReadinessCriterion>()).ToList();
            PersistenceRuntimeReadinessVerdict verdict = bee241Referenced ? PersistenceRuntimeReadinessVerdict.BlockedByBee241Premature : sqlOrServerRequested || list.Any(c => c.RuntimeBlocked) ? PersistenceRuntimeReadinessVerdict.BlockedByRuntimeScope : !serverProgressBeyond230 ? PersistenceRuntimeReadinessVerdict.BlockedByServerAnalysis : runtimeEvidenceInvented || list.Any(c => !c.Passed) ? PersistenceRuntimeReadinessVerdict.NeedsBeeRevision : list.Any(c => c.Warning) ? PersistenceRuntimeReadinessVerdict.ReadyWithWarnings : PersistenceRuntimeReadinessVerdict.ReadyForDesignReview;
            return new PersistenceRuntimeReadinessReport(verdict, list);
        }
    }
}
