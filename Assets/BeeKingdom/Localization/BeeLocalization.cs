using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BeeKingdom.Localization
{
    [Serializable]
    internal sealed class BeeLocalizationCatalogData
    {
        public string locale = string.Empty;
        public BeeLocalizationEntryData[] entries = Array.Empty<BeeLocalizationEntryData>();
    }

    [Serializable]
    internal sealed class BeeLocalizationEntryData
    {
        public string key = string.Empty;
        public string value = string.Empty;
    }

    public static class BeeLocalization
    {
        public const string SourceLocale = "fr-CA";
        private const string CatalogResourcePrefix = "Localization/strings.";
        private const string LocalePreferenceKey = "BeeKingdom.Localization.Locale.v1";
        private static readonly string[] Locales = { "fr-CA", "en-US" };
        private static readonly Dictionary<string, Dictionary<string, string>> Catalogs =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ReportedMissingKeys =
            new HashSet<string>(StringComparer.Ordinal);

        private static string currentLocale = SourceLocale;

        public static event Action<string> LocaleChanged;

        public static string CurrentLocale => currentLocale;
        public static IReadOnlyList<string> SupportedLocales => Locales;
        public static string SavedLocale => PlayerPrefs.GetString(LocalePreferenceKey, string.Empty);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeLocaleBeforeScene()
        {
            ApplySavedOrSystemLocale(Application.systemLanguage);
        }

        public static bool SetLocale(string locale)
        {
            string normalized = NormalizeLocale(locale);
            if (!TryGetCatalog(normalized, out _)) return false;
            if (string.Equals(currentLocale, normalized, StringComparison.OrdinalIgnoreCase)) return true;

            currentLocale = normalized;
            LocaleChanged?.Invoke(currentLocale);
            return true;
        }

        public static bool SetSystemLocale(SystemLanguage language)
        {
            return SetLocale(language == SystemLanguage.French ? "fr-CA" : "en-US");
        }

        public static bool SetLocaleAndSave(string locale)
        {
            if (!SetLocale(locale)) return false;

            PlayerPrefs.SetString(LocalePreferenceKey, currentLocale);
            PlayerPrefs.Save();
            return true;
        }

        public static bool ApplySavedOrSystemLocale(SystemLanguage systemLanguage)
        {
            string savedLocale = SavedLocale;
            if (!string.IsNullOrWhiteSpace(savedLocale) && SetLocale(savedLocale)) return true;

            if (PlayerPrefs.HasKey(LocalePreferenceKey))
            {
                PlayerPrefs.DeleteKey(LocalePreferenceKey);
                PlayerPrefs.Save();
            }

            return SetSystemLocale(systemLanguage);
        }

        public static void ClearSavedLocale()
        {
            if (!PlayerPrefs.HasKey(LocalePreferenceKey)) return;
            PlayerPrefs.DeleteKey(LocalePreferenceKey);
            PlayerPrefs.Save();
        }

        public static string Text(string key)
        {
            return Text(key, null);
        }

        public static string Text(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key)) return fallback ?? string.Empty;

            if (TryGetCatalog(currentLocale, out Dictionary<string, string> current) &&
                current.TryGetValue(key, out string localized))
            {
                return localized;
            }

            if (!string.Equals(currentLocale, SourceLocale, StringComparison.OrdinalIgnoreCase) &&
                TryGetCatalog(SourceLocale, out Dictionary<string, string> source) &&
                source.TryGetValue(key, out string sourceText))
            {
                ReportMissingKeyOnce(currentLocale, key);
                return sourceText;
            }

            ReportMissingKeyOnce(currentLocale, key);
            return fallback ?? key;
        }

        public static string Format(string key, params object[] arguments)
        {
            string format = Text(key);
            try
            {
                return string.Format(CurrentCulture(), format, arguments ?? Array.Empty<object>());
            }
            catch (FormatException)
            {
                Debug.LogWarning("Localization format is invalid for key '" + key + "'.");
                return format;
            }
        }

        public static bool HasText(string locale, string key)
        {
            return TryGetCatalog(NormalizeLocale(locale), out Dictionary<string, string> catalog) &&
                   catalog.ContainsKey(key);
        }

        public static string LocalizedAudioCue(string baseCueId)
        {
            return string.IsNullOrWhiteSpace(baseCueId)
                ? string.Empty
                : baseCueId + "." + currentLocale;
        }

        private static bool TryGetCatalog(string locale, out Dictionary<string, string> catalog)
        {
            if (Catalogs.TryGetValue(locale, out catalog)) return true;

            TextAsset asset = Resources.Load<TextAsset>(CatalogResourcePrefix + locale);
            if (asset == null)
            {
                catalog = null;
                return false;
            }

            BeeLocalizationCatalogData data;
            try
            {
                data = JsonUtility.FromJson<BeeLocalizationCatalogData>(asset.text);
            }
            catch (Exception exception)
            {
                Debug.LogError("Unable to read localization catalog '" + locale + "': " + exception.Message);
                catalog = null;
                return false;
            }

            if (data == null || data.entries == null)
            {
                catalog = null;
                return false;
            }

            catalog = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int index = 0; index < data.entries.Length; index++)
            {
                BeeLocalizationEntryData entry = data.entries[index];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key)) continue;
                catalog[entry.key] = entry.value ?? string.Empty;
            }

            Catalogs[locale] = catalog;
            return true;
        }

        private static string NormalizeLocale(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale)) return SourceLocale;
            string trimmed = locale.Trim();
            if (trimmed.StartsWith("fr", StringComparison.OrdinalIgnoreCase)) return "fr-CA";
            if (trimmed.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en-US";
            return trimmed;
        }

        private static CultureInfo CurrentCulture()
        {
            try
            {
                return CultureInfo.GetCultureInfo(currentLocale);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        private static void ReportMissingKeyOnce(string locale, string key)
        {
            string diagnostic = locale + ":" + key;
            if (!ReportedMissingKeys.Add(diagnostic)) return;
            Debug.LogWarning("Missing localization key '" + key + "' for locale '" + locale + "'.");
        }
    }
}
