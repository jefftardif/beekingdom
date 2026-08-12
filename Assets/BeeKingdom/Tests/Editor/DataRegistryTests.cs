using System;
using System.Collections.Generic;
using BeeKingdom.Config.Runtime;
using BeeKingdom.Core.Services;
using BeeKingdom.Data;
using BeeKingdom.Services;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class DataRegistryTests
    {
        [Test]
        public void RegistryLoadsDefinitionsAndSupportsLookups()
        {
            DataRegistry registry = CreateRegistry(new IConfigurationDefinition[]
            {
                new TestDefinition("bee.worker"),
                new TestDefinition("building.hive")
            });

            Assert.That(registry.Exists<TestDefinition>("bee.worker"), Is.True);
            Assert.That(registry.Get<TestDefinition>("bee.worker").Id.Value, Is.EqualTo("bee.worker"));
            Assert.That(registry.GetAll<TestDefinition>().Count, Is.EqualTo(2));
            Assert.That(registry.Diagnostics.DefinitionCount, Is.EqualTo(2));
        }

        [Test]
        public void ValidatorDetectsDuplicateIds()
        {
            RegistryValidator validator = new RegistryValidator();

            RegistryValidationResult result = validator.Validate(new IConfigurationDefinition[]
            {
                new TestDefinition("duplicate"),
                new TestDefinition("duplicate")
            });

            Assert.That(result.HasErrors, Is.True);
        }

        [Test]
        public void ValidatorDetectsMissingReferences()
        {
            RegistryValidator validator = new RegistryValidator();

            RegistryValidationResult result = validator.Validate(new IConfigurationDefinition[]
            {
                new TestDefinition("building.hive", references: new[] { "resource.missing" })
            });

            Assert.That(result.HasErrors, Is.True);
        }

        [Test]
        public void ValidatorDetectsCircularDependencies()
        {
            RegistryValidator validator = new RegistryValidator();

            RegistryValidationResult result = validator.Validate(new IConfigurationDefinition[]
            {
                new TestDefinition("research.a", dependencies: new[] { "research.b" }),
                new TestDefinition("research.b", dependencies: new[] { "research.a" })
            });

            Assert.That(result.HasErrors, Is.True);
        }

        [Test]
        public void ReloadRebuildsCache()
        {
            FakeConfigurationService configuration = new FakeConfigurationService(new IConfigurationDefinition[]
            {
                new TestDefinition("bee.worker")
            });
            DataRegistry registry = CreateRegistry(configuration);

            configuration.SetDefinitions(new IConfigurationDefinition[]
            {
                new TestDefinition("bee.queen")
            });
            registry.Reload();

            Assert.That(registry.Exists<TestDefinition>("bee.worker"), Is.False);
            Assert.That(registry.Exists<TestDefinition>("bee.queen"), Is.True);
            Assert.That(registry.Diagnostics.ReloadCount, Is.EqualTo(2));
        }

        [Test]
        public void CacheHandlesLargeDefinitionSets()
        {
            List<IConfigurationDefinition> definitions = new List<IConfigurationDefinition>();
            for (int i = 0; i < 5000; i++)
            {
                definitions.Add(new TestDefinition("definition." + i));
            }

            DataRegistry registry = CreateRegistry(definitions);

            Assert.That(registry.Exists<TestDefinition>("definition.4999"), Is.True);
            Assert.That(registry.GetAll<TestDefinition>().Count, Is.EqualTo(5000));
        }

        private static DataRegistry CreateRegistry(IReadOnlyList<IConfigurationDefinition> definitions)
        {
            return CreateRegistry(new FakeConfigurationService(definitions));
        }

        private static DataRegistry CreateRegistry(FakeConfigurationService configuration)
        {
            ServiceContainer container = new ServiceContainer();
            container.Register<IConfigurationService>(configuration);
            DataRegistry registry = new DataRegistry();
            registry.Initialize(container);
            registry.Start();
            return registry;
        }

        private sealed class FakeConfigurationService : IConfigurationService
        {
            private IReadOnlyList<IConfigurationDefinition> definitions;

            public string ServiceName => nameof(FakeConfigurationService);
            public int Priority => 10;
            public ServiceState State { get; private set; } = ServiceState.Running;
            public bool IsInitialized => true;
            public IReadOnlyList<Type> Dependencies => Array.Empty<Type>();
            public ConfigurationLoadResult LastLoadResult { get; private set; }

            public FakeConfigurationService(IReadOnlyList<IConfigurationDefinition> definitions)
            {
                SetDefinitions(definitions);
            }

            public void SetDefinitions(IReadOnlyList<IConfigurationDefinition> newDefinitions)
            {
                definitions = newDefinitions;
                LastLoadResult = new ConfigurationLoadResult(definitions, Array.Empty<ConfigurationValidationIssue>());
            }

            public ConfigurationLoadResult Reload()
            {
                LastLoadResult = new ConfigurationLoadResult(definitions, Array.Empty<ConfigurationValidationIssue>());
                return LastLoadResult;
            }

            public TDefinition GetById<TDefinition>(ConfigurationId id) where TDefinition : class, IConfigurationDefinition
            {
                if (TryGet(id, out TDefinition definition))
                {
                    return definition;
                }

                throw new KeyNotFoundException(id.Value);
            }

            public bool TryGet<TDefinition>(ConfigurationId id, out TDefinition definition) where TDefinition : class, IConfigurationDefinition
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i] is TDefinition typed && typed.Id.Equals(id))
                    {
                        definition = typed;
                        return true;
                    }
                }

                definition = null;
                return false;
            }

            public IReadOnlyList<TDefinition> GetAll<TDefinition>() where TDefinition : class, IConfigurationDefinition
            {
                List<TDefinition> typed = new List<TDefinition>();
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i] is TDefinition definition)
                    {
                        typed.Add(definition);
                    }
                }

                return typed;
            }

            public void Initialize(IServiceRegistry services) { }
            public void Start() { }
            public void Tick(float deltaTime) { }
            public void FixedTick(float deltaTime) { }
            public void LateTick(float deltaTime) { }
            public void Pause() { }
            public void Resume() { }
            public void Shutdown() { }
            public void Dispose() { }
            public void Fail(Exception exception) { State = ServiceState.Failed; }
        }

        private sealed class TestDefinition : IConfigurationDefinition
        {
            private readonly IReadOnlyList<ConfigurationId> references;
            private readonly IReadOnlyList<ConfigurationId> dependencies;

            public ConfigurationId Id { get; }
            public string DisplayName => Id.Value;
            public IReadOnlyList<ConfigurationId> ReferenceIds => references;
            public IReadOnlyList<ConfigurationId> DependencyIds => dependencies;

            public TestDefinition(string id, IReadOnlyList<string> references = null, IReadOnlyList<string> dependencies = null)
            {
                Id = new ConfigurationId(id);
                this.references = ToIds(references);
                this.dependencies = ToIds(dependencies);
            }

            private static IReadOnlyList<ConfigurationId> ToIds(IReadOnlyList<string> values)
            {
                if (values == null)
                {
                    return Array.Empty<ConfigurationId>();
                }

                ConfigurationId[] ids = new ConfigurationId[values.Count];
                for (int i = 0; i < values.Count; i++)
                {
                    ids[i] = new ConfigurationId(values[i]);
                }

                return ids;
            }

            public IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
            {
                return Array.Empty<ConfigurationValidationIssue>();
            }
        }
    }
}
