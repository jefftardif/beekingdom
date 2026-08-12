# Chat — mutations validées avant persistance (2026-07-21)

## Résultat

Toutes les mutations sont maintenant validées avant leur première écriture dans un journal local. Les mêmes contrôles sont répétés avant le rejeu d’une entrée restaurée.

### Envoi

- conversation opaque valide ;
- `clientRequestId` de 1 à 256 caractères, sans remplissage ni contrôle ;
- corps de 1 à 4 000 caractères, sans remplissage périphérique.

### Création

- canal parmi `Alliance`, `Server`, `Private`, `Leaders` ;
- `clientRequestId` valide ;
- serveur et monde optionnels limités à 64 caractères ;
- audience et titre optionnels limités à 256 caractères ;
- maximum absolu de 100 participants, chacun étant un identifiant opaque valide ;
- au moins un participant pour une conversation privée.

### Signalement

- message opaque valide ;
- `clientRequestId` valide ;
- catégorie normalisée de 1 à 64 caractères, sans contrôle.

Les curseurs de lecture et opérations restaurées repassent également par leurs validations absolues avant réseau.

## Comportement sûr

Une mutation invalide :

- n’entre pas dans le journal ;
- n’émet aucune requête ;
- n’incrémente aucun compteur de tentative.

Une ancienne entrée restaurée devenue invalide est préservée pour récupération/quarantaine, mais n’est jamais envoyée ni supprimée silencieusement.

## Validation

- corps rembourré, identifiant de requête à 257 caractères, 101 participants, canal inconnu et catégorie à 65 caractères refusés avant journal et réseau ;
- entrée restaurée avec corps invalide refusée, conservée et non envoyée ;
- drainages valides et erreurs de saturation locale conservés.

Suite isolée Communication : **112/112 tests réussis**, compilation sans erreur ni avertissement.

## Écart SQL découvert

L’inventaire actuel révèle des bornes SQL plus petites que les contrats déjà acceptés par le serveur :

- `ClientRequestId` : SQL `nvarchar(128)` contre validation serveur/client jusqu’à 256 ;
- message `Body` : SQL `nvarchar(1000)` contre capacité autorisable jusqu’à 4 000 ;
- traduction : locale 16 contre 35, modèle 64 contre 128, texte 2 000 contre 16 000.

Ce décalage doit être résolu avant toute preuve SQL ou activation. Il faut soit publier des migrations d’élargissement avec contraintes explicites, soit réduire toutes les capacités et validations publiques à la borne SQL. Une acceptation HTTP suivie d’un échec/troncage SQL est interdite.

## Directive serveur

Ajouter les mêmes validations avant service/repository, ainsi que des tests garantissant zéro écriture et zéro reçu pour chaque champ invalide. Tester aussi les journaux/reçus historiques et les limites exactes.

Produire une migration additive pour aligner les colonnes SQL sur le contrat choisi, avec reconstruction jetable et tests aux bornes exactes et `+1`. Ne jamais modifier une migration déjà appliquée : ajouter une nouvelle version. Le candidat `182401Z` ne couvre pas ce jalon ni l’alignement SQL; son successeur doit le révoquer et rester `DeploymentAuthorized=false`.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
