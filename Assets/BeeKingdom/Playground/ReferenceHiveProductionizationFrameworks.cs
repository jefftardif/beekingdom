using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public enum ReferenceHiveProductionizationVerdict
    {
        Pass,
        PassWithReserves,
        ReworkRequired
    }

    public class ReferenceBackedHiveProductionizationPlan
    {
        public ReferenceBackedHiveProductionizationPlan(string primaryContract, string runtimeSurface, string visualState, string mobileRule, string evidenceRule, string nonClaimRule)
        {
            PrimaryContract = Require(primaryContract);
            RuntimeSurface = Require(runtimeSurface);
            VisualState = Require(visualState);
            MobileRule = Require(mobileRule);
            EvidenceRule = Require(evidenceRule);
            NonClaimRule = Require(nonClaimRule);
        }

        public string PrimaryContract { get; }
        public string RuntimeSurface { get; }
        public string VisualState { get; }
        public string MobileRule { get; }
        public string EvidenceRule { get; }
        public string NonClaimRule { get; }

        private static string Require(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A productionization field is required.");
            return value;
        }
    }

    public sealed class ReferenceHiveClickableHotspotMap
    {
        public ReferenceHiveClickableHotspotMap(IReadOnlyList<ReferenceHiveHotspotDefinition> hotspots, string runtimeSurface, string mobileRule, string evidenceRule, string nonClaimRule)
        {
            Hotspots = hotspots ?? Array.Empty<ReferenceHiveHotspotDefinition>();
            RuntimeSurface = runtimeSurface ?? string.Empty;
            MobileRule = mobileRule ?? string.Empty;
            EvidenceRule = evidenceRule ?? string.Empty;
            NonClaimRule = nonClaimRule ?? string.Empty;
            PrimaryContract = "BEE-622 polygon hotspot map";
            VisualState = Hotspots.Count >= 14 && Hotspots.All(h => h.Polygon.Count >= 4) ? "14 polygon zones ready" : "hotspot map incomplete";
        }

        public string PrimaryContract { get; }
        public string RuntimeSurface { get; }
        public string VisualState { get; }
        public string MobileRule { get; }
        public string EvidenceRule { get; }
        public string NonClaimRule { get; }
        public IReadOnlyList<ReferenceHiveHotspotDefinition> Hotspots { get; }

        public ReferenceHiveHotspotDefinition HitTest(Vector2 artPoint)
        {
            return Hotspots
                .OrderByDescending(h => h.Priority)
                .FirstOrDefault(h => h.Contains(artPoint));
        }
    }

    public sealed class ReferenceHiveHotspotDefinition
    {
        public ReferenceHiveHotspotDefinition(string hotspotId, string cellId, int zoneNumber, string label, string role, string iconId, string visualState, IReadOnlyList<Vector2> polygon, Vector2 tokenAnchor, int priority)
        {
            HotspotId = Require(hotspotId);
            CellId = Require(cellId);
            ZoneNumber = zoneNumber;
            Label = Require(label);
            Role = Require(role);
            IconId = Require(iconId);
            VisualState = Require(visualState);
            Polygon = polygon ?? Array.Empty<Vector2>();
            TokenAnchor = tokenAnchor;
            Priority = priority;
        }

        public string HotspotId { get; }
        public string CellId { get; }
        public int ZoneNumber { get; }
        public string Label { get; }
        public string Role { get; }
        public string IconId { get; }
        public string VisualState { get; }
        public IReadOnlyList<Vector2> Polygon { get; }
        public Vector2 TokenAnchor { get; }
        public int Priority { get; }

        public bool Contains(Vector2 point)
        {
            bool inside = false;
            int count = Polygon.Count;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                Vector2 a = Polygon[i];
                Vector2 b = Polygon[j];
                bool crosses = (a.y > point.y) != (b.y > point.y);
                float denominator = b.y - a.y;
                if (Mathf.Abs(denominator) < 0.0001f) denominator = denominator < 0f ? -0.0001f : 0.0001f;
                if (crosses && point.x < (b.x - a.x) * (point.y - a.y) / denominator + a.x)
                {
                    inside = !inside;
                }
            }

            return inside || IsNearEdge(point, 5f);
        }

        public bool IsNearEdge(Vector2 point, float tolerance)
        {
            for (int i = 0; i < Polygon.Count; i++)
            {
                Vector2 a = Polygon[i];
                Vector2 b = Polygon[(i + 1) % Polygon.Count];
                if (DistanceToSegment(point, a, b) <= tolerance) return true;
            }

            return false;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, Vector2.Dot(ab, ab));
            t = Mathf.Clamp01(t);
            return Vector2.Distance(p, a + ab * t);
        }

        private static string Require(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A hotspot field is required.");
            return value;
        }
    }

    public sealed class HiveHotspotSelectionFocusState : ReferenceBackedHiveProductionizationPlan
    {
        public HiveHotspotSelectionFocusState(string focusedHotspotId, bool polygonHalo, bool panelUsesSameFocus)
            : base("BEE-623 selection focus", "SandboxPlayground", polygonHalo ? "polygon halo and badge" : "missing halo", "same focus in portrait", "selected hotspot proof", "local preview only")
        {
            FocusedHotspotId = focusedHotspotId ?? string.Empty;
            PolygonHalo = polygonHalo;
            PanelUsesSameFocus = panelUsesSameFocus;
        }

        public string FocusedHotspotId { get; }
        public bool PolygonHalo { get; }
        public bool PanelUsesSameFocus { get; }
    }

    public sealed class ReferenceHiveDynamicDetailPanelBinding : ReferenceBackedHiveProductionizationPlan
    {
        public ReferenceHiveDynamicDetailPanelBinding(IReadOnlyList<string> boundHotspotIds)
            : base("BEE-624 dynamic detail panel", "runtime detail panel", "title icon value action disclosure", "compact bottom sheet", "selection changes panel", "no official action")
        {
            BoundHotspotIds = boundHotspotIds ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> BoundHotspotIds { get; }
    }

    public sealed class HiveMobilePortraitCropPanController : ReferenceBackedHiveProductionizationPlan
    {
        public HiveMobilePortraitCropPanController(Vector2 pan, bool antiEmptyBounds, bool focusPreserved)
            : base("BEE-625 portrait crop pan", "reference art crop", "pannable viewport", "bounded drag with focus", "center left right proof", "local mobile preview")
        {
            Pan = pan;
            AntiEmptyBounds = antiEmptyBounds;
            FocusPreserved = focusPreserved;
        }

        public Vector2 Pan { get; }
        public bool AntiEmptyBounds { get; }
        public bool FocusPreserved { get; }
    }

    public sealed class HiveReferenceAssetProvenanceRecord : ReferenceBackedHiveProductionizationPlan
    {
        public HiveReferenceAssetProvenanceRecord(string sourcePath, string approvedBy, string status)
            : base("BEE-626 asset provenance", sourcePath, status, "same art scaled/cropped", "manifest with source", "preview/internal asset")
        {
            SourcePath = sourcePath;
            ApprovedBy = approvedBy;
            Status = status;
        }

        public string SourcePath { get; }
        public string ApprovedBy { get; }
        public string Status { get; }
    }

    public sealed class ReferenceHiveRuntimeLayerStack : ReferenceBackedHiveProductionizationPlan
    {
        public ReferenceHiveRuntimeLayerStack()
            : base("BEE-627 runtime layer stack", "art hit tokens hud panel", "interactive overlays above non-interactive art", "portrait crop below HUD", "player and QA captures separated", "no static-image-only bypass")
        {
        }
    }

    public sealed class HiveMmoPlayerEntrySurface : ReferenceBackedHiveProductionizationPlan
    {
        public HiveMmoPlayerEntrySurface(IReadOnlyList<string> previewEntries)
            : base("BEE-628 MMO entry preview", "profile alliance world defense preview", "server future badges", "tap targets visible", "entry surface proof", "no account chat ranking war")
        {
            PreviewEntries = previewEntries ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> PreviewEntries { get; }
    }

    public sealed class LocalHiveIdentityPreviewCard : ReferenceBackedHiveProductionizationPlan
    {
        public LocalHiveIdentityPreviewCard(string localName, string posture)
            : base("BEE-629 local identity preview", localName, posture, "identity remains readable portrait", "identity visible in HUD", "not saved or shared")
        {
            LocalName = localName;
            Posture = posture;
        }

        public string LocalName { get; }
        public string Posture { get; }
    }

    public sealed class HiveHotspotProductionPreviewLoop : ReferenceBackedHiveProductionizationPlan
    {
        public HiveHotspotProductionPreviewLoop(IReadOnlyList<string> flows)
            : base("BEE-630 production preview loop", "hotspot role flow", "flow arrows without economy claim", "compact flow labels", "flow changes by selection", "no live balance or inventory")
        {
            Flows = flows ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Flows { get; }
    }

    public sealed class HiveDetailPanelActionPreview : ReferenceBackedHiveProductionizationPlan
    {
        public HiveDetailPanelActionPreview(string actionLabel, string actionState)
            : base("BEE-631 action preview", "detail panel button", actionState, "touch sized action", "button feedback local", "no endpoint")
        {
            ActionLabel = actionLabel;
            ActionState = actionState;
        }

        public string ActionLabel { get; }
        public string ActionState { get; }
    }

    public sealed class ReferenceHiveUnifiedInputRouter : ReferenceBackedHiveProductionizationPlan
    {
        public ReferenceHiveUnifiedInputRouter(float dragThresholdPixels)
            : base("BEE-632 unified input", "tap drag panel rail", "drag does not select", "pan threshold", "tap and drag proof", "local input only")
        {
            DragThresholdPixels = dragThresholdPixels;
        }

        public float DragThresholdPixels { get; }
    }

    public sealed class ReferenceHiveResponsiveEvidenceMatrix : ReferenceBackedHiveProductionizationPlan
    {
        public ReferenceHiveResponsiveEvidenceMatrix(IReadOnlyList<string> deviceProfiles)
            : base("BEE-633 responsive evidence", "desktop portrait landscape tablet", "player-facing captures", "mobile crop not compressed", "device matrix manifest", "device proof only")
        {
            DeviceProfiles = deviceProfiles ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> DeviceProfiles { get; }
    }

    public sealed class HiveZonePlayerLandmarkLegend : ReferenceBackedHiveProductionizationPlan
    {
        public HiveZonePlayerLandmarkLegend(IReadOnlyList<string> landmarks)
            : base("BEE-634 zone landmark legend", "14 zone cues", "icon text color secondary cue", "legend optional compact", "QA alignment with zones.png", "legend is descriptive only")
        {
            Landmarks = landmarks ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Landmarks { get; }
    }

    public sealed class MmoEntryNonClaimLanguageLedger : ReferenceBackedHiveProductionizationPlan
    {
        public MmoEntryNonClaimLanguageLedger(IReadOnlyList<string> allowedCopies, IReadOnlyList<string> forbiddenCopies)
            : base("BEE-635 non-claim language", "player-facing copy ledger", "preview future non synchronized", "short portrait copy", "forbidden claim scan", "no official claims")
        {
            AllowedCopies = allowedCopies ?? Array.Empty<string>();
            ForbiddenCopies = forbiddenCopies ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> AllowedCopies { get; }
        public IReadOnlyList<string> ForbiddenCopies { get; }
    }

    public sealed class ReferenceHiveReadabilityPolishChecklist : ReferenceBackedHiveProductionizationPlan
    {
        public ReferenceHiveReadabilityPolishChecklist(IReadOnlyList<string> checks)
            : base("BEE-636 readability polish", "HUD panel icons tokens", "contrast target pairing", "no portrait overflow", "readability checklist", "preview copy only")
        {
            Checks = checks ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Checks { get; }
    }

    public sealed class BuilderReferenceHiveProductionizationBundle : ReferenceBackedHiveProductionizationPlan
    {
        public BuilderReferenceHiveProductionizationBundle(IReadOnlyList<string> artifacts)
            : base("BEE-637 Builder bundle", "SandboxPlayground presenter captures logs manifests", "runtime not debug overlay", "mobile proof included", "non-regression check", "no parallel scene")
        {
            Artifacts = artifacts ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Artifacts { get; }
    }

    public sealed class DemoReferenceHiveProductionizationEvidencePack : ReferenceBackedHiveProductionizationPlan
    {
        public DemoReferenceHiveProductionizationEvidencePack(IReadOnlyList<string> shots)
            : base("BEE-638 Demo evidence pack", "desktop hotspots portrait MMO identity", "player-facing evidence", "panned portrait", "contact sheet ready", "no QA overlay in player shots")
        {
            Shots = shots ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Shots { get; }
    }

    public sealed class QaReferenceHiveMmoEntryValidationProtocol : ReferenceBackedHiveProductionizationPlan
    {
        public QaReferenceHiveMmoEntryValidationProtocol(IReadOnlyList<string> questions)
            : base("BEE-639 QA validation protocol", "hotspot focus panel mobile provenance", "player comprehension", "touch questions", "QA can validate zones.png alignment", "no server authority")
        {
            Questions = questions ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Questions { get; }
    }

    public sealed class ReferenceHiveProductionizationMmoEntryGate : ReferenceBackedHiveProductionizationPlan
    {
        public ReferenceHiveProductionizationMmoEntryGate(IReadOnlyList<string> passedVerdicts, IReadOnlyList<string> reserves)
            : base("BEE-640 productionization MMO entry gate", "hotspots selection panel mobile provenance input responsive non-claims MMO", reserves != null && reserves.Count > 0 ? "pass with reserves" : "pass", "mobile validated by evidence", "gate manifest", "BEE-641 remains blocked")
        {
            PassedVerdicts = passedVerdicts ?? Array.Empty<string>();
            Reserves = reserves ?? Array.Empty<string>();
            Verdict = PassedVerdicts.Count >= 9 && Reserves.Count == 0 ? ReferenceHiveProductionizationVerdict.Pass :
                PassedVerdicts.Count >= 9 ? ReferenceHiveProductionizationVerdict.PassWithReserves :
                ReferenceHiveProductionizationVerdict.ReworkRequired;
        }

        public IReadOnlyList<string> PassedVerdicts { get; }
        public IReadOnlyList<string> Reserves { get; }
        public ReferenceHiveProductionizationVerdict Verdict { get; }
    }
}
