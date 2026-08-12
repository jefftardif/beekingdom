using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxOpeningActObjectivePacingReport
    {
        private const string OutputDirectory = "Artifacts/OpeningActPacing";
        private const string ReportPath = OutputDirectory + "/OpeningActObjectivePacing.md";

        [MenuItem("Bee Kingdom/Playground/QA/Generate Opening Act Objective Pacing Report")]
        public static void Generate()
        {
            Directory.CreateDirectory(OutputDirectory);
            string[] proofRows = HiveViewProductUiPresenter.GuidedOpeningActObjectivePacingForProof();
            ObjectiveRow[] objectives = proofRows
                .Where(row => row.StartsWith("objective:", StringComparison.Ordinal))
                .Select(ParseObjective)
                .ToArray();

            var builder = new StringBuilder();
            builder.AppendLine("# Bee Kingdom - Profil du rythme actif de l'Acte I");
            builder.AppendLine();
            builder.AppendLine("- Source: crochets de preuve `LivingHive`, Unity 6000.5.3f1");
            builder.AppendLine("- Portee: chapitres 1 a 5, 68 objectifs lies");
            builder.AppendLine("- Interaction active: decision, controle ou collecte manuelle");
            builder.AppendLine("- Non mesure: temps de lecture, navigation et hesitation du joueur");
            builder.AppendLine("- Attente seule consideree comme contenu: non");
            builder.AppendLine();
            builder.AppendLine("## Synthese par chapitre");
            builder.AppendLine();
            builder.AppendLine("| Chapitre | Objectifs | Decisions | Controles | Collectes | Interactions actives | Temps chrono rapide | Temps chrono lent |");
            builder.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");

            for (int chapter = 1; chapter <= 5; chapter++)
            {
                ObjectiveRow[] chapterRows = objectives.Where(row => row.Chapter == chapter).ToArray();
                builder.AppendLine(
                    "| " + chapter.ToString(CultureInfo.InvariantCulture)
                    + " | " + chapterRows.Length.ToString(CultureInfo.InvariantCulture)
                    + " | " + chapterRows.Sum(row => row.Decisions).ToString(CultureInfo.InvariantCulture)
                    + " | " + chapterRows.Sum(row => row.Checks).ToString(CultureInfo.InvariantCulture)
                    + " | " + chapterRows.Sum(row => row.Collections).ToString(CultureInfo.InvariantCulture)
                    + " | " + chapterRows.Sum(row => row.ActiveInteractions).ToString(CultureInfo.InvariantCulture)
                    + " | " + chapterRows.Sum(row => row.FastSeconds).ToString(CultureInfo.InvariantCulture) + " s"
                    + " | " + chapterRows.Sum(row => row.SlowSeconds).ToString(CultureInfo.InvariantCulture) + " s |");
            }

            builder.AppendLine();
            builder.AppendLine("## Detail par objectif");
            builder.AppendLine();
            builder.AppendLine("| Objectif | Etape | Rapide | Lent | Decisions | Controles | Collectes |");
            builder.AppendLine("|---|---|---:|---:|---:|---:|---:|");
            foreach (ObjectiveRow objective in objectives)
            {
                builder.AppendLine(
                    "| " + objective.Chapter.ToString(CultureInfo.InvariantCulture) + "." + objective.Objective.ToString(CultureInfo.InvariantCulture)
                    + " | " + objective.Name.Replace('_', ' ')
                    + " | " + objective.FastSeconds.ToString(CultureInfo.InvariantCulture) + " s"
                    + " | " + objective.SlowSeconds.ToString(CultureInfo.InvariantCulture) + " s"
                    + " | " + objective.Decisions.ToString(CultureInfo.InvariantCulture)
                    + " | " + objective.Checks.ToString(CultureInfo.InvariantCulture)
                    + " | " + objective.Collections.ToString(CultureInfo.InvariantCulture) + " |");
            }

            builder.AppendLine();
            builder.AppendLine("## Decision de tranche");
            builder.AppendLine();
            builder.AppendLine("La mesure initiale a identifie le chapitre 5 comme seul chapitre sans controle actif. La tranche de commandement ajoute maintenant trois verifications tactiques a chacun de ses deux circuits: trajectoire, reperes odorants et relais de gardiennes. Le chapitre passe ainsi de 13 a 19 interactions actives sans ajouter de minuteur.");
            builder.AppendLine();
            builder.AppendLine("Chaque circuit certifie gagne +2 securite apres les trois controles uniques. Les doublons ne comptent pas, la recompense reste verrouillee jusqu'aux six controles et aucun achat ou avantage exclusif n'est introduit.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 1 passe de 14 a 20 interactions actives. Apres la charte, le joueur choisit une charge reelle, attend le convoi, recolte manuellement son miel, controle le debit, l'etancheite et le relais, puis signe un sceau durable de reserve ou de couvain.");
            builder.AppendLine();
            builder.AppendLine("La mise en service et la certification d'atelier conservent leurs choix durables. La liaison de l'ouvriere ajoute un neuvieme choix, un lot a recolter, trois controles uniques et une remise reelle sur l'amelioration suivante; le profil strategique migre en v5.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 5 transmet maintenant un mandat durable a la premiere expedition: corridor eclaireur pour +6 pollen, ou escorte pour +4 pollen et +2 securite. Une preparation de 14 a 16 secondes et trois controles uniques portent le chapitre a 23 interactions actives et 151 a 190 secondes.");
            builder.AppendLine();
            builder.AppendLine("Le profil strategique migre en v6 et applique le bonus au vrai butin local du chapitre 7 avant sa reclamation manuelle.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 4 transmet désormais une livraison défensive réelle: boucliers de cire pour réduire de 60 cire le premier barrage, ou grille ventilée pour +4 sécurité. Les deux routes produisent un lot à collecter manuellement et imposent trois contrôles uniques. Le chapitre atteint 13 objectifs, 25 interactions actives et 145 à 165 secondes; le profil stratégique migre en v7.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 1 est désormais le plancher numérique avec 20 interactions actives, cohérent avec son rôle d'ouverture plus courte.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 1 prépare désormais directement le couvain: réserve de gelée avec 60 pollen collecté manuellement et +2 aux soins, ou escorte thermique avec 120 miel collecté et +3 stabilité. Trois contrôles uniques portent le chapitre à 14 objectifs, 25 interactions actives et 150 à 184 secondes. Le profil stratégique migre en v8.");
            builder.AppendLine();
            builder.AppendLine("Les chapitres 2, 3 et 5 partagent maintenant le plancher numérique à 23 interactions actives; le chapitre 2 est la prochaine tranche recommandée dans l'ordre de l'Acte I.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 2 prépare désormais l'émergence: ration produisant 100 miel à collecter et réduisant le soin final de 60 miel, ou opercule produisant 60 cire et réduisant son renforcement de 40 cire. Trois contrôles uniques portent le chapitre à 15 objectifs, 28 interactions actives et 180 à 224 secondes. Le profil stratégique migre en v9.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 3 devient le premier plancher restant à 23 interactions actives.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 3 certifie maintenant une passation technique: gabarit de calibration pour +40 cire au lot temoin, ou trousse d'application pour -40 cire sur le premier chantier. Une preparation de 15 a 18 secondes et trois controles uniques portent le chapitre a 13 objectifs, 27 interactions actives et 145 a 170 secondes; le profil strategique migre en v10.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 5 devient le premier plancher restant a 23 interactions actives.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 5 ajoute maintenant un briefing de sortie apres le mandat: balise solaire avec repere C32_32, ou retour garde avec +3 securite. La preparation de 15 a 18 secondes debouche sur deux decisions de simulation sans penalite, porte le chapitre a 13 objectifs, 26 interactions actives et 166 a 208 secondes, puis transmet son effet au chapitre 6.");
            builder.AppendLine();
            builder.AppendLine("Le profil strategique migre en v11.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 4 exige maintenant trois validations actives et uniques avant d'inscrire sa doctrine finale: gabarit, releve et tracabilite. Aucun temps d'attente supplementaire n'est ajoute; le chapitre conserve 13 objectifs et 145 a 165 secondes, mais atteint 28 interactions actives. Le chapitre 1 devient seul plancher a 25 interactions actives et constitue la prochaine tranche recommandee.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 1 exige maintenant la ratification active de sa charte apres sa preparation: registre, priorite de la nurserie et signal d'alerte. Aucun effet durable ni choix de profil n'est accorde avant les trois controles uniques. Sans attente supplementaire, le chapitre conserve 14 objectifs et 150 a 184 secondes, mais atteint 28 interactions actives. Le chapitre 5 devient seul plancher a 26 interactions actives et constitue la prochaine tranche recommandee.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 5 impose maintenant un debrief tactique apres la premiere defense: traces de breche, passage du couvain et ligne de ravitaillement. Les trois controles uniques produisent une recommandation de nettoyage ou de releve selon la riposte, sans imposer ce choix ni ajouter de minuterie. Le chapitre conserve 13 objectifs et 166 a 208 secondes, mais atteint 29 interactions actives. Le chapitre 3 devient seul plancher a 27 interactions actives et constitue la prochaine tranche recommandee.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 3 oriente maintenant la premiere ouvriere par trois observations uniques: ailes, corbeilles a pollen et signal de releve. L'operculation renforcee recommande la reserve, tandis que l'emergence naturelle recommande la nurserie; cette recommandation reste informative et les deux essais demeurent accessibles. Sans attente ni cout supplementaire, le chapitre conserve 13 objectifs et 145 a 170 secondes, mais atteint 30 interactions actives. Les chapitres 1, 2 et 4 partagent maintenant le plancher a 28 interactions; le chapitre 1 est la prochaine tranche recommandee dans l'ordre de l'Acte I.");
            builder.AppendLine();
            builder.AppendLine("Le chapitre 1 conclut maintenant son installation par une dotation fondatrice choisie: 250 miel pour la reserve, ou 170 miel et 80 pollen pour une fondation mixte. Les deux routes accordent exactement 250 ressources, une seule fois, sans minuterie ni avantage payant. Le choix durable migre avec le profil strategique v12. Le chapitre conserve 14 objectifs et 150 a 184 secondes, mais atteint 29 interactions actives. Les chapitres 2 et 4 partagent maintenant le plancher a 28 interactions; le chapitre 2 est la prochaine tranche recommandee dans l'ordre de l'Acte I.");
            builder.AppendLine();
            builder.AppendLine("Fondations protegees modifiees: aucune. Chat et messagerie: hors du perimetre LivingHive et sous la responsabilite exclusive de Communication.");

            File.WriteAllText(ReportPath, builder.ToString(), new UTF8Encoding(false));
            Debug.Log("Opening Act objective pacing report written to " + ReportPath);
        }

        private static ObjectiveRow ParseObjective(string row)
        {
            string[] fields = row.Split('|');
            string[] identity = fields[0].Substring("objective:".Length).Split('.');
            return new ObjectiveRow(
                ParseInt(identity[0]),
                ParseInt(identity[1]),
                fields[1].Substring("name:".Length),
                ParseField(fields[2], "timed_fast:"),
                ParseField(fields[3], "timed_slow:"),
                ParseField(fields[4], "decisions:"),
                ParseField(fields[5], "checks:"),
                ParseField(fields[6], "collections:"));
        }

        private static int ParseField(string field, string prefix)
        {
            return ParseInt(field.Substring(prefix.Length));
        }

        private static int ParseInt(string value)
        {
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        private readonly struct ObjectiveRow
        {
            public readonly int Chapter;
            public readonly int Objective;
            public readonly string Name;
            public readonly int FastSeconds;
            public readonly int SlowSeconds;
            public readonly int Decisions;
            public readonly int Checks;
            public readonly int Collections;
            public int ActiveInteractions => Decisions + Checks + Collections;

            public ObjectiveRow(int chapter, int objective, string name, int fastSeconds, int slowSeconds, int decisions, int checks, int collections)
            {
                Chapter = chapter;
                Objective = objective;
                Name = name;
                FastSeconds = fastSeconds;
                SlowSeconds = slowSeconds;
                Decisions = decisions;
                Checks = checks;
                Collections = collections;
            }
        }
    }
}
