# Registre de préparation production — 2026-07-21

## État local vérifié

- Configuration source fail-closed : `PersistenceProvider=InMemory`.
- `ChatEnabled=false`, `RealtimeEnabled=false`.
- `RuntimeAvailability=ServerInPreparation`.
- SQL externe et secrets opérateur restent explicitement requis avant toute
  configuration de production.
- Suite serveur : 255 réussis, 7 tests SQL ignorés, 262 total.
- HiveOperations : 20/20.
- BroodVitality HTTP : 2/2.
- Build Release solution : 0 erreur, 2 avertissements existants
  `Microsoft.Data.SqlClient`.
- Dernière vérification de processus : zéro `dotnet`/`testhost`.

## Candidat local

`BeeKingdom.Server.20260721T225554Z` reste `local-validation-only` avec
`DeploymentAuthorized=false`. Son manifeste est valide (55 fichiers, aucune
divergence SHA-256), mais il précède les derniers ajouts de tests et correctifs
BroodVitality. Il ne doit pas être promu ni présenté comme preuve de ces
derniers changements. Une reconstruction locale séparée est nécessaire avant
toute validation de candidat intégrant le modèle v6 et les tests actuels.

## Portes encore ouvertes

1. Instance SQL/LocalDB jetable et reconstruction SQL du modèle JSON v6.
2. Validation .NET 8 native (la VM utilise actuellement le roll-forward vers
   le runtime .NET 10 pour les tests net10).
3. TLS/SNI/IIS et hôte de staging autorisé.
4. Shell d’authentification mobile réel et matrice Android staging.
5. Rebuild d’un candidat local après fermeture des changements, puis nouveau
   manifeste et smoke. Aucun transfert, activation ou synchronisation n’est
   autorisé par ce registre.

Contrôle runtime du 2026-07-21 : seuls les runtimes .NET/ASP.NET Core 10.0.10
et le SDK 10.0.302 sont installés. Les exécutions net8/net10 réalisées ici ont
utilisé explicitement `DOTNET_ROLL_FORWARD=Major`; aucune installation de
runtime n’a été tentée.
