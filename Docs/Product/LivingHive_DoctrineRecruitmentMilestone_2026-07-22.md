# LivingHive — recrutement doctrinal de la Caserne

Date : 22 juillet 2026

## Résultat joueur

`Armée -> Préparer` ne s’arrête plus à signaler deux familles absentes. Le
joueur peut sélectionner Gardiennes, Voltigeuses ou Lanceuses, puis lancer un
lot dans la vraie file locale de la Caserne :

- Gardiennes : 4, pour 680 miel et 180 pollen, 14 secondes;
- Voltigeuses : 6, pour 420 miel et 260 pollen, 14 secondes;
- Lanceuses : 8, pour 500 miel et 120 pollen, 14 secondes.

La ressource est débitée une seule fois, la file survit à une reprise, et le
lot rejoint seulement son compteur doctrinal à la fin. Il devient alors
immédiatement admissible dans le brouillon de préparation. Les anciens
`Soldats` et `Éclaireuses` restent visibles dans la réserve hors doctrine :
aucun compte n’est renommé, copié ou converti.

## Frontière mobile / serveur

### Appareil

- rendu, langue, gestes et animation de la file;
- journal `LocalPreviewHiveProgress v2` non protégé et non officiel;
- migration v1 -> v2 qui conserve bâtiments, Ouvrières, Soldats, Gardiennes et
  Éclaireuses, puis initialise uniquement Voltigeuses/Lanceuses à zéro;
- coûts et minuteur de démonstration, brouillon et dernier état local.

### Serveur

L’Intégrateur a ajouté `DoctrineRosterState` nullable au modèle durable v7.
Un ancien état migre sans seed : `null` signifie toujours `not_recorded`. Un
roster présent possède exactement `guardians`, `wingrunners` et `darters`, une
révision, une opération UTC éventuelle et des reçus idempotents bornés.

Routes préparées, authentifiées et fermées :

- `GET /game/v1/hives/{hiveId}/combat/recruitment`;
- `POST /game/v1/hives/{hiveId}/combat/recruitment/start`;
- `POST /game/v1/hives/{hiveId}/combat/recruitment/{operationId}/claim`;
- `GET /game/v1/hives/{hiveId}/combat/formation-readiness` reflète maintenant
  `not_recorded` ou les trois comptes réellement persistés.

`CombatRecruitment:Enabled=false` et
`CombatFormationReadiness:Enabled=false` restent fermés par défaut et en
Production. Le démarrage officiel vérifiera appartenance, Caserne niveau 1,
révision, soldes, file et clé d’idempotence, puis débitera atomiquement. Le
mobile n’est pas raccordé tant que la session et le transport sécurisé ne le
sont pas. Aucun compte, ressource, puissance ou combat officiel n’est simulé.

Rapport serveur :
`Docs/ProductionIntegration/LivingHive_Phase4_CombatRecruitment_Server_2026-07-22.md`.

## Validation

- Unity 6000.5.3f1 : compilation sans erreur C# et suite LivingHive F8 ratifiée
  dans `Artifacts/LivingHiveDoctrineRecruitment_F8_Ratified.log` : un marqueur
  de réussite, 0 `error CS`, 0 `Compilation failed`, 0 marqueur d’échec et
  sortie 0.
- Deux contrôles supplémentaires portent la campagne de 104 à 106 : migration
  v1 -> v2 sans seed et formation/reprise d’un lot doctrinal sans conversion
  legacy.
- Catalogues : 938/938 clés uniques et alignées, soit trois nouvelles clés
  `formation_readiness.recruitment.*` par langue.
- Assemblages Unity générés : `Assembly-CSharp` passe avec 0 erreur et 0
  avertissement; `Assembly-CSharp-Editor` passe avec 0 erreur et 100
  avertissements éditeur/dépendances non bloquants.
- Serveur : 44/44 tests HiveOperations, build Release 0 erreur; avertissement
  SqlClient historique. La tentative de suite HTTP compile, mais le runtime
  disponible découvre 0 test : elle n’est donc pas comptée comme preuve HTTP.
  Les drapeaux restent fermés et `DeploymentAuthorized=false`.
- Capture : sortie 0, dix PNG exacts dans
  `Docs/Product/Evidence/LivingHiveStrategicPath`; aucune erreur C#.
- Empreintes protégées inchangées : carte
  `927fa2a719033270e8ad4bf66c719fad7a1414a08f9705d400d40a5de122b1b3`,
  scène LivingHive
  `eccfe9aa81ae883317e4e951c8552dcef1a156179f35480567466ab95a9708e7`,
  image de ruche
  `3c0e3b97e8e7ad76fc2c46a9342c4f9d7b03717591356251945c8f3f62b467f6`.
- Communication n’a été ni modifié ni réveillé.

## Preuves visuelles inspectées

- `LivingHive_DoctrineRecruitment_Wingrunners_FR_390x844.png`, 390x844,
  SHA-256 `1285280c0a31749d2b4211e0756276118368a63ae563820bac73ed61d51d1367` :
  Voltigeuses à zéro, `Former +6`, coût, durée, Caserne et réserve historique
  tiennent sans collision dans le panneau mobile.
- `LivingHive_DoctrineRecruitment_Darters_EN_1600x900.png`, 1600x900,
  SHA-256 `ed36b3e5e4451cc0e5989c4afa315b36f61021e94ef4671223ef66ca1dc0ede0` :
  huit Darters enregistrées localement, rapport d’avantage et action de
  formation restent lisibles en paysage.

Le manifeste contient dimensions et empreintes des dix preuves. Aucun fichier
n’a été recadré ou redimensionné après capture.

## Limites et prochaine porte

- le cache mobile reste falsifiable et ne doit jamais devenir autorité;
- routes HTTP, SQL durable et staging Android restent des portes de promotion;
- le roster officiel n’est pas seedé et le client n’est pas raccordé;
- aucune composition, réserve d’unités, envoi ou résolution de combat n’existe.

La prochaine tranche utile est un brouillon de composition borné qui réserve
des unités sans combat et sans résultat, seulement après définition serveur de
la taille d’escouade, des engagements concurrents et de leur libération.

## Test manuel recommandé

Ouvrir `Assets/Scenes/LivingHive.unity`, entrer dans la démo, fermer
l’introduction, puis `Armée -> Préparer`. Sélectionner Voltigeuses, toucher
`Former +6`, vérifier le débit et la file, quitter Play avant la fin, reprendre
et attendre l’arrivée du lot. Les six Voltigeuses doivent devenir admissibles;
les compteurs Soldats et Éclaireuses doivent rester exactement inchangés.

## Synchronisation

Les synchronisations initiale et finale ont encore échoué avant copie avec
`Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun accès direct à `Z:`, remappage ou
contournement du bac à sable n’a été tenté. La tentative finale sur l’état
ratifié date du `2026-07-22T12:54:29Z`. Le dernier rapport valide reste celui du
`2026-07-22T02:57:51Z` : 0 conflit bloqué et 4 suppressions historiques en
attente. Cette tranche demeure donc uniquement sur la copie locale `C:`.
