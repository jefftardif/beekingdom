# M096 — Certification Recherche Économie

## Cause racine
Deux écarts SpeedUp reproduits : une recherche arrivée à échéance consommait encore un objet ; le rejeu idempotent réussi retournait une `Response` nulle, donc une réponse HTTP vide. Les valeurs des dix recherches et leurs dépendances sont conformes au contrat existant ; aucun rééquilibrage.

## Fichiers changés
- `Server/src/BeeKingdom.HiveOperations/SpeedUpContracts.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/ResearchEconomyCertificationTests.cs`
- `Server/tests/BeeKingdom.Tests/ResearchEconomyCertificationEndpointTests.cs`
- Ce rapport.

## Implémentation
Refus sans consommation du SpeedUp recherche/universel sur une recherche prête ; restitution du reçu original et du snapshot courant lors du rejeu réussi. Ajout de 17 cas de certification couvrant les dix offres, chaque dépendance et ressource insuffisante, le cycle durable, les effets cumulés, les accélérateurs et une reconnexion HTTP authentifiée.

## Tests et compilation
Compilation serveur réussie : **23/23** tests ciblés HiveOperations et **1/1** nouveau parcours HTTP ; régressions SpeedUp reproduites avant correction (deux cas par anomalie). Vérification complémentaire existante : **30/32** succès, dont les cinq tests HTTP Recherche ; les deux échecs concernent des assertions de migration préexistantes attendues à v10, alors que `CurrentModelVersion` vaut 12.

Preuves locales : `Temp/M096/M096-server.trx`, `M096-http.trx`, `M096-endpoints.trx`, `M096-before.trx`, `M096-replay-before.trx` dans le même dossier ; `git diff --check` sans erreur. Unity 6000.5.3f1 : deux tentatives EditMode, sortie 1 avant compilation, timeout IPC du Package Manager ; logs `Temp/M096/editor.log` et `editor-escalated.log`, aucun XML Unity.

## Points non résolus
Certification client Unity/visuelle non acquise : UPM ne démarre pas. Les tests existants `MigratorV9WithoutOfflineBlockBecomesV10WithNullBlock` et `DurableJsonRoundTripPreservesPendingRevisionReceiptAndExactReplay` gardent leurs assertions v10 obsolètes, hors correction Recherche ; aucune modification du migrateur.

## Acceptation
- [x] Dix recherches : coûts, durées 120/360/720 s et convergence 1200 s, effets et prérequis de recherche vérifiés ; prérequis de bâtiments exclus conformément au statut du document de conception.
- [x] +2 % miel réel après complétion ; cumul final production miel/cire/pollen +15/+8/+23 %, capacités +10/+33/+15 %, collecte toujours manuelle.
- [x] Débit unique, file unique, refus anticipé, complétion, rejeu et refus de relance.
- [x] Persistance JSON avec recréation du dépôt/service ; reconnexion HTTP et état retrouvé.
- [x] SpeedUp recherche/universel : réduction, borne à maintenant, inventaire, révision, mauvaise cible, rejeu et complétion.
- [ ] Compilation et tests client Unity ; validation visuelle en jeu.
- [x] Worktree dédié, branche `codex/m096-research-economy-certification` ; aucun changement World Map, Chat Royal, FTUE ou Alliance, aucun push/merge main.
