using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Services
{
    public abstract class GameServiceBase : IGameService
    {
        private static readonly IReadOnlyList<Type> EmptyDependencies = Array.Empty<Type>();

        public virtual string ServiceName => GetType().Name;
        public virtual int Priority => 100;
        public ServiceState State { get; private set; } = ServiceState.Registered;
        public bool IsInitialized => State == ServiceState.Initialized || State == ServiceState.Running || State == ServiceState.Paused;
        public virtual IReadOnlyList<Type> Dependencies => EmptyDependencies;

        public void Initialize(IServiceRegistry services)
        {
            if (State != ServiceState.Registered)
            {
                throw new InvalidOperationException($"{ServiceName} cannot initialize from state {State}.");
            }

            Transition(ServiceState.Initializing);
            ExecuteLifecycleStep(() => OnInitialize(services), ServiceState.Initialized);
        }

        public void Start()
        {
            if (State != ServiceState.Initialized)
            {
                throw new InvalidOperationException($"{ServiceName} cannot start from state {State}.");
            }

            Transition(ServiceState.Starting);
            ExecuteLifecycleStep(OnStart, ServiceState.Running);
        }

        public void Tick(float deltaTime)
        {
            if (State == ServiceState.Running)
            {
                OnTick(deltaTime);
            }
        }

        public void FixedTick(float deltaTime)
        {
            if (State == ServiceState.Running)
            {
                OnFixedTick(deltaTime);
            }
        }

        public void LateTick(float deltaTime)
        {
            if (State == ServiceState.Running)
            {
                OnLateTick(deltaTime);
            }
        }

        public void Pause()
        {
            if (State != ServiceState.Running)
            {
                throw new InvalidOperationException($"{ServiceName} cannot pause from state {State}.");
            }

            OnPause();
            Transition(ServiceState.Paused);
        }

        public void Resume()
        {
            if (State != ServiceState.Paused)
            {
                throw new InvalidOperationException($"{ServiceName} cannot resume from state {State}.");
            }

            OnResume();
            Transition(ServiceState.Running);
        }

        public void Shutdown()
        {
            if (State != ServiceState.Initialized && State != ServiceState.Running && State != ServiceState.Paused && State != ServiceState.Failed)
            {
                throw new InvalidOperationException($"{ServiceName} cannot shutdown from state {State}.");
            }

            Transition(ServiceState.ShuttingDown);
            ExecuteLifecycleStep(OnShutdown, ServiceState.Disposed);
        }

        public void Dispose()
        {
            if (State == ServiceState.Disposed)
            {
                return;
            }

            if (State != ServiceState.ShuttingDown && State != ServiceState.Failed)
            {
                Shutdown();
                return;
            }

            Transition(ServiceState.Disposed);
        }

        public void Fail(Exception exception)
        {
            Transition(ServiceState.Failed);
        }

        protected virtual void OnInitialize(IServiceRegistry services)
        {
        }

        protected virtual void OnStart()
        {
        }

        protected virtual void OnTick(float deltaTime)
        {
        }

        protected virtual void OnFixedTick(float deltaTime)
        {
        }

        protected virtual void OnLateTick(float deltaTime)
        {
        }

        protected virtual void OnPause()
        {
        }

        protected virtual void OnResume()
        {
        }

        protected virtual void OnShutdown()
        {
        }

        private void ExecuteLifecycleStep(Action action, ServiceState successState)
        {
            try
            {
                action();
                Transition(successState);
            }
            catch
            {
                Transition(ServiceState.Failed);
                throw;
            }
        }

        private void Transition(ServiceState nextState)
        {
            State = nextState;
        }
    }
}
