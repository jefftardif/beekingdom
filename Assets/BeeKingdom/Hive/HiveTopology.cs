using System;
using System.Collections.Generic;

namespace BeeKingdom.Hive
{
    public sealed class HiveTopology
    {
        private readonly Dictionary<string, HiveChamber> chambers = new Dictionary<string, HiveChamber>();
        private readonly Dictionary<string, HoneycombCell> cells = new Dictionary<string, HoneycombCell>();
        private readonly Dictionary<string, ConstructionSite> sites = new Dictionary<string, ConstructionSite>();
        private readonly HashSet<string> dirtyChambers = new HashSet<string>();
        private int revision;

        public IReadOnlyDictionary<string, HiveChamber> Chambers => chambers;
        public IReadOnlyDictionary<string, HoneycombCell> Cells => cells;
        public IReadOnlyDictionary<string, ConstructionSite> ConstructionSites => sites;
        public int Revision => revision;
        public IReadOnlyCollection<string> DirtyChambers => dirtyChambers;

        public void AddChamber(HiveChamber chamber)
        {
            if (chamber == null)
            {
                throw new ArgumentNullException(nameof(chamber));
            }

            chambers.Add(chamber.ChamberId, chamber);
            MarkDirty(chamber.ChamberId);
        }

        public void AddCell(HoneycombCell cell, string chamberId)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }

            if (!chambers.TryGetValue(chamberId, out HiveChamber chamber))
            {
                throw new KeyNotFoundException($"Chamber {chamberId} was not found.");
            }

            if (!chamber.AddCell(cell.CellId))
            {
                throw new InvalidOperationException($"Chamber {chamberId} cannot accept cell {cell.CellId}.");
            }

            cell.AssignToChamber(chamberId);
            cells.Add(cell.CellId, cell);
            MarkDirty(chamberId);
        }

        public void AddConstructionSite(ConstructionSite site)
        {
            if (site == null)
            {
                throw new ArgumentNullException(nameof(site));
            }

            sites.Add(site.SiteId, site);
            MarkDirty(site.ChamberId);
        }

        public bool ConnectChambers(string firstChamberId, string secondChamberId)
        {
            if (!chambers.TryGetValue(firstChamberId, out HiveChamber first) || !chambers.TryGetValue(secondChamberId, out HiveChamber second))
            {
                return false;
            }

            bool changed = first.Connect(secondChamberId) | second.Connect(firstChamberId);
            if (changed)
            {
                MarkDirty(firstChamberId);
                MarkDirty(secondChamberId);
            }

            return changed;
        }

        public HiveChamber GetChamber(string chamberId)
        {
            return chambers[chamberId];
        }

        public ConstructionSite GetConstructionSite(string siteId)
        {
            return sites[siteId];
        }

        public HiveTopologySnapshot CreateSnapshot()
        {
            return new HiveTopologySnapshot(new List<HiveChamber>(chambers.Values), new List<HoneycombCell>(cells.Values), new List<ConstructionSite>(sites.Values), revision);
        }

        public void Touch(string chamberId)
        {
            MarkDirty(chamberId);
        }

        public void ClearDirty()
        {
            dirtyChambers.Clear();
        }

        private void MarkDirty(string chamberId)
        {
            revision++;
            if (!string.IsNullOrWhiteSpace(chamberId))
            {
                dirtyChambers.Add(chamberId);
            }
        }
    }

    public sealed class HiveTopologySnapshot
    {
        public IReadOnlyList<HiveChamber> Chambers { get; }
        public IReadOnlyList<HoneycombCell> Cells { get; }
        public IReadOnlyList<ConstructionSite> ConstructionSites { get; }
        public int Revision { get; }

        public HiveTopologySnapshot(IReadOnlyList<HiveChamber> chambers, IReadOnlyList<HoneycombCell> cells, IReadOnlyList<ConstructionSite> constructionSites, int revision)
        {
            Chambers = chambers;
            Cells = cells;
            ConstructionSites = constructionSites;
            Revision = revision;
        }
    }
}
