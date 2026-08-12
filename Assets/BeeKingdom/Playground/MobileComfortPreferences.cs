using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public sealed class MobileComfortPreferences
    {
        public int version = MobileComfortPreferencesCodec.CurrentVersion;
        public int revision;
        public bool reducedMotion;
        public bool economyMode;
        public bool soundEnabled = true;
        public bool musicEnabled = true;
        public string miniChatWatchMode = "auto";
        public bool miniChatBlinkEnabled = true;
        public string chatEmojiRecents = string.Empty;
        public bool miniChatOpen;
        public string pinnedMissionIds = string.Empty;
        public bool pinnedMissionsWidgetHidden;
    }

    public enum MobileComfortPreferencesReadStatus
    {
        Missing,
        Valid,
        Corrupted,
        UnsupportedVersion
    }

    public sealed class MobileComfortPreferencesReadResult
    {
        public MobileComfortPreferencesReadResult(
            MobileComfortPreferences preferences,
            MobileComfortPreferencesReadStatus status)
        {
            Preferences = preferences ?? MobileComfortPreferencesCodec.CreateDefault();
            Status = status;
        }

        public MobileComfortPreferences Preferences { get; }
        public MobileComfortPreferencesReadStatus Status { get; }
    }

    public interface IMobileComfortPreferencesStore
    {
        string Read();
        void Write(string json);
        void Delete();
    }

    public sealed class PlayerPrefsMobileComfortPreferencesStore : IMobileComfortPreferencesStore
    {
        private const string Key = "bee.mobile.comfort.v1";

        public string Read()
        {
            return PlayerPrefs.GetString(Key, string.Empty);
        }

        public void Write(string json)
        {
            PlayerPrefs.SetString(Key, json ?? string.Empty);
            PlayerPrefs.Save();
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }

    public static class MobileComfortPreferencesCodec
    {
        public const int CurrentVersion = 2;

        public static MobileComfortPreferences CreateDefault()
        {
            return new MobileComfortPreferences
            {
                version = CurrentVersion,
                revision = 0,
                reducedMotion = false,
                economyMode = false,
                soundEnabled = true,
                musicEnabled = true,
                miniChatWatchMode = "auto",
                miniChatBlinkEnabled = true,
                chatEmojiRecents = string.Empty,
                miniChatOpen = false,
                pinnedMissionIds = string.Empty,
                pinnedMissionsWidgetHidden = false
            };
        }

        public static MobileComfortPreferencesReadResult Read(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new MobileComfortPreferencesReadResult(CreateDefault(), MobileComfortPreferencesReadStatus.Missing);

            MobileComfortPreferences preferences;
            try
            {
                preferences = JsonUtility.FromJson<MobileComfortPreferences>(json);
            }
            catch
            {
                return new MobileComfortPreferencesReadResult(CreateDefault(), MobileComfortPreferencesReadStatus.Corrupted);
            }

            if (preferences == null)
                return new MobileComfortPreferencesReadResult(CreateDefault(), MobileComfortPreferencesReadStatus.Corrupted);
            if (preferences.version != CurrentVersion)
                return new MobileComfortPreferencesReadResult(CreateDefault(), MobileComfortPreferencesReadStatus.UnsupportedVersion);

            preferences.revision = Math.Max(0, preferences.revision);
            return new MobileComfortPreferencesReadResult(preferences, MobileComfortPreferencesReadStatus.Valid);
        }

        public static string Write(MobileComfortPreferences preferences)
        {
            MobileComfortPreferences normalized = preferences ?? CreateDefault();
            normalized.version = CurrentVersion;
            normalized.revision = Math.Max(0, normalized.revision);
            return JsonUtility.ToJson(normalized);
        }
    }
}
