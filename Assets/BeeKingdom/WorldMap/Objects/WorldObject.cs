using System;
using System.Threading;

namespace BeeKingdom.WorldMap
{
    public enum WorldObjectKind
    {
        // Base volontairement vide : les types concrets (ruche, joueur, ressource,
        // insecte, boss, evenement, batiment mondial, portail, merveille...) arriveront
        // dans leurs sprints respectifs, par dessus cette architecture.
        None = 0
    }

    // Identifiant stable d'un objet du monde. Les ids explicites (serie, telemetrie)
    // sont supportes via Fixed ; New() genere un id unique par processus.
    public readonly struct WorldObjectId : IEquatable<WorldObjectId>
    {
        private static long nextValue;

        public long Value { get; }

        public WorldObjectId(long value)
        {
            Value = value;
        }

        public bool IsNone => Value == 0;

        public static WorldObjectId New()
        {
            return new WorldObjectId(Interlocked.Increment(ref nextValue));
        }

        public static WorldObjectId Fixed(long value)
        {
            return new WorldObjectId(value);
        }

        public bool Equals(WorldObjectId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldObjectId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return "obj(" + Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
        }
    }

    // Objet generique du monde. Aucune specialisation : les futurs systemes
    // l'etendront par composition (vue via le pool, position autoritative monde).
    public sealed class WorldObject
    {
        public WorldObjectId Id { get; }
        public WorldObjectKind Kind { get; }
        public string Tag { get; }
        public WorldPosition Position { get; private set; }
        public WorldChunk Chunk { get; internal set; }
        public bool IsActive { get; private set; }

        public event Action<WorldObject, WorldPosition, WorldPosition> PositionChanged;
        public event Action<WorldObject, bool> ActiveChanged;

        public WorldObject(WorldObjectId id, WorldObjectKind kind, WorldPosition position, string tag = null)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Tag = tag;
            IsActive = true;
        }

        public void MoveTo(WorldPosition position)
        {
            if (Position.Equals(position))
            {
                return;
            }

            WorldPosition previous = Position;
            Position = position;
            PositionChanged?.Invoke(this, previous, position);
        }

        public void SetActive(bool active)
        {
            if (IsActive == active)
            {
                return;
            }

            IsActive = active;
            ActiveChanged?.Invoke(this, active);
        }
    }
}
