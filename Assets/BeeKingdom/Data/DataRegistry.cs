using System;
using System.Collections.Generic;
using System.Diagnostics;
using BeeKingdom.Config.Runtime;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Data
{
    public sealed class DataRegistry : IDataRegistry
    {
        private readonly RegistryCache cache;
        private readonly RegistryValidator validator;
        private IDataProvider provider;
        private IReadOnlyList<IConfigurationDefinition> definitions = Array.Empty<IConfigurationDefinition>();
        private ServiceState state = ServiceState.Registered;
        private Exception failure;

        public string ServiceName => nameof(DataRegistry);
        public int Priority => 15;
        public ServiceState State => state;
        public bool IsInitialized => state == ServiceState.Initialized || state == ServiceState.Running || state == ServiceState.Paused;
        public IReadOnlyList<Type> Dependencies => new[] { typeof(IConfigurationService) };
        public RegistryDiagnostics Diagnostics { get; } = new RegistryDiagnostics();

        public DataRegistry()
            : this(new RegistryCache(), new RegistryValidator())
        {
        }

        public DataRegistry(RegistryCache cache, RegistryValidator validator)
        {
            this.cache = cache;
            this.validator = validator;
        }

        public void Initialize(IServiceRegistry services)
        {
            state = ServiceState.Initializing;
            provider = new ConfigurationDataProvider(services.Get<IConfigurationService>());
            Reload();
            state = ServiceState.Initialized;
        }

        public void Start() { state = ServiceState.Running; }
        public void Tick(float deltaTime) { }
        public void FixedTick(float deltaTime) { }
        public void LateTick(float deltaTime) { }
        public void Pause() { state = ServiceState.Paused; }
        public void Resume() { state = ServiceState.Running; }
        public void Shutdown() { state = ServiceState.Disposed; }
        public void Dispose() { Shutdown(); }
        public void Fail(Exception exception) { failure = exception; state = ServiceState.Failed; }

        public TDefinition Get<TDefinition>(string id) where TDefinition : class, IConfigurationDefinition
        {
            if (TryGet(id, out TDefinition definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Definition {typeof(TDefinition).Name}/{id} was not found.");
        }

        public bool TryGet<TDefinition>(string id, out TDefinition definition) where TDefinition : class, IConfigurationDefinition
        {
            return cache.Index.TryGet(new ConfigurationId(id), out definition);
        }

        public IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : class, IConfigurationDefinition
        {
            return cache.Index.GetAll<TDefinition>();
        }

        public bool Exists<TDefinition>(string id) where TDefinition : class, IConfigurationDefinition
        {
            return TryGet<TDefinition>(id, out _);
        }

        public RegistryValidationResult Reload()
        {
            long start = Stopwatch.GetTimestamp();
            definitions = provider.LoadDefinitions();
            RegistryValidationResult result = validator.Validate(definitions);
            Diagnostics.RecordValidation(result);

            if (!result.HasErrors)
            {
                cache.ReplaceAll(definitions);
                Diagnostics.RecordLoad(cache.Index.Count, EstimateMemory(definitions), Stopwatch.GetTimestamp() - start);
            }

            return result;
        }

        public RegistryValidationResult Validate()
        {
            RegistryValidationResult result = validator.Validate(definitions);
            Diagnostics.RecordValidation(result);
            return result;
        }

        private static long EstimateMemory(IReadOnlyList<IConfigurationDefinition> loadedDefinitions)
        {
            return loadedDefinitions.Count * 256L;
        }
    }
}
