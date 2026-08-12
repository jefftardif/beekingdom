# Traduction corrélée avant cache — validation serveur

Date: 2026-07-21 18:24 UTC  
Périmètre: `Server/` uniquement, validation locale, aucune activation ni déploiement.

## Contrat appliqué

- La clé de cache est le triplet exact `(MessageId, TargetLocale, ModelVersion)`; les versions de modèle ne se collisionnent donc pas.
- `TargetLocale` accepte uniquement 2 à 35 caractères ASCII alphanumériques séparés par des tirets, sans tiret initial/final ni double tiret.
- `ModelVersion` accepte 1 à 128 caractères ASCII `[A-Za-z0-9._-]`, sans trim silencieux.
- La réponse est matérialisée comme `completed` uniquement avec une source locale valide et un texte non vide de 16 000 caractères maximum. Toute réponse incohérente devient `translation_response_mismatch` et aucune ligne de cache n'est créée.
- L'autorisation de lecture du message est vérifiée avant fournisseur et cache; le cache reste cloisonné par message, locale et modèle.
- Les diagnostics conservent uniquement résultat et latence, sans texte, identifiant brut ni secret.

## Preuves

- Tests traduction ciblés .NET 10: **13/13**.
- Suite serveur traduction isolée pendant la construction du candidat: **28/28**.
- Suite serveur complète .NET 10: **247 réussis, 7 SQL explicitement ignorés, 0 échec**.
- Publication/smoke locale: **Healthy**, `chat-v1`, rétention reçus 30 jours, `server=false`, `realtime=false`, `PreparationOnly`.
- La cible .NET 8 ne peut pas être exécutée dans cette VM (runtime .NET 8 absent); la compilation net8 Release réussit et la suite isolée est exécutée avec le runtime disponible.

## Fichiers modifiés

- `Server/src/BeeKingdom.Chat/Translations/ChatTranslationService.cs`
- `Server/tests/BeeKingdom.Tests/ChatTranslationServiceTests.cs`

## Candidat

Nouveau candidat local: `Server/artifacts/candidates/BeeKingdom.Server.20260721T182401Z`. Le fichier `CANDIDATE-STATUS.json` révoque automatiquement les candidats précédents; `DeploymentAuthorized=false`.

## Écart restant

Le fournisseur externe reste absent par conception. La validation HTTP .NET 8 et les portes SQL jetables restent à exécuter dans un environnement disposant du runtime/SQL autorisés.
