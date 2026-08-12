using UnityEngine;

namespace BeeKingdom.Core
{
    /// <summary>
    /// GameManager - Le cœur du jeu Bee Kingdom
    /// Gère le cycle de vie du jeu et coordonne tous les autres managers
    /// Pattern: Singleton
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        public static GameManager Instance { get; private set; }

        // Propriété du niveau du joueur
        public int PlayerLevel { get; private set; } = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Initialiser après que tous les Awake() soient terminés
            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // ORDRE IMPORTANT :

            // 1. Initialiser ResourceManager EN PREMIER (crée les ressources vides)
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.Initialize();
            }
            else
            {
                Debug.LogWarning("ResourceManager not found in scene.");
            }

            // 2. Initialiser BeeManager AVANT SaveManager (crée la database des abeilles)
            if (BeeManager.Instance != null)
            {
                BeeManager.Instance.Initialize();
            }
            else
            {
                Debug.LogWarning("⚠️ BeeManager not found in scene!");
            }

            // 2. BuildingManager
            if (BuildingManager.Instance != null)
            {
                BuildingManager.Instance.Initialize();
            }
            else
            {
                Debug.LogWarning("⚠️ BuildingManager not found in scene!");
            }

            // 3. PUIS SaveManager charge et restaure (après que tout soit prêt)
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Initialize();
            }
            else
            {
                Debug.LogWarning("⚠️ SaveManager not found in scene!");
            }


        }

        #endregion

        #region Game Loop

        //private void Start()
        //{
        //    Debug.Log("🎮 Game starting...");
        //    StartGame();
        //}

        private void StartGame()
        {
            // TODO: Charger les données sauvegardées
            // TODO: Initialiser la scène principale
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Appelé quand l'application est mise en pause (mobile)
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        /// <summary>
        /// Appelé quand l'application se ferme
        /// </summary>
        private void OnApplicationQuit()
        {
            SaveGame();
        }

        /// <summary>
        /// Sauvegarde le jeu
        /// </summary>
        public void SaveGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
        }

        /// <summary>
        /// Charge le jeu
        /// </summary>
        public void LoadGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame();
            }
        }

        #endregion

        #region Debug (À enlever en production)

#if UNITY_EDITOR
        [ContextMenu("Test Save Game")]
        private void TestSave()
        {
            SaveGame();
        }

        [ContextMenu("Test Load Game")]
        private void TestLoad()
        {
            LoadGame();
        }
#endif

        #endregion
    }
}
