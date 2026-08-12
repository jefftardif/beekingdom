# Chapitre 2 — lecture serveur de vitalité du couvain

Implémentation préparatoire, sans mutation : `PlayerHiveState` passe au modèle
v6 et porte `BroodVitalityState` (nutrition/stabilité bornées par le contrat,
révision, date UTC et opération active optionnelle). Les anciens états migrent
avec une valeur absente honnête; aucune valeur client n’est autoritaire.

`GET /game/v1/hives/{hiveId}/brood/vitality` utilise les helpers `game.*`, exige
une session Bearer et une ruche valide, puis lit exclusivement le repository
du joueur authentifié. Le feature flag `BroodVitality:Enabled` est false par
défaut et en Production : réponse fermée `503 game.unavailable`, sans mutation.
Il n’existe aucun endpoint de soin ou d’écriture. Une ruche sans vitalité
retourne `initialized=false` avec valeurs nulles, jamais 0/0/epoch. Les états
initialisés sont rejetés s’ils sortent des bornes 0..100, ont une révision
négative, une date non UTC ou une opération incohérente.

Fichiers :

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.HiveOperations/BroodVitalityOptions.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.Tests/BroodVitalityEndpointTests.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOperationServiceTests.cs`

Preuve : build ciblé Release du serveur réussi, 0 erreur (un avertissement
existant de conflit Microsoft.Data.SqlClient). Aucun candidat ni déploiement.

Le candidat `BeeKingdom.Server.20260721T225554Z` précède les derniers tests et
correctifs de validation BroodVitality; il reste donc non promouvable. Un nouveau
candidat local devra être reconstruit explicitement avant toute prétention de
test production de cette tranche.

Tests repository/migration : HiveOperations **20/20**, incluant identité d’opération
non vide et whitelist `feeding`/`stabilization`. Les tests HTTP d’activation
complète sont maintenant dans `Server/tests/BeeKingdom.Tests/BroodVitalityEndpointTests.cs` : **2/2** (flag fermé avec repository espion non lu, 401, ID invalide authentifié, état absent initialized=false/null, état initialisé exact UTC et ruche étrangère avec second joueur). Suite serveur complète rerunée après ces ajouts : **255 réussis, 7 ignorés SQL, 262 total**, avec `DOTNET_ROLL_FORWARD=Major` sur le runtime .NET 10 disponible. Build Release solution rerun : **0 erreur, 2 avertissements Microsoft.Data.SqlClient**. Aucun test de mutation n’existe. Processus dotnet/testhost après nettoyage : **0**.
