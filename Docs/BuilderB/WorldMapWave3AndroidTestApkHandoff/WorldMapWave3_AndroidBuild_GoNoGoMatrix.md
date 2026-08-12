# World Map Wave3 - Matrice GO / NO-GO Android

## Decision actuelle

`CURRENT_BUILD_DECISION = NO-GO`

Le handoff est pret, mais la compilation reste interdite jusqu'aux trois PASS QA stricts.

## Gates avant lancement Unity

| ID | Gate | Exigence GO | Etat observe au handoff | Decision actuelle |
|---|---|---|---|---|
| G01 | QA runtime Step4D | rapport QA `PASS` + SHA-256 | preuve `PASS_WITH_RESERVES` observee, insuffisante | NO-GO |
| G02 | QA art/bundle Wave3 | rapport QA `PASS` + SHA-256 | validations techniques disponibles, PASS QA non etabli ici | NO-GO |
| G03 | QA integration Unity Wave3 | rapport QA `PASS` + SHA-256 | integration non realisee par Builder-B | NO-GO |
| G04 | Handoff Unity Wave3 | manifest 25/25 et validation PASS | PASS, 110 checks | GO |
| G05 | Outils Android | Unity/SDK/JDK/NDK/Gradle/ADB disponibles | PASS au 2026-07-14, a rejouer | GO provisoire |
| G06 | Espace disque | au moins 20 GiB | 463.19 GiB observes | GO provisoire |
| G07 | Sorties reservees | APK/log/manifest absents | APK reserve absent au handoff | GO provisoire |
| G08 | Scenes | splash puis WorldMap, explicites | presentes aux deux premiers index | GO |
| G09 | Signature | debug Unity, aucun custom/release | custom keystore desactive | GO provisoire |
| G10 | Methode build | methode dediee, Development, sans delete | inexistante avant gates; scripts actuels interdits | NO-GO jusqu'a creation apres QA |

GO global seulement si `G01..G10 = GO` dans la meme execution de preflight.

## Refus immediats

Le build ne doit pas demarrer si:

- un des trois gates QA vaut `PENDING`, `FAIL` ou `PASS_WITH_RESERVES`;
- un rapport QA n'a pas de chemin/hash traçable;
- une sortie `_001` existe deja;
- le script choisi contient `File.Delete` sur un APK;
- le build n'utilise pas `Development`;
- la scene splash ou WorldMap manque;
- la liste de scenes contient les onze scenes laboratoire supplementaires;
- un keystore release/custom ou un secret est demande;
- le module Android ou un outil embarque manque;
- espace libre inferieur a 20 GiB;
- un autre Unity verrouille le projet;
- le bundle Wave3 importe ne correspond pas au manifest valide;
- le mode Step4C/Wave3 actif n'est pas celui valide par QA.

## Gates post-build avant installation

| ID | Controle | GO | NO-GO |
|---|---|---|---|
| P01 | BuildResult | Succeeded, code Unity 0 | echec, annulation, exception |
| P02 | Trace APK | chemin exact, taille, SHA-256, UTC | fichier absent/ambigu |
| P03 | Non-ecrasement | anciens APK/logs hashes inchanges | ancien fichier modifie |
| P04 | Package | package attendu, Development/debuggable | package ou mode incorrect |
| P05 | Signature | certificat Android Debug | release/custom/inconnue |
| P06 | Architecture | `arm64-v8a` au minimum | ARM64 absent |
| P07 | Alignement | zipalign PASS | FAIL |
| P08 | Scenes | build manifest contient exactement splash + WorldMap | scene manquante/extra |
| P09 | Projet restaure | hashes/settings stables | ProjectSettings sale/incoherent |
| P10 | Non-claims | local/demo, no live | claim serveur/release/live |

## Gates smoke runtime

| ID | Surface | PASS attendu | BLOCKED |
|---|---|---|---|
| S01 | Lancement | pas de crash/ecran noir | crash, ANR, ecran noir |
| S02 | Splash | selecteur Development visible | absent, mauvais premier ecran |
| S03 | Navigation | bouton WorldMap ouvre la bonne scene | bouton muet/mauvaise scene |
| S04 | Portrait phone | HUD/panneaux lisibles, aucun texte critique coupe | overlap/coupe/inutilisable |
| S05 | Paysage tablette | carte utilise l'espace, HUD fixe | carte minuscule/HUD mobile |
| S06 | Pan | un doigt pan seulement | zoom/selection parasite |
| S07 | Pinch | deux doigts zoom doux/borne | saccade, pan parasite, selection |
| S08 | Alignement | ruches, ressources, halos et vols restent attaches | derive/saut |
| S09 | Vols | arcs aeriens independants du decor | suivi de route au sol |
| S10 | Raccords | aucune grille/couture | grille ou ligne de tuile |
| S11 | Reseau | aucun gameplay live/endpoint requis | dependance live ou claim officiel |

## Appareil absent

L'absence d'appareil n'interdit pas la production de l'APK apres les gates. Elle interdit seulement de fermer la preuve physique.

Valeurs obligatoires:

```text
DEVICE_INSTALL = NOT_TESTED
PHONE_PORTRAIT_PHYSICAL = PENDING
TABLET_LANDSCAPE_PHYSICAL = PENDING
PHYSICAL_DEVICE_PROOF = PENDING
```

Les controles package, BuildReport et Editor Play Mode peuvent etre PASS sans devenir une preuve appareil.

## Verdict d'execution future

- `GO_BUILD`: tous les gates pre-build sont GO.
- `NO_GO_BUILD`: au moins un gate pre-build n'est pas GO.
- `APK_BUILT_STATIC_PASS_DEVICE_PENDING`: package valide, aucun appareil.
- `APK_DEVICE_SMOKE_PASS_CANDIDATE`: artefacts physiques complets, encore soumis a QA.
- `APK_BLOCKED`: build, package ou smoke critique en echec.
