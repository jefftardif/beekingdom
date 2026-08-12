using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BeeKingdom.Playground
{
    [Serializable]
    public sealed class LocalPreviewBestiarySightings
    {
        public List<string> seenTokens = new List<string>();
    }

    public interface ILocalPreviewBestiarySightingsStore
    {
        string Read();
        void Write(string json);
    }

    public sealed class PlayerPrefsLocalPreviewBestiarySightingsStore : ILocalPreviewBestiarySightingsStore
    {
        private const string Key = "BeeKingdom_LivingHive_BestiarySightings_v1";
        public string Read() => PlayerPrefs.GetString(Key, string.Empty);
        public void Write(string json) { PlayerPrefs.SetString(Key, json ?? string.Empty); PlayerPrefs.Save(); }
    }

    // Etat "Apercue" par variante cosmetique (demande de Jeff, 2026-08-01 - Game Design du Carnet du
    // Bestiaire) : purement client-local, jamais transmis au serveur, qui ne connait et ne connaitra
    // jamais la Variante (voir WorldBestiaryNode.Variant dans WorldMapMmoFullscreenFoundationBootstrap).
    // Complete cote client les 14 identites du carnet au-dela des 7 Tiers suivis par le serveur
    // (BestiaryCodexState) - aucune recompense n'y est jamais attachee, seulement de la decouverte.
    public static class LocalPreviewBestiarySightingsTracker
    {
        private static ILocalPreviewBestiarySightingsStore store = new PlayerPrefsLocalPreviewBestiarySightingsStore();
        private static LocalPreviewBestiarySightings state;

        public static void UseStoreForProof(ILocalPreviewBestiarySightingsStore replacement)
        {
            store = replacement ?? new PlayerPrefsLocalPreviewBestiarySightingsStore();
            state = null;
        }

        public static bool HasSeen(int tier, int variant) => EnsureLoaded().seenTokens.Contains(Token(tier, variant));

        public static int SeenCount() => EnsureLoaded().seenTokens.Count;

        // Retourne true seulement la premiere fois que cette identite precise est apercue (utile
        // pour declencher un feedback ponctuel "nouvelle creature decouverte" sans dupliquer l'ecriture).
        public static bool RecordSighting(int tier, int variant)
        {
            LocalPreviewBestiarySightings loaded = EnsureLoaded();
            string token = Token(tier, variant);
            if (loaded.seenTokens.Contains(token)) return false;
            loaded.seenTokens.Add(token);
            store.Write(JsonUtility.ToJson(loaded));
            return true;
        }

        private static string Token(int tier, int variant) => tier.ToString(CultureInfo.InvariantCulture) + "_" + variant.ToString(CultureInfo.InvariantCulture);

        private static LocalPreviewBestiarySightings EnsureLoaded()
        {
            if (state != null) return state;
            string json = store.Read();
            try { state = string.IsNullOrWhiteSpace(json) ? new LocalPreviewBestiarySightings() : JsonUtility.FromJson<LocalPreviewBestiarySightings>(json); }
            catch { state = new LocalPreviewBestiarySightings(); }
            state.seenTokens ??= new List<string>();
            return state;
        }
    }
}
