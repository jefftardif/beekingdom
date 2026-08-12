using System;
using System.Collections.Generic;
using BeeKingdom.Core.Bees;
using BeeKingdom.Core.Data;

namespace BeeKingdom.Core.Save
{
    /// <summary>
    /// Complete player save data. Lists are used because Unity JsonUtility does not serialize dictionaries.
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        public string playerId;
        public string playerName;
        public int playerLevel;
        public string lastSaveTime;
        public string lastPlayTime;

        public List<ResourceSaveData> resources;
        public List<BeeData> bees;
        public List<BuildingSaveData> buildings;

        public float totalPlayTime;
        public int saveVersion;

        public PlayerSaveData()
        {
            playerId = Guid.NewGuid().ToString();
            playerName = "Player";
            playerLevel = 1;
            lastSaveTime = DateTime.Now.ToString("o");
            lastPlayTime = DateTime.Now.ToString("o");

            resources = new List<ResourceSaveData>();
            bees = new List<BeeData>();
            buildings = new List<BuildingSaveData>();

            totalPlayTime = 0f;
            saveVersion = 2;
        }
    }

    [Serializable]
    public class ResourceSaveData
    {
        public string resourceType;
        public int amount;
        public int maxCapacity;

        public ResourceSaveData()
        {
        }

        public ResourceSaveData(string type, int amt, int capacity)
        {
            resourceType = type;
            amount = amt;
            maxCapacity = capacity;
        }
    }

    [Serializable]
    public class BuildingSaveData
    {
        public int slotIndex;
        public string buildingType;
        public int level;
        public bool isConstructing;
        public string constructionStartTime;
        public float constructionDuration;

        public BuildingSaveData()
        {
        }

        public BuildingSaveData(BuildingData building)
        {
            slotIndex = building.slotIndex;
            buildingType = building.buildingType.ToString();
            level = building.level;
            isConstructing = building.isConstructing;
            constructionStartTime = building.constructionStartTime.ToString("o");
            constructionDuration = building.constructionDuration;
        }
    }
}
