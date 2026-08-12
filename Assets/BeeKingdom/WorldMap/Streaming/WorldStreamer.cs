using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.WorldMap
{
    // Fournit la position de focus du streaming (la camera l'implemente).
    public interface IWorldFocusProvider
    {
        WorldPosition FocusPosition { get; }
    }

    // Pilote le chargement/dechargement des chunks autour du focus. Le monde n'est
    // jamais entierement charge : uniquement un carre de chunks autour de la camera,
    // avec hysteresis (UnloadRadius > LoadRadius) pour eviter les allees-retours.
    public sealed class WorldStreamer
    {
        private readonly WorldGrid grid;
        private readonly WorldChunkLoader loader;
        private readonly StreamingSettings settings;
        private readonly IWorldFocusProvider focus;
        private readonly List<ChunkCoordinate> desiredScratch = new List<ChunkCoordinate>();

        public int LoadedChunkCount => grid.ChunkCount;
        public int PendingLoadCount => loader.InFlightLoadCount;
        public int PendingUnloadCount => loader.InFlightUnloadCount;
        public ChunkCoordinate LastFocusChunk { get; private set; }

        public event Action<ChunkCoordinate> ChunkLoadRequested;
        public event Action<ChunkCoordinate> ChunkUnloadRequested;

        public WorldStreamer(WorldGrid grid, WorldChunkLoader loader, StreamingSettings settings, IWorldFocusProvider focus)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.focus = focus ?? throw new ArgumentNullException(nameof(focus));
        }

        // Appeler a chaque frame : recalcule l'ensemble desire autour du focus.
        public void Tick()
        {
            ChunkCoordinate focusChunk = grid.ChunkOf(focus.FocusPosition);
            LastFocusChunk = focusChunk;

            desiredScratch.Clear();
            foreach (ChunkCoordinate desired in WorldCoordinateSystem.ChunksInRadiusByDistance(focusChunk, settings.LoadRadius))
            {
                desiredScratch.Add(desired);
                if (!grid.IsChunkLoaded(desired))
                {
                    loader.LoadChunkAsync(desired);
                    ChunkLoadRequested?.Invoke(desired);
                }
            }

            // Dechargement : chunks charges hors du rayon, du plus lointain au plus proche.
            List<WorldChunk> toUnload = null;
            foreach (WorldChunk chunk in grid.Chunks)
            {
                if (WorldCoordinateSystem.ChebyshevDistance(focusChunk, chunk.Coordinate) > settings.UnloadRadius)
                {
                    if (toUnload == null)
                    {
                        toUnload = new List<WorldChunk>();
                    }

                    toUnload.Add(chunk);
                }
            }

            if (toUnload == null)
            {
                return;
            }

            toUnload.Sort((WorldChunk left, WorldChunk right) =>
                WorldCoordinateSystem.ChebyshevDistance(focusChunk, right.Coordinate)
                    .CompareTo(WorldCoordinateSystem.ChebyshevDistance(focusChunk, left.Coordinate)));
            foreach (WorldChunk chunk in toUnload)
            {
                loader.UnloadChunkAsync(chunk);
                ChunkUnloadRequested?.Invoke(chunk.Coordinate);
            }
        }

        // Decharge tout (changement de monde, arret propre). Ne bloque pas : appeler
        // DrainAsync pour attendre la fin des operations.
        public void UnloadAll()
        {
            List<WorldChunk> loaded = new List<WorldChunk>(grid.Chunks);
            loaded.Sort((WorldChunk left, WorldChunk right) => left.Coordinate.ToString().CompareTo(right.Coordinate.ToString()));
            foreach (WorldChunk chunk in loaded)
            {
                if (chunk.State == WorldChunkState.Loaded)
                {
                    loader.UnloadChunkAsync(chunk);
                    ChunkUnloadRequested?.Invoke(chunk.Coordinate);
                }
            }
        }

        public Task DrainAsync(CancellationToken cancellationToken = default)
        {
            return loader.DrainAsync(cancellationToken);
        }
    }
}
