# Chat et messagerie — enveloppe capabilities bornée

Date : 2026-07-21  
Responsable : Communication

## Résultat

Une réponse capabilities active doit maintenant annoncer le fournisseur `server` et uniquement les canaux `Alliance`, `Server`, `Private` et `Leaders`, sans valeur vide ni doublon insensible à la casse.

Les valeurs numériques sont bornées avant création du bail ou calcul de durée :

- corps : 1 à 4 000 caractères;
- messages par minute et joueur : 1 à 600;
- messages par dix secondes et conversation : 1 à 100;
- créations privées par heure : 1 à 1 000;
- destinataires privés : 1 à 100;
- rétention des reçus : 2 à 3 650 jours.

Un fournisseur inconnu retourne `provider_invalid`; une limite aberrante `limits_invalid`; une liste de canaux inconnue ou dupliquée `channels_invalid`; une rétention hors borne `receipt_retention_invalid`. Ces décisions restent indisponibles et surviennent avant session ou mutation. La borne supérieure de rétention empêche également un dépassement lors de la construction du `TimeSpan`.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 86/86 réussie.
- Fournisseur `mirror`, corps 4 001, rétention `int.MaxValue`, canal `GlobalAdmin` et doublon `Private/private` sont refusés.
- Aucune acquisition de session pour les cinq réponses invalides.
- Les 85 essais précédents restent verts.
- Aucun déploiement, activation ni synchronisation effectué.

## État candidat

L'autorité locale `Server/artifacts/candidates/CANDIDATE-STATUS.json` confirme encore `BeeKingdom.Server.20260721T170747Z` comme `local-validation-only`, avec `deploymentAuthorized=false`. Les candidats `170156Z` et `170435Z` restent révoqués. Le candidat courant doit être remplacé après intégration complète des nouveaux contrats capabilities.

## Directive d'intégration

Appliquer des bornes supérieures correspondantes dans `ChatOptions` avec `ValidateOnStart`, afin que le serveur ne puisse jamais publier une valeur que le client refuserait. Ajouter des tests pour chaque minimum/maximum et juste hors borne, plus canaux vides/inconnus/dupliqués et rétention extrême. Le préflight staging doit vérifier la même enveloppe. Toute extension future de canal ou de plage exige une nouvelle version de protocole ou une compatibilité client préalable, jamais une modification silencieuse de `chat-v1`. Reconstruire ensuite un candidat local et conserver `DeploymentAuthorized=false`.
