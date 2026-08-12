using System;
using System.Collections.Generic;

namespace BeeKingdom.Core.Services
{
    /// <summary>
    /// Base contract for infrastructure services managed by the composition root.
    /// Services expose lifecycle only; gameplay systems should depend on narrower interfaces.
    /// </summary>
    public interface IGameService
    {
        string ServiceName { get; }
        int Priority { get; }
        ServiceState State { get; }
        bool IsInitialized { get; }
        IReadOnlyList<Type> Dependencies { get; }
        void Initialize(IServiceRegistry services);
        void Start();
        void Tick(float deltaTime);
        void FixedTick(float deltaTime);
        void LateTick(float deltaTime);
        void Pause();
        void Resume();
        void Shutdown();
        void Dispose();
        void Fail(Exception exception);
    }
}
