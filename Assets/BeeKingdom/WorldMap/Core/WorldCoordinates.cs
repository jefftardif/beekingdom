using System;
using System.Collections.Generic;

namespace BeeKingdom.WorldMap
{
    // Position continue en unites-monde (camera, calculs). Les objets du monde, eux,
    // utilisent toujours WorldPosition (entiers longs).
    public readonly struct WorldVector2 : IEquatable<WorldVector2>
    {
        public double X { get; }
        public double Y { get; }

        public WorldVector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static WorldVector2 operator +(WorldVector2 left, WorldVector2 right)
        {
            return new WorldVector2(left.X + right.X, left.Y + right.Y);
        }

        public static WorldVector2 operator -(WorldVector2 left, WorldVector2 right)
        {
            return new WorldVector2(left.X - right.X, left.Y - right.Y);
        }

        public static WorldVector2 operator *(WorldVector2 value, double scalar)
        {
            return new WorldVector2(value.X * scalar, value.Y * scalar);
        }

        public static WorldVector2 operator /(WorldVector2 value, double scalar)
        {
            return new WorldVector2(value.X / scalar, value.Y / scalar);
        }

        public bool Equals(WorldVector2 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldVector2 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "(" + X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
        }
    }

    // Position autoritative du monde : entiers longs, partagee par tous les objets
    // du monde. L'echelle est infinie pour les besoins du jeu (long).
    public readonly struct WorldPosition : IEquatable<WorldPosition>
    {
        public long X { get; }
        public long Y { get; }

        public WorldPosition(long x, long y)
        {
            X = x;
            Y = y;
        }

        public static WorldPosition operator +(WorldPosition left, WorldPosition right)
        {
            return new WorldPosition(left.X + right.X, left.Y + right.Y);
        }

        public static WorldPosition operator -(WorldPosition left, WorldPosition right)
        {
            return new WorldPosition(left.X - right.X, left.Y - right.Y);
        }

        public static WorldPosition FromVector2(WorldVector2 value)
        {
            return new WorldPosition((long)Math.Round(value.X), (long)Math.Round(value.Y));
        }

        public bool Equals(WorldPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "(" + X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
        }
    }

    // Coordonnee de chunk de la carte (entiers longs, monde potentiellement infini).
    public readonly struct ChunkCoordinate : IEquatable<ChunkCoordinate>
    {
        public long X { get; }
        public long Y { get; }

        public ChunkCoordinate(long x, long y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(ChunkCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is ChunkCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "chunk(" + X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
        }
    }

    // Coordonnee de tuile (grille fine de la carte, sous-chunk).
    public readonly struct TileCoordinate : IEquatable<TileCoordinate>
    {
        public long X { get; }
        public long Y { get; }

        public TileCoordinate(long x, long y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(TileCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is TileCoordinate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "tile(" + X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
        }
    }

    // Systeme de coordonnees mondiales : conversions monde <-> chunk <-> tuile.
    // Pure fonction arithmetique, sans etat ni dependance Unity.
    public static class WorldCoordinateSystem
    {
        public static long FloorDiv(long value, long divisor)
        {
            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(divisor), "The divisor must be strictly positive.");
            }

            long quotient = value / divisor;
            if (value % divisor < 0)
            {
                quotient--;
            }

            return quotient;
        }

        public static long FloorMod(long value, long divisor)
        {
            long remainder = value % divisor;
            if (remainder < 0)
            {
                remainder += divisor;
            }

            return remainder;
        }

        public static ChunkCoordinate ChunkOf(WorldPosition position, long chunkSize)
        {
            ValidateChunkSize(chunkSize);
            return new ChunkCoordinate(FloorDiv(position.X, chunkSize), FloorDiv(position.Y, chunkSize));
        }

        public static WorldPosition ChunkOrigin(ChunkCoordinate chunk, long chunkSize)
        {
            ValidateChunkSize(chunkSize);
            return new WorldPosition(chunk.X * chunkSize, chunk.Y * chunkSize);
        }

        // Position locale au chunk, dans [0, chunkSize) x [0, chunkSize).
        public static WorldPosition LocalWithinChunk(WorldPosition position, ChunkCoordinate chunk, long chunkSize)
        {
            WorldPosition origin = ChunkOrigin(chunk, chunkSize);
            return new WorldPosition(position.X - origin.X, position.Y - origin.Y);
        }

        public static bool ChunkContains(WorldPosition position, ChunkCoordinate chunk, long chunkSize)
        {
            return ChunkOf(position, chunkSize).Equals(chunk);
        }

        public static TileCoordinate TileOf(WorldPosition position, long tileSize)
        {
            ValidateTileSize(tileSize);
            return new TileCoordinate(FloorDiv(position.X, tileSize), FloorDiv(position.Y, tileSize));
        }

        public static WorldPosition TileOrigin(TileCoordinate tile, long tileSize)
        {
            ValidateTileSize(tileSize);
            return new WorldPosition(tile.X * tileSize, tile.Y * tileSize);
        }

        public static ChunkCoordinate ChunkOfTile(TileCoordinate tile, long tilesPerChunk)
        {
            if (tilesPerChunk <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tilesPerChunk), "The tiles per chunk must be strictly positive.");
            }

            return new ChunkCoordinate(FloorDiv(tile.X, tilesPerChunk), FloorDiv(tile.Y, tilesPerChunk));
        }

        // Cle compacte de tuile (index dans un chunk local, coordonnees dans la
        // plage encodable [-2^31, 2^31-1]). Pour les index de contenu, la grille
        // utilise des dictionnaires a cle TileCoordinate : sans collision, le monde
        // est infini. Cette API sert aux index denses optimises.
        public static long PackTileIndex(long tileX, long tileY)
        {
            return ((tileX & 0xFFFFFFFFL) << 32) | (tileY & 0xFFFFFFFFL);
        }

        public static TileCoordinate UnpackTileIndex(long packed)
        {
            long x = (packed >> 32) & 0xFFFFFFFFL;
            long y = packed & 0xFFFFFFFFL;
            if ((x & 0x80000000L) != 0) x -= 0x100000000L;
            if ((y & 0x80000000L) != 0) y -= 0x100000000L;
            return new TileCoordinate(x, y);
        }

        public static long ChebyshevDistance(ChunkCoordinate left, ChunkCoordinate right)
        {
            return Math.Max(Math.Abs(left.X - right.X), Math.Abs(left.Y - right.Y));
        }

        public static long ManhattanDistance(ChunkCoordinate left, ChunkCoordinate right)
        {
            return Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y);
        }

        // Enumere le carre (2*radius+1)^2 centre sur center, du plus proche au plus
        // lointain (ordre par distance de Chebyshev puis par distance euclidienne
        // approchee, pour un chargement prioritaire au centre).
        public static IEnumerable<ChunkCoordinate> ChunksInRadiusByDistance(ChunkCoordinate center, long radius)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "The radius must be positive.");
            }

            List<ChunkCoordinate> ordered = new List<ChunkCoordinate>((int)((2 * radius + 1) * (2 * radius + 1)));
            for (long dx = -radius; dx <= radius; dx++)
            {
                for (long dy = -radius; dy <= radius; dy++)
                {
                    ordered.Add(new ChunkCoordinate(center.X + dx, center.Y + dy));
                }
            }

            ordered.Sort((ChunkCoordinate left, ChunkCoordinate right) =>
            {
                long leftCheb = ChebyshevDistance(center, left);
                long rightCheb = ChebyshevDistance(center, right);
                int chebCompare = leftCheb.CompareTo(rightCheb);
                if (chebCompare != 0)
                {
                    return chebCompare;
                }

                long leftEuclid = (left.X - center.X) * (left.X - center.X) + (left.Y - center.Y) * (left.Y - center.Y);
                long rightEuclid = (right.X - center.X) * (right.X - center.X) + (right.Y - center.Y) * (right.Y - center.Y);
                return leftEuclid.CompareTo(rightEuclid);
            });

            return ordered;
        }

        private static void ValidateChunkSize(long chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "The chunk size must be strictly positive.");
            }
        }

        private static void ValidateTileSize(long tileSize)
        {
            if (tileSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tileSize), "The tile size must be strictly positive.");
            }
        }
    }
}
