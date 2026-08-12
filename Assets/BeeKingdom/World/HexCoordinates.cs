using System;

namespace BeeKingdom.World
{
    public readonly struct HexCoordinates : IEquatable<HexCoordinates>
    {
        private static readonly HexCoordinates[] Directions =
        {
            new HexCoordinates(1, 0),
            new HexCoordinates(1, -1),
            new HexCoordinates(0, -1),
            new HexCoordinates(-1, 0),
            new HexCoordinates(-1, 1),
            new HexCoordinates(0, 1)
        };

        public int Q { get; }
        public int R { get; }
        public int S => -Q - R;

        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
        }

        public HexCoordinates Neighbor(int direction)
        {
            HexCoordinates offset = Directions[((direction % 6) + 6) % 6];
            return new HexCoordinates(Q + offset.Q, R + offset.R);
        }

        public int DistanceTo(HexCoordinates other)
        {
            return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
        }

        public WorldChunkCoordinate ToChunkCoordinate(int chunkSize)
        {
            int size = chunkSize <= 0 ? 16 : chunkSize;
            return new WorldChunkCoordinate(FloorDiv(Q, size), FloorDiv(R, size));
        }

        public bool Equals(HexCoordinates other)
        {
            return Q == other.Q && R == other.R;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinates other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Q * 397) ^ R;
            }
        }

        private static int FloorDiv(int value, int divisor)
        {
            int result = value / divisor;
            int remainder = value % divisor;
            if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
            {
                result--;
            }

            return result;
        }
    }
}
