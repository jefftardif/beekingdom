# M076-CL — World Map PvE Closed Loop Alpha

Date : 2026-09-09

## Résumé

Avant tout code, inspection exhaustive du repository (deux passes de recherche
dédiées, client + serveur). Conclusion : la boucle PvE demandée existe déjà à
~90%, bâtie et fonctionnelle, sous le nom **Combat Patrol** — ce n'est pas un
prototype visuel, c'est un système server-authoritative complet avec formule
de combat déterministe, pertes réelles, récompenses créditées, et persistance
SQL transactionnelle. Le travail de cette mission a donc consisté à
**combler deux trous précis** plutôt qu'à reconstruire quoi que ce soit.

## Ce qui existait déjà et a été réutilisé tel quel

- **Roster/armée réelle** : `HiveSquadReservationPanelController` /
  `IHiveSquadReservationClient` — source unique de vérité (Caserne → Armée),
  synchronisée serveur, revision-guarded.
- **Entraînement Caserne** : `HiveDoctrineRecruitmentPanelController` /
  `IHiveDoctrineRecruitmentClient` — 3 familles réelles (`guardians`,
  `wingrunners`, `darters`).
- **Tiers de troupes** : `HiveTroopTierClient` (promotion, bonus combat).
- **Champion assignment** : `HiveChampionBeeClient` / `ChampionBeeCatalog` —
  assignation persistante, contribution calculée côté serveur.
- **World Map → sélection PNJ → Attaquer** : `TrySelectAt` (hit-test sur les
  nœuds bestiaire) + bouton "ATTAQUER" déjà câblé sur
  `HiveViewProductUiPresenter.OpenCombatPatrolOverlayForWorldMap(tier)`.
  L'Araignée palier 3 existe et a été retrouvée en Play Mode
  (`id=beast_t3_core_demo`, `label=Araignee`).
- **Combat serveur** : `CombatPatrolService` / `CombatPatrolResolution` —
  formule déterministe (puissance disponible/requise, avantage
  pierre-papier-ciseaux par famille de danger, bandes Blocked/HardWon/
  Victory/DecisiveVictory), intégrant déjà Champion, tier de troupe, voie
  stratégique et bonus d'alliance. Répartition des pertes 15% définitives /
  85% blessées.
- **Anti-double-usage des troupes** : `HiveTroopDeploymentAccounting`
  (compte les troupes déjà engagées dans une réservation, une patrouille ou
  un vol de collecte avant de calculer le roster disponible).
- **Vrai déplacement/temps** : chaque encounter a un `StartedAtUtc`/
  `EndsAtUtc` réel — pas de victoire instantanée. Auto-claim silencieux à
  l'arrivée (`AutoClaimFinishedEncountersAsync`) pour que les troupes
  reviennent seules.
- **Debrief déjà complet** : `DrawCombatPatrolPanel` (état `Debrief`) affiche
  déjà nom de la créature, issue (Band), puissance requise/disponible,
  butin, bonus cible-du-jour/évènement-monde, bonus tier de troupe, bonus
  Champion (avec noms), blessées ET pertes définitives par famille.
- **Persistance** : `SqlHiveStateRepository` — transaction `Serializable` +
  app-lock SQL Server, tout l'état (roster, blessés/convalescence,
  ressources, encounters) sérialisé dans une ligne JSON par (joueur, ruche),
  revision-guardée.

## Ce qui manquait (les vrais trous)

1. **Convalescence/Infirmerie invisible** : le serveur modélise déjà les
   blessées en convalescence (`RemoteCombatPatrolSnapshot.Recovering` —
   famille, quantité, heure de retour) et les fait revenir automatiquement
   dans le roster à échéance, mais **rien côté client n'affichait jamais ce
   champ** en dehors du debrief immédiat qui suit un combat. Aucune
   Infirmerie dédiée n'existe (juste un placeholder `"future"` dans le
   catalogue des bâtiments) — conformément à la consigne de ne pas
   reconstruire ce qui n'existe pas, la convalescence réelle déjà en place
   est maintenant simplement rendue visible.
2. **Récompenses pas immédiatement répercutées sur le stock affiché** :
   après une réclamation (manuelle ou automatique), le solde
   miel/pollen (`ResultingBalances`) renvoyé par le serveur était ignoré
   côté client — le panneau de stock ne le découvrait qu'au prochain poll
   indépendant. Risque de contredire la consigne "aucun résultat uniquement
   local/UI" en donnant l'impression d'un décalage entre le debrief et
   l'inventaire réel.

## Ce qui a été implémenté

Deux changements ciblés, sans nouveau système parallèle :

- `HiveViewProductUiPresenter.cs` : nouvelle méthode interne
  `NotifyStockMightHaveChanged()` qui déclenche un `Refresh()` immédiat du
  contrôleur de stock existant (celui déjà utilisé par le bouton
  "Actualiser" du Sac) — aucune nouvelle tuyauterie, juste un
  déclenchement plus rapide de code déjà présent.
- `CombatPatrolPresentation.cs` : appel de `NotifyStockMightHaveChanged()`
  juste après un `ClaimAsync` réussi (chemin manuel `ClaimCoreAsync` ET
  chemin automatique `AutoClaimFinishedEncountersAsync`), pour que le
  butin crédité par le serveur se reflète sans délai perceptible.
- `HiveViewProductUiPresenter.cs` (`DrawCombatPatrolPanel`) : nouveau bloc
  "En convalescence" listant chaque lot `Recovering` (famille, quantité,
  temps restant avant retour) dans la vue principale de la Patrouille de
  combat — rend visible une donnée déjà réelle et persistée, sans bâtir de
  nouveau bâtiment/écran Infirmerie.

Aucun changement serveur, aucun changement au ciblage (reste par palier
abstrait — l'ajout d'un ciblage par instance de créature aurait nécessité un
changement de contrat serveur non trivial, explicitement hors du périmètre
"pas de gros refactor" de cette mission).

## Où le serveur est autoritaire

Partout où ça compte : calcul de puissance/issue de combat
(`CombatPatrolResolution`), répartition pertes/blessées, crédit des
récompenses, capacité de population, anti-double-usage des troupes,
verrouillage de concurrence (app-lock SQL + isolation Serializable). Le
client ne fait que prévisualiser (`PreviewAsync`) et afficher les résultats
déjà calculés — aucune formule de combat dupliquée côté Unity.

## État de la persistance

Confirmé par lecture du code serveur (non ré-audité en profondeur cette
mission, hérité de l'architecture existante) : troupes, convalescence et
ressources vivent dans la même ligne SQL transactionnelle par
(joueur, ruche), avec revision optimiste — une reconnexion relit cet état
réel, ne peut pas "ressusciter" des troupes perdues ni faire disparaître des
récompenses déjà créditées.

## Validation effectuée dans cet environnement

- Compilation propre (`assets-refresh`, 0 erreur) après chaque changement.
- Play Mode depuis le vrai point d'entrée
  (`Environment2D5D_HiveMap_Test` → démo locale → World Map), nœud bestiaire
  palier 3 "Araignee" localisé et flux Attaque déclenché exactement comme le
  ferait un vrai clic joueur (même méthode `OpenCombatPatrolOverlayForWorldMap`
  appelée par le vrai bouton "ATTAQUER") : le panneau de Patrouille s'ouvre
  sans exception, le nouveau bloc "En convalescence" ne casse rien
  (liste vide correctement masquée).
- **Non testable dans cet environnement** : cet environnement de
  développement n'a pas de session de compte réel authentifiée (pas
  d'identifiants CEO) — `CombatPatrolPanelController` reste donc
  `UnavailableCombatPatrolPanelController` (mode démo locale, aucune donnée
  serveur réelle). La résolution de combat réelle, les pertes/récompenses
  effectives et la persistance après reconnexion n'ont **pas** pu être
  vérifiées de bout en bout avec un vrai compte — ceci nécessite le retest
  CEO décrit ci-dessous.

## Blocage restant

Aucun blocage technique. Le seul élément non vérifiable ici est la
validation "compte réel" explicitement demandée par la mission — elle
requiert les identifiants du CEO, absents de cet environnement de
développement.

## Commit

Commit local uniquement (aucun push) — ne contient que les deux hunks
ci-dessus dans `HiveViewProductUiPresenter.cs` (le fichier avait déjà
d'autres modifications locales non liées, en cours, non committées ici) et
le changement complet de `CombatPatrolPresentation.cs`.

## READY FOR CEO PVE CLOSED LOOP RETEST

Test à effectuer avec un vrai compte : Ruche → World Map → clic sur
l'Araignée (ou tout palier) → composer l'escouade → Lancer la patrouille →
attendre l'arrivée (ou revenir plus tard, l'auto-claim s'en charge) → vérifier
le debrief (pertes/blessées/butin/Champion) → revenir à la Ruche → retourner
sur la World Map → confirmer que l'armée, le stock et la convalescence
affichée reflètent bien l'état réel, y compris après reconnexion.
