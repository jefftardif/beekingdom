using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.WorldMap;
using UnityEngine;

namespace BeeKingdom.Tests.Editor
{
    // Source de contenu scriptee pour les tests du streaming : chaque chargement
    // attend un signal explicite (TaskCompletionSource), ce qui rend les tests
    // deterministes sans temps reel.
    internal sealed class ScriptedContentSource : IWorldChunkContentSource
    {
        private readonly Func<ChunkCoordinate, long, WorldChunkContent> contentFactory;
        private readonly Dictionary<ChunkCoordinate, TaskCompletionSource<WorldChunkContent>> pending = new Dictionary<ChunkCoordinate, TaskCompletionSource<WorldChunkContent>>();
        private readonly List<ChunkCoordinate> loadCalls = new List<ChunkCoordinate>();
        private readonly List<ChunkCoordinate> unloadCalls = new List<ChunkCoordinate>();

        public IReadOnlyList<ChunkCoordinate> LoadCalls => loadCalls;
        public IReadOnlyList<ChunkCoordinate> UnloadCalls => unloadCalls;
        public int UnloadCallCount => unloadCalls.Count;

        public ScriptedContentSource(Func<ChunkCoordinate, long, WorldChunkContent> contentFactory = null)
        {
            this.contentFactory = contentFactory ?? ((ChunkCoordinate chunk, long size) => new WorldChunkContent());
        }

        public Task<WorldChunkContent> LoadAsync(ChunkCoordinate chunk, long chunkSize, CancellationToken cancellationToken)
        {
            loadCalls.Add(chunk);
            TaskCompletionSource<WorldChunkContent> completion = new TaskCompletionSource<WorldChunkContent>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending[chunk] = completion;
            return completion.Task;
        }

        public void CompleteNext(ChunkCoordinate chunk)
        {
            pending[chunk].SetResult(contentFactory(chunk, 64L));
        }

        public void CompleteAll()
        {
            foreach (KeyValuePair<ChunkCoordinate, TaskCompletionSource<WorldChunkContent>> entry in new List<KeyValuePair<ChunkCoordinate, TaskCompletionSource<WorldChunkContent>>>(pending))
            {
                entry.Value.SetResult(contentFactory(entry.Key, 64L));
            }

            pending.Clear();
        }

        public void FailNext(ChunkCoordinate chunk)
        {
            pending[chunk].SetException(new InvalidOperationException("scripted failure"));
        }

        public void Unload(ChunkCoordinate chunk, WorldChunkContent content)
        {
            unloadCalls.Add(chunk);
        }
    }

    internal sealed class ManualFocusProvider : IWorldFocusProvider
    {
        public WorldPosition FocusPosition { get; set; }

        public ManualFocusProvider(WorldPosition position)
        {
            FocusPosition = position;
        }
    }

    internal sealed class FakeWorldObjectView : IWorldObjectView
    {
        public WorldObject Owner { get; private set; }
        public WorldPosition LastPosition { get; private set; }
        public bool LastVisible { get; private set; }
        public int AttachCount { get; private set; }
        public int DetachCount { get; private set; }

        public void Attach(WorldObject owner)
        {
            Owner = owner;
            AttachCount++;
        }

        public void Detach(WorldObject owner)
        {
            if (Owner == owner)
            {
                Owner = null;
            }

            DetachCount++;
        }

        public void SetWorldPosition(WorldPosition position)
        {
            LastPosition = position;
        }

        public void SetVisible(bool visible)
        {
            LastVisible = visible;
        }
    }

    internal sealed class MemoryWorldMapSaveStore : IWorldMapSaveStore
    {
        private readonly Dictionary<string, string> entries = new Dictionary<string, string>();

        public string Read(string key)
        {
            return entries.TryGetValue(key, out string json) ? json : null;
        }

        public void Write(string key, string json)
        {
            entries[key] = json;
        }

        public void Delete(string key)
        {
            entries.Remove(key);
        }
    }

    internal sealed class FakeInputSource : IWorldInputSource
    {
        public bool PrimaryDown { get; set; }
        public Vector2 PrimaryPosition { get; set; }
        public Vector2 ScreenSize { get; set; } = new Vector2(1080f, 1920f);
        public float ScrollDelta { get; set; }
        public bool PinchActive { get; set; }
        public float PinchRatio { get; set; } = 1f;
        public Vector2 PinchPivot { get; set; }
        public bool MoveLeft { get; set; }
        public bool MoveRight { get; set; }
        public bool MoveUp { get; set; }
        public bool MoveDown { get; set; }
    }

    internal sealed class FakeInputClock : IWorldInputClock
    {
        public double NowSeconds { get; set; }

        public FakeInputClock(double now = 0d)
        {
            NowSeconds = now;
        }
    }

    internal static class WorldMapTestConfiguration
    {
        public static WorldConfiguration Create(long chunkSize = 64L, int loadRadius = 2, int unloadRadius = 4)
        {
            return new WorldConfiguration(
                chunkSize,
                tileSize: 1,
                streaming: new StreamingSettings(loadRadius, unloadRadius, maxConcurrentLoads: 256, maxConcurrentUnloads: 256),
                camera: new CameraSettings(),
                lod: new LodSettings(),
                pool: new PoolSettings());
        }
    }
}
