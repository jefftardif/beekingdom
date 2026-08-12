using System;

namespace BeeKingdom.Hive
{
    public sealed class ConstructionSite
    {
        public string SiteId { get; }
        public string ChamberId { get; }
        public HiveChamberType ChamberType { get; }
        public ConstructionSiteState State { get; private set; }
        public double WaxCost { get; }
        public double RequiredWorkSeconds { get; }
        public double ProgressSeconds { get; private set; }
        public string TaskId { get; private set; }

        public ConstructionSite(string siteId, string chamberId, HiveChamberType chamberType, double waxCost, double requiredWorkSeconds)
        {
            SiteId = string.IsNullOrWhiteSpace(siteId) ? Guid.NewGuid().ToString("N") : siteId;
            ChamberId = chamberId;
            ChamberType = chamberType;
            WaxCost = waxCost < 0d ? 0d : waxCost;
            RequiredWorkSeconds = requiredWorkSeconds <= 0d ? 1d : requiredWorkSeconds;
            State = ConstructionSiteState.Planned;
        }

        public bool Reserve()
        {
            if (State != ConstructionSiteState.Planned)
            {
                return false;
            }

            State = ConstructionSiteState.Reserved;
            return true;
        }

        public bool Start(string taskId)
        {
            if (State != ConstructionSiteState.Reserved && State != ConstructionSiteState.Planned)
            {
                return false;
            }

            TaskId = taskId;
            State = ConstructionSiteState.UnderConstruction;
            return true;
        }

        public bool AddProgress(double workSeconds)
        {
            if (State != ConstructionSiteState.UnderConstruction || workSeconds <= 0d)
            {
                return false;
            }

            ProgressSeconds = Math.Min(RequiredWorkSeconds, ProgressSeconds + workSeconds);
            if (ProgressSeconds >= RequiredWorkSeconds)
            {
                State = ConstructionSiteState.Completed;
            }

            return true;
        }

        public bool MarkUpgradeable()
        {
            if (State != ConstructionSiteState.Completed)
            {
                return false;
            }

            State = ConstructionSiteState.Upgradeable;
            return true;
        }
    }
}
