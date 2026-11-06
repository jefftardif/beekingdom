using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Persistent selection identity for the World Map target overlay.
    /// Never stores client guesses about server-owned combat or collection.
    /// </summary>
    /// <summary>Pixel-safe, DPI-independent target window bounds.</summary>
    public static class WorldTargetPanelGeometry
    {
        public static bool Compact(int screenWidth, int screenHeight, Rect deviceSafe)
        {
            Rect safe=WorldMapHudLayout.SafeGuiRect(screenWidth,screenHeight,deviceSafe);
            return screenWidth<800 || safe.height>safe.width*1.10f;
        }
        public static Rect Calculate(int screenWidth,int screenHeight,
            Rect deviceSafe,bool composition=false)
        {
            Rect safe=WorldMapHudLayout.SafeGuiRect(screenWidth,screenHeight,deviceSafe);
            bool compact=Compact(screenWidth,screenHeight,deviceSafe);
            float logicalWidth=compact?420f:600f;
            float logicalHeight=composition?(compact?610f:525f):(compact?600f:520f);
            float scale=Mathf.Max(.1f,Mathf.Min(
                (safe.width-24f)/logicalWidth,(safe.height-24f)/logicalHeight,
                compact?2.6f:1.35f));
            return new Rect(safe.center.x-logicalWidth*scale*.5f,
                safe.center.y-logicalHeight*scale*.5f,
                logicalWidth*scale,logicalHeight*scale);
        }
    }
    public enum WorldTargetPanelMode { None, Enemy, Resource }
    public enum WorldTargetPrimaryAction
    {
        None, PrepareArmy, PlanAttack, PrepareGather, PreviewGather, RecallGatherers, ClaimGathering
    }

    public sealed class WorldTargetPanelSelection
    {
        public WorldTargetPanelMode Mode { get; private set; } = WorldTargetPanelMode.None;
        public string TargetId { get; private set; } = string.Empty;
        public bool Visible => Mode != WorldTargetPanelMode.None &&
                               !string.IsNullOrWhiteSpace(TargetId);
        public void Open(WorldTargetPanelMode mode, string targetId)
        {
            if ((mode != WorldTargetPanelMode.Enemy && mode != WorldTargetPanelMode.Resource) ||
                string.IsNullOrWhiteSpace(targetId))
            {
                Close();
                return;
            }
            Mode = mode;
            TargetId = targetId;
        }
        public bool IsTarget(WorldTargetPanelMode mode, string targetId) =>
            Visible && Mode == mode && string.Equals(TargetId, targetId, StringComparison.Ordinal);
        public void Close()
        {
            Mode = WorldTargetPanelMode.None;
            TargetId = string.Empty;
        }
    }

    public readonly struct WorldTargetActionDecision
    {
        public readonly WorldTargetPrimaryAction Action;
        public readonly string ReasonKey;
        public bool Enabled => Action != WorldTargetPrimaryAction.None;
        public WorldTargetActionDecision(WorldTargetPrimaryAction action, string reasonKey)
        {
            Action = action;
            ReasonKey = reasonKey ?? string.Empty;
        }
    }

    /// <summary>
    /// Fail-closed action decisions. A local/demonstration node must not
    /// masquerade as a server-backed harvest or a guaranteed combat victory.
    /// </summary>
    public static class WorldTargetPanelPolicy
    {
        private static WorldTargetActionDecision Denied(string reason) =>
            new WorldTargetActionDecision(WorldTargetPrimaryAction.None, reason);
        public static WorldTargetActionDecision Enemy(bool exists, bool hasSquad,
            bool patrolConfigured, bool patrolBusy, bool hasFreeSlot)
        {
            if (!exists) return Denied("target.unavailable");
            if (!hasSquad)
                return new WorldTargetActionDecision(WorldTargetPrimaryAction.PrepareArmy,
                    "target.reason.prepare_army");
            if (!patrolConfigured) return Denied("target.reason.combat_offline");
            if (patrolBusy) return Denied("target.reason.server_busy");
            if (!hasFreeSlot) return Denied("target.reason.march_limit");
            return new WorldTargetActionDecision(WorldTargetPrimaryAction.PlanAttack,
                "target.reason.combat_preview");
        }

        public static WorldTargetActionDecision Resource(
            bool exists, bool hasHive, bool official, bool modelAvailable,
            bool serverBusy, bool claimReadyHere, bool activeHere,
            bool activeElsewhere, bool hasNode, bool placementReady,
            bool rosterAvailable, bool marchSlotAvailable, bool canLaunch)
        {
            if (!exists) return Denied("target.unavailable");
            if (!hasHive) return Denied("target.reason.no_hive");
            // Keep the existing guided local flight playable, explicitly
            // labeled as a demo with NO server credit or inventory mutation.
            if (!official)
                return canLaunch
                    ? new WorldTargetActionDecision(WorldTargetPrimaryAction.PreviewGather,
                        "target.reason.preview_only")
                    : Denied("target.reason.not_ready");
            if (!modelAvailable) return Denied("target.reason.server_offline");
            if (serverBusy) return Denied("target.reason.server_busy");
            if (claimReadyHere)
                return new WorldTargetActionDecision(WorldTargetPrimaryAction.ClaimGathering,
                    "target.reason.ready_to_claim");
            if (activeHere)
                return new WorldTargetActionDecision(WorldTargetPrimaryAction.RecallGatherers,
                    "target.reason.active_flight");
            if (activeElsewhere) return Denied("target.reason.other_flight");
            if (!hasNode) return Denied("target.reason.server_offline");
            if (!placementReady) return Denied("target.reason.placement_missing");
            if (!rosterAvailable) return Denied("target.reason.no_bees");
            if (!marchSlotAvailable) return Denied("target.reason.march_limit");
            if (!canLaunch) return Denied("target.reason.not_ready");
            return new WorldTargetActionDecision(WorldTargetPrimaryAction.PrepareGather,
                "target.reason.gather_ready");
        }
    }
}
