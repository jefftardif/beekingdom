using System;

namespace BeeKingdom.WorldMap
{
    public enum WorldLodLevel
    {
        Lod0 = 0,
        Lod1 = 1,
        Lod2 = 2,
        Culled = 3
    }

    // Niveaux de detail de la carte. Aucune optimisation graphique n'est encore
    // appliquee : ce systeme est la base que les rendus futurs consommeront.
    public sealed class WorldLOD
    {
        private readonly LodSettings settings;

        public WorldLOD(LodSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public WorldLodLevel Evaluate(WorldPosition focus, WorldPosition target)
        {
            return EvaluateDistance(Math.Sqrt(
                (double)(target.X - focus.X) * (target.X - focus.X) +
                (double)(target.Y - focus.Y) * (target.Y - focus.Y)));
        }

        public WorldLodLevel Evaluate(WorldVector2 focus, WorldVector2 target)
        {
            double dx = target.X - focus.X;
            double dy = target.Y - focus.Y;
            return EvaluateDistance(Math.Sqrt(dx * dx + dy * dy));
        }

        // Distance au rectangle du chunk (0 si le focus est dedans).
        public WorldLodLevel EvaluateChunk(WorldPosition focus, ChunkCoordinate chunk, long chunkSize)
        {
            WorldPosition origin = WorldCoordinateSystem.ChunkOrigin(chunk, chunkSize);
            double dx = Math.Max(0d, Math.Max((double)origin.X - focus.X, (double)focus.X - (origin.X + chunkSize)));
            double dy = Math.Max(0d, Math.Max((double)origin.Y - focus.Y, (double)focus.Y - (origin.Y + chunkSize)));
            return EvaluateDistance(Math.Sqrt(dx * dx + dy * dy));
        }

        private WorldLodLevel EvaluateDistance(double distance)
        {
            if (distance <= settings.NearDistance)
            {
                return WorldLodLevel.Lod0;
            }

            if (distance <= settings.MidDistance)
            {
                return WorldLodLevel.Lod1;
            }

            if (distance <= settings.FarDistance)
            {
                return WorldLodLevel.Lod2;
            }

            return WorldLodLevel.Culled;
        }
    }
}
