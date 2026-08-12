using UnityEngine;
using System;
using System.Collections.Generic;
using BeeKingdom.Core.Bees;
using BeeKingdom.Core.Data;
using BeeKingdom.Core.Save;

namespace BeeKingdom.Core
{
    /// <summary>
    /// SaveManager - Version stable avec listes (compatible JsonUtility)
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        #region Singleton

        public static SaveManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        #endregion

        #region Configuration

        [Header("Save Configuration")]
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float autoSaveInterval = 60f;

        private const string SAVE_KEY = "BeeKingdom_PlayerSave";

        #endregion

        #region Data

        private PlayerSaveData currentSaveData;
        private float autoSaveTimer;
        private bool hasUnsavedChanges = false;

        #endregion

        #region Events

        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;

        #endregion

        #region Initialization

        public void Initialize()
        {
            LoadGame();
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (autoSaveEnabled)
            {
                autoSaveTimer += UnityEngine.Time.deltaTime;

                if (autoSaveTimer >= autoSaveInterval)
                {
                    autoSaveTimer = 0f;

                    if (hasUnsavedChanges)
                    {
                        SaveGame();
                    }
                }
            }
        }

        #endregion

        #region Public Methods - Save

        public bool SaveGame()
        {
            try
            {
                CollectCurrentGameData();
                currentSaveData.lastSaveTime = DateTime.Now.ToString("o");

                string json = JsonUtility.ToJson(currentSaveData, true);

                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();

                hasUnsavedChanges = false;

                OnSaveCompleted?.Invoke();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Save failed: {e.Message}");
                return false;
            }
        }

        private void CollectCurrentGameData()
        {
            if (currentSaveData == null)
            {
                currentSaveData = new PlayerSaveData();
            }
            EnsureSaveLists();

            currentSaveData.lastPlayTime = DateTime.Now.ToString("o");
            currentSaveData.totalPlayTime += UnityEngine.Time.deltaTime;

            if (ResourceManager.Instance != null)
            {
                currentSaveData.resources.Clear();

                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    string key = type.ToString();
                    int amount = ResourceManager.Instance.GetResource(type);
                    int capacity = ResourceManager.Instance.GetMaxCapacity(type);

                    currentSaveData.resources.Add(new ResourceSaveData(key, amount, capacity));
                }
            }

            if (BeeManager.Instance != null)
            {
                currentSaveData.bees = BeeManager.Instance.GetBeesForSave();
            }

            if (BuildingManager.Instance != null)
            {
                currentSaveData.buildings.Clear();

                foreach (BuildingData building in BuildingManager.Instance.GetBuildingsForSave())
                {
                    currentSaveData.buildings.Add(new BuildingSaveData(building));
                }
            }
        }

        public void MarkDirty()
        {
            hasUnsavedChanges = true;
        }

        #endregion

        #region Public Methods - Load

        public bool LoadGame()
        {
            try
            {
                if (!PlayerPrefs.HasKey(SAVE_KEY))
                {
                    CreateNewGame();
                    return true;
                }

                string json = PlayerPrefs.GetString(SAVE_KEY);
                currentSaveData = JsonUtility.FromJson<PlayerSaveData>(json);

                if (currentSaveData == null || currentSaveData.resources == null)
                {
                    Debug.LogWarning("⚠️ Save data corrupted, creating new game");
                    CreateNewGame();
                    return false;
                }
                EnsureSaveLists();

                ApplySaveDataToGame();
                CalculateOfflineProgress();

                OnLoadCompleted?.Invoke();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Load failed: {e.Message}");
                CreateNewGame();
                return false;
            }
        }

        private void CreateNewGame()
        {
            currentSaveData = new PlayerSaveData();
        }

        private void ApplySaveDataToGame()
        {
            if (BuildingManager.Instance != null && currentSaveData.buildings != null)
            {
                BuildingManager.Instance.LoadBuildings(ConvertSavedBuildings(currentSaveData.buildings));
            }

            if (BeeManager.Instance != null && currentSaveData.bees != null)
            {
                BeeManager.Instance.LoadBees(currentSaveData.bees);
            }

            if (ResourceManager.Instance != null && currentSaveData.resources != null)
            {
                foreach (var resourceData in currentSaveData.resources)
                {
                    if (Enum.TryParse<ResourceType>(resourceData.resourceType, out ResourceType type))
                    {
                        ResourceManager.Instance.SetResource(type, resourceData.amount, resourceData.maxCapacity);
                    }
                }
            }
        }

        private void EnsureSaveLists()
        {
            currentSaveData.resources ??= new List<ResourceSaveData>();
            currentSaveData.bees ??= new List<BeeData>();
            currentSaveData.buildings ??= new List<BuildingSaveData>();
        }

        private BuildingData[] ConvertSavedBuildings(List<BuildingSaveData> savedBuildings)
        {
            int totalSlots = BuildingManager.Instance != null ? BuildingManager.Instance.GetTotalSlots() : 20;
            BuildingData[] buildings = new BuildingData[totalSlots];

            for (int i = 0; i < buildings.Length; i++)
            {
                buildings[i] = new BuildingData(i);
            }

            foreach (BuildingSaveData savedBuilding in savedBuildings)
            {
                if (savedBuilding.slotIndex < 0 || savedBuilding.slotIndex >= buildings.Length)
                {
                    continue;
                }

                if (!Enum.TryParse(savedBuilding.buildingType, out BuildingType buildingType))
                {
                    buildingType = BuildingType.Empty;
                }

                DateTime startTime = DateTime.Now;
                if (!string.IsNullOrEmpty(savedBuilding.constructionStartTime))
                {
                    DateTime.TryParse(savedBuilding.constructionStartTime, out startTime);
                }

                buildings[savedBuilding.slotIndex] = new BuildingData(
                    savedBuilding.slotIndex,
                    buildingType,
                    savedBuilding.level,
                    savedBuilding.isConstructing,
                    startTime,
                    savedBuilding.constructionDuration
                );
            }

            return buildings;
        }

        private void CalculateOfflineProgress()
        {
            if (ResourceManager.Instance != null && !string.IsNullOrEmpty(currentSaveData.lastPlayTime))
            {
                try
                {
                    DateTime lastPlayTime = DateTime.Parse(currentSaveData.lastPlayTime);
                    ResourceManager.Instance.CalculateOfflineProduction(lastPlayTime);
                }
                catch
                {
                    Debug.LogWarning("⚠️ Could not parse last play time");
                }
            }
        }

        #endregion

        #region Unity Events

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        #endregion

        #region Debug Methods

#if UNITY_EDITOR
        [ContextMenu("Force Save Now")]
        private void DebugForceSave()
        {
            SaveGame();
        }

        [ContextMenu("Delete Save")]
        private void DebugDeleteSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("🗑️ Save deleted");
        }

        [ContextMenu("Print Save Data")]
        private void DebugPrintSaveData()
        {
            if (currentSaveData != null)
            {
                Debug.Log("=== SAVE DATA ===");
                Debug.Log($"Player: {currentSaveData.playerName}");
                Debug.Log($"Resources ({currentSaveData.resources.Count}):");
                foreach (var res in currentSaveData.resources)
                {
                    Debug.Log($"  {res.resourceType}: {res.amount}/{res.maxCapacity}");
                }
                Debug.Log($"Bees: {currentSaveData.bees?.Count ?? 0}");
                Debug.Log($"Buildings: {currentSaveData.buildings?.Count ?? 0}");
                Debug.Log("=================");
            }
        }
#endif

        #endregion
    }
}
