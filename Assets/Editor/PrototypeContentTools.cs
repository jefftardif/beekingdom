using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeeKingdom.Core;
using BeeKingdom.Core.Buildings;
using BeeKingdom.Core.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeContentTools
{
    private const string BuildingsFolder = "Assets/_Project/ScriptableObjects/Buildings";
    private const string BootScenePath = "Assets/_Project/Scenes/_Boot.unity";

    [MenuItem("Bee Kingdom/Prototype/Create Starter Buildings")]
    public static void CreateStarterBuildings()
    {
        Directory.CreateDirectory(BuildingsFolder);

        List<BuildingSO> buildings = new List<BuildingSO>
        {
            CreateOrUpdateBuilding(
                "QueensChamber",
                "queens_chamber",
                "Queen's Chamber",
                BuildingType.QueensChamber,
                "The heart of the hive. Upgrading it unlocks more colony options.",
                "Q",
                new[] { new ResourceCost(ResourceType.Honey, 100) },
                10f,
                false,
                ResourceType.Honey,
                0f,
                false,
                ResourceType.Honey,
                0,
                10
            ),
            CreateOrUpdateBuilding(
                "HoneyStorage",
                "honey_storage",
                "Honey Storage",
                BuildingType.HoneyStorage,
                "Stores more honey for construction and recruitment.",
                "H",
                new[] { new ResourceCost(ResourceType.Honey, 75), new ResourceCost(ResourceType.Wax, 5) },
                12f,
                false,
                ResourceType.Honey,
                0f,
                true,
                ResourceType.Honey,
                5000,
                10
            ),
            CreateOrUpdateBuilding(
                "FlowerGarden",
                "flower_garden",
                "Flower Garden",
                BuildingType.FlowerGarden,
                "Produces pollen for recruiting and improving bees.",
                "P",
                new[] { new ResourceCost(ResourceType.Honey, 150) },
                15f,
                true,
                ResourceType.Pollen,
                3f,
                false,
                ResourceType.Pollen,
                0,
                10
            ),
            CreateOrUpdateBuilding(
                "WaxWorkshop",
                "wax_workshop",
                "Wax Workshop",
                BuildingType.WaxWorkshop,
                "Produces wax used for buildings and hive upgrades.",
                "W",
                new[] { new ResourceCost(ResourceType.Honey, 250), new ResourceCost(ResourceType.Pollen, 60) },
                20f,
                true,
                ResourceType.Wax,
                1f,
                false,
                ResourceType.Wax,
                0,
                10
            ),
            CreateOrUpdateBuilding(
                "Barracks",
                "barracks",
                "Guard Barracks",
                BuildingType.Barracks,
                "A future training place for defender bees.",
                "B",
                new[] { new ResourceCost(ResourceType.Honey, 300), new ResourceCost(ResourceType.Wax, 20) },
                25f,
                false,
                ResourceType.Honey,
                0f,
                false,
                ResourceType.Honey,
                0,
                5
            )
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AssignBuildingsToBootScene();

        Debug.Log($"Starter prototype buildings ready: {buildings.Count}");
    }

    private static BuildingSO CreateOrUpdateBuilding(
        string assetName,
        string buildingId,
        string displayName,
        BuildingType type,
        string description,
        string emoji,
        ResourceCost[] buildCost,
        float buildDuration,
        bool canProduceResources,
        ResourceType producedResourceType,
        float productionRate,
        bool isStorage,
        ResourceType storedResourceType,
        int storageCapacityBonus,
        int maxLevel)
    {
        string path = $"{BuildingsFolder}/{assetName}.asset";
        BuildingSO building = AssetDatabase.LoadAssetAtPath<BuildingSO>(path);

        if (building == null)
        {
            building = ScriptableObject.CreateInstance<BuildingSO>();
            AssetDatabase.CreateAsset(building, path);
        }

        building.buildingId = buildingId;
        building.buildingName = displayName;
        building.buildingType = type;
        building.description = description;
        building.emoji = emoji;
        building.requiredQueenLevel = 1;
        building.isUnlockedByDefault = true;
        building.buildCost = buildCost;
        building.buildDuration = buildDuration;
        building.canProduceResources = canProduceResources;
        building.producedResourceType = producedResourceType;
        building.productionRate = productionRate;
        building.isStorage = isStorage;
        building.storedResourceType = storedResourceType;
        building.storageCapacityBonus = storageCapacityBonus;
        building.maxLevel = maxLevel;
        building.costMultiplierPerLevel = 1.5f;
        building.durationMultiplierPerLevel = 1.2f;
        building.productionIncreasePerLevel = canProduceResources ? 1f : 0f;
        building.storageIncreasePerLevel = isStorage ? storageCapacityBonus / 2 : 0;

        EditorUtility.SetDirty(building);
        return building;
    }

    private static void AssignBuildingsToBootScene()
    {
        Scene scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
        BuildingManager manager = Object.FindFirstObjectByType<BuildingManager>();

        if (manager == null)
        {
            Debug.LogWarning("BuildingManager was not found in _Boot scene.");
            return;
        }

        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty allBuildings = serializedManager.FindProperty("allBuildings");
        List<BuildingSO> orderedBuildings = AssetDatabase.FindAssets("t:BuildingSO", new[] { BuildingsFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<BuildingSO>)
            .Where(building => building != null)
            .OrderBy(building => building.requiredQueenLevel)
            .ThenBy(building => building.buildingName)
            .ToList();

        if (allBuildings == null)
        {
            Debug.LogWarning("BuildingManager allBuildings field was not found.");
            return;
        }

        allBuildings.arraySize = orderedBuildings.Count;
        for (int i = 0; i < orderedBuildings.Count; i++)
        {
            allBuildings.GetArrayElementAtIndex(i).objectReferenceValue = orderedBuildings[i];
        }

        serializedManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
