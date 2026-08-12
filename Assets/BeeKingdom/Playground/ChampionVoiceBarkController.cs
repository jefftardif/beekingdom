using System;
using System.Collections.Generic;
using BeeKingdom.Audio;
using UnityEngine;

namespace BeeKingdom.Playground
{
    // Voix des Championnes (demande de Jeff, 2026-08-02) : couche de reaction extremement legere qui
    // reutilise exclusivement des evenements DEJA existants (aucune nouvelle mecanique, aucun nouveau
    // panneau, aucun systeme de dialogue). Une Championne ne parle QUE si elle a une raison reelle de
    // le faire pour ce moment precis - sinon silence, jamais un mot pour combler le vide. Deux garde-
    // fous independants imposent cette retenue : un delai anti-chevauchement (base sur la duree reelle
    // du dernier clip) et un delai minimal par TYPE d'evenement (les moments rares - victoire,
    // decouverte legendaire - n'ont presque aucune retenue ; les actions frequentes - selection,
    // construction - en ont une large).
    //
    // Architecture prete pour des centaines de fichiers audio sans toucher au code : chaque clip vit
    // sous Resources/PremiumBeeReference/ChampionVoices/{championId}/{categorie}/*.mp3 (meme
    // convention que les portraits deja charges via Resources.Load). Ajouter un fichier dans un
    // dossier existant, ou creer un nouveau dossier de categorie, suffit - aucune recompilation requise
    // au-dela d'un rafraichissement d'assets.
    public static class ChampionVoiceBarkController
    {
        private const string ResourceRoot = "PremiumBeeReference/ChampionVoices";
        private const string FallbackCategory = "cit";

        // Marge ajoutee a la duree reelle du clip pour eviter deux voix qui se chevauchent.
        private const float OverlapBufferSeconds = 0.6f;

        // Volume dedie aux voix (plus fort que le volume SFX par defaut) - une voix qui a reellement
        // quelque chose a dire doit rester intelligible par-dessus la musique, jamais couverte par
        // elle (retour direct de Jeff, 2026-08-05 : voix trop faibles face a la musique).
        private const float VoiceVolumeScale = 1.15f;

        // Delai minimal (secondes) par TYPE d'evenement - c'est ce qui empeche "une phrase a chaque
        // clic" tout en laissant les moments rares et memorables toujours passer. Ajouter un futur
        // evenement se fait ici, avec une seule ligne.
        private static readonly Dictionary<string, float> EventMinGapSeconds = new(StringComparer.Ordinal)
        {
            ["select"] = 45f,
            ["building_launch"] = 90f,
            ["patrol_launch"] = 20f,
            ["collection_launch"] = 20f,
            ["victory"] = 0f,
            ["legendary_discovery"] = 0f,
            ["bestiary_open"] = 300f,
        };

        // Categorie audio primaire par evenement - reutilise directement les categories deja
        // enregistrees (select/spawn/move) quand elles correspondent naturellement, sinon reserve une
        // categorie dediee qui restera silencieuse jusqu'a ce que des fichiers y soient ajoutes.
        private static readonly Dictionary<string, string> EventPrimaryCategory = new(StringComparer.Ordinal)
        {
            ["select"] = "select",
            ["building_launch"] = "building",
            ["patrol_launch"] = "spawn",
            ["collection_launch"] = "move",
            ["victory"] = "victory",
            ["legendary_discovery"] = "legendary",
            ["bestiary_open"] = "bestiary",
        };

        private static readonly Dictionary<string, AudioClip[]> clipCache = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> lastClipIndexByKey = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, float> lastBarkTimeByEvent = new(StringComparer.Ordinal);
        private static float busyUntilUnscaledTime = float.NegativeInfinity;

        public static void BarkForSelection(string beeId)
        {
            if (string.IsNullOrEmpty(beeId)) return;
            TryBark("select", new[] { beeId });
        }

        public static void BarkForBuildingLaunch(IReadOnlyList<string> assignedBeeIds) => TryBark("building_launch", assignedBeeIds);

        public static void BarkForCollectionLaunch(IReadOnlyList<string> assignedBeeIds) => TryBark("collection_launch", assignedBeeIds);

        // La Championne la plus pertinente pour une patrouille est celle dont le role de combat
        // correspond a la famille de troupes dominante engagee (ex: majorite de Gardiennes -> Striga).
        public static void BarkForPatrolLaunch(IReadOnlyList<string> assignedBeeIds, IReadOnlyDictionary<string, long> committedTroops)
        {
            string preferredBeeId = PreferredByDominantFamily(assignedBeeIds, committedTroops);
            TryBark("patrol_launch", preferredBeeId != null ? new[] { preferredBeeId } : assignedBeeIds);
        }

        // Ne parle que si elle a reellement contribue a CE combat precis - jamais une championne
        // absente qui s'attribuerait une victoire qui n'est pas la sienne.
        public static void BarkForVictory(IReadOnlyList<string> contributingChampionBeeIds) => TryBark("victory", contributingChampionBeeIds);

        public static void BarkForLegendaryDiscovery(IReadOnlyList<string> lastContributingChampionBeeIds) => TryBark("legendary_discovery", lastContributingChampionBeeIds);

        public static void BarkForBestiaryOpen(IReadOnlyList<string> assignedBeeIds) => TryBark("bestiary_open", assignedBeeIds);

        private static string PreferredByDominantFamily(IReadOnlyList<string> assignedBeeIds, IReadOnlyDictionary<string, long> committedTroops)
        {
            if (assignedBeeIds == null || assignedBeeIds.Count == 0 || committedTroops == null) return null;
            string dominantFamily = null;
            long best = 0;
            foreach (KeyValuePair<string, long> troop in committedTroops)
            {
                if (troop.Value <= best) continue;
                best = troop.Value;
                dominantFamily = troop.Key;
            }
            if (dominantFamily == null) return null;
            foreach (string beeId in assignedBeeIds)
            {
                if (!ChampionBeeCatalog.TryResolve(beeId, out ChampionBeeDefinition definition)) continue;
                if (string.Equals(ChampionBeeCatalog.CombatFamilyId(definition.Role), dominantFamily, StringComparison.Ordinal)) return beeId;
            }
            return null;
        }

        private static void TryBark(string eventKey, IReadOnlyList<string> candidateBeeIds)
        {
            if (candidateBeeIds == null || candidateBeeIds.Count == 0) return; // personne de pertinent - silence
            float now = Time.unscaledTime;
            if (now < busyUntilUnscaledTime) return; // une autre championne parle encore
            float minGap = EventMinGapSeconds.GetValueOrDefault(eventKey, 30f);
            if (lastBarkTimeByEvent.TryGetValue(eventKey, out float lastTime) && now - lastTime < minGap) return;

            string beeId = candidateBeeIds.Count == 1 ? candidateBeeIds[0] : candidateBeeIds[UnityEngine.Random.Range(0, candidateBeeIds.Count)];
            string primaryCategory = EventPrimaryCategory.GetValueOrDefault(eventKey, FallbackCategory);
            AudioClip clip = PickClip(beeId, primaryCategory);
            if (clip == null && primaryCategory != FallbackCategory) clip = PickClip(beeId, FallbackCategory);
            if (clip == null) return; // rien d'enregistre pour cette championne - silence, jamais de substitut invente

            float duckSeconds = clip.length + OverlapBufferSeconds;
            MusicManager.Instance?.DuckForVoice(duckSeconds);
            AudioManager.EnsureInstance().PlaySound(clip, VoiceVolumeScale);
            busyUntilUnscaledTime = now + duckSeconds;
            lastBarkTimeByEvent[eventKey] = now;
        }

        private static AudioClip PickClip(string beeId, string category)
        {
            string cacheKey = beeId + "|" + category;
            if (!clipCache.TryGetValue(cacheKey, out AudioClip[] clips))
            {
                clips = Resources.LoadAll<AudioClip>(ResourceRoot + "/" + beeId + "/" + category);
                clipCache[cacheKey] = clips;
            }
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];

            int lastIndex = lastClipIndexByKey.GetValueOrDefault(cacheKey, -1);
            int index;
            do { index = UnityEngine.Random.Range(0, clips.Length); } while (index == lastIndex);
            lastClipIndexByKey[cacheKey] = index;
            return clips[index];
        }
    }
}
