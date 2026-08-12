# LivingHive — fondation serveur de sortie au périmètre

## Résultat

Le contrat `phase5-hive-perimeter-sortie-v1` prépare la première sortie après la composition d’escouade sans prétendre que la carte mondiale est active. Il décrit deux signaux non-combat, `foraging_scout` et `brood_watch`, dans un cycle UTC de huit heures. Chaque signal expose une instance serveur liée au joueur, à la ruche et au cycle, ainsi qu’un risque doctrinal canonique utilisable par la future recommandation mobile.

Le lancement exige une réservation `phase4-combat-squad-reservation-v1` réelle. L’escouade reste réservée pendant la sortie. Une réclamation après `EndsAtUtc` crédite la récompense exactement une fois puis libère les trois familles; un rappel libère sans récompense. Un signal déjà réclamé ne peut plus produire de récompense dans le même cycle. Les reçus idempotents survivent au redémarrage et au changement de cycle.

## Frontière mobile

- Appareil : cache de lecture borné, brouillon de composition, rendu, compte à rebours dérivé de `EndsAtUtc` et notifications locales.
- Serveur : identité du signal, cycle, horloge, éligibilité, réservation, sortie active, révisions, idempotence, réclamation et soldes de ressources.
- Interdit sur l’appareil : inventer l’heure officielle, une récompense, une victoire, une perte, une coordonnée ou un état de sortie.

La carte mondiale demeure `PreparationOnly/ReadOnlyNonLiveFoundation`. Les routes sont authentifiées et restent fermées par `HivePerimeterSortie:Enabled=false` dans les configurations par défaut et Production. `DeploymentAuthorized=false`.

## Preuves serveur

- `HivePerimeterSortieTests` : 5/5, avec DurableJson, reconstruction, rollover UTC, crédit unique, rappel, capacité atomique, isolation et corruption migrateur.
- `BeeKingdom.HiveOperations.Tests` : 52/52.
- `HivePerimeterSortieEndpointTests` sur la cible net10 prévue par le projet : 5/5, avec bearer, dépôt isolé et horloge injectable.
- `BeeKingdom.Tests` net10 : 265 réussis, 7 tests SQL ignorés, 0 échec.
- Build Release serveur : 0 erreur; avertissement de conflit SqlClient préexistant.

Le rapport d’intégration détaillé est `Docs/ProductionIntegration/LivingHive_Phase5_HivePerimeterSortie_Server_2026-07-22.md`.

## Porte client encore ouverte

Le contrat mobile injectable existe maintenant dans `BeeKingdom.Networking` et
valide strictement les snapshots, les routes, les identités et les révisions.
L'audit de ce raccordement a fait ajouter `Revision` au snapshot serveur; la
séquence rev0 → rev1 → rev2 → second lancement rev3 est couverte côté service,
HTTP et client. Le même audit a ajouté `ServerTimeUtc`, issu de l'horloge
injectable et couvert côté service/HTTP, afin que le mobile dérive son compte à
rebours sans croire son horloge murale autoritaire. Le transport authentifié de production, le cache protégé et le
présentateur restent volontairement non raccordés. Tant que la session mobile
officielle et le flag serveur restent fermés, l’interface ne doit pas simuler
ces mutations. Rapport client :
`Docs/Product/LivingHive_HivePerimeterSortieMobileContractMilestone_2026-07-22.md`.

La ratification Unity de la composition précédente est maintenant acquise : le
F8 global contemporain est vert et le harnais stratégique produit 12/12 images
exactes. Les compositions mixtes FR 390x844 et EN 1600x900 ont été inspectées à
résolution native sans collision.

## Synchronisation et fondations protégées

La synchronisation officielle tentée à `2026-07-22T15:20:11Z` a échoué avant toute copie avec `Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport `.codex/vm-sync-last-report.txt` reste daté de `2026-07-22T02:57:51Z`, avec 0 conflit et 4 suppressions historiques en attente. Le jalon demeure uniquement sur `C:`; aucun accès direct à `Z:` ni remappage n’a été tenté.

- scène canonique 50x50 : `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive : `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Ces empreintes sont inchangées. Communication est resté entièrement gelé.
