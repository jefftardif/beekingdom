# Builder-B - Step5A Wave3 Unity Import Payload

## Resume

Le payload d'import Wave3 Step5A est genere et verrouille hors `Assets`. Il fournit a Builder-A les 25 textures runtime precontrolees sans toucher a Unity, a la scene, aux `ProjectSettings` ou aux `.meta`.

Le rapport canonique se trouve hors de la racine inscriptible de cette session. Le present fichier est le fallback autorise.

## Emplacements

Payload:

`C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_UnityImportPayload_staging\`

Documentation:

`C:\projets\beekingdomgame-master\Docs\BuilderB\WorldMapStep5AImportPayload\`

## Identite immuable

- payload ID: `step5a-uib-wave3-continuous-v1-f458571e4e2de481`;
- master SHA-256: `d3cdc2dde9d56cac58be6833790b6fd8fc38ac157f72a01dcebd8117583a95b4`;
- aggregate des 25 tuiles: `f458571e4e2de48145282af234f4ada385d5de1af5d0b7d20dbc59f9c52a6c3a`;
- arbre payload hors preflight: `377ca038ad9364cd194d49319f5ddd45136b43ae613d6a542b6548948d21b823`;
- fichiers verrouilles hors preflight: `31`;
- rebuild sur le meme dossier: refuse.

## Resultats du preflight

Preflight initial machine-readable: `PASS`, 25 checks PASS.

Reverification en lecture seule: `PASS`, 30 checks PASS.

Controles:

- 25/25 PNG `516x516 RGB`;
- noms `R0C0_g2.png..R4C4_g2.png`, uniques;
- IDs, coordonnees, SHA PNG et SHA pixels uniques;
- SHA conformes au manifest runtime et au handoff valide;
- copies payload byte-identiques 25/25;
- interieur `512x512`: `0` pixel different;
- runtime complet avec gutters/clamp: `0` pixel different;
- 40 frontieres internes verifiees;
- 80 cotes internes diriges issus des vrais voisins;
- 20 cotes externes clampes;
- orientation identite row-major validee;
- 25 destinations futures uniques;
- Repeat et remplissage modulo 64x64 interdits.

Le resultat est disponible dans:

`artifacts/WorldMapWave3_UnityImportPayload_staging/preflight.result.json`

## Inventaire futur

Les 25 lignes source vers destination se trouvent dans:

`artifacts/WorldMapWave3_UnityImportPayload_staging/source-to-future-destination.csv`

Destination cible commune (etat et integration sous responsabilite Builder-A):

`Assets/BeeKingdom/Playground/Resources/WorldMapWave3Runtime/UIB_ContinuousMaster5x5_v1/`

Aucune creation, copie ou modification sous `Assets` n'a ete effectuee par Builder-B. Le chemin cible peut exister ou evoluer pendant le travail parallele de Builder-A; il n'appartient pas au payload Builder-B.

## Import recommande

Editor: Default 2D, sRGB, RGB sans alpha, Read/Write Off, NPOT None, Clamp, Bilinear, aniso 1, mipmaps Off, max 1024, premiere preuve non compressee RGB24.

Android: ASTC 6x6 RGB Best sans Crunch; fallback ETC2 RGB4 sans Crunch seulement si requis par les appareils.

UV visibles: `2/516..514/516`. Les gutters soutiennent le filtrage et ne deviennent jamais du contenu visible.

## Memoire

- PNG disque: `11.2539 MiB`;
- RGB24 brut: `19.0441 MiB`;
- RGBA32 de reference: `25.3922 MiB`;
- ASTC 6x6: `2.8214 MiB`;
- ETC2 RGB4: `3.1740 MiB`.

Fenetre stable: 25 textures. Transition: plafond 30. Chargement complet 64x64 et duplication des tuiles interdits.

## Rollback

Builder-A doit conserver Step4C et selectionner le provider Wave3 par flag. Toute couture, grille, orientation fausse, derive, hash divergent ou regression de pan declenche un retour au provider Step4C sans modifier le payload ni supprimer les versions precedentes.

## Non-claims et portee

- Unity non invoque;
- aucun fichier Unity produit modifie par Builder-B;
- scene et terrain non modifies;
- aucun `.meta` cree;
- integration et validation Unity non declarees;
- aucune carte/serveur live;
- aucune route terrestre.

## Verdicts

STEP5A_IMPORT_PAYLOAD=PASS

NO_UNITY_PRODUCT_FILES_MODIFIED=YES

READY_FOR_BUILDER_A=YES
