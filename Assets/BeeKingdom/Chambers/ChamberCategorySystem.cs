using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Chambers
{
    public sealed class ChamberCategoryDefinition
    {
        public string CategoryId { get; }
        public IReadOnlyList<string> AllowedActivities { get; }
        public IReadOnlyList<string> AcceptedResources { get; }
        public IReadOnlyList<string> AllowedCastes { get; }
        public int MaxCapacity { get; }
        public IReadOnlyList<string> CompatibleCategories { get; }
        public IReadOnlyList<string> IncompatibleCategories { get; }
        public IReadOnlyList<string> RecommendedNeighbors { get; }
        public IReadOnlyList<string> ForbiddenNeighbors { get; }
        public int ConstructionPriority { get; }
        public int LogisticPriority { get; }
        public int MaintenancePriority { get; }
        public int EnergyPriority { get; }

        public ChamberCategoryDefinition(string categoryId, IReadOnlyList<string> allowedActivities = null, IReadOnlyList<string> acceptedResources = null, IReadOnlyList<string> allowedCastes = null, int maxCapacity = 0, IReadOnlyList<string> compatibleCategories = null, IReadOnlyList<string> incompatibleCategories = null, IReadOnlyList<string> recommendedNeighbors = null, IReadOnlyList<string> forbiddenNeighbors = null, int constructionPriority = 0, int logisticPriority = 0, int maintenancePriority = 0, int energyPriority = 0)
        {
            CategoryId = string.IsNullOrWhiteSpace(categoryId) ? throw new ArgumentException("Category id is required.", nameof(categoryId)) : categoryId;
            AllowedActivities = allowedActivities ?? Array.Empty<string>();
            AcceptedResources = acceptedResources ?? Array.Empty<string>();
            AllowedCastes = allowedCastes ?? Array.Empty<string>();
            MaxCapacity = maxCapacity < 0 ? 0 : maxCapacity;
            CompatibleCategories = compatibleCategories ?? Array.Empty<string>();
            IncompatibleCategories = incompatibleCategories ?? Array.Empty<string>();
            RecommendedNeighbors = recommendedNeighbors ?? Array.Empty<string>();
            ForbiddenNeighbors = forbiddenNeighbors ?? Array.Empty<string>();
            ConstructionPriority = constructionPriority;
            LogisticPriority = logisticPriority;
            MaintenancePriority = maintenancePriority;
            EnergyPriority = energyPriority;
        }
    }

    public static class ChamberCategoryCatalog
    {
        private static readonly string[] BaseCategoryIds =
        {
            "Entrance", "Corridor", "Nursery", "Brood", "Royal", "HoneyStorage", "PollenStorage", "WaterStorage", "WaxStorage", "RoyalJellyStorage",
            "PropolisStorage", "FoodProcessing", "Ventilation", "Defense", "Hospital", "RestArea", "Waste", "NurserySupport", "Utility", "Decoration"
        };

        public static IReadOnlyList<ChamberCategoryDefinition> CreateBaseDefinitions()
        {
            List<ChamberCategoryDefinition> definitions = new List<ChamberCategoryDefinition>(BaseCategoryIds.Length);
            for (int i = 0; i < BaseCategoryIds.Length; i++)
            {
                definitions.Add(new ChamberCategoryDefinition(BaseCategoryIds[i]));
            }

            return definitions;
        }
    }

    public sealed class ChamberCategoryRegistry
    {
        private readonly Dictionary<string, ChamberCategoryDefinition> categories = new Dictionary<string, ChamberCategoryDefinition>();
        public int Count => categories.Count;

        public bool RegisterCategory(ChamberCategoryDefinition definition)
        {
            if (definition == null || categories.ContainsKey(definition.CategoryId)) return false;
            categories.Add(definition.CategoryId, definition);
            return true;
        }

        public bool GetCategory(string categoryId, out ChamberCategoryDefinition definition) => categories.TryGetValue(categoryId, out definition);

        public IReadOnlyList<ChamberCategoryDefinition> QueryCategories()
        {
            List<ChamberCategoryDefinition> result = new List<ChamberCategoryDefinition>(categories.Values);
            result.Sort((left, right) => string.CompareOrdinal(left.CategoryId, right.CategoryId));
            return result;
        }
    }

    public sealed class ChamberCategoryResolver
    {
        public bool AreCompatible(ChamberCategoryDefinition left, ChamberCategoryDefinition right)
        {
            if (left == null || right == null) return false;
            if (Contains(left.IncompatibleCategories, right.CategoryId) || Contains(right.IncompatibleCategories, left.CategoryId)) return false;
            if (left.CompatibleCategories.Count == 0 && right.CompatibleCategories.Count == 0) return true;
            return Contains(left.CompatibleCategories, right.CategoryId) || Contains(right.CompatibleCategories, left.CategoryId);
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == value) return true;
            }

            return false;
        }
    }

    public sealed class ChamberCategoryDiagnostics
    {
        public int Registered { get; private set; }
        public int Assigned { get; private set; }
        public int Removed { get; private set; }
        public int Validated { get; private set; }
        public int Invalid { get; private set; }

        public void RecordRegistered(int count) => Registered = count;
        public void RecordAssigned() => Assigned++;
        public void RecordRemoved() => Removed++;
        public void RecordValidated(bool valid) { Validated++; if (!valid) Invalid++; }
    }

    public sealed class ChamberCategoryManager
    {
        private readonly ChamberCategoryRegistry registry = new ChamberCategoryRegistry();
        private readonly ChamberCategoryResolver resolver = new ChamberCategoryResolver();
        private readonly Dictionary<string, HashSet<string>> assignments = new Dictionary<string, HashSet<string>>();
        private readonly IEventBus eventBus;

        public ChamberCategoryDiagnostics Diagnostics { get; } = new ChamberCategoryDiagnostics();

        public ChamberCategoryManager(IEventBus eventBus = null)
        {
            this.eventBus = eventBus;
        }

        public bool RegisterCategory(ChamberCategoryDefinition definition)
        {
            bool registered = registry.RegisterCategory(definition);
            if (registered) { Diagnostics.RecordRegistered(registry.Count); eventBus?.Publish(new CategoryRegistered(definition.CategoryId)); }
            return registered;
        }

        public bool GetCategory(string categoryId, out ChamberCategoryDefinition definition) => registry.GetCategory(categoryId, out definition);
        public IReadOnlyList<ChamberCategoryDefinition> QueryCategories() => registry.QueryCategories();

        public bool AssignCategory(string chamberEntityId, string categoryId)
        {
            if (!registry.GetCategory(categoryId, out _)) return false;
            if (!assignments.TryGetValue(chamberEntityId, out HashSet<string> set))
            {
                set = new HashSet<string>();
                assignments[chamberEntityId] = set;
            }

            bool added = set.Add(categoryId);
            if (added) { Diagnostics.RecordAssigned(); eventBus?.Publish(new CategoryAssigned(chamberEntityId, categoryId)); }
            return added;
        }

        public bool RemoveCategory(string chamberEntityId, string categoryId)
        {
            if (!assignments.TryGetValue(chamberEntityId, out HashSet<string> set)) return false;
            bool removed = set.Remove(categoryId);
            if (removed) { Diagnostics.RecordRemoved(); eventBus?.Publish(new CategoryRemoved(chamberEntityId, categoryId)); }
            return removed;
        }

        public bool ValidateCategory(string chamberEntityId)
        {
            bool valid = true;
            if (assignments.TryGetValue(chamberEntityId, out HashSet<string> set))
            {
                List<string> ids = new List<string>(set);
                for (int i = 0; i < ids.Count; i++)
                {
                    for (int j = i + 1; j < ids.Count; j++)
                    {
                        registry.GetCategory(ids[i], out ChamberCategoryDefinition left);
                        registry.GetCategory(ids[j], out ChamberCategoryDefinition right);
                        if (!resolver.AreCompatible(left, right)) valid = false;
                    }
                }
            }

            Diagnostics.RecordValidated(valid);
            eventBus?.Publish(new CategoryValidated(chamberEntityId, valid));
            return valid;
        }

        public IReadOnlyList<ChamberCategoryDefinition> QueryCompatibleCategories(string categoryId)
        {
            List<ChamberCategoryDefinition> result = new List<ChamberCategoryDefinition>();
            if (!registry.GetCategory(categoryId, out ChamberCategoryDefinition source)) return result;
            IReadOnlyList<ChamberCategoryDefinition> categories = registry.QueryCategories();
            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i].CategoryId != categoryId && resolver.AreCompatible(source, categories[i])) result.Add(categories[i]);
            }
            return result;
        }
    }

    public readonly struct CategoryRegistered : IGameplayEvent, IBuildingEvent { public string CategoryId { get; } public CategoryRegistered(string categoryId) { CategoryId = categoryId; } }
    public readonly struct CategoryAssigned : IGameplayEvent, IBuildingEvent { public string ChamberId { get; } public string CategoryId { get; } public CategoryAssigned(string chamberId, string categoryId) { ChamberId = chamberId; CategoryId = categoryId; } }
    public readonly struct CategoryRemoved : IGameplayEvent, IBuildingEvent { public string ChamberId { get; } public string CategoryId { get; } public CategoryRemoved(string chamberId, string categoryId) { ChamberId = chamberId; CategoryId = categoryId; } }
    public readonly struct CategoryValidated : IGameplayEvent, IBuildingEvent { public string ChamberId { get; } public bool Valid { get; } public CategoryValidated(string chamberId, bool valid) { ChamberId = chamberId; Valid = valid; } }
    public readonly struct CategoryChanged : IGameplayEvent, IBuildingEvent { public string ChamberId { get; } public CategoryChanged(string chamberId) { ChamberId = chamberId; } }
}
