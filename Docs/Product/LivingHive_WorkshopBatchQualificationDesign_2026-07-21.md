# LivingHive — qualification active du lot témoin

Date : 21 juillet 2026  
Statut : réalisé et ratifié après la fermeture du test manuel

La réalisation et ses preuves sont consignées dans
`Docs/Product/LivingHive_WorkshopBatchQualificationMilestone_2026-07-21.md`.
Le présent document conserve la conception et la frontière d'autorité qui ont
guidé l'implémentation.

## Problème produit

Le chapitre 4 comporte actuellement 13 objectifs, 28 interactions actives et
145 à 165 secondes de tâches chronométrées. Il est le seul plancher de l'Acte I.

Après la calibration de l'atelier, le joueur collecte manuellement 120 cire, ou
160 si le gabarit du chapitre 3 a été préparé. L'écran confirme ensuite la
spécialisation et conduit directement au choix de la première application. Cette
transition explique le résultat, mais ne demande pas au joueur de relier la
spécialisation choisie au risque opérationnel qu'elle crée.

La prochaine tranche doit renforcer cette compréhension. Elle ne doit pas
allonger artificiellement une minuterie, ajouter un coût, modifier le terrain ou
remplacer l'image LivingHive.

## Tranche proposée

Remplacer la confirmation passive `UpgradeCalibrationResult` par une étape
`UpgradeBatchQualificationChoice` immédiatement après la collecte autoritaire du
lot témoin et avant `UpgradeApplicationReady`. Son titre peut conserver
« Spécialisation validée » et son récapitulatif de collecte, mais son action
principale devient la qualification elle-même plutôt qu'un bouton de passage.

Le panneau rappelle :

- la spécialisation réellement confirmée par l'opération précédente;
- la quantité réellement récoltée dans le lot témoin;
- deux points de contrôle tactiles, dont un seul correspond au risque dominant.

Les réponses attendues sont :

| Spécialisation reconnue | Risque à identifier | Explication pédagogique |
| --- | --- | --- |
| Rendement | Maîtriser la chaleur | Un débit accru exige une température régulière pour conserver une cire homogène. |
| Stockage | Vérifier la tenue sous charge | Une capacité accrue exige des parois et des joints qui restent stables quand l'atelier se remplit. |

Une réponse incorrecte :

- ne consomme aucune cire, aucun miel et aucun pollen;
- n'ajoute aucun temps;
- ne modifie ni bonus, ni capacité, ni production;
- ne fait pas avancer le tutoriel;
- explique le lien avec la spécialisation et permet de réessayer.

Une réponse correcte ouvre la première application. Le résultat peut recommander
la réserve après une spécialisation Rendement et la nurserie après une
spécialisation Stockage, afin d'expliquer une continuité opérationnelle. Cette
recommandation reste informative : Réserve et Nurserie demeurent toutes deux
accessibles, avec leurs coûts, durées et effets actuels.

La tranche ajoute exactement une décision active. Le chapitre 4 conserve 13
objectifs et 145 à 165 secondes, mais passe de 28 à 29 interactions actives. Elle
ne prétend pas, à elle seule, atteindre la cible finale de 7 à 12 minutes.

## Expérience mobile

- Deux cibles directes d'au moins 44 px restent visibles en portrait 390 × 844 et
  en paysage 1600 × 900.
- Le titre, la spécialisation, la quantité du lot, l'explication et la fermeture
  doivent tenir sans défilement obligatoire ni collision avec le HUD ou le rail.
- Le toucher incorrect reste réversible et ne déclenche aucune commande
  économique.
- Le téléphone dérive le libellé attendu et la recommandation du dernier état
  reconnu par le serveur. La quantité affichée provient du dernier reçu reconnu;
  elle n'est jamais renvoyée comme précondition ou preuve dans la commande de
  qualification. Il peut garder temporairement l'état d'affichage et le dernier
  choix non officiel, mais ne crée aucun nouvel état autoritaire.
- Une perte de réseau conserve le dernier instantané reconnu en lecture seule. La
  qualification officielle ne progresse pas hors ligne.

## Autorité serveur

Le serveur reste propriétaire de :

- la spécialisation de l'atelier et sa révision;
- la quantité du lot produite, en attente et collectée;
- les soldes de ressources et les bonus de production ou de capacité;
- l'état ordonné du chapitre 4;
- l'éligibilité, le coût, la durée et le résultat de la première application;
- les horodatages UTC et les preuves persistées.

La future commande de progression tutorielle devra porter l'identité Bearer, la
ruche, l'étape observée, la réponse, `expectedRevision` et `idempotencyKey`. Le
serveur devra vérifier l'appartenance à la ruche, la spécialisation déjà
persistée, la collecte préalable du lot, l'ordre monotone et la réponse attendue.
Un rejeu identique devra retourner le même reçu; une commande contradictoire ou
périmée devra être refusée sans mutation.

La séquence autoritaire attendue est :

1. l'opération de collecte réussie persiste la quantité réellement collectée,
   puis place le tutoriel à `chapter4.upgrade_batch_qualification`;
2. le mobile rend cet état sous `UpgradeBatchQualificationChoice`;
3. une réponse incorrecte garde cet état et la même révision;
4. une réponse correcte fait avancer une seule fois vers
   `chapter4.upgrade_application_ready`, rendu par `UpgradeApplicationReady`.

La commande n'accepte jamais une spécialisation ou une quantité fournie par le
mobile. Elle relit dans la même transaction la spécialisation terminée, le reçu
de collecte et la quantité persistée. Spécialisation absente ou inconnue, lot non
collecté, étape antérieure, étape déjà dépassée et révision obsolète sont des
préconditions invalides; aucune préférence client ne résout ces états.

Codes de résultat prévus, alignés sur l'enveloppe `game.*` existante :

| Situation | HTTP | Code stable | Mutation |
| --- | ---: | --- | --- |
| Session absente | 401 | `game.session_required` | aucune |
| Identifiant, réponse, révision ou clé mal formés | 400 | `game.invalid_request` | aucune |
| Ruche introuvable ou non visible pour ce joueur | 404 | `game.not_found` | aucune |
| Même clé avec une charge canonique différente | 409 | `game.idempotency_conflict` | aucune |
| Révision durable différente | 409 | `game.revision_conflict` | aucune |
| Spécialisation, collecte ou étape incompatibles | 409 | `game.tutorial_precondition_failed` | aucune |
| Réponse pédagogique incorrecte | 200 | `game.tutorial_answer_incorrect` | aucune; même révision |
| Réponse correcte | 200 | `game.tutorial_advanced` | étape et révision seulement |

Le reçu pédagogique contient l'étape précédente, l'étape résultante, la réponse
canonique, la révision avant/après, un code de retour localisable et un
horodatage UTC généré par le serveur. Il ne contient aucun reçu économique. Une
heure mobile, une quantité affichée ou une recommandation ne font jamais partie
de la preuve canonique.

La portée d'idempotence minimale est joueur authentifié + ruche + commande. Les
reçus doivent être retenus au moins pendant toute la fenêtre maximale de reprise
mobile. Une coupure après commit mais avant réception doit donc permettre au
mobile de renvoyer exactement la même clé et d'obtenir exactement le même reçu,
sans seconde progression. La révision augmente uniquement sur la réponse
correcte; une erreur pédagogique ou de précondition ne la consomme pas.

Cette commande pédagogique ne devra jamais être utilisée pour débiter la cire ou
appliquer un bonus. La première application restera une opération économique
serveur distincte, transactionnelle et idempotente.

## Responsabilités d'intégration

- `Architecte` possède l'ancrage LivingHive, le déroulement du tutoriel, la
  localisation et les preuves Unity.
- `Intégrateur de production` possède tout futur contrat, modèle, repository,
  test et déploiement sous `Server/`.
- `Communication` conserve la propriété exclusive du chat. Cette tranche ne
  modifie ni son présentateur, ni ses modules, ni ses tests, ni ses documents.

Aucun code serveur ne doit être ajouté avant l'audit de contrat par
l'Intégrateur. Aucun endpoint ou drapeau de production ne sera ouvert avant le
raccordement du shell mobile, de l'authentification et de l'adaptateur HTTP.

## Plan de preuve avant ratification

La future implémentation devra vérifier au minimum :

1. branche Rendement : erreur Tenue sous charge, puis correction Chaleur;
2. branche Stockage : erreur Chaleur, puis correction Tenue sous charge;
3. absence de coût, délai, gain et progression après une erreur;
4. conservation de la quantité réellement collectée, y compris le lot de 160
   issu du gabarit du chapitre 3;
5. maintien des deux applications après la recommandation;
6. une seule réussite comptabilisée malgré des touchers répétés;
7. cibles tactiles utilisables en portrait et paysage;
8. catalogues `fr-CA` et `en-US` alignés, sans texte codé en dur ajouté;
9. compilation globale et suite LivingHive F8;
10. captures dédiées 390 × 844 et 1600 × 900 avec contrôle strict des dimensions;
11. isolation cross-player et cross-hive : aucun reçu, état ou idempotency key ne
    peut être réutilisé pour une autre identité ou une autre ruche;
12. coupure simulée après commit et avant réception, suivie d'un rejeu qui rend le
    même reçu sans seconde progression;
13. spécialisation absente ou inconnue, lot non collecté, étape dépassée et
    révision obsolète refusés sans mutation;
14. empreintes inchangées de la scène canonique, du terrain 50 × 50, de ses images
    et de l'image de base LivingHive.

## Hors périmètre

- aucun nouveau minuteur;
- aucune nouvelle ressource ou monnaie;
- aucun achat, accélérateur ou avantage payant;
- aucune automatisation de la collecte manuelle;
- aucune modification du chat;
- aucune modification du terrain ou de l'image de base LivingHive;
- aucun déploiement serveur.

Cette conception adapte un besoin fonctionnel de lecture active à l'univers des
abeilles. Elle ne copie ni texte, ni interface, ni contenu d'Ant Legion.

## Cartographie d'implémentation en attente de levée du gel

Cette cartographie a été établie en lecture seule pendant le test manuel. Elle ne
constitue pas une modification du jeu.

### Présentateur LivingHive

Dans `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` :

1. ajouter `UpgradeBatchQualificationChoice` entre
   `UpgradeCalibrationCollect` et `UpgradeApplicationReady`; l'ancien
   `UpgradeCalibrationResult` passif est remplacé par ce panneau actif;
2. faire aboutir la collecte manuelle de l'atelier directement sur ce nouvel
   état, en conservant `guidedUpgradeCalibrationCollectedWax` comme valeur de
   récapitulatif;
3. ajouter des compteurs de preuve remis à zéro avec le chapitre : tentatives,
   erreurs et réussites; une seule réussite est possible;
4. exposer `upgrade_batch_qualification_expected`,
   `upgrade_batch_qualification_attempt_count`,
   `upgrade_batch_qualification_error_count`,
   `upgrade_batch_qualification_success_count` et
   `upgrade_batch_qualification_recommendation` dans
   `GuidedCollectionTutorialForProof()`;
5. ajouter la clé d'état `upgrade_batch_qualification_choice`, la maintenir dans
   l'objectif 6/13 et l'autoriser dans le bloqueur de choix guidé;
6. réutiliser les deux rectangles tactiles existants de
   `TutorialBroodChoiceButtonRect`, sans créer une troisième commande ni réduire
   leur hauteur;
7. ajouter un helper unique de décision. Il compare le choix à
   `guidedUpgradePlan == "production"`, garde le même état après une erreur et
   ouvre `UpgradeApplicationReady` après la première bonne réponse;
8. ne modifier ressources, bonus, compteurs de commit, ouvrières ni horloge dans
   ce helper;
9. conserver les deux boutons Réserve et Nurserie dans l'état suivant, quelle que
   soit la recommandation;
10. porter la décision dans la ligne de rythme `lot_etalonnage`, qui passe de 0 à
    1 décision et conserve sa collecte manuelle et ses 10 secondes.

La preview locale peut compter les erreurs pour la preuve, mais ces compteurs ne
sont ni une économie ni un profil stratégique. L'état officiel futur reste le
contrat serveur décrit plus haut.

Le checkpoint local actuel ne sérialise pas l'ordinal de l'enum : il conserve le
nom de l'objectif interrompu pour l'explication, puis restaure la transaction au
début du chapitre. Le remplacement de l'état passif ne demande donc pas de
migration de sauvegarde. Il faut néanmoins conserver le nouvel état à l'intérieur
de la plage contiguë du chapitre 4 et mettre à jour tous les switches de rendu,
d'objectif, de blocage tactile et de preuve qui citent l'ancien état.

### Localisation

Ajouter les mêmes neuf clés aux catalogues `fr-CA` et `en-US`, sans doublon :

- `tutorial.chapter_04.batch_qualification.title`;
- `tutorial.chapter_04.batch_qualification.production.body`;
- `tutorial.chapter_04.batch_qualification.capacity.body`;
- `tutorial.chapter_04.batch_qualification.heat.button`;
- `tutorial.chapter_04.batch_qualification.load.button`;
- `tutorial.chapter_04.batch_qualification.retry.production`;
- `tutorial.chapter_04.batch_qualification.retry.capacity`;
- `tutorial.chapter_04.batch_qualification.confirmed.production`;
- `tutorial.chapter_04.batch_qualification.confirmed.capacity`.

Les corps reçoivent uniquement la quantité reconnue `{0}`. Aucun texte ne doit
présenter cette valeur comme une donnée envoyée au serveur. Les retours confirmés
recommandent une application tout en nommant explicitement que l'autre reste
accessible.

Copie prête à intégrer :

| Clé | `fr-CA` | `en-US` |
| --- | --- | --- |
| `.title` | Qualifier le lot témoin | Qualify the test batch |
| `.production.body` | Le rendement augmente le débit. Le lot reconnu contient {0} cire. Quel risque faut-il contrôler avant l'affectation? | Yield increases throughput. The acknowledged batch contains {0} wax. Which risk must be checked before assignment? |
| `.capacity.body` | Le stockage augmente la capacité locale. Le lot reconnu contient {0} cire. Quel risque faut-il contrôler avant l'affectation? | Storage increases local capacity. The acknowledged batch contains {0} wax. Which risk must be checked before assignment? |
| `.heat.button` | Maîtriser la chaleur | Control the heat |
| `.load.button` | Vérifier la tenue sous charge | Check load-bearing strength |
| `.retry.production` | Un débit accru rend la régularité thermique prioritaire. Aucune ressource ni aucun temps n'est perdu : essaie encore. | Higher throughput makes temperature consistency critical. No resource or time is lost: try again. |
| `.retry.capacity` | Une capacité accrue charge davantage les parois et les joints. Aucune ressource ni aucun temps n'est perdu : essaie encore. | Higher capacity puts more load on walls and joints. No resource or time is lost: try again. |
| `.confirmed.production` | Lot qualifié. L'étanchéité de la réserve est recommandée pour ce débit, mais la doublure de nurserie reste disponible. | Batch qualified. Reserve sealing is recommended for this throughput, but nursery lining remains available. |
| `.confirmed.capacity` | Lot qualifié. La doublure de nurserie est recommandée pour cette stratégie de stockage, mais l'étanchéité de la réserve reste disponible. | Batch qualified. Nursery lining is recommended for this storage strategy, but reserve sealing remains available. |

Les préfixes complets restent
`tutorial.chapter_04.batch_qualification.*`. Les apostrophes et espaces doivent
être conservés en UTF-8, et les deux catalogues doivent rester strictement
alignés.

### Tests éditeur

Dans `SandboxLivingHiveManualCollectionTests.cs` :

- étendre le parcours Rendement après la collecte de 120 cire : choisir d'abord
  Charge, vérifier l'absence de mutation et le maintien de l'état, puis choisir
  Chaleur et vérifier la recommandation Réserve;
- étendre le parcours Stockage jusqu'à la collecte réelle : choisir d'abord
  Chaleur, corriger avec Charge et vérifier la recommandation Nurserie;
- couvrir le lot de 160 cire provenant du gabarit du chapitre 3 et vérifier que
  la qualification ne le remplace pas par 120;
- vérifier que touchers répétés après réussite ne comptent pas une seconde fois;
- vérifier les deux rectangles en portrait et paysage;
- mettre à jour la matrice de rythme de 28 à 29 interactions pour le chapitre 4.

Les preuves doivent comparer avant/après au minimum la cire, le miel, le pollen,
la stabilité, les bonus de production/capacité, le temps guidé et tous les
compteurs économiques de commit.

Matrice comportementale minimale :

| Action | État suivant | Ressources/bonus/temps | Compteurs pédagogiques |
| --- | --- | --- | --- |
| Charge sur spécialisation Rendement | qualification inchangée | strictement inchangés | tentatives +1, erreurs +1, réussites 0 |
| Chaleur sur spécialisation Rendement | application prête | strictement inchangés | tentatives +1, réussites +1 |
| Chaleur sur spécialisation Stockage | qualification inchangée | strictement inchangés | tentatives +1, erreurs +1, réussites 0 |
| Charge sur spécialisation Stockage | application prête | strictement inchangés | tentatives +1, réussites +1 |
| Nouveau toucher après réussite | application prête inchangée | strictement inchangés | aucun compteur supplémentaire |

Le test du lot de 160 doit prouver simultanément
`upgrade_calibration_collected_wax:160`, l'absence de remise à 120 et le maintien
du coût d'application à 80 cire, puisque ce lot provient du gabarit et non de la
trousse. Le parcours séparé de la trousse doit continuer à prouver un lot de 120
et un coût d'application réduit à 40 cire.

### Captures

Dans `SandboxGuidedOpeningInstallationCapture.cs` :

- ajouter `Chapter4_BatchQualification_390x844.png`;
- ajouter `Chapter4_BatchQualification_1600x900.png`;
- préparer un état Rendement avec 120 cire réellement collectée et s'arrêter sur
  `UpgradeBatchQualificationChoice`;
- conserver le contrôle strict des dimensions, le manifeste et l'absence de
  recadrage ou redimensionnement après capture.

La ratification visuelle doit inspecter le titre, la spécialisation, la quantité,
l'explication, les deux commandes, la fermeture et l'absence de collision avec
le HUD et le rail inférieur.

### Fichiers prévus

La future tranche doit rester limitée à :

- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxGuidedOpeningInstallationCapture.cs`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`;
- documents produit et de continuité associés.

Elle ne requiert aucune modification de scène, prefab, image, terrain, module de
chat ou code serveur durant la tranche Unity.

## Empreinte préalable des fondations protégées

Lecture effectuée pendant le gel, sans ouvrir Unity ni réécrire un fichier :

| Fondation | Taille | Horodatage local | SHA-256 |
| --- | ---: | --- | --- |
| `Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity` | 7 776 octets | 2026-07-17 17:11:05 | `927fa2a719033270e8ad4bf66c719fad7a1414a08f9705d400d40a5de122b1b3` |
| `Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/background_hive.png` | 7 489 785 octets | 2026-07-13 11:10:51 | `3c0e3b97e8e7ad76fc2c46a9342c4f9d7b03717591356251945c8f3f62b467f6` |
| manifeste runtime Wave 6 exact-crop | 862 548 octets | 2026-07-17 19:12:39 | `880b30c432d44803ba118c29adae0b0a6f0093d1e64a2707fc46d5395b3f230d` |

Le manifeste déclare le schéma
`bee-kingdom.world-map.wave6-unity-runtime-bundle.v2`, une grille 50 × 50 et
2 500 tuiles; les 2 500 identifiants et fichiers déclarés sont distincts. Son
master est `bb79d07543d80624e3f3727cce5d03a4b85a9892e7615cd3ecdfe2b3ee8214a8` et
sa source superpanel est
`3ce816052fff97bcde78251fa930c4d725dc622120d3644c806a9c1be1330697`.

Cette lecture prouve la référence du manifeste, pas le contenu courant de chaque
PNG. Après l'implémentation et hors créneau de test utilisateur, la validation
exact-crop existante devra recontrôler les 2 500 fichiers et leurs empreintes;
aucune simple comparaison du manifeste ne suffira à ratifier le terrain.

## Porte de synchronisation

Le dernier rapport `.codex/vm-sync-last-report.txt`, daté du 21 juillet 2026 à
15:23:11 heure locale, bloque encore deux conflits :

- `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md`;
- `Docs/VM/Codex_VM_Continuation.md`.

Il signale aussi sept suppressions en attente. Conformément à `AGENTS.md`, aucune
nouvelle synchronisation ne doit être lancée et aucun conflit ne doit être écrasé
pendant le test. Le rapport est antérieur au présent document et ne constitue pas
une preuve que les fichiers documentaires récents ont été transférés. Après la
levée du gel, il faudra d'abord réconcilier explicitement les deux documents avec
la copie principale, puis relire le nouveau rapport avant toute implémentation ou
synchronisation finale.
