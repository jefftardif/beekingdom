using System;
using System.Collections.Generic;

namespace BeeKingdom.WorldMap
{
    public sealed class WorldSelectionChanged
    {
        public IReadOnlyList<WorldObjectId> Added { get; }
        public IReadOnlyList<WorldObjectId> Removed { get; }

        public WorldSelectionChanged(IReadOnlyList<WorldObjectId> added, IReadOnlyList<WorldObjectId> removed)
        {
            Added = added;
            Removed = removed;
        }
    }

    // Selection generique d'objets du monde. Aucun type concret : une ruche, une
    // ressource, un ennemi, un allie ou un boss seront selectionnes par leur
    // WorldObjectId. Mono-selection par defaut, multi selectionnable.
    public sealed class WorldSelection
    {
        private readonly HashSet<WorldObjectId> selected = new HashSet<WorldObjectId>();
        private readonly List<WorldObjectId> ordered = new List<WorldObjectId>();
        private readonly bool multiSelectEnabled;

        public bool MultiSelectEnabled => multiSelectEnabled;
        public int Count => selected.Count;
        public bool IsEmpty => selected.Count == 0;
        public IReadOnlyList<WorldObjectId> Selected => ordered;

        public event Action<WorldSelectionChanged> SelectionChanged;

        public WorldSelection(bool multiSelectEnabled = false)
        {
            this.multiSelectEnabled = multiSelectEnabled;
        }

        public bool Contains(WorldObjectId id)
        {
            return selected.Contains(id);
        }

        public void Select(WorldObjectId id)
        {
            if (id.IsNone || selected.Contains(id))
            {
                return;
            }

            List<WorldObjectId> removed = null;
            if (!multiSelectEnabled && selected.Count > 0)
            {
                removed = new List<WorldObjectId>(ordered);
                selected.Clear();
                ordered.Clear();
            }

            selected.Add(id);
            ordered.Add(id);
            Raise(removed, new List<WorldObjectId> { id });
        }

        public void Select(IEnumerable<WorldObjectId> ids)
        {
            List<WorldObjectId> added = new List<WorldObjectId>();
            List<WorldObjectId> removed = null;
            if (!multiSelectEnabled)
            {
                removed = new List<WorldObjectId>(ordered);
                selected.Clear();
                ordered.Clear();
            }

            foreach (WorldObjectId id in ids)
            {
                if (id.IsNone || selected.Contains(id))
                {
                    continue;
                }

                selected.Add(id);
                ordered.Add(id);
                added.Add(id);
            }

            if (added.Count > 0 || (removed != null && removed.Count > 0))
            {
                Raise(removed ?? new List<WorldObjectId>(), added);
            }
        }

        public void Deselect(WorldObjectId id)
        {
            if (!selected.Remove(id))
            {
                return;
            }

            ordered.Remove(id);
            Raise(new List<WorldObjectId> { id }, new List<WorldObjectId>());
        }

        public void Toggle(WorldObjectId id)
        {
            if (id.IsNone)
            {
                return;
            }

            if (selected.Contains(id))
            {
                Deselect(id);
            }
            else
            {
                Select(id);
            }
        }

        public void Clear()
        {
            if (selected.Count == 0)
            {
                return;
            }

            List<WorldObjectId> removed = new List<WorldObjectId>(ordered);
            selected.Clear();
            ordered.Clear();
            Raise(removed, new List<WorldObjectId>());
        }

        private void Raise(List<WorldObjectId> removed, List<WorldObjectId> added)
        {
            if ((removed == null || removed.Count == 0) && (added == null || added.Count == 0))
            {
                return;
            }

            SelectionChanged?.Invoke(new WorldSelectionChanged(added ?? new List<WorldObjectId>(), removed ?? new List<WorldObjectId>()));
        }
    }
}
