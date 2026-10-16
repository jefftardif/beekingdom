using System;
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Networking;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Read-only view of official persisted combat convalescence.
    /// The combat patrol API remains authoritative for all wounded counts, roster and
    /// automatic recovery. Never fabricate an operation or credit bees locally.
    /// </summary>
    public sealed class InfirmaryRecoveryRow
    {
        public string Family { get; }
        public long Wounded { get; }
        public DateTimeOffset? NextReadyAtUtc { get; }
        public DateTimeOffset? LastReadyAtUtc { get; }
        public int BatchCount { get; }

        public InfirmaryRecoveryRow(string family, long wounded,
            DateTimeOffset? nextReadyAtUtc, DateTimeOffset? lastReadyAtUtc, int batchCount)
        {
            Family = family;
            Wounded = wounded;
            NextReadyAtUtc = nextReadyAtUtc;
            LastReadyAtUtc = lastReadyAtUtc;
            BatchCount = batchCount;
        }
    }

    public static class InfirmaryRecoveryOverview
    {
        public const string Guardians = "guardians";
        public const string Wingrunners = "wingrunners";
        public const string Darters = "darters";

        public static IReadOnlyList<InfirmaryRecoveryRow> Aggregate(
            IReadOnlyList<RemoteCombatPatrolRecoveringBatch> source)
        {
            var families = new[] { Guardians, Wingrunners, Darters };
            var result = new List<InfirmaryRecoveryRow>(3);
            foreach (string family in families)
            {
                long wounded = 0L;
                DateTimeOffset? first = null;
                DateTimeOffset? last = null;
                int batchCount = 0;
                if (source != null)
                {
                    for (int i = 0; i < source.Count; i++)
                    {
                        RemoteCombatPatrolRecoveringBatch batch = source[i];
                        if (batch == null || !string.Equals(batch.Family, family,
                            StringComparison.Ordinal) || batch.Count <= 0L)
                            continue;
                        wounded = batch.Count > long.MaxValue - wounded
                            ? long.MaxValue : wounded + batch.Count;
                        batchCount++;
                        DateTimeOffset ready = batch.ReadyAtUtc.ToUniversalTime();
                        if (!first.HasValue || ready < first.Value) first = ready;
                        if (!last.HasValue || ready > last.Value) last = ready;
                    }
                }
                result.Add(new InfirmaryRecoveryRow(family, wounded, first, last, batchCount));
            }
            return result;
        }

        public static long Total(IReadOnlyList<InfirmaryRecoveryRow> rows)
        {
            long total = 0;
            if (rows == null) return total;
            foreach (InfirmaryRecoveryRow row in rows)
            {
                if (row == null || row.Wounded <= 0L) continue;
                total = row.Wounded > long.MaxValue - total ? long.MaxValue : total + row.Wounded;
            }
            return total;
        }

        public static string FamilyName(string family, bool french)
        {
            if (family == Guardians) return french ? "Gardiennes" : "Guardians";
            if (family == Wingrunners) return french ? "Voltigeuses" : "Wingrunners";
            if (family == Darters) return french ? "Lanceuses" : "Darters";
            return french ? "Troupes" : "Troops";
        }

        public static string Countdown(DateTimeOffset? readyAtUtc,
            DateTimeOffset nowUtc, bool french)
        {
            if (!readyAtUtc.HasValue) return french ? "Aucune blessée" : "No wounded bees";
            TimeSpan remaining = readyAtUtc.Value.ToUniversalTime() - nowUtc.ToUniversalTime();
            if (remaining <= TimeSpan.Zero)
                return french ? "Guérison terminée · actualiser" : "Recovery finished · refresh";
            long totalSeconds = Math.Max(1L, (long)Math.Ceiling(remaining.TotalSeconds));
            long hours = totalSeconds / 3600L;
            long minutes = (totalSeconds % 3600L) / 60L;
            long seconds = totalSeconds % 60L;
            string duration = hours > 0
                ? string.Format(CultureInfo.InvariantCulture,
                    "{0} h {1:00} min", hours, minutes)
                : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", minutes, seconds);
            return (french ? "Prochaine guérison : " : "Next recovery: ") + duration;
        }

        public static string Signature(IReadOnlyList<InfirmaryRecoveryRow> rows,
            bool configured, bool busy, string error)
        {
            string result = (configured ? "1" : "0") + (busy ? "B" : "R") + "|" + (error ?? "");
            if (rows != null)
                foreach (InfirmaryRecoveryRow row in rows)
                    result += "|" + row.Family + ":" + row.Wounded + ":" + row.BatchCount +
                        ":" + ((row.NextReadyAtUtc.HasValue ? row.NextReadyAtUtc.Value.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture) : "-") ?? "-") +
                        ":" + ((row.LastReadyAtUtc.HasValue ? row.LastReadyAtUtc.Value.UtcDateTime.Ticks.ToString(CultureInfo.InvariantCulture) : "-") ?? "-");
            return result;
        }
    }
}
