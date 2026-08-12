# Chapitre 4 — audit du contrat de qualification du lot témoin

Date : 2026-07-21  
Mode : lecture seule pendant le test manuel utilisateur. Aucun code, processus, test, candidat ou déploiement n’a été lancé.

Référence auditée : `Docs/Product/LivingHive_WorkshopBatchQualificationDesign_2026-07-21.md`.

## Frontière mobile / serveur

Le design respecte la frontière d’autorité : le mobile peut afficher la spécialisation, la quantité du lot et une recommandation issus du dernier instantané reconnu, mais ne doit jamais les déclarer comme faits autoritaires. Le serveur doit relire ces valeurs depuis la ruche authentifiée avant d’accepter la qualification. Une réponse mobile, un compteur local, une partition ou un cache ne peut pas établir la spécialisation, la collecte, un bonus ou un solde.

La mauvaise réponse est purement pédagogique : aucun coût, délai, bonus, production, capacité ou progression. La première application reste une commande économique distincte; elle ne doit pas être déclenchée par la qualification et ses coûts/durées/effets doivent être vérifiés dans son propre contrat transactionnel.

## Séquence et préconditions

L’ordre proposé est cohérent, mais les préconditions doivent être formulées comme des invariants serveur :

1. la spécialisation précédente existe, est terminée et appartient à la ruche et au joueur authentifié;
2. le lot témoin est effectivement produit puis collecté par une opération serveur, avec quantité persistée (120 ou 160 selon le résultat réel), sans confiance dans une quantité envoyée par le mobile;
3. l’étape courante est exactement celle qui précède `UpgradeBatchQualificationChoice`;
4. la réponse correspond au risque attendu pour la spécialisation persistée;
5. une réussite avance une seule fois vers `UpgradeApplicationReady`; une erreur laisse l’état et la révision inchangés.

Une égalité ou une spécialisation inconnue doit être refusée comme état serveur invalide, jamais résolue par une préférence client. Les transitions hors ordre, retour arrière et répétition après progression doivent être des refus définitifs sans mutation.

## `expectedRevision` et idempotence

`expectedRevision` doit être comparé à la révision durable de la ruche avant toute écriture. La révision effective doit augmenter uniquement lors d’une transition réussie; une erreur de précondition ou une réponse incorrecte ne doit ni la consommer ni créer de progression.

`idempotencyKey` doit être obligatoire, bornée et indexée au minimum par joueur authentifié + ruche + commande. Un rejeu avec la même charge canonique doit retourner exactement le même reçu, la même révision et le même état final. La même clé avec spécialisation, étape, réponse ou révision attendue contradictoire doit produire un conflit stable, sans relire une autorité d’un autre joueur ni muter la ruche. La charge canonique ne doit pas inclure des champs de présentation ou une heure mobile non autoritaire.

Le serveur doit générer l’horodatage UTC et la preuve de transition; les valeurs `clientObservedAtUtc`, quantité affichée ou recommandation ne peuvent pas servir de preuve.

## Correction nécessaire avant implémentation

- Ajouter explicitement dans le contrat la distinction entre quantité affichée et quantité collectée autoritaire.
- Définir les noms/stats exacts des états précédents et suivants, ainsi que le comportement pour spécialisation absente, lot non collecté, étape déjà qualifiée et révision obsolète.
- Définir le code d’erreur stable pour conflit d’idempotence, révision obsolète, précondition métier et réponse incorrecte.
- Préciser que la qualification ne crée jamais de reçu économique, ne débite aucune ressource et ne démarre aucun minuteur.
- Définir la rétention des reçus au moins sur la fenêtre maximale de reprise mobile.
- Ajouter dans le futur plan de preuve des cas cross-player/cross-hive et coupure après commit avant réception du reçu.

## Décision

Le contrat est compatible avec une implémentation serveur future, sous réserve de ces clarifications. Il ne justifie actuellement aucune route ni activation. La première application doit rester un contrat économique séparé, autoritaire, transactionnel et idempotent.

Ce rapport est le seul fichier ajouté dans cette fenêtre documentaire.
