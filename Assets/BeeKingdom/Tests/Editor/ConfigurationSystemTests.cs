using System.Collections.Generic;
using System.Diagnostics;
using BeeKingdom.Config.Loaders;
using BeeKingdom.Config.Runtime;
using BeeKingdom.Config.Validators;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class ConfigurationSystemTests
    {
        [Test]
        public void ServiceLoadsValidDefinitionsIntoCache()
        {
            TestDefinition definition = new TestDefinition("bee.worker");
            ConfigurationService service = CreateService(new[] { definition });

            ConfigurationLoadResult result = service.Reload();

            Assert.That(result.HasErrors, Is.False);
            Assert.That(service.TryGet(new ConfigurationId("bee.worker"), out TestDefinition resolved), Is.True);
            Assert.That(resolved, Is.SameAs(definition));
        }

        [Test]
        public void ValidatorDetectsDuplicateIds()
        {
            ConfigurationService service = CreateService(new[]
            {
                new TestDefinition("duplicate"),
                new TestDefinition("duplicate")
            });

            ConfigurationLoadResult result = service.Reload();

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Issues, Has.Some.Matches<ConfigurationValidationIssue>(issue => issue.Message.Contains("Duplicate")));
        }

        [Test]
        public void ValidatorDetectsMissingReferences()
        {
            ConfigurationService service = CreateService(new[]
            {
                new TestDefinition("building.honey", references: new[] { "resource.missing" })
            });

            ConfigurationLoadResult result = service.Reload();

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Issues, Has.Some.Matches<ConfigurationValidationIssue>(issue => issue.Message.Contains("Missing")));
        }

        [Test]
        public void ValidatorDetectsCircularDependencies()
        {
            ConfigurationService service = CreateService(new[]
            {
                new TestDefinition("research.a", dependencies: new[] { "research.b" }),
                new TestDefinition("research.b", dependencies: new[] { "research.a" })
            });

            ConfigurationLoadResult result = service.Reload();

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Issues, Has.Some.Matches<ConfigurationValidationIssue>(issue => issue.Message.Contains("Circular")));
        }

        [Test]
        public void CacheHandlesLargeDefinitionSets()
        {
            List<IConfigurationDefinition> definitions = new List<IConfigurationDefinition>();
            for (int i = 0; i < 5000; i++)
            {
                definitions.Add(new TestDefinition($"definition.{i}"));
            }

            ConfigurationService service = CreateService(definitions);
            Stopwatch stopwatch = Stopwatch.StartNew();
            ConfigurationLoadResult result = service.Reload();
            stopwatch.Stop();

            Assert.That(result.HasErrors, Is.False);
            Assert.That(service.GetAll<TestDefinition>().Count, Is.EqualTo(5000));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000));
        }

        private static ConfigurationService CreateService(IReadOnlyList<IConfigurationDefinition> definitions)
        {
            return new ConfigurationService(
                new InMemoryConfigurationLoader(definitions),
                new ConfigurationValidator(),
                new ConfigurationCache()
            );
        }

        private sealed class TestDefinition : IConfigurationDefinition
        {
            private readonly IReadOnlyList<ConfigurationId> references;
            private readonly IReadOnlyList<ConfigurationId> dependencies;

            public ConfigurationId Id { get; }
            public string DisplayName { get; }
            public IReadOnlyList<ConfigurationId> ReferenceIds => references;
            public IReadOnlyList<ConfigurationId> DependencyIds => dependencies;

            public TestDefinition(string id, IReadOnlyList<string> references = null, IReadOnlyList<string> dependencies = null)
            {
                Id = new ConfigurationId(id);
                DisplayName = id;
                this.references = ToIds(references);
                this.dependencies = ToIds(dependencies);
            }

            public IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
            {
                yield break;
            }

            private static IReadOnlyList<ConfigurationId> ToIds(IReadOnlyList<string> values)
            {
                List<ConfigurationId> ids = new List<ConfigurationId>();
                if (values == null)
                {
                    return ids;
                }

                foreach (string value in values)
                {
                    ids.Add(new ConfigurationId(value));
                }

                return ids;
            }
        }
    }
}
