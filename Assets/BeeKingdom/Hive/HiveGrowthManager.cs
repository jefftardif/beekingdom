using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;
using BeeKingdom.Core.Simulation;

namespace BeeKingdom.Hive
{
    public sealed class HiveGrowthManager : ISimulationSystem
    {
        private readonly HiveTopology topology;
        private readonly HiveExpansionPlanner planner;
        private readonly HiveLayoutValidator validator;
        private readonly TaskManager taskManager;
        private readonly IEventBus eventBus;
        private int chamberCounter;
        private int siteCounter;

        public Type SystemType => typeof(HiveGrowthManager);
        public string Name => nameof(HiveGrowthManager);
        public SimulationPhase Phase => SimulationPhase.Simulation;
        public int Priority => 150;
        public IReadOnlyList<Type> RunsAfter => Array.Empty<Type>();
        public IReadOnlyList<Type> RunsBefore => new[] { typeof(TaskManager) };
        public HiveGrowthDiagnostics Diagnostics { get; } = new HiveGrowthDiagnostics();

        public HiveGrowthManager(IEventBus eventBus = null, TaskManager taskManager = null)
            : this(new HiveTopology(), new HiveExpansionPlanner(), new HiveLayoutValidator(), eventBus, taskManager)
        {
        }

        public HiveGrowthManager(HiveTopology topology, HiveExpansionPlanner planner, HiveLayoutValidator validator, IEventBus eventBus = null, TaskManager taskManager = null)
        {
            this.topology = topology;
            this.planner = planner;
            this.validator = validator;
            this.eventBus = eventBus;
            this.taskManager = taskManager;
        }

        public HiveExpansionPlan PlanExpansion(HiveExpansionRequest request)
        {
            return planner.PlanExpansion(topology, request);
        }

        public ConstructionSite CreateChamber(HiveExpansionPlan plan, string connectToChamberId = null)
        {
            if (plan == null || !plan.IsApproved)
            {
                throw new InvalidOperationException("Expansion plan must be approved before creating a chamber.");
            }

            string chamberId = "chamber-" + (++chamberCounter);
            HiveChamber chamber = new HiveChamber(chamberId, plan.ChamberType, plan.Position, plan.CellCount);
            topology.AddChamber(chamber);

            for (int i = 0; i < plan.CellCount; i++)
            {
                HoneycombCell cell = new HoneycombCell(chamberId + "-cell-" + (i + 1), new HivePosition(plan.Position.X, plan.Position.Y + i, plan.Position.Layer), chamber.Function);
                topology.AddCell(cell, chamberId);
            }

            if (!string.IsNullOrWhiteSpace(connectToChamberId))
            {
                ConnectChambers(connectToChamberId, chamberId);
            }

            ConstructionSite site = new ConstructionSite("site-" + (++siteCounter), chamberId, plan.ChamberType, plan.WaxCost, plan.RequiredWorkSeconds);
            topology.AddConstructionSite(site);
            site.Reserve();
            StartConstruction(site);

            Diagnostics.RecordPlan();
            PublishTopologyChanged();
            eventBus?.Publish(new ChamberPlanned(chamberId));
            return site;
        }

        public bool ConnectChambers(string firstChamberId, string secondChamberId)
        {
            bool changed = topology.ConnectChambers(firstChamberId, secondChamberId);
            if (changed)
            {
                PublishTopologyChanged();
            }

            return changed;
        }

        public bool UpgradeChamber(string chamberId)
        {
            if (!topology.Chambers.TryGetValue(chamberId, out HiveChamber chamber))
            {
                return false;
            }

            chamber.Upgrade();
            foreach (string cellId in chamber.CellIds)
            {
                if (topology.Cells.TryGetValue(cellId, out HoneycombCell cell))
                {
                    cell.Upgrade();
                }
            }

            topology.Touch(chamberId);
            PublishTopologyChanged();
            return true;
        }

        public HiveLayoutValidationResult ValidateTopology()
        {
            HiveLayoutValidationResult result = validator.Validate(topology);
            Diagnostics.RecordValidation(result);
            topology.ClearDirty();
            return result;
        }

        public HiveTopologySnapshot GetLayout()
        {
            return topology.CreateSnapshot();
        }

        public void Execute(in SimulationExecutionContext context)
        {
            foreach (ConstructionSite site in topology.ConstructionSites.Values)
            {
                if (site.State != ConstructionSiteState.UnderConstruction)
                {
                    continue;
                }

                site.AddProgress(context.DeltaSeconds);
                if (site.State == ConstructionSiteState.Completed)
                {
                    CompleteChamber(site);
                }
            }
        }

        private void StartConstruction(ConstructionSite site)
        {
            string taskId = site.SiteId + "-build";
            site.Start(taskId);
            taskManager?.CreateTask(taskId, new TaskDefinition("build-" + site.ChamberType, ColonyTaskType.BuildCell, new TaskPriority(80, 0, 10), site.WaxCost, site.RequiredWorkSeconds, BeeLifecycleRole.Builder), 0d, double.PositiveInfinity);
            eventBus?.Publish(new ChamberConstructionStarted(site.ChamberId, site.SiteId));
        }

        private void CompleteChamber(ConstructionSite site)
        {
            HiveChamber chamber = topology.GetChamber(site.ChamberId);
            foreach (string cellId in chamber.CellIds)
            {
                HoneycombCell cell = topology.Cells[cellId];
                cell.StartConstruction();
                cell.Complete();
            }

            site.MarkUpgradeable();
            taskManager?.CompleteTask(site.TaskId);
            topology.Touch(site.ChamberId);
            Diagnostics.RecordCompletion();
            PublishTopologyChanged();
            eventBus?.Publish(new ChamberCompleted(site.ChamberId));
            eventBus?.Publish(new HiveExpanded(site.ChamberId));
        }

        private void PublishTopologyChanged()
        {
            Diagnostics.RecordTopologyRevision(topology.Revision);
            eventBus?.Publish(new TopologyChanged(topology.Revision));
        }
    }
}
