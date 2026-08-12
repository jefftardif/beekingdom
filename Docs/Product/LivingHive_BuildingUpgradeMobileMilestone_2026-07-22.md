# LivingHive — file de construction officielle et frontière mobile/serveur

## Résultat

LivingHive possède maintenant une tranche verticale de construction persistante sous le contrat `living-hive-building-upgrade-v1`. Le serveur est l'unique auteur du catalogue, des coûts, des soldes, du créneau de construction, de l'heure UTC, de la durée, du niveau obtenu, de la révision et des reçus idempotents. Le mobile valide ces données, les présente avec des cibles tactiles d'au moins 44 pixels et ne peut jamais débiter, terminer ou créditer une amélioration localement.

La fonctionnalité reste volontairement fermée par `BuildingUpgrades:Enabled=false` par défaut et en Production. Le catalogue live est vide. Les valeurs 20 miel, 60 pollen et une heure appartiennent uniquement aux tests; elles ne constituent pas un équilibrage produit.

## Frontière appareil / serveur

### Sur l'appareil

- rendu, localisation, interaction tactile et état transitoire du panneau;
- access token en mémoire et futur refresh token dans le coffre Android;
- dernier GET validé dans le cache protégé, borné et partitionné par joueur, ruche, contrat et route;
- même clé d'idempotence conservée pour une demande dont le résultat réseau est incertain;
- compte à rebours d'affichage extrapolé par temps monotone seulement;
- lecture hors ligne explicitement sans mutation.

### Sur le serveur

- appartenance joueur/ruche et révision autoritaire;
- catalogue, coût, durée, niveau courant et niveau suivant;
- soldes, débit atomique et unicité du créneau de construction;
- heure UTC, échéance et décision de complétion;
- persistance de l'opération, du niveau et du reçu;
- rejeu idempotent et conflits de révision, de clé, de ressources ou de créneau.

Les deux POST contiennent seulement `expectedRevision` et `idempotencyKey`. Le mobile ne transmet aucun coût, montant, niveau, délai, heure ou résultat faisant autorité.

## Contrat et comportement mobile

- `GET /game/v1/hives/{hiveId}/building-upgrades`;
- `POST /game/v1/hives/{hiveId}/building-upgrades/{buildingKey}/start`;
- `POST /game/v1/hives/{hiveId}/building-upgrades/{operationId}/complete`;
- une seule rotation de session après `401`, puis rejeu de la requête strictement identique;
- aucune répétition aveugle d'une mutation après panne réseau;
- acceptation sûre d'un reçu idempotent original même si un snapshot ultérieur montre déjà l'opération terminée;
- complétion activée uniquement lorsque le serveur publie `awaiting_completion`; un compte à rebours local arrivé à zéro ne suffit jamais;
- codes de refus serveur conservés jusqu'au texte joueur : révision, file occupée, ressources, niveau, échéance, opération, idempotence et indisponibilité.

Le contrôleur est composé par `MobileAccountSessionRuntimeBootstrap` et fermé au logout ou au changement de joueur. Le panneau du bâtiment `wax_workshop` et la file latérale utilisent les données officielles lorsqu'un contrôleur est injecté. Sans environnement officiel, l'interface reste `NotConfigured` et la démonstration locale conserve son statut de prévisualisation non officielle.

## Validation obtenue

- serveur ciblé, service + HTTP : **6/6**;
- serveur complet net10.0 : **321 réussis, 0 échec, 8 SQL ignorés**, TRX `Server/tests/BeeKingdom.Tests/TestResults/LivingHiveBuildingUpgradeFullNet10.trx`;
- harnais autonome du client mobile : **13/13**;
- `BeeKingdom.Networking`, `Assembly-CSharp`, `BeeKingdom.Tests` et `Assembly-CSharp-Editor` : **0 erreur** avec les nouveaux fichiers inclus temporairement dans les projets générés, puis projets restaurés sans ces lignes temporaires;
- catalogues `fr-CA` et `en-US` : **1115 entrées chacun**, **35 clés construction**, 0 doublon, 0 valeur vide;
- scène canonique : SHA-256 `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive : SHA-256 `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : SHA-256 `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Le premier passage autonome a révélé une assertion de test erronée : `101/1000` était qualifié de dépassement. Le test utilise maintenant réellement `capacité + 1` et les 13 preuves passent. La compilation a aussi détecté puis fait corriger un conflit de nom local dans le présentateur.

## Validation Unity encore ouverte

Unity est resté fermé pendant la tranche. La tentative de lancement automatisé a été refusée par la limite d'utilisation de l'outil, et non par le projet. Aucun contournement n'a été tenté. Il ne faut donc pas encore annoncer F8 ni les preuves visuelles comme ratifiés.

Le harnais `SandboxLivingHiveBuildingUpgradeCapture` est prêt pour deux preuves honnêtes `NotConfigured` :

- portrait français 390x844;
- paysage anglais 1600x900.

À la prochaine ouverture Unity : laisser finir l'import, confirmer zéro erreur C#, lancer F8, exécuter le harnais de capture, vérifier les dimensions, inspecter les deux images à résolution native et confirmer qu'aucun coût, niveau, succès ou reçu serveur n'est inventé.

## Fichiers client principaux

- `Assets/BeeKingdom/Networking/HiveBuildingUpgradeClient.cs`;
- `Assets/BeeKingdom/Playground/HiveBuildingUpgradePresentation.cs`;
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Tests/Editor/HiveBuildingUpgradeClientTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingUpgradeTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveBuildingUpgradeCapture.cs`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`;
- `Artifacts/BuildingUpgradeClientHarness/BuildingUpgradeClientHarness.csproj`;
- `Artifacts/BuildingUpgradeClientHarness/Program.cs`.

Le détail serveur est dans `Docs/ProductionIntegration/LivingHive_BuildingUpgrade_ServerAuthority_2026-07-22.md`.

## Portes encore ouvertes

1. ratifier compilation Unity, F8 et les deux captures exactes;
2. choisir, auditer et versionner le catalogue économique réel;
3. prouver la persistance SQL avec une base jetable;
4. configurer HTTPS/TLS staging, proxy de confiance, environnement mobile et HiveId;
5. valider deux joueurs, changement de compte, reprise réseau et Android Keystore sur appareil physique;
6. terminer le budget de contenu et produire un paquet Android IL2CPP installable;
7. seulement ensuite ouvrir les flags, reconstruire un candidat serveur, déployer et observer la télémétrie.

Communication est resté gelé. Aucun module, test, document ou ancrage chat n'a été modifié. Aucun terrain 50x50, PNG de terrain, image de base ou scène n'a été modifié.

## Synchronisation VM

La synchronisation de début de tranche a échoué avant toute copie parce que le partage hôte était inaccessible. Aucun conflit n'a été écrasé, aucun accès direct à `Z:` et aucun relâchement du bac à sable n'ont été tentés. Tous les changements de cette tranche restent sur la copie locale `C:` jusqu'à la prochaine synchronisation autorisée.
