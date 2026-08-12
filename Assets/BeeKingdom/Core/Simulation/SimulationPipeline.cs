using System;
using System.Collections.Generic;

namespace BeeKingdom.Core.Simulation
{
    public sealed class SimulationPipeline
    {
        private static readonly SimulationPhase[] PhaseOrder =
        {
            SimulationPhase.PreSimulation,
            SimulationPhase.Simulation,
            SimulationPhase.PostSimulation,
            SimulationPhase.LateSimulation
        };

        public SimulationPipelineEntry[] Build(IReadOnlyList<SimulationPipelineEntry> entries)
        {
            List<SimulationPipelineEntry> ordered = new List<SimulationPipelineEntry>(entries.Count);

            for (int i = 0; i < PhaseOrder.Length; i++)
            {
                BuildPhase(entries, PhaseOrder[i], ordered);
            }

            return ordered.ToArray();
        }

        private static void BuildPhase(
            IReadOnlyList<SimulationPipelineEntry> entries,
            SimulationPhase phase,
            List<SimulationPipelineEntry> ordered)
        {
            List<SimulationPipelineEntry> phaseEntries = new List<SimulationPipelineEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                SimulationPipelineEntry entry = entries[i];
                if (entry.IsEnabled && entry.System.Phase == phase)
                {
                    phaseEntries.Add(entry);
                }
            }

            phaseEntries.Sort(CompareStable);
            int count = phaseEntries.Count;
            int[] incoming = new int[count];
            bool[,] edges = new bool[count, count];

            for (int i = 0; i < count; i++)
            {
                ISimulationSystem system = phaseEntries[i].System;
                AddRunsAfterEdges(system, phaseEntries, edges, incoming, i);
                AddRunsBeforeEdges(system, phaseEntries, edges, incoming, i);
            }

            bool[] processed = new bool[count];
            int processedCount = 0;
            while (processedCount < count)
            {
                int next = FindNextReady(phaseEntries, incoming, processed);
                if (next < 0)
                {
                    throw new InvalidOperationException($"Circular simulation dependency detected in phase {phase}.");
                }

                processed[next] = true;
                processedCount++;
                ordered.Add(phaseEntries[next]);

                for (int target = 0; target < count; target++)
                {
                    if (edges[next, target])
                    {
                        incoming[target]--;
                    }
                }
            }
        }

        private static void AddRunsAfterEdges(
            ISimulationSystem system,
            List<SimulationPipelineEntry> phaseEntries,
            bool[,] edges,
            int[] incoming,
            int systemIndex)
        {
            IReadOnlyList<Type> runsAfter = system.RunsAfter;
            for (int i = 0; i < runsAfter.Count; i++)
            {
                int dependencyIndex = IndexOf(phaseEntries, runsAfter[i]);
                if (dependencyIndex >= 0)
                {
                    AddEdge(edges, incoming, dependencyIndex, systemIndex);
                }
            }
        }

        private static void AddRunsBeforeEdges(
            ISimulationSystem system,
            List<SimulationPipelineEntry> phaseEntries,
            bool[,] edges,
            int[] incoming,
            int systemIndex)
        {
            IReadOnlyList<Type> runsBefore = system.RunsBefore;
            for (int i = 0; i < runsBefore.Count; i++)
            {
                int targetIndex = IndexOf(phaseEntries, runsBefore[i]);
                if (targetIndex >= 0)
                {
                    AddEdge(edges, incoming, systemIndex, targetIndex);
                }
            }
        }

        private static void AddEdge(bool[,] edges, int[] incoming, int source, int target)
        {
            if (source == target || edges[source, target])
            {
                return;
            }

            edges[source, target] = true;
            incoming[target]++;
        }

        private static int FindNextReady(List<SimulationPipelineEntry> entries, int[] incoming, bool[] processed)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (!processed[i] && incoming[i] == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int IndexOf(List<SimulationPipelineEntry> entries, Type systemType)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].System.SystemType == systemType)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int CompareStable(SimulationPipelineEntry left, SimulationPipelineEntry right)
        {
            int priority = left.System.Priority.CompareTo(right.System.Priority);
            return priority != 0 ? priority : left.RegistrationOrder.CompareTo(right.RegistrationOrder);
        }
    }

    public sealed class SimulationPipelineEntry
    {
        public ISimulationSystem System { get; }
        public int RegistrationOrder { get; }
        public bool IsEnabled { get; set; }
        public long LastExecutionTicks { get; set; }

        public SimulationPipelineEntry(ISimulationSystem system, int registrationOrder)
        {
            System = system ?? throw new ArgumentNullException(nameof(system));
            RegistrationOrder = registrationOrder;
            IsEnabled = true;
        }
    }
}
