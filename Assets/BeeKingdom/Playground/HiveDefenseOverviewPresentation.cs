using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Playground
{
    // Defensive building information is an honest read-only projection of the
    // same official squad and combat states used by Barracks and Patrol.
    // There is no automated siege, garrison deployment or shield service yet.
    public sealed class HiveDefenseOverviewSnapshot
    {
        public bool RosterKnown { get; internal set; }
        public bool CombatKnown { get; internal set; }
        public bool OfflineRoster { get; internal set; }

        public long GuardiansReady { get; internal set; }
        public long WingrunnersReady { get; internal set; }
        public long DartersReady { get; internal set; }
        public long GuardiansRoster { get; internal set; }
        public long WingrunnersRoster { get; internal set; }
        public long DartersRoster { get; internal set; }
        public long GuardiansReserved { get; internal set; }
        public long WingrunnersReserved { get; internal set; }
        public long DartersReserved { get; internal set; }
        public bool HasPreparedPatrol { get; internal set; }
        public IReadOnlyList<string> ReservedChampionIds { get; internal set; } =
            Array.Empty<string>();
        public int ActivePatrols { get; internal set; }
        public long RecoveringTroops { get; internal set; }

        public long ReadyTotal => GuardiansReady + WingrunnersReady + DartersReady;
        public long ReservedTotal => GuardiansReserved + WingrunnersReserved + DartersReserved;
        public long RosterTotal => GuardiansRoster + WingrunnersRoster + DartersRoster;
    }

    public static class HiveDefenseOverviewPresentation
    {
        public static HiveDefenseOverviewSnapshot Project(
            HiveSquadReservationScreenModel squad,
            CombatPatrolScreenModel patrol)
        {
            var state = new HiveDefenseOverviewSnapshot();
            bool validRoster = squad != null &&
                (squad.State == HiveSquadReservationScreenState.Ready ||
                 squad.State == HiveSquadReservationScreenState.OfflineReadOnly ||
                 squad.State == HiveSquadReservationScreenState.Mutating ||
                 squad.State == HiveSquadReservationScreenState.PendingConfirmation);
            if (validRoster)
            {
                state.RosterKnown = true;
                state.OfflineRoster = squad.ReadOnlyOffline ||
                    squad.State == HiveSquadReservationScreenState.OfflineReadOnly;
                state.GuardiansReady = squad.AvailableGuardians;
                state.WingrunnersReady = squad.AvailableWingrunners;
                state.DartersReady = squad.AvailableDarters;
                state.GuardiansRoster = squad.RosterGuardians;
                state.WingrunnersRoster = squad.RosterWingrunners;
                state.DartersRoster = squad.RosterDarters;
                state.GuardiansReserved = squad.ReservedGuardians;
                state.WingrunnersReserved = squad.ReservedWingrunners;
                state.DartersReserved = squad.ReservedDarters;
                state.HasPreparedPatrol = squad.HasReservation;
                state.ReservedChampionIds = squad.ChampionBeeIds ??
                    Array.Empty<string>();
            }

            bool validCombat = patrol != null &&
                patrol.State != CombatPatrolScreenState.NotConfigured &&
                patrol.State != CombatPatrolScreenState.Loading &&
                patrol.State != CombatPatrolScreenState.Error;
            if (validCombat)
            {
                state.CombatKnown = true;
                state.ActivePatrols = patrol.ActiveEncounters?.Count ?? 0;
                // Wounded troops are *not* counted as battle-ready. Their
                // server-origin recovering batches are already used by Infirmary.
                var batches = InfirmaryRecoveryOverview.Aggregate(patrol.Recovering);
                state.RecoveringTroops = InfirmaryRecoveryOverview.Total(batches);
            }
            return state;
        }
    }
}