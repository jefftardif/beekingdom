using System.Collections.Generic;

namespace BeeKingdom.Tutorial
{
    public static class FtueChapterDefinitions
    {
        public static FtueChapterDefinition BuildFtueHiveIntroPart1()
        {
            // Mapping M037 spec — 9 steps, names joueur-facing Caserne = guard_post, Palais Royal = administration_core
            var steps = new List<FtueStepDefinition>
            {
                // STEP 1 — Welcome dialogue
                new FtueStepDefinition(
                    "ftue.intro.welcome",
                    FtueStepKind.Dialogue,
                    FtueInteractionMode.HighlightOnly,
                    null,
                    "zephyra",
                    "Bienvenue dans ta ruche ! Je suis Zephyra, ta guide. Ta colonie t'attend.",
                    "ftue.intro.royal_intro",
                    FtueEventKind.DialogueContinue),

                // STEP 2 — Présenter Palais Royal, flèche vers Royal Palace
                new FtueStepDefinition(
                    "ftue.intro.royal_intro",
                    FtueStepKind.HighlightBuilding,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetRoyalPalace,
                    "zephyra",
                    "Voici le Palais Royal — le coeur de ta ruche. Il détermine le niveau max des autres bâtiments.",
                    "ftue.intro.royal_tap",
                    FtueEventKind.DialogueContinue),

                // STEP 3 — Tap Palais Royal (required)
                new FtueStepDefinition(
                    "ftue.intro.royal_tap",
                    FtueStepKind.RequireBuildingTap,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetRoyalPalace,
                    "zephyra",
                    "Touche le Palais Royal pour l'ouvrir.",
                    "ftue.intro.colony_dialogue",
                    FtueEventKind.BuildingSelected,
                    "administration_core"),

                // STEP 4 — Dialogue court bâtiments
                new FtueStepDefinition(
                    "ftue.intro.colony_dialogue",
                    FtueStepKind.Dialogue,
                    FtueInteractionMode.HighlightOnly,
                    null,
                    "zephyra",
                    "Chaque bâtiment fait progresser ta colonie. Améliore-les pour débloquer de nouvelles capacités.",
                    "ftue.intro.barrack_intro",
                    FtueEventKind.DialogueContinue),

                // STEP 5 — Diriger vers Caserne (guard_post)
                new FtueStepDefinition(
                    "ftue.intro.barrack_intro",
                    FtueStepKind.HighlightBuilding,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetGuardPost,
                    "striga",
                    "Voici la Caserne — elle entraîne tes gardiennes et augmente ta puissance.",
                    "ftue.intro.barrack_open",
                    FtueEventKind.DialogueContinue),

                // STEP 6 — Ouvrir fenêtre Caserne
                new FtueStepDefinition(
                    "ftue.intro.barrack_open",
                    FtueStepKind.RequireWindowOpened,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetGuardPost,
                    "striga",
                    "Ouvre la Caserne.",
                    "ftue.intro.upgrade_highlight",
                    FtueEventKind.WindowOpened,
                    "guard_post"),

                // STEP 7 — Flèche vers bouton Upgrade
                new FtueStepDefinition(
                    "ftue.intro.upgrade_highlight",
                    FtueStepKind.HighlightUpgradeButton,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetUpgradeButton,
                    "striga",
                    "Lance l'amélioration pour passer au niveau 2. Coût : 972 Miel, 251 Cire.",
                    "ftue.intro.upgrade_started",
                    FtueEventKind.DialogueContinue),

                // STEP 8 — Démarrer amélioration (vrai gameplay)
                new FtueStepDefinition(
                    "ftue.intro.upgrade_started",
                    FtueStepKind.RequireUpgradeStarted,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetUpgradeButton,
                    "striga",
                    "Appuie sur Améliorer pour démarrer.",
                    "ftue.intro.timer_dialogue",
                    FtueEventKind.UpgradeStarted,
                    "guard_post"),

                // STEP 9 — Dialogue timer
                new FtueStepDefinition(
                    "ftue.intro.timer_dialogue",
                    FtueStepKind.Dialogue,
                    FtueInteractionMode.HighlightOnly,
                    null,
                    "zephyra",
                    "Parfait ! L'amélioration est en cours (3 min). Tu peux suivre le timer. Continue de développer ta ruche !",
                    "ftue.intro.upgrade_claim",
                    FtueEventKind.DialogueContinue),

                // STEP 10 — M040-CL: reclamer reellement l'amelioration une fois le minuteur ecoule
                // (demande live de Jeff - demarrer l'amelioration n'est pas la tache complete, le
                // joueur doit aussi la valider/recuperer). Meme bouton reel que le demarrage
                // (TargetUpgradeButton), qui affiche "Valider" une fois le timer termine.
                new FtueStepDefinition(
                    "ftue.intro.upgrade_claim",
                    FtueStepKind.RequireUpgradeCompleted,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetUpgradeButton,
                    "striga",
                    "Ton amélioration est prête ! Appuie sur Valider pour la récupérer.",
                    null, // chapter complete
                    FtueEventKind.UpgradeCompleted,
                    "guard_post")
            };

            return new FtueChapterDefinition(FtueTutorialRegistry.ChapterFtueHiveIntroPart1, "ftue.intro.welcome", steps);
        }

        // M038-CL — FTUE_HIVE_CORE_PART2: Research, Training, Army. Same engine, own chapter (kept
        // separate from Part1 per the mission's explicit preference, chained via
        // FtueTutorialBootstrap.OnChapterCompleted rather than merged into one giant chapter).
        // First Research = tempered_combs_i (180 honey / 120 pollen, no prerequisite — see M038 report).
        // First Training = darters (500 honey / 120 pollen, always-unlocked, cheapest family).
        // Army section observes the real roster and a real local squad-selection interaction; it does
        // NOT require Confirm Squad (CombatSquadReservation is disabled server-side today — see report).
        public static FtueChapterDefinition BuildFtueHiveCorePart2()
        {
            var steps = new List<FtueStepDefinition>
            {
                // STEP 1 — Transition dialogue
                new FtueStepDefinition(
                    "ftue.core2.welcome",
                    FtueStepKind.Dialogue,
                    FtueInteractionMode.HighlightOnly,
                    null,
                    "zephyra",
                    "Ta Caserne s'améliore. Voyons maintenant comment faire progresser ta colonie durablement.",
                    "ftue.core2.research_intro",
                    FtueEventKind.DialogueContinue),

                // ---- RESEARCH ----
                // STEP 2 — Flèche vers le Noeud de Recherche
                new FtueStepDefinition(
                    "ftue.core2.research_intro",
                    FtueStepKind.HighlightBuilding,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetResearchNode,
                    "zephyra",
                    "Voici le Noeud de Recherche. La Recherche améliore durablement ta colonie.",
                    "ftue.core2.research_open",
                    FtueEventKind.DialogueContinue),

                // STEP 3 — Ouvrir la vraie fenêtre Research
                new FtueStepDefinition(
                    "ftue.core2.research_open",
                    FtueStepKind.RequireWindowOpened,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetResearchNode,
                    "zephyra",
                    "Touche le Noeud de Recherche pour l'ouvrir.",
                    "ftue.core2.research_select_highlight",
                    FtueEventKind.WindowOpened,
                    "research_node"),

                // STEP 4 — Flèche vers le bouton de démarrage, choisir "Rayons tempérés"
                // M040-CL: corrige le nom affiche - le catalogue reel (LocalPreviewResearchCatalog,
                // meme source que le panneau officiel) nomme tempered_combs_i "Rayons tempérés",
                // pas "Combs tempérés" (texte jamais verifie contre le vrai catalogue).
                new FtueStepDefinition(
                    "ftue.core2.research_select_highlight",
                    FtueStepKind.HighlightActionButton,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetResearchStartButton,
                    "zephyra",
                    "Choisis \"Rayons tempérés\" puis lance la recherche.",
                    "ftue.core2.research_started",
                    FtueEventKind.DialogueContinue),

                // STEP 5 — Démarrer la vraie recherche (gameplay réel)
                new FtueStepDefinition(
                    "ftue.core2.research_started",
                    FtueStepKind.RequireResearchStarted,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetResearchStartButton,
                    "zephyra",
                    "Lance la recherche.",
                    "ftue.core2.research_timer_dialogue",
                    FtueEventKind.ResearchStarted,
                    FtueTutorialRegistry.FirstResearchId),

                // STEP 6 — Dialogue timer Research
                new FtueStepDefinition(
                    "ftue.core2.research_timer_dialogue",
                    FtueStepKind.Dialogue,
                    FtueInteractionMode.HighlightOnly,
                    null,
                    "zephyra",
                    "Bien joué ! Cette recherche continue même si tu quittes le jeu.",
                    "ftue.core2.collect_intro",
                    FtueEventKind.DialogueContinue),

                // ---- COLLECT (M038B-CL: real step, closes the FTUE economy gap before Training) ----
                // STEP 6b — Flèche vers la Réserve de miel
                new FtueStepDefinition(
                    "ftue.core2.collect_intro",
                    FtueStepKind.HighlightBuilding,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetHoneyReserve,
                    "zephyra",
                    "Ta Réserve de miel a accumulé des ressources en attendant. Récolte-les avant l'entraînement.",
                    "ftue.core2.collect_started",
                    FtueEventKind.DialogueContinue),

                // STEP 6c — Récolter réellement (vrai gameplay, vraie mutation serveur)
                new FtueStepDefinition(
                    "ftue.core2.collect_started",
                    FtueStepKind.RequireProductionCollected,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetHoneyReserve,
                    "zephyra",
                    "Touche la Réserve de miel pour récolter.",
                    "ftue.core2.training_intro",
                    FtueEventKind.ProductionCollected,
                    "honey_storage"),

                // ---- TRAINING ----
                // STEP 7 — Flèche vers la Caserne pour l'entraînement
                new FtueStepDefinition(
                    "ftue.core2.training_intro",
                    FtueStepKind.HighlightBuilding,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetGuardPost,
                    "striga",
                    "Retournons à la Caserne pour entraîner tes premières troupes.",
                    "ftue.core2.training_open",
                    FtueEventKind.DialogueContinue),

                // STEP 8 — Ouvrir la Caserne
                new FtueStepDefinition(
                    "ftue.core2.training_open",
                    FtueStepKind.RequireWindowOpened,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetGuardPost,
                    "striga",
                    "Ouvre la Caserne.",
                    "ftue.core2.training_select_highlight",
                    FtueEventKind.WindowOpened,
                    "guard_post"),

                // STEP 9 — Flèche vers le bouton de démarrage, choisir Lanceuses (Darters)
                new FtueStepDefinition(
                    "ftue.core2.training_select_highlight",
                    FtueStepKind.HighlightActionButton,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetTrainingStartButton,
                    "striga",
                    "Choisis les Lanceuses puis lance l'entraînement.",
                    "ftue.core2.training_started",
                    FtueEventKind.DialogueContinue),

                // STEP 10 — Démarrer le vrai entraînement (gameplay réel)
                new FtueStepDefinition(
                    "ftue.core2.training_started",
                    FtueStepKind.RequireTrainingStarted,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetTrainingStartButton,
                    "striga",
                    "Lance l'entraînement.",
                    "ftue.core2.training_timer_dialogue",
                    FtueEventKind.TrainingStarted,
                    FtueTutorialRegistry.FirstTrainingFamily),

                // STEP 11 — Dialogue timer Training
                new FtueStepDefinition(
                    "ftue.core2.training_timer_dialogue",
                    FtueStepKind.Dialogue,
                    FtueInteractionMode.HighlightOnly,
                    null,
                    "striga",
                    "Tes troupes s'entraînent. Cette opération continue même lorsque tu quittes le jeu.",
                    "ftue.core2.army_intro",
                    FtueEventKind.DialogueContinue),

                // ---- ARMY ----
                // STEP 12 — Flèche vers le menu Armée
                new FtueStepDefinition(
                    "ftue.core2.army_intro",
                    FtueStepKind.HighlightActionButton,
                    FtueInteractionMode.HighlightOnly,
                    FtueTutorialRegistry.TargetArmyMenu,
                    "zephyra",
                    "Les troupes entraînées rejoignent réellement ton armée. Ouvre le menu Armée.",
                    "ftue.core2.army_open",
                    FtueEventKind.DialogueContinue),

                // STEP 13 — Ouvrir la vraie fenêtre Army
                new FtueStepDefinition(
                    "ftue.core2.army_open",
                    FtueStepKind.RequireWindowOpened,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetArmyMenu,
                    "zephyra",
                    "Touche le menu Armée pour l'ouvrir.",
                    "ftue.core2.army_interact",
                    FtueEventKind.WindowOpened,
                    "army"),

                // STEP 14 — Interagir réellement avec la composition d'escouade (sélection locale réelle,
                // ne dépend pas de la confirmation d'escouade actuellement désactivée en production)
                new FtueStepDefinition(
                    "ftue.core2.army_interact",
                    FtueStepKind.RequireArmyInteraction,
                    FtueInteractionMode.RequiredTarget,
                    FtueTutorialRegistry.TargetArmyMenu,
                    "zephyra",
                    "Touche + à côté d'une famille de troupes pour l'ajouter à ton escouade.",
                    "ftue.core2.farewell",
                    FtueEventKind.ArmyInteracted),

                // STEP 15 — Transition narrative vers PART3 (non implémentée ici)
                new FtueStepDefinition(
                    "ftue.core2.farewell",
                    FtueStepKind.Dialogue,
                    FtueInteractionMode.HighlightOnly,
                    null,
                    "zephyra",
                    "Ta colonie grandit. Bientôt, nous nous aventurerons au-delà de la Ruche.",
                    null, // chapter complete — PART3 (WorldMap) not implemented by this mission
                    FtueEventKind.DialogueContinue)
            };

            return new FtueChapterDefinition(FtueTutorialRegistry.ChapterFtueHiveCorePart2, "ftue.core2.welcome", steps);
        }

        public static Dictionary<string, FtueChapterDefinition> All => new Dictionary<string, FtueChapterDefinition>(System.StringComparer.Ordinal)
        {
            { FtueTutorialRegistry.ChapterFtueHiveIntroPart1, BuildFtueHiveIntroPart1() },
            { FtueTutorialRegistry.ChapterFtueHiveCorePart2, BuildFtueHiveCorePart2() }
        };
    }
}
