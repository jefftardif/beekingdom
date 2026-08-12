# QA-A - DEMO-100 Packet Readiness Receipt

Date : 2026-07-14  
Portee : prevalidation hors Unity, paquet staging en lecture seule  
Produit modifie par QA : non  
Verdict produit Step5A : non evalue

## Verdict de preparation

**NOT READY**

La documentation, le template et l'organisation de capture couvrent correctement Step5A. Toutefois, le validateur final peut accepter une preuve purement declarative et ne garantit donc pas encore que les futures preuves sont de vraies captures interactives.

## Controles conformes

- baseline initiale SHA-256 : `23/23` fichiers presents et conformes avant ajout UI-A ;
- baseline initiale JSON : `8/8` lisibles avant ajout du contrat UI-A ;
- scripts PowerShell : `6/6` parsables, zero erreur de syntaxe ;
- copies QA, DEMO-099, Architecte et handoff Wave3 identiques aux sources canoniques ;
- Step4D explicitement revoque et aucun verdict produit anticipe ;
- protocole paysage `1920x1080` et portrait `720x1280` ;
- reperes naturels, 25 tuiles, HUD fixe, video/strips, bornes camera et vols air-only documentes ;
- initialisation/copie/strip/hashes prevus hors `Temp` et `Library`.

## Reference UI-A recue

Les quatre copies UI-A maintenant presentes sous `references/` correspondent bit pour bit aux sources canoniques. Le contrat contient bien `13` landmarks uniques, `3` paires de pan coherentes et `3` pivots de zoom coherents.

Elles ne sont toutefois pas encore integrees au gate DEMO-100 :

- `DEMO-100_SHA256SUMS.txt` contient toujours `23` entrees alors que le staging contient maintenant `27` fichiers hors inventaire ;
- les quatre fichiers UI-A ne figurent pas dans `DEMO-100_StagingManifest.json` ni dans `references/README.md` ;
- le protocole Demo, son template et `Test-DEMO100Evidence.ps1` n'exigent pas `L01..L13`, `PH01/PH02/PV01` ou `Z01/Z02/Z03` ;
- les seuils UI-A `2 px`, HUD `1 px` et ratio `0.995..1.005` ne sont pas appliques ;
- aucune garde n'interdit encore les annotations UI-A dans les captures runtime.

La presence des copies ne suffit donc pas a rendre la reference obligatoire ou executable.

## Blocker decisif

Un test negatif QA a fourni au script `Test-DEMO100Evidence.ps1` :

- zero PNG, video, GIF ou WebM ;
- un unique fichier texte reutilise pour toutes les frames, strips et vues produit ;
- zero mesure de mouvement, pivot ou HUD ;
- aucun repere naturel renseigne ;
- un rapport Builder-A inexistant ;
- des artefacts situes sous le repertoire temporaire ;
- uniquement des booleens `pass=true` dans le manifeste.

Resultat observe :

```text
validator exit code = 0
validator pass = true
failed checks = 0
real image/video files = 0
gesture metrics present = false
handoff report exists = false
```

Le paquet ne refuse donc pas encore une preuve declarative fabriquee.

## Causes

1. `Test-ArtifactRecord` controle seulement chemin et hash, sans verifier extension, decodage, resolution ou nature du media.
2. Le handoff est accepte sur `received=true` et un chemin non vide ; existence, SHA-256 et inventaire Builder-A ne sont pas verifies par le validateur final.
3. Le run persistant et l'absence de `Temp/Library` sont lus comme declarations ; les chemins reels du manifeste et des artefacts ne sont pas controles.
4. Les champs `metrics`, les descriptions `T0/T1` et `natural_terrain` peuvent rester nuls.
5. Transformation, HUD fixe, bornes, selection et vol sont acceptes via leurs seuls champs `pass`.
6. Le resultat de `Test-DEMO100Wave3Bundle.ps1` n'est pas exige ni hashe par le validateur final ; les comptes et hashes Wave3 peuvent etre declares.
7. Le protocole annonce video **ou** strips, alors que le validateur exige actuellement chaque strip meme lorsqu'une video est fournie.
8. Les quatre sources UI-A sont copiees mais absentes de l'inventaire contractuel et des controles du validateur.

## Corrections requises avant readiness

- verifier et decoder les PNG aux resolutions attendues ; verifier type/duree/resolution des videos ; interdire la reutilisation d'un fichier unique ;
- exiger un rapport Builder-A existant dont le hash correspond au manifeste et dont l'inventaire est present ;
- resoudre le run et tous les artefacts, exiger qu'ils soient sous le run persistant et hors `Temp/Library` ;
- exiger des reperes naturels non vides et des metriques numeriques conformes aux seuils QA, sans faire confiance aux seuls booleens `pass` ;
- verifier individuellement HUD, quatre bornes, selection/halos/hit zones et ancrage du vol ;
- exiger le recu runtime Wave3 25/25, son hash et son inventaire reel ;
- appliquer exactement l'alternative `video continue OU strips complets + observation directe tracee` ;
- inscrire et hasher les quatre sources UI-A dans le staging, puis exiger les 13 landmarks, les trois pans, les trois pivots et les seuils UI-A ;
- refuser toute annotation UI-A dans le runtime et n'autoriser les anneaux/croix/numeros que sur des derives QA relies aux captures brutes ;
- ajouter le test negatif ci-dessus comme regression obligatoire.

Le paquet pourra etre represente a QA apres correction des scripts. Unity doit rester ferme et aucun PASS produit ne peut etre emis d'ici la.

```text
QA_DEMO_100_PACKET_READY=NO
UIA_STEP5A_REFERENCE_REQUIRED=YES
UIA_REFERENCE_IN_DEMO100_GATE=NO
UNITY_LAUNCHED_BY_QA=NO
DEMO_100_PRODUCT_VERDICT=NOT_EVALUATED
READY_FOR_OFFICIAL_RUNTIME_VALIDATION=NO
```
