using System;
using System.Globalization;
using BeeKingdom.Localization;
using BeeKingdom.Playground;
using UnityEngine;

namespace BeeKingdom.LivingHiveMenu
{
    // ÉTAT ET LOGIQUE PURS de la fenêtre Recherche plein écran (Local Preview) pour la
    // scène Environment2D5D_SpatialV3.
    //
    // Le monolithe (HiveViewProductUiPresenter) ne pilote la recherche qu'en "aperçu local"
    // (pas de serveur). Cette classe porte la MÊME logique locale en s'appuyant sur la même
    // source de vérité partagée (LocalPreviewQueueJournal + PlayerPrefsLocalPreviewQueueJournalStore),
    // afin qu'une recherche lancée dans une session se retrouve à la réouverture (miroir de
    // RestoreLocalPreviewResearch L.21353 / StartPreviewResearch L.41611 /
    // CompletePreviewResearchIfReady L.24933).
    //
    // Aucune dépendance UI : la fenêtre lit cet état. Testable hors play-mode (horloges
    // injectables ForProof).
    public sealed class LivingHiveResearchState
    {
        // --- Préviews économiques locales (miroir des statiques monolithe L.615/619) ---
        public float LocalPreviewHoney = LivingHiveResearchSpec.PreviewHoney;
        public float LocalPreviewPollen = LivingHiveResearchSpec.PreviewPollen;

        // --- Référence de persistance partagée (même PlayerPrefs que le monolithe) ---
        public ILocalPreviewQueueJournalStore Store { get; set; }
        public LocalPreviewQueueJournal Journal { get; private set; }
        public bool JournalLoaded { get; private set; }

        // --- État local en cours (miroir L.680-686) ---
        public string LocalPreviewResearchId = string.Empty;
        public float LocalPreviewResearchStartedAt = -100f;
        public float LocalPreviewResearchDuration = 16f;
        public bool LocalPreviewResearchApplied;

        // --- Filtre plein écran actif (miroir researchFullscreenFilter L.1237, défaut "all") ---
        private string filter = LivingHiveResearchSpec.FilterAll;

        public string SelectedFilterForProof
        {
            get { return filter; }
        }

        public void SetFilterForProof(string value)
        {
            filter = LivingHiveResearchSpec.Filters != null
                && System.Array.IndexOf(LivingHiveResearchSpec.Filters, value) >= 0
                ? value
                : LivingHiveResearchSpec.FilterAll;
        }

        // --- Horloges injectables (ForProof, défaut miroir du monolithe) ---
        public Func<float> NowProvider;
        public Func<long> UtcNowTicksProvider;

        public LivingHiveResearchState()
        {
            Store = new PlayerPrefsLocalPreviewQueueJournalStore();
            NowProvider = DefaultNow;
            UtcNowTicksProvider = () => DateTime.UtcNow.Ticks;
        }

        public static float DefaultNow()
        {
            return Application.isPlaying ? Time.realtimeSinceStartup : 1.6f;
        }

        public float NowForUi()
        {
            return NowProvider != null ? NowProvider() : DefaultNow();
        }

        private long UtcNowTicks()
        {
            return UtcNowTicksProvider != null ? UtcNowTicksProvider() : DateTime.UtcNow.Ticks;
        }

        // --- Persistance (miroir EnsureLocalPreviewQueueJournalLoaded L.20964) ---

        public void EnsureLoaded()
        {
            if (JournalLoaded) return;
            JournalLoaded = true;
            Journal = LocalPreviewQueueJournalCodec.Read(Store);
            RestoreResearch(Journal != null ? Journal.research : null);
        }

        public void ResetForProof()
        {
            JournalLoaded = false;
            Journal = new LocalPreviewQueueJournal();
            LocalPreviewResearchId = string.Empty;
            LocalPreviewResearchApplied = false;
            LocalPreviewResearchStartedAt = -100f;
            LocalPreviewResearchDuration = 16f;
            LocalPreviewHoney = LivingHiveResearchSpec.PreviewHoney;
            LocalPreviewPollen = LivingHiveResearchSpec.PreviewPollen;
        }

        public void ClearPersistedForProof()
        {
            if (Store != null) Store.Delete();
            ResetForProof();
        }

        // Miroir de RestoreLocalPreviewResearch (L.21353) : reprend la recherche en cours
        // depuis le journal (durée = ticks, élapsed = ticks, applied = completionClaimed).
        private void RestoreResearch(LocalPreviewQueueOperation operation)
        {
            if (operation == null || !operation.Exists) return;
            LocalPreviewResearchDefinition definition = LocalPreviewResearchCatalog.Find(operation.targetId);
            if (definition == null) return;

            LocalPreviewResearchId = operation.targetId;
            LocalPreviewResearchDuration = Mathf.Max(0.1f, (float)new TimeSpan(Math.Max(1L, operation.endsUtcTicks - operation.startedUtcTicks)).TotalSeconds);
            float elapsed = Mathf.Max(0f, (float)new TimeSpan(Math.Max(0L, UtcNowTicks() - operation.startedUtcTicks)).TotalSeconds);
            LocalPreviewResearchStartedAt = NowForUi() - elapsed;
            LocalPreviewResearchApplied = operation.completionClaimed;
        }

        // --- État actif (miroir IsResearchRunning L.25566 / ResearchProgress01 L.25571) ---

        public bool IsResearchRunning()
        {
            return !string.IsNullOrWhiteSpace(LocalPreviewResearchId) && !LocalPreviewResearchApplied;
        }

        public float ResearchProgress01()
        {
            if (string.IsNullOrWhiteSpace(LocalPreviewResearchId)) return 0f;
            return Mathf.Clamp01((NowForUi() - LocalPreviewResearchStartedAt) / Mathf.Max(0.1f, LocalPreviewResearchDuration));
        }

        public bool IsCompleted(string researchId)
        {
            EnsureLoaded();
            return Journal != null && LocalPreviewResearchCatalog.Contains(Journal.completedResearchIds, researchId);
        }

        // Miroir de ResearchDisabledReason (L.41550) : raison de blocage d'une carte.
        public string ResearchDisabledReason(LocalPreviewResearchDefinition definition)
        {
            EnsureLoaded();
            if (definition == null) return BeeLocalization.Text("research.reason.unknown", "Étude inconnue");
            if (IsCompleted(definition.ResearchId)) return BeeLocalization.Text("research.reason.completed", "Étude déjà terminée");
            if (IsResearchRunning())
            {
                return string.Equals(LocalPreviewResearchId, definition.ResearchId, StringComparison.Ordinal)
                    ? BeeLocalization.Text("research.reason.already_running", "Étude déjà en cours")
                    : BeeLocalization.Text("research.reason.queue_busy", "Une autre étude est en cours");
            }
            if (LocalPreviewHoney < definition.HoneyCost)
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    BeeLocalization.Text("research.reason.honey_missing", "Miel manquant : {0}"),
                    Mathf.CeilToInt(definition.HoneyCost - LocalPreviewHoney));
            }
            if (LocalPreviewPollen < definition.PollenCost)
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    BeeLocalization.Text("research.reason.pollen_missing", "Pollen manquant : {0}"),
                    Mathf.CeilToInt(definition.PollenCost - LocalPreviewPollen));
            }
            return string.Empty;
        }

        // Miroir de ResearchShortageResource (L.41573).
        public string ResearchShortageResource(LocalPreviewResearchDefinition definition)
        {
            EnsureLoaded();
            if (definition == null || IsResearchRunning() || IsCompleted(definition.ResearchId)) return string.Empty;
            if (LocalPreviewHoney < definition.HoneyCost) return "honey";
            if (LocalPreviewPollen < definition.PollenCost) return "pollen";
            return string.Empty;
        }

        // Miroir de ResearchCostText (L.41600).
        public static string ResearchCostText(LocalPreviewResearchDefinition definition)
        {
            if (definition == null) return string.Empty;
            return string.Format(
                CultureInfo.CurrentCulture,
                BeeLocalization.Text("research.cost", "{0} miel · {1} pollen · {2}s"),
                Mathf.RoundToInt(definition.HoneyCost),
                Mathf.RoundToInt(definition.PollenCost),
                Mathf.RoundToInt(definition.DurationSeconds));
        }

        // Miroir de ResearchTitle / ResearchResult (L.25577/25583) via la localisation.
        public static string ResearchTitle(LocalPreviewResearchDefinition definition)
        {
            if (definition == null) return string.Empty;
            return BeeLocalization.Text(definition.TitleKey, definition.ResearchId);
        }

        public static string ResearchSummary(LocalPreviewResearchDefinition definition)
        {
            if (definition == null) return string.Empty;
            return BeeLocalization.Text(definition.SummaryKey, definition.ResearchId);
        }

        // Miroir de NewResearchQueueOperation (L.21406).
        private static LocalPreviewQueueOperation NewResearchQueueOperation(LocalPreviewResearchDefinition definition)
        {
            long started = DateTime.UtcNow.Ticks;
            return new LocalPreviewQueueOperation
            {
                operationId = Guid.NewGuid().ToString("N"),
                targetId = definition.ResearchId,
                startedUtcTicks = started,
                endsUtcTicks = started + TimeSpan.FromSeconds(definition.DurationSeconds).Ticks,
                honeyCost = definition.HoneyCost,
                waxCost = 0f,
                pollenCost = definition.PollenCost,
                completionClaimed = false,
                resultValue = 1
            };
        }

        // Miroir de StartPreviewResearch (L.41611) : déduit le coût, écrit le journal.
        // Retourne false (avec raison) si bloqué.
        public bool StartPreviewResearch(string researchId)
        {
            EnsureLoaded();
            LocalPreviewResearchDefinition definition = LocalPreviewResearchCatalog.Find(researchId);
            string reason = ResearchDisabledReason(definition);
            if (!string.IsNullOrWhiteSpace(reason)) return false;

            LocalPreviewHoney -= definition.HoneyCost;
            LocalPreviewPollen -= definition.PollenCost;
            LocalPreviewResearchId = definition.ResearchId;
            LocalPreviewResearchDuration = definition.DurationSeconds;
            LocalPreviewResearchStartedAt = NowForUi();
            LocalPreviewResearchApplied = false;
            Journal.research = NewResearchQueueOperation(definition);
            SaveLocalPreviewQueueJournal();
            return true;
        }

        // Miroir de CompletePreviewResearchIfReady (L.24933) : clôture la recherche arrivée
        // à 100%, marque completionClaimed + completedResearchIds, sauvegarde.
        public bool CompletePreviewResearchIfReady()
        {
            EnsureLoaded();
            if (!IsResearchRunning() || ResearchProgress01() < 1f || LocalPreviewResearchApplied) return false;

            LocalPreviewQueueOperation operation = Journal.research;
            if (operation == null || !operation.Exists || !string.Equals(operation.targetId, LocalPreviewResearchId, StringComparison.Ordinal)) return false;

            operation.completionClaimed = true;
            if (!LocalPreviewResearchCatalog.Contains(Journal.completedResearchIds, LocalPreviewResearchId))
            {
                Journal.completedResearchIds.Add(LocalPreviewResearchId);
            }
            SaveLocalPreviewQueueJournal();
            LocalPreviewResearchApplied = true;
            return true;
        }

        public void SaveLocalPreviewQueueJournal()
        {
            if (Journal == null) return;
            LocalPreviewQueueJournalCodec.Write(Store, Journal);
        }

        // --- Helpers ForProof utilisés par la fenêtre / les tests ---

        public enum CardStatus
        {
            Available,
            Running,
            Completed
        }

        // Recherche inconnue => raison "Étude inconnue" (miroir ResearchDisabledReason L.41550).
        public string ResearchDisabledReasonForProof(string researchId)
        {
            LocalPreviewResearchDefinition definition = LocalPreviewResearchCatalog.Find(researchId);
            return ResearchDisabledReason(definition);
        }

        // État d'affichage d'une carte (miroir des branches de DrawResearchFullscreenCard).
        public CardStatus StatusForProof(string researchId)
        {
            EnsureLoaded();
            if (IsCompleted(researchId)) return CardStatus.Completed;
            if (IsResearchRunning() && string.Equals(LocalPreviewResearchId, researchId, System.StringComparison.Ordinal))
                return CardStatus.Running;
            return CardStatus.Available;
        }

        // Le bouton n'est cliquable que si la recherche est lançable (miroir L.33454/33459).
        public bool IsCardEnabledForProof(string researchId)
        {
            LocalPreviewResearchDefinition definition = LocalPreviewResearchCatalog.Find(researchId);
            if (definition == null) return false;
            return string.IsNullOrWhiteSpace(ResearchDisabledReason(definition));
        }

        // Progression globale courante (miroir ResearchProgress01, utilisée pour la barre).
        public float ResearchProgress01State()
        {
            return ResearchProgress01();
        }

        // Re-dérive la recherche en cours depuis le journal à chaque ouverture afin de
        // refléter le vrai écoulement (complétée pendant l'absence => reprise exacte).
        public void RefreshRunningFromJournal()
        {
            EnsureLoaded();
            if (Journal == null) return;
            RestoreResearch(Journal.research);
        }
    }
}