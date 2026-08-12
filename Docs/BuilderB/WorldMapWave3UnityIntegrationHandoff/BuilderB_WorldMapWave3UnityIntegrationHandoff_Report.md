# Builder-B - World Map Wave 3 Unity Integration Handoff

## Resume

Le handoff d'integration Unity du bundle 5x5 a gouttieres est prepare hors Unity. Le rapport principal demande se trouve hors de la racine inscriptible de cette session; le present fichier est donc le fallback autorise.

Le lot reste conditionne aux gates Builder-C et QA. Aucun PNG n'a ete copie sous `Assets`, aucune scene, aucun `.meta`, aucun script Unity, aucun `ProjectSettings` et aucun asset produit n'a ete modifie par Builder-B.

## Sources auditees

- rapport d'ingest reel Builder-B;
- bundle `artifacts/WorldMapWave3_RuntimeBundle_staging/run1`;
- `WorldMapMmoFullscreenFoundationBootstrap.cs` en lecture seule;
- `WorldMapWave4ManifestContentProvider.cs` en lecture seule;
- `WorldMapMmoFullscreenFoundationSceneBuilder.cs` en lecture seule.

Baseline SHA-256 des trois sources Unity:

| Fichier | SHA-256 |
|---|---|
| `WorldMapMmoFullscreenFoundationBootstrap.cs` | `3c823dac84bfb62f1049d36257efaac7005619e83c27d242d45442ec39319171` |
| `WorldMapWave4ManifestContentProvider.cs` | `ddc0e12f398f8a7ffc2582e15ee99c99282f5da9f5d9bd1f551a4c3b8ac5732b` |
| `WorldMapMmoFullscreenFoundationSceneBuilder.cs` | `8f352317cfebac8fec214624f7ac84a5d49d3c502fb153b72085d94b078f42dc` |

Ces hashes sont inclus dans le manifeste et reverifies par `verify_handoff.py`.

## Livrables produits

- manifeste machine-readable 25/25 avec hashes, crop, UV, voisins, clamps et coordonnees;
- inventaire CSV source vers destination future;
- procedure d'import, mapping, chargement mobile, anti-coutures et rollback;
- matrice de self-checks Builder-A;
- generateur deterministe et validateur local;
- resultat machine-readable de validation du handoff.

Tous les fichiers sont sous:

`C:\projets\beekingdomgame-master\Docs\BuilderB\WorldMapWave3UnityIntegrationHandoff\`

Validation locale du handoff:

- `110` controles machine-readable PASS;
- manifeste genere deux fois avec le meme SHA-256: `bde8c07b6430afe964e136256acfcc1f25854331476354bbb9eda9104e391911`;
- inventaire CSV genere deux fois avec le meme SHA-256: `cdd24b290c6331e9e4f2e068c0160a589ab01ac687cc0d90049377567434ab37`;
- 25/25 PNG presents, en `516x516 RGB`, hashes conformes;
- UV exactes sur 25/25;
- trois sources Unity relues avec leurs hashes baseline inchanges.

## Architecture d'integration recommandee

- conserver `Texture2D` + IMGUI, chemin actuellement observe;
- ajouter un provider Wave 3 distinct derriere un flag reversible;
- ancrer `R0C0..R4C4` aux chunks `(30,30)..(34,34)`;
- mapping identite: colonne vers X, ligne vers Y bas;
- supprimer le modulo pour ce lot borne;
- dessiner 512 unites monde avec UV `2/516..514/516`;
- utiliser les gouttieres seulement pour le filtrage bilineaire;
- conserver overlays/vols en coordonnees monde et HUD fixe;
- ne jamais laisser le decor definir une route de vol.

## Import et anti-coutures

Reglages recommandes: Texture Type Default, sRGB On, Alpha None, Read/Write Off, NPOT None, Clamp, Bilinear, mipmaps Off, max size 1024. Android principal: ASTC 6x6 RGB, qualite Best, Crunch Off; fallback ETC2 RGB4 si le profil appareils l'exige.

L'integration doit d'abord etre verifiee sans compression dans l'Editor. Une preuve d'identite d'orientation et l'inspection des 40 frontieres sont obligatoires avant le profil Android.

## Memoire mobile

Pour 25 tuiles runtime `516x516`:

- PNG disque: `11.2539 MiB`;
- RGB24 brut: `19.0441 MiB`;
- RGBA32 brut de reference: `25.3922 MiB`;
- ETC2 RGB4: `3.1740 MiB`;
- ASTC 6x6 RGB: `2.8214 MiB`.

Regime stable: 25 tuiles. Transition d'une frontiere: plafond temporaire 30, puis liberation des cinq sortantes. Le chargement complet du monde logique 64x64 est interdit.

## Rollback

Le dossier Step4C existant reste intact. En cas d'echec de hash, orientation, couture, cache, HUD ou vol, Builder-A doit desactiver le mode Wave 3, liberer son cache et reactiver le provider Step4C. Aucun rollback destructif ou reecriture de PNG n'est requis.

## Limites et non-claims

- handoff uniquement;
- integration Unity non effectuee;
- validation Unity/Android/device non effectuee;
- monde immense/live non livre;
- serveur officiel absent;
- persistance officielle absente;
- aucune logique de route terrestre;
- execution Builder-A interdite avant les gates Builder-C/QA.

## Verdicts

WORLD_MAP_WAVE3_UNITY_INTEGRATION_HANDOFF = PASS

RUNTIME_TILE_MANIFEST_READY = YES

MOBILE_MEMORY_BUDGET_DOCUMENTED = YES

NO_UNITY_PRODUCT_FILES_MODIFIED = YES

READY_FOR_BUILDER_A_AFTER_GATES = YES
