# 47 — Recette Alpha reproductible à réaliser avant validation

## But

Prouver avec **au moins deux comptes distincts réellement authentifiés** que le système n'est pas une simulation d'UI locale et que toutes les opérations partagées sont cohérentes.

## Environnement contrôlé

- Version Git serveur et Unity précisément notées.
- Feature flags officiellement actifs pour test, avec contexte monde/site explicite.
- Deux comptes et ruches QA connus, données de test réinitialisables **sans toucher à d'autres joueurs**.
- Deux clients ou sessions distinctes.
- Horloge officielle IServerClock, horloge fausse en client pour test.
- Captures de snapshots et soldes avant/après.
- Catalogue test QA validé, Site A et Site B avec compatibilité.

## Recette 1 — Cohorte civile

A démarre cohorte valide. Rejouer start avec même clé. Capturer receipt et total. Avancer horloge côté serveur test. Réclamer deux fois. Attendu : même cohorte, civils nés une seule fois, aucun soldat supplémentaire, ancien état intact.

## Recette 2 — Route normale hors ligne

A choisit mode équilibré, N civils, source A et destination B. Prévisualisation affiche coûts. Start renvoie expéditionId, taille réservée et échéances. Fermer Unity, simuler temps écoulé, reconnecter. Attendu : pollen source en baisse exacte ; B bénéficie de l'effet écologique ; récompenses officielles une fois ; civils disponibles/retour conformes.

## Recette 3 — Concurrent sur un même site

A et B lancent à la même seconde des missions consommant une réserve proche de sa limite. Vérifier le total réellement prélevé <= réserve + régénération connue ; aucun wallet positif supplémentaire sans stock source valide ; échecs 409 correctement localisés.

## Recette 4 — Projet collectif

Créer projet avec minimum 2 joueurs distincts. A fait 10 missions ; même si tous les points atteints, le jalon collectif reste en attente du second participant. B apporte une contribution qualifiée ; le jalon se réalise une fois. Récompenser A et B chacun selon contrat ; retenter claim et vérifier aucun doublon.

## Recette 5 — Progression joueur/alliance

Deux joueurs avec même nombre de civils mais compétences officielles différentes ; comparer rendement borné. Deux niveaux d'alliance si système introduit ; vérifier capacité d'organisation différente mais qu'aucun niveau n'ignore le nombre de membres distincts.

## Recette 6 — Déconnexion/crash/retry

Débrancher le client après que le serveur a commis start mais avant réponse. Réémettre même clé. Couper le serveur test au moment pré-règlement et relancer. Aucun nouveau start, aucune perte d'effectifs, pas de rewards dupes. Reprendre après délai.

## Recette 7 — Modes de mission

Intensif, équilibré, restauration, sur un site identique remis dans un **état QA isolé** ; valider différences sur récompenses et impact futur, sans changer les observations d'autres joueurs du serveur.

## Recette 8 — Droits

Un joueur sans alliance pollinise site public. Un joueur externe tente projet privé/relais : refus sans fuite d'infos. Exclusion d'un membre pendant expédition : ancienne contribution conservée, nouveau départ non autorisé. Si diplomatie pas encore livrée, marquer tests différés et ne pas prétendre PASS.

## Recette 9 — UX bilingue et non-régression

Vérifier FR/EN, accents, status, boutons stables, scroll, clic traversant, filtre carte, zoom, labels distincts fertilité/projet/stocks. En parallèle, exécuter tests des systèmes connus : WorldResourceCollection, Nurserie Feed/Stabilize, Académie, Alliances, Combat Patrol, Défense/Infirmerie et UI.

## Rapport de recette attendu

| ID | État | Preuve requise |
|---|---|---|
| ALPHA-01 Cohorte | PASS/FAIL/NOT RUN | capture états/révisions/reçu |
| ALPHA-02 Route hors ligne | PASS/FAIL/NOT RUN | mission/timestamps/ressources |
| ALPHA-03 Stock concurrent | PASS/FAIL/NOT RUN | ledger et stock preuve |
| ALPHA-04 Projet collectif | PASS/FAIL/NOT RUN | deux IDs et compte de qualifiés |
| ALPHA-05 Progression | PASS/FAIL/NOT RUN | formule et paramètres |
| ALPHA-06 Crash/retry | PASS/FAIL/NOT RUN | reçu unique |
| ALPHA-07 Modes | PASS/FAIL/NOT RUN | sorties comparées |
| ALPHA-08 Permissions | PASS/FAIL/NOT RUN | refus/cas d'accès |
| ALPHA-09 UI/régressions | PASS/FAIL/NOT RUN | captures et tests |

**Ne jamais écrire PASS par déduction** : seules les exécutions comptent. Les cas hors lot restent « NOT RUN/NOT IMPLEMENTED », sans masquer la limite.

