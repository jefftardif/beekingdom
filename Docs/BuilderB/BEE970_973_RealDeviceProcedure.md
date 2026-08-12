# BEE-970 a BEE-973 - Procedure appareil reel DEMO-077

Date : 2026-07-12  
Auteur : Builder-B  
Statut : support protocole uniquement  
Scope : install/lancement APK reel, telephone portrait, tablette paysage, matrice gestes physiques

## Position Builder-B

Ce document prepare la procedure appareil reel pour DEMO-077 / QA-077. Il ne constitue pas une preuve physique fermee.

`PHYSICAL_DEVICE_PROOF = PENDING` reste obligatoire tant que Demo-A ou QA-A ne joint pas de vrais artefacts appareil : capture, video, logs d'installation/lancement, modele appareil, OS, horodatage et outcome.

Builder-B n'a pas modifie le runtime, les scenes, les assets, le serveur, l'APK, la carte monde ou BEE-881.

## Trace APK a utiliser

APK courant a verifier avant installation :

- Path : `C:/projets/beekingdomgame-master/Builds/Android/BeeKingdom.apk`
- Size : `42953385`
- SHA256 : `5A4867C35C95F6621C0EA72B6A61BD9E42D87E8218CCAA7A61FA738B29889554`
- Last write local : `2026-07-12T10:26:31-04:00`

Si l'une de ces valeurs change, Demo-A doit ouvrir une nouvelle trace APK au lieu de reutiliser ce protocole tel quel.

## BEE-970 - Procedure install/lancement APK reel

### Preconditions

- Appareil Android reel disponible.
- Batterie suffisante ou alimentation branchee.
- Orientation auto active si les tests phone/tablet en dependent.
- APK copie depuis le chemin trace ci-dessus.
- Ancienne version de l'application desinstallee ou statut d'upgrade explicitement note.
- Reseau note comme actif/inactif, sans claim serveur officiel.

### Etapes

1. Noter appareil, modele, version Android, resolution si disponible, date/heure locale.
2. Recalculer ou confirmer SHA256 de l'APK avant transfert.
3. Installer l'APK sur l'appareil reel.
4. Capturer la preuve d'installation : photo/capture/video ou sortie outil, avec horodatage si possible.
5. Lancer Bee Kingdom depuis l'icone ou l'ecran d'installation.
6. Capturer le premier ecran charge.
7. Confirmer que la ruche locale/demo est accessible.
8. Noter tout crash, ecran noir, blocage permission, lenteur majeure ou souci tactile.

### Artefacts requis pour fermer BEE-970

- `install_proof` : capture/video/log montrant installation reussie.
- `launch_proof` : capture/video montrant le lancement et le premier ecran.
- `device_metadata` : modele, OS, orientation testee, date/heure.
- `apk_metadata` : path, size, SHA256, last write.
- `outcome` : PASS, PASS_WITH_RESERVES ou BLOCKED.

Sans ces artefacts reels, BEE-970 reste en support protocole et la preuve physique reste pending.

## BEE-971 - Procedure telephone portrait physique

### But

Prouver sur telephone reel en portrait que les actions essentielles de la ruche sont visibles et utilisables, sans fermer de claim officiel/live.

### Etapes minimales

1. Verrouiller ou placer l'appareil en portrait.
2. Lancer l'application depuis l'APK trace.
3. Capturer l'ecran ruche initial : HUD, ressources, panneaux essentiels.
4. Tester une collecte ou action accepted visible.
5. Tester un choix d'amelioration : cout, duree, bouton, feedback ou pending.
6. Tester un entrainement : type troupe, cout, queue ou feedback.
7. Tester un refus/disabled : cause lisible, aucun cout, aucun timer.
8. Tester recovery court : prochaine action locale visible.
9. Capturer toute zone ou texte coupe si present.

### Criteres phone portrait

- Les menus et panneaux essentiels restent visibles.
- Les textes critiques ne sont pas coupes.
- Les boutons essentiels sont tapables.
- Les taps ne sont pas muets.
- Aucun label interdit n'apparait : `Live`, `Serveur officiel`, `Endpoint actif`, `Sauvegarde officielle`, `Economie officielle`, `Armee persistante`, `BEE-881`.
- Aucune carte monde, exploration, alliance, guerre ou map MMO n'est declenchee.

### Artefacts requis pour fermer BEE-971

- Capture ou video telephone portrait reelle.
- Metadata appareil.
- Liste des actions executees.
- Resultat par action : PASS, PASS_WITH_RESERVES ou BLOCKED.

Sans artefacts reels, BEE-971 reste pending.

## BEE-972 - Procedure tablette paysage physique

### But

Prouver sur tablette reelle en paysage que la ruche occupe l'espace utile, que le HUD reste fixe et que les actions principales restent lisibles.

### Etapes minimales

1. Placer l'appareil en paysage.
2. Lancer l'application depuis l'APK trace.
3. Capturer l'ecran ruche initial.
4. Verifier que la ruche est dominante visuellement.
5. Verifier que HUD, menus permanents et panneaux ne zooment pas avec la ruche.
6. Tester selection batiment, amelioration, entrainement et inspection armee locale.
7. Tester refus/disabled avec texte lisible.
8. Tester pan et pinch selon la matrice BEE-973.
9. Capturer toute instabilite : decalage hotspots, halo mal aligne, panneau masque, texte coupe.

### Criteres tablette paysage

- Ruche dominante dans l'espace utile.
- HUD et menus fixes pendant pan/zoom.
- Panneau secondaire lisible.
- Boutons et feedback visibles.
- Pas de zoom UI parasite.
- Pas de carte monde, BEE-881 ou claim officiel/live.

### Artefacts requis pour fermer BEE-972

- Capture ou video tablette paysage reelle.
- Metadata appareil.
- Resultat lisibilite ruche/HUD/panneaux.
- Resultat gestes physiques si combines avec BEE-973.

Sans artefacts reels, BEE-972 reste pending.

## BEE-973 - Matrice preuve gestes physiques

| ID | Geste | Appareil | Preuve attendue | PASS | BLOCKED |
| --- | --- | --- | --- | --- | --- |
| G01 | Tap bouton accepted | Phone + tablette | Video/capture avant-apres | Feedback visible, action appliquee une fois | Bouton muet, double action |
| G02 | Tap bouton disabled | Phone + tablette | Video/capture raison disabled | Raison lisible, aucun cout | Cout applique, raison absente |
| G03 | Rapid tap ameliorer | Phone + tablette | Video ou log + capture etat | Cout une fois, timer une fois | Double debit, double timer |
| G04 | Rapid tap entrainer | Phone + tablette | Video ou log + capture queue | Queue une fois, cout une fois | Double queue, double debit |
| G05 | Pan un doigt ruche | Phone + tablette | Video geste + resultat | Ruche pan seulement | Zoom declenche, UI bouge |
| G06 | Pinch deux doigts ruche | Phone + tablette | Video geste + resultat | Zoom doux ruche seulement | Pan erratique, saccades fortes |
| G07 | Menus fixes pendant zoom | Tablette prioritaire | Video pinch avec HUD visible | HUD/panneaux ne zooment pas | HUD zoome ou sort de l'ecran |
| G08 | Tap UI pendant pan/zoom | Phone + tablette | Video interaction UI | UI bloque pan/zoom quand touchee | La ruche bouge sous un bouton UI |
| G09 | Scroll panneau | Phone + tablette | Video panneau | Panneau scroll, ruche stable | Pan ruche parasite |
| G10 | Halo/hotspot apres pan/zoom | Tablette prioritaire | Video selection apres geste | Halo aligne zone selectionnee | Halo decale ou hotspot faux |

## Distinction des statuts de preuve

| Statut | Signification | Peut fermer physical proof ? |
| --- | --- | --- |
| `support_only` | Procedure ou checklist Builder-B | Non |
| `local_demo` | Capture locale/editor/demo sans appareil reel | Non |
| `physical_device_pending` | Procedure prete, artefacts reels absents | Non |
| `physical_device_proof` | Capture/video/log sur appareil reel, avec metadata | Oui, si QA valide |
| `official_live` | Serveur officiel/live/save/economie/armee persistante | Interdit dans ce scope |

## Lignes manifest DEMO-077 recommandees

- `REAL_DEVICE_INSTALL_PROCEDURE = PRESENT`
- `REAL_DEVICE_INSTALL_PROOF = PRESENT|PENDING`
- `REAL_DEVICE_LAUNCH_PROOF = PRESENT|PENDING`
- `PHONE_PORTRAIT_PHYSICAL_PROOF = PRESENT|PENDING`
- `TABLET_LANDSCAPE_PHYSICAL_PROOF = PRESENT|PENDING`
- `PHYSICAL_GESTURE_MATRIX = PRESENT`
- `PHYSICAL_DEVICE_PROOF = PENDING` tant qu'un des artefacts reels manque
- `WORLD_MAP_RUNTIME = FALSE`
- `BEE_881_CREATED_OR_UNLOCKED = FALSE`
- `OFFICIAL_SERVER_LIVE_CLAIM = FALSE`

## Gate QA-077

PASS possible seulement si les artefacts reels existent, sont lisibles, correspondent a l'APK trace et ne contiennent aucun scope leak.

PASS_WITH_RESERVES si le protocole est complet mais qu'une partie des preuves physiques reste absente ou incomplete.

BLOCKED si :

- physical proof est declaree fermee sans artefacts reels ;
- APK installe ne correspond pas a la trace attendue ;
- crash ou blocage lancement empeche tout test ;
- carte monde, BEE-881, exploration, alliance, guerre ou map MMO apparait ;
- claim serveur officiel/live/save/economie/armee persistante apparait ;
- gestes critiques cassent HUD, panneaux ou alignement hotspots.

## Verdict Builder-B

READY_FOR_DEMO_077_REAL_DEVICE_PROCEDURE_SUPPORT = YES
