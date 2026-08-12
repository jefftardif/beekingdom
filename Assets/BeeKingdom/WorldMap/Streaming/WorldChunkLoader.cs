using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.WorldMap
{
    // Charge et decharge les chunks via la source de contenu, avec capacites
    // concurrentes bornees, deduplication des demandes et annulation propre.
    public sealed class WorldChunkLoader
    {
        private readonly WorldGrid grid;
        private readonly IWorldChunkContentSource source;
        private readonly StreamingSettings settings;
        private readonly SemaphoreSlim loadGate;
        private readonly SemaphoreSlim unloadGate;
        private readonly object sync = new object();
        private readonly Dictionary<ChunkCoordinate, Task<WorldChunk>> inFlightLoads = new Dictionary<ChunkCoordinate, Task<WorldChunk>>();
        private readonly Dictionary<ChunkCoordinate, Task> inFlightUnloads = new Dictionary<ChunkCoordinate, Task>();

        public event Action<ChunkCoordinate> LoadStarted;
        public event Action<WorldChunk> LoadCompleted;
        public event Action<ChunkCoordinate> LoadFailed;
        public event Action<WorldChunk> UnloadCompleted;

        public int InFlightLoadCount
        {
            get
            {
                lock (sync)
                {
                    return inFlightLoads.Count;
                }
            }
        }

        public int InFlightUnloadCount
        {
            get
            {
                lock (sync)
                {
                    return inFlightUnloads.Count;
                }
            }
        }

        public WorldChunkLoader(WorldGrid grid, IWorldChunkContentSource source, StreamingSettings settings)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            loadGate = new SemaphoreSlim(settings.MaxConcurrentLoads, settings.MaxConcurrentLoads);
            unloadGate = new SemaphoreSlim(settings.MaxConcurrentUnloads, settings.MaxConcurrentUnloads);
        }

        public Task<WorldChunk> LoadChunkAsync(ChunkCoordinate coordinate, CancellationToken cancellationToken = default)
        {
            if (grid.TryGetChunk(coordinate, out WorldChunk existing))
            {
                if (existing.State == WorldChunkState.Loaded)
                {
                    return Task.FromResult(existing);
                }

                if (existing.State == WorldChunkState.Unloading && TryGetPendingUnload(coordinate, out Task pendingUnload))
                {
                    return LoadAfterUnloadAsync(coordinate, pendingUnload, cancellationToken);
                }
            }

            if (TryGetPendingLoad(coordinate, out Task<WorldChunk> pending))
            {
                return pending;
            }

            Task<WorldChunk> load = RunTrackedLoadAsync(coordinate, cancellationToken);
            LoadStarted?.Invoke(coordinate);
            return load;
        }

        private bool TryGetPendingLoad(ChunkCoordinate coordinate, out Task<WorldChunk> pending)
        {
            lock (sync)
            {
                return inFlightLoads.TryGetValue(coordinate, out pending);
            }
        }

        public Task UnloadChunkAsync(WorldChunk chunk, CancellationToken cancellationToken = default)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            if (chunk.State != WorldChunkState.Loaded)
            {
                return Task.CompletedTask;
            }

            if (TryGetPendingUnload(chunk.Coordinate, out Task pending))
            {
                return pending;
            }

            Task unload = RunTrackedUnloadAsync(chunk, cancellationToken);
            return unload;
        }

        private bool TryGetPendingUnload(ChunkCoordinate coordinate, out Task pending)
        {
            lock (sync)
            {
                return inFlightUnloads.TryGetValue(coordinate, out pending);
            }
        }

        // Enregistre la tache dans inFlightLoads avant que le corps ne puisse se
        // completer : l'Add et le Remove sont dans le meme flux synchrone, donc
        // une completion inline du corps ne peut ni retirer une entree jamais
        // ajoutee, ni laisser une tache terminee coincer DrainAsync. Le corps
        // (LoadCoreAsync) s'execute inline jusqu'a sa premiere suspension, ce
        // qui preserve le contrat synchrone de LoadChunkAsync (source appelee
        // des le retour de l'appel).
        private async Task<WorldChunk> RunTrackedLoadAsync(ChunkCoordinate coordinate, CancellationToken cancellationToken)
        {
            Task<WorldChunk> task = LoadCoreAsync(coordinate, cancellationToken);
            lock (sync)
            {
                inFlightLoads.Add(coordinate, task);
            }

            try
            {
                return await task.ConfigureAwait(false);
            }
            finally
            {
                lock (sync)
                {
                    inFlightLoads.Remove(coordinate);
                }
            }
        }

        private async Task RunTrackedUnloadAsync(WorldChunk chunk, CancellationToken cancellationToken)
        {
            Task task = UnloadCoreAsync(chunk, cancellationToken);
            lock (sync)
            {
                inFlightUnloads.Add(chunk.Coordinate, task);
            }

            try
            {
                await task.ConfigureAwait(false);
            }
            finally
            {
                lock (sync)
                {
                    inFlightUnloads.Remove(chunk.Coordinate);
                }
            }
        }

        // Attend la fin de toutes les operations en vol (tests, arret propre).
        public async Task DrainAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                List<Task> snapshot;
                lock (sync)
                {
                    if (inFlightLoads.Count == 0 && inFlightUnloads.Count == 0)
                    {
                        return;
                    }

                    snapshot = new List<Task>(inFlightLoads.Count + inFlightUnloads.Count);
                    snapshot.AddRange(inFlightLoads.Values);
                    snapshot.AddRange(inFlightUnloads.Values);
                }

                await Task.WhenAll(snapshot).ConfigureAwait(false);
            }
        }

        private async Task<WorldChunk> LoadAfterUnloadAsync(ChunkCoordinate coordinate, Task pendingUnload, CancellationToken cancellationToken)
        {
            await pendingUnload.ConfigureAwait(false);
            return await LoadChunkAsync(coordinate, cancellationToken).ConfigureAwait(false);
        }

        private async Task<WorldChunk> LoadCoreAsync(ChunkCoordinate coordinate, CancellationToken cancellationToken)
        {
            WorldChunk chunk = grid.GetOrCreateChunk(coordinate);
            chunk.SetState(WorldChunkState.Loading);
            await loadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (chunk.State == WorldChunkState.Loaded)
                {
                    return chunk;
                }

                WorldChunkContent content = await source.LoadAsync(coordinate, grid.ChunkSize, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                chunk.Content = content;
                foreach (WorldObject worldObject in content.Objects)
                {
                    if (grid.TryGetObject(worldObject.Id, out _))
                    {
                        continue;
                    }

                    if (!WorldCoordinateSystem.ChunkOf(worldObject.Position, grid.ChunkSize).Equals(coordinate))
                    {
                        continue;
                    }

                    grid.RegisterObject(worldObject);
                }

                foreach (KeyValuePair<TileCoordinate, WorldTile> tile in content.Tiles)
                {
                    WorldTile value = tile.Value;
                    chunk.SetTile(value.Coordinate, value);
                }

                chunk.SetState(WorldChunkState.Loaded);
                LoadCompleted?.Invoke(chunk);
                return chunk;
            }
            catch (OperationCanceledException)
            {
                chunk.SetState(WorldChunkState.Unloaded);
                grid.RemoveChunk(coordinate);
                throw;
            }
            catch (Exception)
            {
                chunk.SetState(WorldChunkState.Unloaded);
                grid.RemoveChunk(coordinate);
                LoadFailed?.Invoke(coordinate);
                throw;
            }
            finally
            {
                loadGate.Release();
            }
        }

        private async Task UnloadCoreAsync(WorldChunk chunk, CancellationToken cancellationToken)
        {
            chunk.SetState(WorldChunkState.Unloading);
            await unloadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                WorldChunkContent content = chunk.Content;
                foreach (WorldObject worldObject in new List<WorldObject>(chunk.Objects))
                {
                    grid.UnregisterObject(worldObject);
                }

                chunk.ResetContent();
                if (content != null)
                {
                    source.Unload(chunk.Coordinate, content);
                }

                chunk.SetState(WorldChunkState.Unloaded);
                grid.RemoveChunk(chunk.Coordinate);
                UnloadCompleted?.Invoke(chunk);
            }
            finally
            {
                unloadGate.Release();
            }
        }
    }
}
