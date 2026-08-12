using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Colony
{
    public enum ColonyLaunchPrerequisiteStatus { ReadyToLaunch, MissingEvidence, MissingOwner, BlockedByServerEscalation, BlockedByQaObservation, BlockedByDemoGap, OutOfScope }
    public enum ColonyLaunchBlockingReason { None, EvidenceMissing, OwnerMissing, ServerEscalationOpen, QaObservationOpen, DemoGapOpen, Bee301Blocked }
    public enum ImplementationLaunchChecklistDiagnosticCode { LaunchChecklistItemMissing, LaunchOwnerMissing, LaunchEvidenceMissing, PrematureSliceExecutionRequested, Bee301LaunchAttemptBlocked, MultiplayerArmyHandoffMissing }

    public sealed class ColonyLaunchChecklistItem
    {
        public ColonyLaunchChecklistItem(string itemId, string sourceBee, string sliceId, string owner, string requiredEvidence, ColonyLaunchPrerequisiteStatus status, ColonyLaunchBlockingReason blockingReason, string nextAction, bool prematureSliceExecutionRequested = false, bool multiplayerArmyHandoffMissing = false)
        {
            ItemId = ColonyIntegrationIds.Require(itemId);
            SourceBee = sourceBee ?? string.Empty;
            SliceId = sliceId ?? string.Empty;
            Owner = owner ?? string.Empty;
            RequiredEvidence = requiredEvidence ?? string.Empty;
            Status = status;
            BlockingReason = blockingReason;
            NextAction = nextAction ?? string.Empty;
            PrematureSliceExecutionRequested = prematureSliceExecutionRequested;
            MultiplayerArmyHandoffMissing = multiplayerArmyHandoffMissing;
        }

        public string ItemId { get; }
        public string SourceBee { get; }
        public string SliceId { get; }
        public string Owner { get; }
        public string RequiredEvidence { get; }
        public ColonyLaunchPrerequisiteStatus Status { get; }
        public ColonyLaunchBlockingReason BlockingReason { get; }
        public string NextAction { get; }
        public bool PrematureSliceExecutionRequested { get; }
        public bool MultiplayerArmyHandoffMissing { get; }
    }

    public sealed class ColonyLaunchChecklistExport
    {
        public ColonyLaunchChecklistExport(string checklistId, ColonyLaunchPrerequisiteStatus overallStatus, string bee301Status)
        {
            ChecklistId = checklistId ?? string.Empty;
            OverallStatus = overallStatus;
            Bee301Status = bee301Status ?? string.Empty;
        }

        public string ChecklistId { get; }
        public ColonyLaunchPrerequisiteStatus OverallStatus { get; }
        public string Bee301Status { get; }
    }

    public sealed class ColonyImplementationLaunchChecklist
    {
        public const string Bee301BlockedStatus = "BEE-301 bloquee jusqu'a validation architecte.";

        public ColonyImplementationLaunchChecklist(string checklistId, string beeRange, string gateReference, IReadOnlyList<ColonyLaunchChecklistItem> items, bool bee301LaunchAttempted = false)
        {
            ChecklistId = ColonyIntegrationIds.Require(checklistId);
            BeeRange = beeRange ?? string.Empty;
            GateReference = gateReference ?? string.Empty;
            Items = items ?? Array.Empty<ColonyLaunchChecklistItem>();
            Bee301LaunchAttempted = bee301LaunchAttempted;
        }

        public string ChecklistId { get; }
        public string BeeRange { get; }
        public string GateReference { get; }
        public IReadOnlyList<ColonyLaunchChecklistItem> Items { get; }
        public bool Bee301LaunchAttempted { get; }

        public ImplementationLaunchChecklistDiagnostics Evaluate()
        {
            var findings = new List<ImplementationLaunchChecklistDiagnosticCode>();
            if (Items.Count == 0 || Items.Any(i => string.IsNullOrWhiteSpace(i.SliceId))) findings.Add(ImplementationLaunchChecklistDiagnosticCode.LaunchChecklistItemMissing);
            if (Items.Any(i => string.IsNullOrWhiteSpace(i.Owner))) findings.Add(ImplementationLaunchChecklistDiagnosticCode.LaunchOwnerMissing);
            if (Items.Any(i => string.IsNullOrWhiteSpace(i.RequiredEvidence))) findings.Add(ImplementationLaunchChecklistDiagnosticCode.LaunchEvidenceMissing);
            if (Items.Any(i => i.PrematureSliceExecutionRequested)) findings.Add(ImplementationLaunchChecklistDiagnosticCode.PrematureSliceExecutionRequested);
            if (Bee301LaunchAttempted) findings.Add(ImplementationLaunchChecklistDiagnosticCode.Bee301LaunchAttemptBlocked);
            if (Items.Any(i => i.MultiplayerArmyHandoffMissing)) findings.Add(ImplementationLaunchChecklistDiagnosticCode.MultiplayerArmyHandoffMissing);
            return new ImplementationLaunchChecklistDiagnostics(findings, new ColonyLaunchChecklistExport(ChecklistId, BuildStatus(findings), Bee301BlockedStatus));
        }

        private static ColonyLaunchPrerequisiteStatus BuildStatus(IReadOnlyList<ImplementationLaunchChecklistDiagnosticCode> findings)
        {
            if (findings.Contains(ImplementationLaunchChecklistDiagnosticCode.Bee301LaunchAttemptBlocked) || findings.Contains(ImplementationLaunchChecklistDiagnosticCode.PrematureSliceExecutionRequested)) return ColonyLaunchPrerequisiteStatus.OutOfScope;
            if (findings.Contains(ImplementationLaunchChecklistDiagnosticCode.LaunchOwnerMissing)) return ColonyLaunchPrerequisiteStatus.MissingOwner;
            if (findings.Contains(ImplementationLaunchChecklistDiagnosticCode.LaunchEvidenceMissing)) return ColonyLaunchPrerequisiteStatus.MissingEvidence;
            return findings.Count == 0 ? ColonyLaunchPrerequisiteStatus.ReadyToLaunch : ColonyLaunchPrerequisiteStatus.BlockedByDemoGap;
        }
    }

    public sealed class ImplementationLaunchChecklistDiagnostics
    {
        public ImplementationLaunchChecklistDiagnostics(IReadOnlyList<ImplementationLaunchChecklistDiagnosticCode> findings, ColonyLaunchChecklistExport export) { Findings = findings ?? Array.Empty<ImplementationLaunchChecklistDiagnosticCode>(); Export = export; }
        public IReadOnlyList<ImplementationLaunchChecklistDiagnosticCode> Findings { get; }
        public ColonyLaunchChecklistExport Export { get; }
        public bool Contains(ImplementationLaunchChecklistDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonySliceVerificationResultKind { VerifiedReady, VerifiedWithWarnings, Blocked, NeedsReplan, OutOfScope, InvalidOwnership }
    public enum SliceExecutionVerificationDiagnosticCode { SliceVerificationMissing, SliceOwnerChanged, SliceDependencyInvalid, SliceLimitHidden, SliceReplanRequired, RuntimeMutationRequested, MultiplayerArmySignalHidden }

    public sealed class ColonySliceVerificationCase
    {
        public ColonySliceVerificationCase(string caseId, string sliceId, string expectedOwner, IReadOnlyList<string> dependencySet, string requiredCriterion, bool forbiddenMutation, bool multiplayerArmySignalDeclared = true)
        {
            CaseId = ColonyIntegrationIds.Require(caseId);
            SliceId = sliceId ?? string.Empty;
            ExpectedOwner = expectedOwner ?? string.Empty;
            DependencySet = dependencySet ?? Array.Empty<string>();
            RequiredCriterion = requiredCriterion ?? string.Empty;
            ForbiddenMutation = forbiddenMutation;
            MultiplayerArmySignalDeclared = multiplayerArmySignalDeclared;
        }

        public string CaseId { get; }
        public string SliceId { get; }
        public string ExpectedOwner { get; }
        public IReadOnlyList<string> DependencySet { get; }
        public string RequiredCriterion { get; }
        public bool ForbiddenMutation { get; }
        public bool MultiplayerArmySignalDeclared { get; }
    }

    public sealed class ColonySliceVerificationResult
    {
        public ColonySliceVerificationResult(string caseId, ColonySliceVerificationResultKind result, string evidence, IReadOnlyList<string> warnings, string nextAction, bool ownerChanged = false, bool dependencyInvalid = false, bool limitHidden = false, bool replanRequired = false)
        {
            CaseId = caseId ?? string.Empty;
            Result = result;
            Evidence = evidence ?? string.Empty;
            Warnings = warnings ?? Array.Empty<string>();
            NextAction = nextAction ?? string.Empty;
            OwnerChanged = ownerChanged;
            DependencyInvalid = dependencyInvalid;
            LimitHidden = limitHidden;
            ReplanRequired = replanRequired;
        }

        public string CaseId { get; }
        public ColonySliceVerificationResultKind Result { get; }
        public string Evidence { get; }
        public IReadOnlyList<string> Warnings { get; }
        public string NextAction { get; }
        public bool OwnerChanged { get; }
        public bool DependencyInvalid { get; }
        public bool LimitHidden { get; }
        public bool ReplanRequired { get; }
    }

    public sealed class ColonySliceReplanRequest
    {
        public ColonySliceReplanRequest(string sliceId, string reason, string owner) { SliceId = sliceId ?? string.Empty; Reason = reason ?? string.Empty; Owner = owner ?? string.Empty; }
        public string SliceId { get; }
        public string Reason { get; }
        public string Owner { get; }
    }

    public sealed class ColonySliceExecutionVerification
    {
        public ColonySliceExecutionVerification(string verificationId, string sourceSliceMap, IReadOnlyList<ColonySliceVerificationCase> cases, IReadOnlyList<ColonySliceVerificationResult> results, string exportStatus)
        {
            VerificationId = ColonyIntegrationIds.Require(verificationId);
            SourceSliceMap = sourceSliceMap ?? string.Empty;
            Cases = cases ?? Array.Empty<ColonySliceVerificationCase>();
            Results = results ?? Array.Empty<ColonySliceVerificationResult>();
            ExportStatus = exportStatus ?? string.Empty;
        }

        public string VerificationId { get; }
        public string SourceSliceMap { get; }
        public IReadOnlyList<ColonySliceVerificationCase> Cases { get; }
        public IReadOnlyList<ColonySliceVerificationResult> Results { get; }
        public string ExportStatus { get; }
        public SliceExecutionVerificationDiagnostics Evaluate()
        {
            var findings = new List<SliceExecutionVerificationDiagnosticCode>();
            if (Cases.Count == 0 || Results.Count == 0) findings.Add(SliceExecutionVerificationDiagnosticCode.SliceVerificationMissing);
            if (Results.Any(r => r.OwnerChanged || r.Result == ColonySliceVerificationResultKind.InvalidOwnership)) findings.Add(SliceExecutionVerificationDiagnosticCode.SliceOwnerChanged);
            if (Results.Any(r => r.DependencyInvalid)) findings.Add(SliceExecutionVerificationDiagnosticCode.SliceDependencyInvalid);
            if (Results.Any(r => r.LimitHidden)) findings.Add(SliceExecutionVerificationDiagnosticCode.SliceLimitHidden);
            if (Results.Any(r => r.ReplanRequired || r.Result == ColonySliceVerificationResultKind.NeedsReplan)) findings.Add(SliceExecutionVerificationDiagnosticCode.SliceReplanRequired);
            if (Cases.Any(c => c.ForbiddenMutation)) findings.Add(SliceExecutionVerificationDiagnosticCode.RuntimeMutationRequested);
            if (Cases.Any(c => !c.MultiplayerArmySignalDeclared)) findings.Add(SliceExecutionVerificationDiagnosticCode.MultiplayerArmySignalHidden);
            return new SliceExecutionVerificationDiagnostics(findings);
        }
    }

    public sealed class SliceExecutionVerificationDiagnostics
    {
        public SliceExecutionVerificationDiagnostics(IReadOnlyList<SliceExecutionVerificationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<SliceExecutionVerificationDiagnosticCode>(); }
        public IReadOnlyList<SliceExecutionVerificationDiagnosticCode> Findings { get; }
        public bool Contains(SliceExecutionVerificationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyAdapterSmokeStatus { SmokePass, SmokeWarning, SmokeBlocked, MissingPort, MissingSource, ForbiddenMutation, ServerAuthorityRequired }
    public enum AdapterSmokeValidationDiagnosticCode { AdapterSmokeProbeMissing, AdapterSmokeSourceMissing, AdapterSmokePortMissing, AdapterSmokeMutationRequested, AdapterSmokeGapUnclassified, ServerAuthorityRequired }

    public sealed class ColonyAdapterSmokeProbe
    {
        public ColonyAdapterSmokeProbe(string probeId, string adapterId, ColonyRuntimeAdapterPort? port, AdapterSourceOfTruth? sourceOfTruth, string expectedSignal, bool mutationAllowed = false)
        {
            ProbeId = ColonyIntegrationIds.Require(probeId);
            AdapterId = adapterId ?? string.Empty;
            Port = port;
            SourceOfTruth = sourceOfTruth;
            ExpectedSignal = expectedSignal ?? string.Empty;
            MutationAllowed = mutationAllowed;
        }

        public string ProbeId { get; }
        public string AdapterId { get; }
        public ColonyRuntimeAdapterPort? Port { get; }
        public AdapterSourceOfTruth? SourceOfTruth { get; }
        public string ExpectedSignal { get; }
        public bool MutationAllowed { get; }
    }

    public sealed class ColonyAdapterSmokeGap
    {
        public ColonyAdapterSmokeGap(string gapId, string classification, string limitation) { GapId = gapId ?? string.Empty; Classification = classification ?? string.Empty; Limitation = limitation ?? string.Empty; }
        public string GapId { get; }
        public string Classification { get; }
        public string Limitation { get; }
    }

    public sealed class ColonyAdapterSmokeSignal
    {
        public ColonyAdapterSmokeSignal(string probeId, ColonyAdapterSmokeStatus status, string observedSignal, ColonyAdapterSmokeGap gap, string limitation)
        {
            ProbeId = probeId ?? string.Empty;
            Status = status;
            ObservedSignal = observedSignal ?? string.Empty;
            Gap = gap;
            Limitation = limitation ?? string.Empty;
        }

        public string ProbeId { get; }
        public ColonyAdapterSmokeStatus Status { get; }
        public string ObservedSignal { get; }
        public ColonyAdapterSmokeGap Gap { get; }
        public string Limitation { get; }
    }

    public sealed class ColonyAdapterSmokeReport
    {
        public ColonyAdapterSmokeReport(IReadOnlyList<ColonyAdapterSmokeSignal> signals) { Signals = signals ?? Array.Empty<ColonyAdapterSmokeSignal>(); }
        public IReadOnlyList<ColonyAdapterSmokeSignal> Signals { get; }
    }

    public sealed class ColonyAdapterSmokeValidation
    {
        public ColonyAdapterSmokeValidation(string validationId, string adapterRegistry, IReadOnlyList<ColonyAdapterSmokeProbe> probes, ColonyAdapterSmokeReport report)
        {
            ValidationId = ColonyIntegrationIds.Require(validationId);
            AdapterRegistry = adapterRegistry ?? string.Empty;
            Probes = probes ?? Array.Empty<ColonyAdapterSmokeProbe>();
            Report = report;
        }

        public string ValidationId { get; }
        public string AdapterRegistry { get; }
        public IReadOnlyList<ColonyAdapterSmokeProbe> Probes { get; }
        public ColonyAdapterSmokeReport Report { get; }
        public AdapterSmokeValidationDiagnostics Evaluate()
        {
            var findings = new List<AdapterSmokeValidationDiagnosticCode>();
            if (Probes.Count == 0) findings.Add(AdapterSmokeValidationDiagnosticCode.AdapterSmokeProbeMissing);
            if (Probes.Any(p => p.SourceOfTruth == null)) findings.Add(AdapterSmokeValidationDiagnosticCode.AdapterSmokeSourceMissing);
            if (Probes.Any(p => p.Port == null)) findings.Add(AdapterSmokeValidationDiagnosticCode.AdapterSmokePortMissing);
            if (Probes.Any(p => p.MutationAllowed) || (Report != null && Report.Signals.Any(s => s.Status == ColonyAdapterSmokeStatus.ForbiddenMutation))) findings.Add(AdapterSmokeValidationDiagnosticCode.AdapterSmokeMutationRequested);
            if (Report != null && Report.Signals.Any(s => s.Gap != null && string.IsNullOrWhiteSpace(s.Gap.Classification))) findings.Add(AdapterSmokeValidationDiagnosticCode.AdapterSmokeGapUnclassified);
            if (Report != null && Report.Signals.Any(s => s.Status == ColonyAdapterSmokeStatus.ServerAuthorityRequired)) findings.Add(AdapterSmokeValidationDiagnosticCode.ServerAuthorityRequired);
            return new AdapterSmokeValidationDiagnostics(findings);
        }
    }

    public sealed class AdapterSmokeValidationDiagnostics
    {
        public AdapterSmokeValidationDiagnostics(IReadOnlyList<AdapterSmokeValidationDiagnosticCode> findings) { Findings = findings ?? Array.Empty<AdapterSmokeValidationDiagnosticCode>(); }
        public IReadOnlyList<AdapterSmokeValidationDiagnosticCode> Findings { get; }
        public bool Contains(AdapterSmokeValidationDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyReadModelFreshnessStatus { Consistent, Stale, Mismatch, MissingBinding, SurfaceOutOfScope, InvalidMutableProjection }
    public enum ReadModelDemoConsistencyDiagnosticCode { DemoReadModelMismatch, DemoReadModelStale, DemoBindingMissing, DemoProjectionMutable, DemoSurfaceUnregistered, ServerAuthorityLimitMissing }

    public sealed class ColonyDemoReadModelSurface
    {
        public ColonyDemoReadModelSurface(string surfaceId, string demoId, string readModelId, IReadOnlyList<string> fieldSet, ColonyReadModelFreshnessStatus freshnessStatus, bool registered = true, bool mutableProjection = false, bool serverAuthorityLimitMissing = false)
        {
            SurfaceId = ColonyIntegrationIds.Require(surfaceId);
            DemoId = demoId ?? string.Empty;
            ReadModelId = readModelId ?? string.Empty;
            FieldSet = fieldSet ?? Array.Empty<string>();
            FreshnessStatus = freshnessStatus;
            Registered = registered;
            MutableProjection = mutableProjection;
            ServerAuthorityLimitMissing = serverAuthorityLimitMissing;
        }

        public string SurfaceId { get; }
        public string DemoId { get; }
        public string ReadModelId { get; }
        public IReadOnlyList<string> FieldSet { get; }
        public ColonyReadModelFreshnessStatus FreshnessStatus { get; }
        public bool Registered { get; }
        public bool MutableProjection { get; }
        public bool ServerAuthorityLimitMissing { get; }
    }

    public sealed class ColonyDemoConsistencyMismatch
    {
        public ColonyDemoConsistencyMismatch(string mismatchId, string field, string expectedSource, string observedSurfaceA, string observedSurfaceB, int severity)
        {
            MismatchId = mismatchId ?? string.Empty;
            Field = field ?? string.Empty;
            ExpectedSource = expectedSource ?? string.Empty;
            ObservedSurfaceA = observedSurfaceA ?? string.Empty;
            ObservedSurfaceB = observedSurfaceB ?? string.Empty;
            Severity = Math.Max(0, severity);
        }

        public string MismatchId { get; }
        public string Field { get; }
        public string ExpectedSource { get; }
        public string ObservedSurfaceA { get; }
        public string ObservedSurfaceB { get; }
        public int Severity { get; }
    }

    public sealed class ColonyDemoConsistencyExport
    {
        public ColonyDemoConsistencyExport(string checkId, ColonyReadModelFreshnessStatus exportStatus) { CheckId = checkId ?? string.Empty; ExportStatus = exportStatus; }
        public string CheckId { get; }
        public ColonyReadModelFreshnessStatus ExportStatus { get; }
    }

    public sealed class ColonyReadModelDemoConsistency
    {
        public ColonyReadModelDemoConsistency(string checkId, IReadOnlyList<ColonyDemoReadModelSurface> surfaces, IReadOnlyList<ReadModelBindingField> fields, IReadOnlyList<ColonyDemoConsistencyMismatch> mismatches, ColonyReadModelFreshnessStatus exportStatus)
        {
            CheckId = ColonyIntegrationIds.Require(checkId);
            Surfaces = surfaces ?? Array.Empty<ColonyDemoReadModelSurface>();
            Fields = fields ?? Array.Empty<ReadModelBindingField>();
            Mismatches = mismatches ?? Array.Empty<ColonyDemoConsistencyMismatch>();
            ExportStatus = exportStatus;
        }

        public string CheckId { get; }
        public IReadOnlyList<ColonyDemoReadModelSurface> Surfaces { get; }
        public IReadOnlyList<ReadModelBindingField> Fields { get; }
        public IReadOnlyList<ColonyDemoConsistencyMismatch> Mismatches { get; }
        public ColonyReadModelFreshnessStatus ExportStatus { get; }
        public ReadModelDemoConsistencyDiagnostics Evaluate()
        {
            var findings = new List<ReadModelDemoConsistencyDiagnosticCode>();
            if (Mismatches.Count > 0 || Surfaces.Any(s => s.FreshnessStatus == ColonyReadModelFreshnessStatus.Mismatch)) findings.Add(ReadModelDemoConsistencyDiagnosticCode.DemoReadModelMismatch);
            if (Surfaces.Any(s => s.FreshnessStatus == ColonyReadModelFreshnessStatus.Stale)) findings.Add(ReadModelDemoConsistencyDiagnosticCode.DemoReadModelStale);
            if (Fields.Count == 0 || Surfaces.Any(s => s.FreshnessStatus == ColonyReadModelFreshnessStatus.MissingBinding)) findings.Add(ReadModelDemoConsistencyDiagnosticCode.DemoBindingMissing);
            if (Surfaces.Any(s => s.MutableProjection || s.FreshnessStatus == ColonyReadModelFreshnessStatus.InvalidMutableProjection)) findings.Add(ReadModelDemoConsistencyDiagnosticCode.DemoProjectionMutable);
            if (Surfaces.Any(s => !s.Registered)) findings.Add(ReadModelDemoConsistencyDiagnosticCode.DemoSurfaceUnregistered);
            if (Surfaces.Any(s => s.ServerAuthorityLimitMissing)) findings.Add(ReadModelDemoConsistencyDiagnosticCode.ServerAuthorityLimitMissing);
            return new ReadModelDemoConsistencyDiagnostics(findings, new ColonyDemoConsistencyExport(CheckId, ExportStatus));
        }
    }

    public sealed class ReadModelDemoConsistencyDiagnostics
    {
        public ReadModelDemoConsistencyDiagnostics(IReadOnlyList<ReadModelDemoConsistencyDiagnosticCode> findings, ColonyDemoConsistencyExport export) { Findings = findings ?? Array.Empty<ReadModelDemoConsistencyDiagnosticCode>(); Export = export; }
        public IReadOnlyList<ReadModelDemoConsistencyDiagnosticCode> Findings { get; }
        public ColonyDemoConsistencyExport Export { get; }
        public bool Contains(ReadModelDemoConsistencyDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyQaEvidenceSourceType { Observation, Scenario, Risk, Checklist, SliceVerification, AdapterSmoke, DemoConsistency, MultiplayerArmyHandoffEvidence }
    public enum ColonyQaEvidenceExportStatus { Exportable, Incomplete, Blocked, OutOfScope, RejectedAsFinalVerdict }
    public enum ColonyQaEvidenceExportDiagnosticCode { QaEvidenceSourceMissing, QaEvidenceLimitMissing, QaEvidenceFinalVerdictClaimed, QaEvidenceExportBlocked, PromptQaCreationRequested }

    public sealed class ColonyQaEvidenceLimitation
    {
        public ColonyQaEvidenceLimitation(string limitationId, string description) { LimitationId = limitationId ?? string.Empty; Description = description ?? string.Empty; }
        public string LimitationId { get; }
        public string Description { get; }
    }

    public sealed class ColonyQaEvidenceRecord
    {
        public ColonyQaEvidenceRecord(string evidenceId, string sourceBee, ColonyQaEvidenceSourceType sourceType, string reference, ColonyQaEvidenceExportStatus status, string owner, string qaMeaning, ColonyQaEvidenceLimitation limitation)
        {
            EvidenceId = ColonyIntegrationIds.Require(evidenceId);
            SourceBee = sourceBee ?? string.Empty;
            SourceType = sourceType;
            Reference = reference ?? string.Empty;
            Status = status;
            Owner = owner ?? string.Empty;
            QaMeaning = qaMeaning ?? string.Empty;
            Limitation = limitation;
        }

        public string EvidenceId { get; }
        public string SourceBee { get; }
        public ColonyQaEvidenceSourceType SourceType { get; }
        public string Reference { get; }
        public ColonyQaEvidenceExportStatus Status { get; }
        public string Owner { get; }
        public string QaMeaning { get; }
        public ColonyQaEvidenceLimitation Limitation { get; }
    }

    public sealed class ColonyQaEvidenceExportPackage
    {
        public ColonyQaEvidenceExportPackage(string packageId, string beeRange, IReadOnlyList<ColonyQaEvidenceRecord> records, IReadOnlyList<ColonyQaEvidenceRecord> blockedRecords, IReadOnlyList<ColonyQaEvidenceLimitation> limitations, bool finalVerdictAllowed = false, bool promptQaCreationRequested = false)
        {
            PackageId = ColonyIntegrationIds.Require(packageId);
            BeeRange = beeRange ?? string.Empty;
            Records = records ?? Array.Empty<ColonyQaEvidenceRecord>();
            BlockedRecords = blockedRecords ?? Array.Empty<ColonyQaEvidenceRecord>();
            Limitations = limitations ?? Array.Empty<ColonyQaEvidenceLimitation>();
            FinalVerdictAllowed = finalVerdictAllowed;
            PromptQaCreationRequested = promptQaCreationRequested;
        }

        public string PackageId { get; }
        public string BeeRange { get; }
        public IReadOnlyList<ColonyQaEvidenceRecord> Records { get; }
        public IReadOnlyList<ColonyQaEvidenceRecord> BlockedRecords { get; }
        public IReadOnlyList<ColonyQaEvidenceLimitation> Limitations { get; }
        public bool FinalVerdictAllowed { get; }
        public bool PromptQaCreationRequested { get; }
        public ColonyQaEvidenceExportDiagnostics Evaluate()
        {
            IReadOnlyList<ColonyQaEvidenceRecord> all = Records.Concat(BlockedRecords).ToArray();
            var findings = new List<ColonyQaEvidenceExportDiagnosticCode>();
            if (all.Count == 0 || all.Any(r => string.IsNullOrWhiteSpace(r.SourceBee))) findings.Add(ColonyQaEvidenceExportDiagnosticCode.QaEvidenceSourceMissing);
            if (Limitations.Count == 0 || all.Any(r => r.Limitation == null || string.IsNullOrWhiteSpace(r.Limitation.Description))) findings.Add(ColonyQaEvidenceExportDiagnosticCode.QaEvidenceLimitMissing);
            if (FinalVerdictAllowed || all.Any(r => r.Status == ColonyQaEvidenceExportStatus.RejectedAsFinalVerdict)) findings.Add(ColonyQaEvidenceExportDiagnosticCode.QaEvidenceFinalVerdictClaimed);
            if (BlockedRecords.Count > 0 || all.Any(r => r.Status == ColonyQaEvidenceExportStatus.Blocked)) findings.Add(ColonyQaEvidenceExportDiagnosticCode.QaEvidenceExportBlocked);
            if (PromptQaCreationRequested) findings.Add(ColonyQaEvidenceExportDiagnosticCode.PromptQaCreationRequested);
            return new ColonyQaEvidenceExportDiagnostics(findings);
        }
    }

    public sealed class ColonyQaEvidenceExportDiagnostics
    {
        public ColonyQaEvidenceExportDiagnostics(IReadOnlyList<ColonyQaEvidenceExportDiagnosticCode> findings) { Findings = findings ?? Array.Empty<ColonyQaEvidenceExportDiagnosticCode>(); }
        public IReadOnlyList<ColonyQaEvidenceExportDiagnosticCode> Findings { get; }
        public bool Contains(ColonyQaEvidenceExportDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyServerEscalationVerdict { NoServerImpact, ResolvedLocally, NeedsServerAnalysis, FutureServerSpecCandidate, BlockedByMissingEvidence, InvalidServerImplementationRequest }
    public enum ServerEscalationReviewDiagnosticCode { ServerEscalationReviewMissing, ServerVerdictWithoutEvidence, ServerSpecPremature, EndpointCreationRequested, SqlCreationRequested, ServerProgressOutOfDate }

    public sealed class ColonyServerFutureSpecCandidate
    {
        public ColonyServerFutureSpecCandidate(string candidateId, string hint, bool premature = false) { CandidateId = candidateId ?? string.Empty; Hint = hint ?? string.Empty; Premature = premature; }
        public string CandidateId { get; }
        public string Hint { get; }
        public bool Premature { get; }
    }

    public sealed class ColonyServerEscalationReviewItem
    {
        public ColonyServerEscalationReviewItem(string escalationId, string sourceBee, string observedNeed, string evidence, ColonyServerEscalationVerdict verdict, string owner, ColonyServerFutureSpecCandidate futureServerSpecHint, bool endpointCreationRequested = false, bool sqlCreationRequested = false)
        {
            EscalationId = escalationId ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            ObservedNeed = observedNeed ?? string.Empty;
            Evidence = evidence ?? string.Empty;
            Verdict = verdict;
            Owner = owner ?? string.Empty;
            FutureServerSpecHint = futureServerSpecHint;
            EndpointCreationRequested = endpointCreationRequested;
            SqlCreationRequested = sqlCreationRequested;
        }

        public string EscalationId { get; }
        public string SourceBee { get; }
        public string ObservedNeed { get; }
        public string Evidence { get; }
        public ColonyServerEscalationVerdict Verdict { get; }
        public string Owner { get; }
        public ColonyServerFutureSpecCandidate FutureServerSpecHint { get; }
        public bool EndpointCreationRequested { get; }
        public bool SqlCreationRequested { get; }
    }

    public sealed class ColonyServerReviewExport
    {
        public ColonyServerReviewExport(string reviewGateId, string finalSummary, string beeServerNextAction) { ReviewGateId = reviewGateId ?? string.Empty; FinalSummary = finalSummary ?? string.Empty; BeeServerNextAction = beeServerNextAction ?? string.Empty; }
        public string ReviewGateId { get; }
        public string FinalSummary { get; }
        public string BeeServerNextAction { get; }
    }

    public sealed class ColonyServerEscalationReviewGate
    {
        public ColonyServerEscalationReviewGate(string reviewGateId, ColonyServerEscalationQueue sourceQueue, IReadOnlyList<ColonyServerEscalationReviewItem> reviewItems, string finalSummary, string beeServerNextAction, bool serverProgressOutOfDate = false)
        {
            ReviewGateId = ColonyIntegrationIds.Require(reviewGateId);
            SourceQueue = sourceQueue;
            ReviewItems = reviewItems ?? Array.Empty<ColonyServerEscalationReviewItem>();
            FinalSummary = finalSummary ?? string.Empty;
            BeeServerNextAction = beeServerNextAction ?? string.Empty;
            ServerProgressOutOfDate = serverProgressOutOfDate;
        }

        public string ReviewGateId { get; }
        public ColonyServerEscalationQueue SourceQueue { get; }
        public IReadOnlyList<ColonyServerEscalationReviewItem> ReviewItems { get; }
        public string FinalSummary { get; }
        public string BeeServerNextAction { get; }
        public bool ServerProgressOutOfDate { get; }
        public ServerEscalationReviewDiagnostics Evaluate()
        {
            var findings = new List<ServerEscalationReviewDiagnosticCode>();
            if (SourceQueue == null || ReviewItems.Count == 0) findings.Add(ServerEscalationReviewDiagnosticCode.ServerEscalationReviewMissing);
            if (ReviewItems.Any(i => string.IsNullOrWhiteSpace(i.Evidence))) findings.Add(ServerEscalationReviewDiagnosticCode.ServerVerdictWithoutEvidence);
            if (ReviewItems.Any(i => i.FutureServerSpecHint != null && i.FutureServerSpecHint.Premature)) findings.Add(ServerEscalationReviewDiagnosticCode.ServerSpecPremature);
            if (ReviewItems.Any(i => i.EndpointCreationRequested || i.Verdict == ColonyServerEscalationVerdict.InvalidServerImplementationRequest)) findings.Add(ServerEscalationReviewDiagnosticCode.EndpointCreationRequested);
            if (ReviewItems.Any(i => i.SqlCreationRequested)) findings.Add(ServerEscalationReviewDiagnosticCode.SqlCreationRequested);
            if (ServerProgressOutOfDate) findings.Add(ServerEscalationReviewDiagnosticCode.ServerProgressOutOfDate);
            return new ServerEscalationReviewDiagnostics(findings, new ColonyServerReviewExport(ReviewGateId, FinalSummary, BeeServerNextAction));
        }
    }

    public sealed class ServerEscalationReviewDiagnostics
    {
        public ServerEscalationReviewDiagnostics(IReadOnlyList<ServerEscalationReviewDiagnosticCode> findings, ColonyServerReviewExport export) { Findings = findings ?? Array.Empty<ServerEscalationReviewDiagnosticCode>(); Export = export; }
        public IReadOnlyList<ServerEscalationReviewDiagnosticCode> Findings { get; }
        public ColonyServerReviewExport Export { get; }
        public bool Contains(ServerEscalationReviewDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyRegressionRunbookStatus { Runnable, Blocked, NeedsFixture, NeedsOwner, OutOfScope, FinalSuiteClaimBlocked }
    public enum RegressionRunbookDiagnosticCode { RegressionRunStepMissing, RegressionSeedMissing, RegressionFixtureMissing, RegressionOwnerMissing, RegressionFinalSuiteClaimed, RegressionOrderAmbiguous }

    public sealed class ColonyRegressionRunSeed
    {
        public ColonyRegressionRunSeed(int? seed, string source) { Seed = seed; Source = source ?? string.Empty; }
        public int? Seed { get; }
        public string Source { get; }
    }

    public sealed class ColonyRegressionExpectedProvisionalResult
    {
        public ColonyRegressionExpectedProvisionalResult(string expectedSignal, string limitation) { ExpectedSignal = expectedSignal ?? string.Empty; Limitation = limitation ?? string.Empty; }
        public string ExpectedSignal { get; }
        public string Limitation { get; }
    }

    public sealed class ColonyRegressionRunStep
    {
        public ColonyRegressionRunStep(string stepId, string scenarioId, int order, ColonyRegressionRunSeed seed, string owner, string surface, ColonyRegressionExpectedProvisionalResult expectedProvisionalResult, ColonyRegressionRunbookStatus status, bool fixtureMissing = false)
        {
            StepId = ColonyIntegrationIds.Require(stepId);
            ScenarioId = scenarioId ?? string.Empty;
            Order = order;
            Seed = seed;
            Owner = owner ?? string.Empty;
            Surface = surface ?? string.Empty;
            ExpectedProvisionalResult = expectedProvisionalResult;
            Status = status;
            FixtureMissing = fixtureMissing;
        }

        public string StepId { get; }
        public string ScenarioId { get; }
        public int Order { get; }
        public ColonyRegressionRunSeed Seed { get; }
        public string Owner { get; }
        public string Surface { get; }
        public ColonyRegressionExpectedProvisionalResult ExpectedProvisionalResult { get; }
        public ColonyRegressionRunbookStatus Status { get; }
        public bool FixtureMissing { get; }
    }

    public sealed class ColonyRegressionRunbookExport
    {
        public ColonyRegressionRunbookExport(string runbookId, int stepCount) { RunbookId = runbookId ?? string.Empty; StepCount = stepCount; }
        public string RunbookId { get; }
        public int StepCount { get; }
    }

    public class ColonyIntegrationRegressionRunbook
    {
        public ColonyIntegrationRegressionRunbook(string runbookId, string scenarioPack, IReadOnlyList<ColonyRegressionRunStep> steps, string limitations, bool finalSuiteAllowed = false)
        {
            RunbookId = ColonyIntegrationIds.Require(runbookId);
            ScenarioPack = scenarioPack ?? string.Empty;
            Steps = steps ?? Array.Empty<ColonyRegressionRunStep>();
            Limitations = limitations ?? string.Empty;
            FinalSuiteAllowed = finalSuiteAllowed;
        }

        public string RunbookId { get; }
        public string ScenarioPack { get; }
        public IReadOnlyList<ColonyRegressionRunStep> Steps { get; }
        public string Limitations { get; }
        public bool FinalSuiteAllowed { get; }
        public RegressionRunbookDiagnostics Evaluate()
        {
            var findings = new List<RegressionRunbookDiagnosticCode>();
            if (Steps.Count == 0) findings.Add(RegressionRunbookDiagnosticCode.RegressionRunStepMissing);
            if (Steps.Any(s => s.Seed == null || s.Seed.Seed == null)) findings.Add(RegressionRunbookDiagnosticCode.RegressionSeedMissing);
            if (Steps.Any(s => s.FixtureMissing)) findings.Add(RegressionRunbookDiagnosticCode.RegressionFixtureMissing);
            if (Steps.Any(s => string.IsNullOrWhiteSpace(s.Owner))) findings.Add(RegressionRunbookDiagnosticCode.RegressionOwnerMissing);
            if (FinalSuiteAllowed || Steps.Any(s => s.Status == ColonyRegressionRunbookStatus.FinalSuiteClaimBlocked)) findings.Add(RegressionRunbookDiagnosticCode.RegressionFinalSuiteClaimed);
            if (Steps.GroupBy(s => s.Order).Any(g => g.Count() > 1) || Steps.Any(s => s.Order <= 0)) findings.Add(RegressionRunbookDiagnosticCode.RegressionOrderAmbiguous);
            return new RegressionRunbookDiagnostics(findings, new ColonyRegressionRunbookExport(RunbookId, Steps.Count));
        }
    }

    public sealed class RegressionRunbookDiagnostics
    {
        public RegressionRunbookDiagnostics(IReadOnlyList<RegressionRunbookDiagnosticCode> findings, ColonyRegressionRunbookExport export) { Findings = findings ?? Array.Empty<RegressionRunbookDiagnosticCode>(); Export = export; }
        public IReadOnlyList<RegressionRunbookDiagnosticCode> Findings { get; }
        public ColonyRegressionRunbookExport Export { get; }
        public bool Contains(RegressionRunbookDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyDemoAcceptanceStatus { CaptureReady, CaptureIncomplete, CaptureBlocked, SurfaceMissing, VisualLimitHidden, QaFinalClaimBlocked }
    public enum ColonyDemoAcceptanceDiagnosticCode { DemoAcceptanceSurfaceMissing, DemoAcceptanceCaptureMissing, DemoAcceptanceBlockerHidden, DemoAcceptanceQaClaimed, DemoAcceptanceLimitMissing }

    public sealed class ColonyDemoAcceptanceSurface
    {
        public ColonyDemoAcceptanceSurface(string surfaceId, string demoId, ColonyDemoAcceptanceStatus status) { SurfaceId = surfaceId ?? string.Empty; DemoId = demoId ?? string.Empty; Status = status; }
        public string SurfaceId { get; }
        public string DemoId { get; }
        public ColonyDemoAcceptanceStatus Status { get; }
    }

    public sealed class ColonyDemoAcceptanceBlocker
    {
        public ColonyDemoAcceptanceBlocker(string blockerId, string owner, string reason, bool hidden = false) { BlockerId = blockerId ?? string.Empty; Owner = owner ?? string.Empty; Reason = reason ?? string.Empty; Hidden = hidden; }
        public string BlockerId { get; }
        public string Owner { get; }
        public string Reason { get; }
        public bool Hidden { get; }
    }

    public sealed class ColonyDemoAcceptanceCaptureRequirement
    {
        public ColonyDemoAcceptanceCaptureRequirement(string captureId, string demoId, string visibleProof, string sourceBee, string expectedState, ColonyDemoAcceptanceBlocker blocker, string limitation)
        {
            CaptureId = ColonyIntegrationIds.Require(captureId);
            DemoId = demoId ?? string.Empty;
            VisibleProof = visibleProof ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            ExpectedState = expectedState ?? string.Empty;
            Blocker = blocker;
            Limitation = limitation ?? string.Empty;
        }

        public string CaptureId { get; }
        public string DemoId { get; }
        public string VisibleProof { get; }
        public string SourceBee { get; }
        public string ExpectedState { get; }
        public ColonyDemoAcceptanceBlocker Blocker { get; }
        public string Limitation { get; }
    }

    public sealed class ColonyDemoAcceptanceExport
    {
        public ColonyDemoAcceptanceExport(string snapshotId, ColonyDemoAcceptanceStatus exportStatus) { SnapshotId = snapshotId ?? string.Empty; ExportStatus = exportStatus; }
        public string SnapshotId { get; }
        public ColonyDemoAcceptanceStatus ExportStatus { get; }
    }

    public sealed class ColonyDemoAcceptanceSnapshot
    {
        public ColonyDemoAcceptanceSnapshot(string snapshotId, IReadOnlyList<ColonyDemoAcceptanceSurface> surfaces, IReadOnlyList<ColonyDemoAcceptanceCaptureRequirement> captureRequirements, IReadOnlyList<ColonyDemoAcceptanceBlocker> blockers, IReadOnlyList<string> limitations, ColonyDemoAcceptanceStatus exportStatus, bool qaFinalClaimed = false)
        {
            SnapshotId = ColonyIntegrationIds.Require(snapshotId);
            Surfaces = surfaces ?? Array.Empty<ColonyDemoAcceptanceSurface>();
            CaptureRequirements = captureRequirements ?? Array.Empty<ColonyDemoAcceptanceCaptureRequirement>();
            Blockers = blockers ?? Array.Empty<ColonyDemoAcceptanceBlocker>();
            Limitations = limitations ?? Array.Empty<string>();
            ExportStatus = exportStatus;
            QaFinalClaimed = qaFinalClaimed;
        }

        public string SnapshotId { get; }
        public IReadOnlyList<ColonyDemoAcceptanceSurface> Surfaces { get; }
        public IReadOnlyList<ColonyDemoAcceptanceCaptureRequirement> CaptureRequirements { get; }
        public IReadOnlyList<ColonyDemoAcceptanceBlocker> Blockers { get; }
        public IReadOnlyList<string> Limitations { get; }
        public ColonyDemoAcceptanceStatus ExportStatus { get; }
        public bool QaFinalClaimed { get; }
        public ColonyDemoAcceptanceDiagnostics Evaluate()
        {
            var findings = new List<ColonyDemoAcceptanceDiagnosticCode>();
            if (Surfaces.Count == 0 || Surfaces.Any(s => s.Status == ColonyDemoAcceptanceStatus.SurfaceMissing)) findings.Add(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceSurfaceMissing);
            if (CaptureRequirements.Count == 0 || CaptureRequirements.Any(c => string.IsNullOrWhiteSpace(c.VisibleProof))) findings.Add(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceCaptureMissing);
            if (Blockers.Any(b => b.Hidden) || CaptureRequirements.Any(c => c.Blocker != null && c.Blocker.Hidden)) findings.Add(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceBlockerHidden);
            if (QaFinalClaimed || ExportStatus == ColonyDemoAcceptanceStatus.QaFinalClaimBlocked) findings.Add(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceQaClaimed);
            if (Limitations.Count == 0 || CaptureRequirements.Any(c => string.IsNullOrWhiteSpace(c.Limitation)) || ExportStatus == ColonyDemoAcceptanceStatus.VisualLimitHidden) findings.Add(ColonyDemoAcceptanceDiagnosticCode.DemoAcceptanceLimitMissing);
            return new ColonyDemoAcceptanceDiagnostics(findings, new ColonyDemoAcceptanceExport(SnapshotId, ExportStatus));
        }
    }

    public sealed class ColonyDemoAcceptanceDiagnostics
    {
        public ColonyDemoAcceptanceDiagnostics(IReadOnlyList<ColonyDemoAcceptanceDiagnosticCode> findings, ColonyDemoAcceptanceExport export) { Findings = findings ?? Array.Empty<ColonyDemoAcceptanceDiagnosticCode>(); Export = export; }
        public IReadOnlyList<ColonyDemoAcceptanceDiagnosticCode> Findings { get; }
        public ColonyDemoAcceptanceExport Export { get; }
        public bool Contains(ColonyDemoAcceptanceDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyAlphaProjectionStatus { ProjectedOnTrack, ProjectedWithWarnings, BlockedByWorker, BlockedByQa, BlockedByServer, BlockedByDemo, BlockedByMultiplayerArmyHandoff, BlockedByPlayerInvestmentModel, NotAlphaReady }
    public enum ColonyAlphaProjectionDiagnosticCode { AlphaConditionMissing, AlphaOwnerMissing, AlphaReadyClaimBlocked, AlphaServerDependencyOpen, AlphaQaEvidenceIncomplete, AlphaDemoSnapshotIncomplete, MultiplayerArmyConditionMissing, PlayerInvestmentModelMissing, ServerAuthorityForConflictMissing, PayToWinRiskUnclassified }

    public sealed class ColonyAlphaCondition
    {
        public ColonyAlphaCondition(string conditionId, string domain, string sourceBee, string owner, ColonyAlphaProjectionStatus status, string evidence, string missingReason)
        {
            ConditionId = ColonyIntegrationIds.Require(conditionId);
            Domain = domain ?? string.Empty;
            SourceBee = sourceBee ?? string.Empty;
            Owner = owner ?? string.Empty;
            Status = status;
            Evidence = evidence ?? string.Empty;
            MissingReason = missingReason ?? string.Empty;
        }

        public string ConditionId { get; }
        public string Domain { get; }
        public string SourceBee { get; }
        public string Owner { get; }
        public ColonyAlphaProjectionStatus Status { get; }
        public string Evidence { get; }
        public string MissingReason { get; }
    }

    public sealed class ColonyAlphaOpenRisk
    {
        public ColonyAlphaOpenRisk(string riskId, string owner, ColonyImplementationRiskSeverity severity, bool payToWinRiskUnclassified = false) { RiskId = riskId ?? string.Empty; Owner = owner ?? string.Empty; Severity = severity; PayToWinRiskUnclassified = payToWinRiskUnclassified; }
        public string RiskId { get; }
        public string Owner { get; }
        public ColonyImplementationRiskSeverity Severity { get; }
        public bool PayToWinRiskUnclassified { get; }
    }

    public sealed class ColonyAlphaMissingCondition
    {
        public ColonyAlphaMissingCondition(string conditionId, string owner, string reason) { ConditionId = conditionId ?? string.Empty; Owner = owner ?? string.Empty; Reason = reason ?? string.Empty; }
        public string ConditionId { get; }
        public string Owner { get; }
        public string Reason { get; }
    }

    public sealed class ColonyPlayerInvestmentProjection
    {
        public ColonyPlayerInvestmentProjection(bool timeAxis, bool moneyAxis, bool effortAxis, bool runtimeMonetizationClaimed = false)
        {
            TimeAxis = timeAxis;
            MoneyAxis = moneyAxis;
            EffortAxis = effortAxis;
            RuntimeMonetizationClaimed = runtimeMonetizationClaimed;
        }

        public bool TimeAxis { get; }
        public bool MoneyAxis { get; }
        public bool EffortAxis { get; }
        public bool RuntimeMonetizationClaimed { get; }
        public bool IsComplete => TimeAxis && MoneyAxis && EffortAxis && !RuntimeMonetizationClaimed;
    }

    public sealed class ColonyMultiplayerArmyTransitionCondition
    {
        public ColonyMultiplayerArmyTransitionCondition(string conditionId, string targetBeeRange, IReadOnlyList<string> playerProfileCoverage, bool armyTrainingReadiness, ColonyPlayerInvestmentProjection investmentModelStatus, string serverAuthorityRequirement, ColonyAlphaOpenRisk risk)
        {
            ConditionId = ColonyIntegrationIds.Require(conditionId);
            TargetBeeRange = targetBeeRange ?? string.Empty;
            PlayerProfileCoverage = playerProfileCoverage ?? Array.Empty<string>();
            ArmyTrainingReadiness = armyTrainingReadiness;
            InvestmentModelStatus = investmentModelStatus;
            ServerAuthorityRequirement = serverAuthorityRequirement ?? string.Empty;
            Risk = risk;
        }

        public string ConditionId { get; }
        public string TargetBeeRange { get; }
        public IReadOnlyList<string> PlayerProfileCoverage { get; }
        public bool ArmyTrainingReadiness { get; }
        public ColonyPlayerInvestmentProjection InvestmentModelStatus { get; }
        public string ServerAuthorityRequirement { get; }
        public ColonyAlphaOpenRisk Risk { get; }
        public bool IsComplete => PlayerProfileCoverage.Count >= 4 && ArmyTrainingReadiness && InvestmentModelStatus != null && InvestmentModelStatus.IsComplete && !string.IsNullOrWhiteSpace(ServerAuthorityRequirement);
    }

    public sealed class ColonyAlphaProjectionExport
    {
        public ColonyAlphaProjectionExport(string projectionId, ColonyAlphaProjectionStatus projectionStatus, string limitation) { ProjectionId = projectionId ?? string.Empty; ProjectionStatus = projectionStatus; Limitation = limitation ?? string.Empty; }
        public string ProjectionId { get; }
        public ColonyAlphaProjectionStatus ProjectionStatus { get; }
        public string Limitation { get; }
    }

    public sealed class ColonyAlphaReadinessProjection
    {
        public ColonyAlphaReadinessProjection(string projectionId, string beeRange, IReadOnlyList<ColonyAlphaCondition> conditions, IReadOnlyList<ColonyAlphaOpenRisk> openRisks, IReadOnlyList<ColonyAlphaMissingCondition> missingConditions, IReadOnlyList<ColonyMultiplayerArmyTransitionCondition> transitionConditions, ColonyAlphaProjectionStatus projectionStatus, string limitations, bool alphaReadyClaimed = false, bool serverDependencyOpen = false, bool qaEvidenceIncomplete = false, bool demoSnapshotIncomplete = false)
        {
            ProjectionId = ColonyIntegrationIds.Require(projectionId);
            BeeRange = beeRange ?? string.Empty;
            Conditions = conditions ?? Array.Empty<ColonyAlphaCondition>();
            OpenRisks = openRisks ?? Array.Empty<ColonyAlphaOpenRisk>();
            MissingConditions = missingConditions ?? Array.Empty<ColonyAlphaMissingCondition>();
            TransitionConditions = transitionConditions ?? Array.Empty<ColonyMultiplayerArmyTransitionCondition>();
            ProjectionStatus = projectionStatus;
            Limitations = limitations ?? string.Empty;
            AlphaReadyClaimed = alphaReadyClaimed;
            ServerDependencyOpen = serverDependencyOpen;
            QaEvidenceIncomplete = qaEvidenceIncomplete;
            DemoSnapshotIncomplete = demoSnapshotIncomplete;
        }

        public string ProjectionId { get; }
        public string BeeRange { get; }
        public IReadOnlyList<ColonyAlphaCondition> Conditions { get; }
        public IReadOnlyList<ColonyAlphaOpenRisk> OpenRisks { get; }
        public IReadOnlyList<ColonyAlphaMissingCondition> MissingConditions { get; }
        public IReadOnlyList<ColonyMultiplayerArmyTransitionCondition> TransitionConditions { get; }
        public ColonyAlphaProjectionStatus ProjectionStatus { get; }
        public string Limitations { get; }
        public bool AlphaReadyClaimed { get; }
        public bool ServerDependencyOpen { get; }
        public bool QaEvidenceIncomplete { get; }
        public bool DemoSnapshotIncomplete { get; }
        public ColonyAlphaReadinessDiagnostics Evaluate()
        {
            var findings = new List<ColonyAlphaProjectionDiagnosticCode>();
            if (Conditions.Count == 0 || MissingConditions.Count > 0) findings.Add(ColonyAlphaProjectionDiagnosticCode.AlphaConditionMissing);
            if (Conditions.Any(c => string.IsNullOrWhiteSpace(c.Owner)) || MissingConditions.Any(c => string.IsNullOrWhiteSpace(c.Owner))) findings.Add(ColonyAlphaProjectionDiagnosticCode.AlphaOwnerMissing);
            if (AlphaReadyClaimed) findings.Add(ColonyAlphaProjectionDiagnosticCode.AlphaReadyClaimBlocked);
            if (ServerDependencyOpen) findings.Add(ColonyAlphaProjectionDiagnosticCode.AlphaServerDependencyOpen);
            if (QaEvidenceIncomplete) findings.Add(ColonyAlphaProjectionDiagnosticCode.AlphaQaEvidenceIncomplete);
            if (DemoSnapshotIncomplete) findings.Add(ColonyAlphaProjectionDiagnosticCode.AlphaDemoSnapshotIncomplete);
            if (TransitionConditions.Count == 0 || TransitionConditions.Any(t => !t.IsComplete)) findings.Add(ColonyAlphaProjectionDiagnosticCode.MultiplayerArmyConditionMissing);
            if (TransitionConditions.Any(t => t.InvestmentModelStatus == null || !t.InvestmentModelStatus.IsComplete)) findings.Add(ColonyAlphaProjectionDiagnosticCode.PlayerInvestmentModelMissing);
            if (TransitionConditions.Any(t => string.IsNullOrWhiteSpace(t.ServerAuthorityRequirement))) findings.Add(ColonyAlphaProjectionDiagnosticCode.ServerAuthorityForConflictMissing);
            if (OpenRisks.Any(r => r.PayToWinRiskUnclassified) || TransitionConditions.Any(t => t.Risk != null && t.Risk.PayToWinRiskUnclassified)) findings.Add(ColonyAlphaProjectionDiagnosticCode.PayToWinRiskUnclassified);
            return new ColonyAlphaReadinessDiagnostics(findings, new ColonyAlphaProjectionExport(ProjectionId, ProjectionStatus, Limitations));
        }
    }

    public sealed class ColonyAlphaReadinessDiagnostics
    {
        public ColonyAlphaReadinessDiagnostics(IReadOnlyList<ColonyAlphaProjectionDiagnosticCode> findings, ColonyAlphaProjectionExport export) { Findings = findings ?? Array.Empty<ColonyAlphaProjectionDiagnosticCode>(); Export = export; }
        public IReadOnlyList<ColonyAlphaProjectionDiagnosticCode> Findings { get; }
        public ColonyAlphaProjectionExport Export { get; }
        public bool Contains(ColonyAlphaProjectionDiagnosticCode code) { return Findings.Contains(code); }
    }

    public enum ColonyIntegrationClosureVerdictType { ReadyForArchitectValidation, ReadyWithWarningsForArchitectValidation, NeedsPlannerRevision, BlockedByWorkerGap, BlockedByQaEvidence, BlockedByServerEscalation, BlockedByDemoSnapshot, BlockedByAlphaProjection, BlockedByArch026Handoff, BlockedByBee301Premature }
    public enum ColonyClosureGapSeverity { Low, Medium, High, Critical }
    public enum ColonyIntegrationClosureDiagnosticCode { ClosureInputMissing, ClosureEvidenceMissing, ClosureGapOwnerMissing, ClosureAlphaReadyClaimed, ClosureQaFinalClaimed, ClosureServerReadyClaimed, Arch026HandoffMissing, MultiplayerArmyHandoffMissing, ServerAuthorityForPvpMissing, Bee301Premature }

    public sealed class ColonyIntegrationClosureInputSet
    {
        public ColonyIntegrationClosureInputSet(string launchChecklist, string sliceVerification, string adapterSmokeValidation, string readModelDemoConsistency, string qaEvidenceExport, string serverEscalationReview, string regressionRunbook, string demoAcceptanceSnapshot, string alphaReadinessProjection)
        {
            LaunchChecklist = launchChecklist ?? string.Empty;
            SliceVerification = sliceVerification ?? string.Empty;
            AdapterSmokeValidation = adapterSmokeValidation ?? string.Empty;
            ReadModelDemoConsistency = readModelDemoConsistency ?? string.Empty;
            QaEvidenceExport = qaEvidenceExport ?? string.Empty;
            ServerEscalationReview = serverEscalationReview ?? string.Empty;
            RegressionRunbook = regressionRunbook ?? string.Empty;
            DemoAcceptanceSnapshot = demoAcceptanceSnapshot ?? string.Empty;
            AlphaReadinessProjection = alphaReadinessProjection ?? string.Empty;
        }

        public string LaunchChecklist { get; }
        public string SliceVerification { get; }
        public string AdapterSmokeValidation { get; }
        public string ReadModelDemoConsistency { get; }
        public string QaEvidenceExport { get; }
        public string ServerEscalationReview { get; }
        public string RegressionRunbook { get; }
        public string DemoAcceptanceSnapshot { get; }
        public string AlphaReadinessProjection { get; }
        public bool HasMissingInput => string.IsNullOrWhiteSpace(LaunchChecklist)
            || string.IsNullOrWhiteSpace(SliceVerification)
            || string.IsNullOrWhiteSpace(AdapterSmokeValidation)
            || string.IsNullOrWhiteSpace(ReadModelDemoConsistency)
            || string.IsNullOrWhiteSpace(QaEvidenceExport)
            || string.IsNullOrWhiteSpace(ServerEscalationReview)
            || string.IsNullOrWhiteSpace(RegressionRunbook)
            || string.IsNullOrWhiteSpace(DemoAcceptanceSnapshot)
            || string.IsNullOrWhiteSpace(AlphaReadinessProjection);
    }

    public sealed class ColonyIntegrationClosureGap
    {
        public ColonyIntegrationClosureGap(string gapId, string sourceBee, string owner, ColonyClosureGapSeverity severity, string requiredAction)
        {
            GapId = ColonyIntegrationIds.Require(gapId);
            SourceBee = sourceBee ?? string.Empty;
            Owner = owner ?? string.Empty;
            Severity = severity;
            RequiredAction = requiredAction ?? string.Empty;
        }

        public string GapId { get; }
        public string SourceBee { get; }
        public string Owner { get; }
        public ColonyClosureGapSeverity Severity { get; }
        public string RequiredAction { get; }
    }

    public sealed class ColonyMultiplayerArmyHandoffSummary
    {
        public ColonyMultiplayerArmyHandoffSummary(string handoffId, string archReference, string targetBeeRange, IReadOnlyList<string> playerProfiles, IReadOnlyList<string> armyTrainingDomains, IReadOnlyList<string> playerInvestmentAxes, IReadOnlyList<string> serverAuthorityTopics, IReadOnlyList<string> demoProofTopics, string bee301Status)
        {
            HandoffId = ColonyIntegrationIds.Require(handoffId);
            ArchReference = archReference ?? string.Empty;
            TargetBeeRange = targetBeeRange ?? string.Empty;
            PlayerProfiles = playerProfiles ?? Array.Empty<string>();
            ArmyTrainingDomains = armyTrainingDomains ?? Array.Empty<string>();
            PlayerInvestmentAxes = playerInvestmentAxes ?? Array.Empty<string>();
            ServerAuthorityTopics = serverAuthorityTopics ?? Array.Empty<string>();
            DemoProofTopics = demoProofTopics ?? Array.Empty<string>();
            Bee301Status = bee301Status ?? string.Empty;
        }

        public string HandoffId { get; }
        public string ArchReference { get; }
        public string TargetBeeRange { get; }
        public IReadOnlyList<string> PlayerProfiles { get; }
        public IReadOnlyList<string> ArmyTrainingDomains { get; }
        public IReadOnlyList<string> PlayerInvestmentAxes { get; }
        public IReadOnlyList<string> ServerAuthorityTopics { get; }
        public IReadOnlyList<string> DemoProofTopics { get; }
        public string Bee301Status { get; }
        public bool HasArch026Coverage => string.Equals(ArchReference, "ARCH-026", StringComparison.Ordinal)
            && PlayerProfiles.Count >= 4
            && ArmyTrainingDomains.Count > 0
            && PlayerInvestmentAxes.Count >= 3
            && ServerAuthorityTopics.Count > 0
            && DemoProofTopics.Count > 0
            && !string.IsNullOrWhiteSpace(Bee301Status);
        public bool HasPvpServerAuthority => ServerAuthorityTopics.Any(t => t.IndexOf("combat", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("pvp", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("conflit", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public sealed class ColonyIntegrationClosureVerdict
    {
        public ColonyIntegrationClosureVerdict(ColonyIntegrationClosureVerdictType verdictType, string evidenceSummary, string limitation)
        {
            VerdictType = verdictType;
            EvidenceSummary = evidenceSummary ?? string.Empty;
            Limitation = limitation ?? string.Empty;
        }

        public ColonyIntegrationClosureVerdictType VerdictType { get; }
        public string EvidenceSummary { get; }
        public string Limitation { get; }
    }

    public sealed class ColonyIntegrationClosureExport
    {
        public ColonyIntegrationClosureExport(string gateId, ColonyIntegrationClosureVerdictType verdictType, string bee301Status)
        {
            GateId = gateId ?? string.Empty;
            VerdictType = verdictType;
            Bee301Status = bee301Status ?? string.Empty;
        }

        public string GateId { get; }
        public ColonyIntegrationClosureVerdictType VerdictType { get; }
        public string Bee301Status { get; }
    }

    public sealed class ColonyIntegrationClosureGate
    {
        public const string Bee301BlockedStatus = "BEE-301 bloquee jusqu'a validation architecte.";

        public ColonyIntegrationClosureGate(string gateId, string beeRange, ColonyIntegrationClosureInputSet inputSet, IReadOnlyList<ColonyIntegrationClosureGap> gaps, string evidenceSummary, ColonyMultiplayerArmyHandoffSummary multiplayerArmyHandoff, bool alphaReadyClaimed = false, bool qaFinalClaimed = false, bool serverReadyClaimed = false, bool bee301PrematureAttempt = false)
        {
            GateId = ColonyIntegrationIds.Require(gateId);
            BeeRange = beeRange ?? string.Empty;
            InputSet = inputSet;
            Gaps = gaps ?? Array.Empty<ColonyIntegrationClosureGap>();
            EvidenceSummary = evidenceSummary ?? string.Empty;
            MultiplayerArmyHandoff = multiplayerArmyHandoff;
            AlphaReadyClaimed = alphaReadyClaimed;
            QaFinalClaimed = qaFinalClaimed;
            ServerReadyClaimed = serverReadyClaimed;
            Bee301PrematureAttempt = bee301PrematureAttempt;
        }

        public string GateId { get; }
        public string BeeRange { get; }
        public ColonyIntegrationClosureInputSet InputSet { get; }
        public IReadOnlyList<ColonyIntegrationClosureGap> Gaps { get; }
        public string EvidenceSummary { get; }
        public ColonyMultiplayerArmyHandoffSummary MultiplayerArmyHandoff { get; }
        public bool AlphaReadyClaimed { get; }
        public bool QaFinalClaimed { get; }
        public bool ServerReadyClaimed { get; }
        public bool Bee301PrematureAttempt { get; }
        public string Bee301Status => Bee301BlockedStatus;

        public ColonyIntegrationClosureDiagnostics Evaluate()
        {
            var findings = new List<ColonyIntegrationClosureDiagnosticCode>();
            if (InputSet == null || InputSet.HasMissingInput) findings.Add(ColonyIntegrationClosureDiagnosticCode.ClosureInputMissing);
            if (string.IsNullOrWhiteSpace(EvidenceSummary)) findings.Add(ColonyIntegrationClosureDiagnosticCode.ClosureEvidenceMissing);
            if (Gaps.Any(g => string.IsNullOrWhiteSpace(g.Owner))) findings.Add(ColonyIntegrationClosureDiagnosticCode.ClosureGapOwnerMissing);
            if (AlphaReadyClaimed) findings.Add(ColonyIntegrationClosureDiagnosticCode.ClosureAlphaReadyClaimed);
            if (QaFinalClaimed) findings.Add(ColonyIntegrationClosureDiagnosticCode.ClosureQaFinalClaimed);
            if (ServerReadyClaimed) findings.Add(ColonyIntegrationClosureDiagnosticCode.ClosureServerReadyClaimed);
            if (MultiplayerArmyHandoff == null || !MultiplayerArmyHandoff.HasArch026Coverage) findings.Add(ColonyIntegrationClosureDiagnosticCode.Arch026HandoffMissing);
            if (MultiplayerArmyHandoff == null || MultiplayerArmyHandoff.ArmyTrainingDomains.Count == 0) findings.Add(ColonyIntegrationClosureDiagnosticCode.MultiplayerArmyHandoffMissing);
            if (MultiplayerArmyHandoff == null || !MultiplayerArmyHandoff.HasPvpServerAuthority) findings.Add(ColonyIntegrationClosureDiagnosticCode.ServerAuthorityForPvpMissing);
            if (Bee301PrematureAttempt) findings.Add(ColonyIntegrationClosureDiagnosticCode.Bee301Premature);

            ColonyIntegrationClosureVerdictType verdictType = BuildVerdict(findings);
            var verdict = new ColonyIntegrationClosureVerdict(verdictType, EvidenceSummary, "Closure is not Alpha ready, QA final, server ready or multiplayer runtime.");
            var export = new ColonyIntegrationClosureExport(GateId, verdictType, Bee301BlockedStatus);
            return new ColonyIntegrationClosureDiagnostics(verdict, findings, export);
        }

        private static ColonyIntegrationClosureVerdictType BuildVerdict(IReadOnlyList<ColonyIntegrationClosureDiagnosticCode> findings)
        {
            if (findings.Contains(ColonyIntegrationClosureDiagnosticCode.Bee301Premature)) return ColonyIntegrationClosureVerdictType.BlockedByBee301Premature;
            if (findings.Contains(ColonyIntegrationClosureDiagnosticCode.Arch026HandoffMissing) || findings.Contains(ColonyIntegrationClosureDiagnosticCode.MultiplayerArmyHandoffMissing) || findings.Contains(ColonyIntegrationClosureDiagnosticCode.ServerAuthorityForPvpMissing)) return ColonyIntegrationClosureVerdictType.BlockedByArch026Handoff;
            if (findings.Contains(ColonyIntegrationClosureDiagnosticCode.ClosureServerReadyClaimed)) return ColonyIntegrationClosureVerdictType.BlockedByServerEscalation;
            if (findings.Contains(ColonyIntegrationClosureDiagnosticCode.ClosureQaFinalClaimed)) return ColonyIntegrationClosureVerdictType.BlockedByQaEvidence;
            if (findings.Contains(ColonyIntegrationClosureDiagnosticCode.ClosureAlphaReadyClaimed)) return ColonyIntegrationClosureVerdictType.BlockedByAlphaProjection;
            if (findings.Contains(ColonyIntegrationClosureDiagnosticCode.ClosureInputMissing) || findings.Contains(ColonyIntegrationClosureDiagnosticCode.ClosureEvidenceMissing)) return ColonyIntegrationClosureVerdictType.NeedsPlannerRevision;
            if (findings.Count > 0) return ColonyIntegrationClosureVerdictType.ReadyWithWarningsForArchitectValidation;
            return ColonyIntegrationClosureVerdictType.ReadyForArchitectValidation;
        }
    }

    public sealed class ColonyIntegrationClosureDiagnostics
    {
        public ColonyIntegrationClosureDiagnostics(ColonyIntegrationClosureVerdict verdict, IReadOnlyList<ColonyIntegrationClosureDiagnosticCode> findings, ColonyIntegrationClosureExport export)
        {
            Verdict = verdict;
            Findings = findings ?? Array.Empty<ColonyIntegrationClosureDiagnosticCode>();
            Export = export;
        }

        public ColonyIntegrationClosureVerdict Verdict { get; }
        public IReadOnlyList<ColonyIntegrationClosureDiagnosticCode> Findings { get; }
        public ColonyIntegrationClosureExport Export { get; }
        public bool Contains(ColonyIntegrationClosureDiagnosticCode code) { return Findings.Contains(code); }
    }
}
