# Chapitre 2 — audit de contrat : interprétation de la vitalité

Date : 2026-07-21  
Mode : documentation seulement, pendant la fenêtre de test manuel Unity. Aucun code, aucune compilation, aucun candidat et aucun déploiement n’ont été effectués pour cet audit.

## Contrat observé côté client

Après chaque observation, le client compare `nutrition` et `stability` et identifie la valeur la plus basse; en cas d’égalité, la nutrition est prioritaire. Une réponse incorrecte est sans coût, sans temps et sans progression. Une réponse correcte ouvre les contrôles d’incubation et propose une recommandation (`jelly_support` pour la nutrition, `hygiene_rotation` pour la stabilité), sans imposer le soin.

Ces choix sont pédagogiques. Ils ne modifient ni la vitalité, ni les ressources, ni les minuteries et ne créent aucune mutation offline à rejouer.

## Autorité serveur attendue

Le serveur reste propriétaire de `nutrition`, `stability`, `revision`, `updatedAtUtc` et de l’opération active. Le mobile ne conserve et ne rend que le dernier instantané reconnu; il ne peut pas déclarer une nouvelle valeur, une progression officielle ou une opération économique.

La progression officielle du tutoriel doit être persistée côté serveur avec une révision monotone et une clé d’idempotence. La progression issue d’une mauvaise réponse ne doit pas être enregistrée; une bonne réponse doit être rejouable sans double progression. Le choix pédagogique ne doit pas créditer de ressource ni démarrer de minuterie.

## Alignement avec la lecture serveur existante

La lecture `GET /game/v1/hives/{hiveId}/brood/vitality` est déjà décrite derrière `BroodVitality:Enabled=false`, avec appartenance Bearer/ruche et état non initialisé honnête (`initialized=false`, champs null). Aucun endpoint de soin ou de mutation n’est actuellement autorisé.

La future commande de progression devra être distincte de cette lecture et devra au minimum vérifier : identité Bearer, appartenance de la ruche, état/tutoriel précédent, `expectedRevision`, `idempotencyKey`, ordre monotone et horodatage UTC serveur. Une réponse tardive ou un rejeu d’un autre joueur ne doit ni modifier la progression ni la vitalité.

## Écarts et portes

- Aucun contrat serveur de progression tutorielle n’est encore livré ou ratifié.
- Les coûts, préconditions, minuteries et reçus idempotents des soins restent volontairement non définis; aucune mutation de soin ne doit être ouverte.
- Le raccordement session/adaptateur HTTP/cache protégé du mobile reste à faire.
- Les preuves .NET 8 natif, SQL jetable et staging mobile restent requises avant toute promotion.

## Décision de tranche

Le comportement pédagogique client est compatible avec la frontière d’autorité serveur, à condition de traiter les valeurs affichées comme instantané de lecture et non comme preuve de progression. Aucune implémentation Server n’est commencée dans cette fenêtre; elle attend un signal explicite de levée du gel.

Références :

- `Docs/Product/LivingHive_BroodVitalityInterpretationMilestone_2026-07-21.md`
- `Docs/ProductionIntegration/Chapter2_BroodVitality_ServerRead_2026-07-21.md`
