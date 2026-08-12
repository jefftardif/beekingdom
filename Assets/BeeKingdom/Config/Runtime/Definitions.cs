using System.Collections.Generic;
using BeeKingdom.Gameplay.Domain.Enums;
using UnityEngine;

namespace BeeKingdom.Config.Runtime
{
    [CreateAssetMenu(fileName = "BeeDefinition", menuName = "Bee Kingdom/Data/Bee Definition")]
    public sealed class BeeDefinition : ConfigurationDefinition
    {
        [SerializeField] private BeeRole role;
        [SerializeField] private int maxHealth;
        [SerializeField] private int maxEnergy;

        public BeeRole Role => role;
        public int MaxHealth => maxHealth;
        public int MaxEnergy => maxEnergy;

        public override IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
        {
            foreach (ConfigurationValidationIssue issue in base.ValidateConfiguration()) yield return issue;
            if (maxHealth <= 0) yield return Error("Max health must be greater than zero.");
            if (maxEnergy <= 0) yield return Error("Max energy must be greater than zero.");
        }
    }

    [CreateAssetMenu(fileName = "BuildingDefinition", menuName = "Bee Kingdom/Data/Building Definition")]
    public sealed class BuildingDefinition : ConfigurationDefinition
    {
        [SerializeField] private BuildingType type;
        [SerializeField] private int maxLevel;

        public BuildingType Type => type;
        public int MaxLevel => maxLevel;

        public override IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
        {
            foreach (ConfigurationValidationIssue issue in base.ValidateConfiguration()) yield return issue;
            if (maxLevel <= 0) yield return Error("Max level must be greater than zero.");
        }
    }

    [CreateAssetMenu(fileName = "ResourceDefinition", menuName = "Bee Kingdom/Data/Resource Definition")]
    public sealed class ResourceDefinition : ConfigurationDefinition
    {
        [SerializeField] private ResourceType type;
        [SerializeField] private long storageLimit;

        public ResourceType Type => type;
        public long StorageLimit => storageLimit;

        public override IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
        {
            foreach (ConfigurationValidationIssue issue in base.ValidateConfiguration()) yield return issue;
            if (storageLimit < 0) yield return Error("Storage limit cannot be negative.");
        }
    }

    [CreateAssetMenu(fileName = "ResearchDefinition", menuName = "Bee Kingdom/Data/Research Definition")]
    public sealed class ResearchDefinition : ConfigurationDefinition
    {
        [SerializeField] private int tier;

        public int Tier => tier;

        public override IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
        {
            foreach (ConfigurationValidationIssue issue in base.ValidateConfiguration()) yield return issue;
            if (tier < 0) yield return Error("Research tier cannot be negative.");
        }
    }

    [CreateAssetMenu(fileName = "FlowerDefinition", menuName = "Bee Kingdom/Data/Flower Definition")]
    public sealed class FlowerDefinition : ConfigurationDefinition
    {
        [SerializeField] private ResourceType producedResource;
        [SerializeField] private float yieldRate;

        public ResourceType ProducedResource => producedResource;
        public float YieldRate => yieldRate;

        public override IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
        {
            foreach (ConfigurationValidationIssue issue in base.ValidateConfiguration()) yield return issue;
            if (yieldRate < 0f) yield return Error("Yield rate cannot be negative.");
        }
    }

    [CreateAssetMenu(fileName = "RegionDefinition", menuName = "Bee Kingdom/Data/Region Definition")]
    public sealed class RegionDefinition : ConfigurationDefinition
    {
        [SerializeField] private RegionType type;

        public RegionType Type => type;
    }

    [CreateAssetMenu(fileName = "WeatherDefinition", menuName = "Bee Kingdom/Data/Weather Definition")]
    public sealed class WeatherDefinition : ConfigurationDefinition
    {
        [SerializeField] private WeatherType type;
        [SerializeField] private float movementModifier;

        public WeatherType Type => type;
        public float MovementModifier => movementModifier;

        public override IEnumerable<ConfigurationValidationIssue> ValidateConfiguration()
        {
            foreach (ConfigurationValidationIssue issue in base.ValidateConfiguration()) yield return issue;
            if (movementModifier < 0f) yield return Error("Movement modifier cannot be negative.");
        }
    }

    [CreateAssetMenu(fileName = "SeasonDefinition", menuName = "Bee Kingdom/Data/Season Definition")]
    public sealed class SeasonDefinition : ConfigurationDefinition
    {
        [SerializeField] private Season season;

        public Season Season => season;
    }
}
