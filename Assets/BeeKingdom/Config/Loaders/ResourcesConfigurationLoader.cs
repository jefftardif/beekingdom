using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Config.Runtime;
using UnityEngine;

namespace BeeKingdom.Config.Loaders
{
    public sealed class ResourcesConfigurationLoader : IConfigurationLoader
    {
        private readonly string resourcesPath;

        public ResourcesConfigurationLoader(string resourcesPath = "")
        {
            this.resourcesPath = resourcesPath;
        }

        public IReadOnlyList<IConfigurationDefinition> LoadDefinitions()
        {
            return Resources
                .LoadAll<ConfigurationDefinition>(resourcesPath)
                .Cast<IConfigurationDefinition>()
                .ToList();
        }
    }
}
