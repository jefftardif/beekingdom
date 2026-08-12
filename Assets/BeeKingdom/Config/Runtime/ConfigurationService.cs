using System;
using System.Collections.Generic;
using BeeKingdom.Config.Loaders;
using BeeKingdom.Config.Validators;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Config.Runtime
{
    public sealed class ConfigurationService : IConfigurationService
    {
        private readonly IConfigurationLoader loader;
        private readonly ConfigurationValidator validator;
        private readonly ConfigurationCache cache;

        public string ServiceName => nameof(ConfigurationService);
        public int Priority => 10;
        public ServiceState State { get; private set; } = ServiceState.Registered;
        public bool IsInitialized => State == ServiceState.Initialized || State == ServiceState.Running || State == ServiceState.Paused;
        public IReadOnlyList<Type> Dependencies => Array.Empty<Type>();
        public ConfigurationLoadResult LastLoadResult { get; private set; }

        public ConfigurationService(IConfigurationLoader loader, ConfigurationValidator validator, ConfigurationCache cache)
        {
            this.loader = loader;
            this.validator = validator;
            this.cache = cache;
        }

        public void Initialize(IServiceRegistry services)
        {
            if (State != ServiceState.Registered)
            {
                throw new InvalidOperationException($"{ServiceName} cannot initialize from state {State}.");
            }

            State = ServiceState.Initializing;
            try
            {
                Reload();
                State = ServiceState.Initialized;
            }
            catch
            {
                State = ServiceState.Failed;
                throw;
            }
        }

        public void Start()
        {
            if (State != ServiceState.Initialized)
            {
                throw new InvalidOperationException($"{ServiceName} cannot start from state {State}.");
            }

            State = ServiceState.Running;
        }

        public void Tick(float deltaTime)
        {
        }

        public void FixedTick(float deltaTime)
        {
        }

        public void LateTick(float deltaTime)
        {
        }

        public void Pause()
        {
            if (State != ServiceState.Running)
            {
                throw new InvalidOperationException($"{ServiceName} cannot pause from state {State}.");
            }

            State = ServiceState.Paused;
        }

        public void Resume()
        {
            if (State != ServiceState.Paused)
            {
                throw new InvalidOperationException($"{ServiceName} cannot resume from state {State}.");
            }

            State = ServiceState.Running;
        }

        public void Shutdown()
        {
            if (State != ServiceState.Initialized && State != ServiceState.Running && State != ServiceState.Paused && State != ServiceState.Failed)
            {
                throw new InvalidOperationException($"{ServiceName} cannot shutdown from state {State}.");
            }

            State = ServiceState.ShuttingDown;
            State = ServiceState.Disposed;
        }

        public void Dispose()
        {
            if (State != ServiceState.Disposed)
            {
                Shutdown();
            }
        }

        public void Fail(Exception exception)
        {
            State = ServiceState.Failed;
        }

        public TDefinition GetById<TDefinition>(ConfigurationId id) where TDefinition : class, IConfigurationDefinition
        {
            return cache.GetById<TDefinition>(id);
        }

        public bool TryGet<TDefinition>(ConfigurationId id, out TDefinition definition) where TDefinition : class, IConfigurationDefinition
        {
            return cache.TryGet(id, out definition);
        }

        public IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : class, IConfigurationDefinition
        {
            return cache.GetAll<TDefinition>();
        }

        public ConfigurationLoadResult Reload()
        {
            IReadOnlyList<IConfigurationDefinition> definitions = loader.LoadDefinitions();
            IReadOnlyList<ConfigurationValidationIssue> issues = validator.Validate(definitions);
            LastLoadResult = new ConfigurationLoadResult(definitions, issues);

            if (!LastLoadResult.HasErrors)
            {
                cache.ReplaceAll(definitions);
            }

            return LastLoadResult;
        }
    }
}
