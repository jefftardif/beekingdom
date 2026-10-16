using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.Playground
{
    public enum HiveStartupStepState { Waiting, Ready, Unavailable }

    public sealed class HiveStartupStep
    {
        public readonly string Id;
        public readonly bool Required;
        public readonly Func<HiveStartupStepState> Read;
        public HiveStartupStep(string id, bool required, Func<HiveStartupStepState> read)
        { Id = id; Required = required; Read = read ?? throw new ArgumentNullException(nameof(read)); }
    }

    public sealed class HiveStartupSnapshot
    {
        public readonly bool Ready, Failed;
        public readonly int Completed, Total;
        public readonly string WaitingFor;
        public readonly string[] Unavailable;
        public float Progress => Ready ? 1f : Total == 0 ? 0f : Math.Min(.95f, (float)Completed / Total);
        public HiveStartupSnapshot(bool ready, bool failed, int complete, int total, string waiting, string[] unavailable)
        { Ready = ready; Failed = failed; Completed = complete; Total = total; WaitingFor = waiting; Unavailable = unavailable; }
    }

    // Reads existing controller state; this class never starts a request or mutates gameplay.
    // Time uses an injected monotonic clock, so paused games and tests cannot stall a timeout.
    public sealed class HiveStartupReadiness
    {
        private readonly HiveStartupStep[] steps;
        private readonly double startedAt, timeout;
        private readonly Func<double> clock;
        private HiveStartupSnapshot terminal;
        public HiveStartupReadiness(IEnumerable<HiveStartupStep> steps, Func<double> clock, double timeoutSeconds = 45)
        {
            this.steps = (steps ?? throw new ArgumentNullException(nameof(steps))).ToArray();
            if (this.steps.Length == 0 || this.steps.Select(s => s.Id).Distinct().Count() != this.steps.Length)
                throw new ArgumentException("Startup requires non-empty, unique steps.");
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (timeoutSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            startedAt = clock(); timeout = timeoutSeconds;
        }
        public HiveStartupSnapshot Poll()
        {
            if (terminal != null) return terminal;
            bool timedOut = clock() - startedAt >= timeout;
            int complete = 0; bool requiredFailed = false; string waiting = null;
            var unavailable = new List<string>();
            foreach (var step in steps)
            {
                HiveStartupStepState state;
                try { state = step.Read(); } catch { state = HiveStartupStepState.Unavailable; }
                if (state == HiveStartupStepState.Ready) { complete++; continue; }
                if (state == HiveStartupStepState.Unavailable || timedOut)
                {
                    complete++; unavailable.Add(step.Id);
                    if (step.Required) { requiredFailed = true; waiting = step.Id; }
                }
                else if (waiting == null) waiting = step.Id;
            }
            bool ready = !requiredFailed && complete == steps.Length;
            var result = new HiveStartupSnapshot(ready, requiredFailed, complete, steps.Length,
                waiting ?? string.Empty, unavailable.ToArray());
            if (ready || requiredFailed) terminal = result;
            return result;
        }
        public static HiveStartupStepState FromModel(string state, bool hasRequiredData = true)
        {
            if (string.IsNullOrEmpty(state) || state == "NotConfigured" || state == "Loading")
                return HiveStartupStepState.Waiting;
            if (state == "Error" || state == "OfflineReadOnly") return HiveStartupStepState.Unavailable;
            return hasRequiredData ? HiveStartupStepState.Ready : HiveStartupStepState.Unavailable;
        }
    }

    // Tokens are not retained here: clients already read the current token from their source.
    // Refreshing a token does not recreate controllers; changing account/hive/server does.
    public sealed class HiveGameplayBindingIdentity
    {
        private readonly object source;
        private readonly Guid player, hive;
        private readonly string server;
        private readonly bool online;
        public HiveGameplayBindingIdentity(object source, Guid player, Guid hive, string server, bool online)
        { this.source = source; this.player = player; this.hive = hive; this.server = server; this.online = online; }
        public bool Matches(object otherSource, Guid otherPlayer, Guid otherHive, string otherServer, bool otherOnline)
        { return ReferenceEquals(source, otherSource) && player == otherPlayer && hive == otherHive && server == otherServer && online == otherOnline; }
    }
}