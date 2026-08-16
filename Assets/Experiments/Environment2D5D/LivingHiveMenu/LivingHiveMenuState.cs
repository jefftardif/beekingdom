using System;
using UnityEngine;

namespace BeeKingdom.LivingHiveMenu
{
    // ÉTAT AUTONOME du menu inférieur LivingHive réimplanté en uGUI.
    //
    // Classe pure C# (aucune dépendance scène/Canvas) : elle modélise le menu actif,
    // le changement de surface (Ruche <-> Carte), les panneaux « Plus » et « Paramètres »
    // (confort mobile), et la persistance des préférences via PlayerPrefs. Ce miroir de
    // HiveViewProductUiPresenter est volontairement indépendant du monolithe pour garder
    // le port testable et autoporté.
    public sealed class LivingHiveMenuState
    {
        // Clés PlayerPrefs (isolées du projet pour ne jamais polluer les réglages officiels).
        private const string PrefSound = "LivingHiveMenuPrep.sound";
        private const string PrefMusic = "LivingHiveMenuPrep.music";
        private const string PrefMotion = "LivingHiveMenuPrep.motion";
        private const string PrefEconomy = "LivingHiveMenuPrep.economy";
        private const string PrefLocale = "LivingHiveMenuPrep.locale";

        private bool usePersistentStorage = true;
        private string activeMenuId = string.Empty;
        private bool chatOpen;
        private string currentLocale = "fr-CA";
        private bool soundEnabled = true;
        private bool musicEnabled = true;
        private bool reducedMotionEnabled;
        private bool economyModeEnabled;

        public enum SurfaceBoundary
        {
            Hive,
            World
        }

        public SurfaceBoundary SurfaceMode { get; private set; } = SurfaceBoundary.Hive;

        public bool UsePersistentStorage
        {
            get => usePersistentStorage;
            set
            {
                usePersistentStorage = value;
                if (!value) ResetToDefaultsForProof();
            }
        }

        public string ActiveMenuId => activeMenuId;
        public bool ChatOpen => chatOpen;
        public string CurrentLocale => currentLocale;
        public bool SoundEnabled => soundEnabled;
        public bool MusicEnabled => musicEnabled;
        public bool ReducedMotionEnabled => reducedMotionEnabled;
        public bool EconomyModeEnabled => economyModeEnabled;

        public string SurfaceSwitchLabelForProof
        {
            get => SurfaceMode == SurfaceBoundary.World ? "Ruche" : "Carte";
        }

        public void LoadFromPlayerPrefs()
        {
            if (!usePersistentStorage)
            {
                ResetToDefaultsForProof();
                return;
            }

            soundEnabled = ReadBool(PrefSound, true);
            musicEnabled = ReadBool(PrefMusic, true);
            reducedMotionEnabled = ReadBool(PrefMotion, false);
            economyModeEnabled = ReadBool(PrefEconomy, false);
            string locale = PlayerPrefs.GetString(PrefLocale, "fr-CA");
            currentLocale = string.IsNullOrEmpty(locale) ? "fr-CA" : locale;
        }

        public void SaveToPlayerPrefs()
        {
            if (!usePersistentStorage) return;
            PlayerPrefs.SetInt(PrefSound, soundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefMusic, musicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefMotion, reducedMotionEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PrefEconomy, economyModeEnabled ? 1 : 0);
            PlayerPrefs.SetString(PrefLocale, currentLocale);
        }

        public static void ClearPersistedPrefs()
        {
            PlayerPrefs.DeleteKey(PrefSound);
            PlayerPrefs.DeleteKey(PrefMusic);
            PlayerPrefs.DeleteKey(PrefMotion);
            PlayerPrefs.DeleteKey(PrefEconomy);
            PlayerPrefs.DeleteKey(PrefLocale);
        }

        private bool ReadBool(string key, bool fallback)
        {
            return PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;
        }

        private void ResetToDefaultsForProof()
        {
            activeMenuId = string.Empty;
            chatOpen = false;
            SurfaceMode = SurfaceBoundary.Hive;
            currentLocale = "fr-CA";
            soundEnabled = true;
            musicEnabled = true;
            reducedMotionEnabled = false;
            economyModeEnabled = false;
        }

        // --- Navigation (miroir des handlers du rail). ---

        public void ToggleEntry(string itemId)
        {
            if (LivingHiveMenuSpec.IsSurfaceSwitch(itemId))
            {
                ToggleSurfaceSwitch();
                return;
            }

            if (LivingHiveMenuSpec.IsChat(itemId))
            {
                chatOpen = !chatOpen;
                activeMenuId = string.Empty;
                return;
            }

            if (LivingHiveMenuSpec.IsMore(itemId) && activeMenuId == LivingHiveMenuSpec.SettingsId)
            {
                CloseActiveMenuPanel();
                return;
            }

            if (activeMenuId == itemId)
            {
                CloseActiveMenuPanel();
                return;
            }

            activeMenuId = itemId;
        }

        public void ToggleSurfaceSwitch()
        {
            if (SurfaceMode == SurfaceBoundary.World)
            {
                SurfaceMode = SurfaceBoundary.Hive;
            }
            else
            {
                SurfaceMode = SurfaceBoundary.World;
            }
            CloseActiveMenuPanelForSurfaceSwitch();
        }

        public void CloseActiveMenuPanel()
        {
            activeMenuId = string.Empty;
        }

        public void OpenMenu(string menuId)
        {
            activeMenuId = menuId;
        }

        public void OpenSettings()
        {
            activeMenuId = LivingHiveMenuSpec.SettingsId;
        }

        public bool IsMenuOpen(string menuId)
        {
            if (string.IsNullOrWhiteSpace(menuId)) return string.IsNullOrEmpty(activeMenuId);
            return string.Equals(activeMenuId, menuId, StringComparison.Ordinal);
        }

        // Le bouton « Plus » apparaît actif quand le panneau Paramètres est ouvert
        // (règle exacte du monolithe : entry.ItemId == "More" && activeMainMenuId == "Settings").
        public bool IsMoreActiveForProof()
        {
            return activeMenuId == LivingHiveMenuSpec.SettingsId;
        }

        private void CloseActiveMenuPanelForSurfaceSwitch()
        {
            activeMenuId = string.Empty;
        }

        // --- Confort mobile (Paramètres). ---

        public void SetSoundEnabled(bool value)
        {
            soundEnabled = value;
            SaveToPlayerPrefs();
        }

        public void SetMusicEnabled(bool value)
        {
            musicEnabled = value;
            SaveToPlayerPrefs();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotionEnabled = value;
            SaveToPlayerPrefs();
        }

        public void SetEconomyMode(bool value)
        {
            economyModeEnabled = value;
            SaveToPlayerPrefs();
        }

        public void SetPreferredLocale(string locale)
        {
            if (string.IsNullOrEmpty(locale)) return;
            currentLocale = locale;
            SaveToPlayerPrefs();
        }

        public bool IsFrenchForProof()
        {
            return string.Equals(currentLocale, "fr-CA", StringComparison.OrdinalIgnoreCase);
        }

        // Le libellé du bouton Paramètres porte un badge « personnalisé » dès qu'un réglage
        // diffère des valeurs par défaut (même règle que le monolithe).
        public bool IsCustomSettingsForProof()
        {
            return reducedMotionEnabled || economyModeEnabled || !soundEnabled || !musicEnabled;
        }
    }
}