# Matrice de préparation production — état local

Ce document est une photographie de la copie locale `C:\projets\beekingdomgame-master`. Il ne vaut pas autorisation de déploiement.

## Fonctions Phase 4 préparées

| Fonction | Route / noyau | Drapeau par défaut | État local |
|---|---|---:|---|
| Voie stratégique | GET/POST `/game/v1/hives/{hiveId}/strategic-path` | false | noyau + routes compilés |
| Catalogue doctrine | GET `/game/v1/combat/doctrine` | false | catalogue `phase4-combat-v1` |
| Formation-readiness | GET `/game/v1/hives/{hiveId}/combat/formation-readiness` | false | roster absent = `not_recorded`; roster présent projeté |
| Recrutement doctrinal | GET/POST `/game/v1/hives/{hiveId}/combat/recruitment...` | false | catalogue, état nullable, start/claim atomiques |

Les anciennes données ne sont jamais converties en familles doctrinales. La migration actuelle est v6→v7 sans seed de roster; un roster présent est validé (familles canoniques, comptes non négatifs, révision et opération UTC cohérentes).

## Preuves locales

- Suite `BeeKingdom.HiveOperations.Tests` : **44/44** réussis.
- Suite `BeeKingdom.Tests` WebApplicationFactory : compilation réussie, mais **0 test découvert** sous le runtime disponible; cette exécution ne constitue pas une preuve HTTP.
- Build Release `BeeKingdom.Server` : **0 erreur**; avertissement SqlClient MSB3277 préexistant.
- Exécution avec `DOTNET_ROLL_FORWARD=Major`; le runtime .NET 8 natif n’est pas installé dans la VM.
- Aucun candidat, transfert, activation ou déploiement effectué; `DeploymentAuthorized=false` reste obligatoire.

## Portes avant production

1. Installer ou fournir un runtime .NET 8 x64 natif puis exécuter les tests WebApplicationFactory réellement découverts (flags false, 401, 400, 404, isolation et mutations derrière activation contrôlée).
2. Exécuter la reconstruction SQL jetable et vérifier migration v6→v7, concurrence, reçus et rollback.
3. Vérifier TLS/SNI/IIS/Cloudflare Full strict sans redirection et les limites HTTP effectives.
4. Exécuter le scénario Android A → logout → B → retour A avec bearer, partitions et reçus cloisonnés.
5. Raccorder le shell authentifié mobile; aucune valeur locale, roster legacy ou cache ne peut devenir autorité.

Jusqu’à ces preuves, `StrategicPath`, `CombatDoctrine`, `CombatFormationReadiness` et `CombatRecruitment` restent fermés en Production. Aucun coefficient de combat, dégât, victoire ou activation économique n’est exposé par ces tranches.
