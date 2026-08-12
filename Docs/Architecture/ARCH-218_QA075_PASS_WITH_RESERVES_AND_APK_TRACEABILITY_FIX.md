# ARCH-218 - QA-075 Pass With Reserves And APK Traceability Fix

Date: 2026-07-12

## Decision

QA-075 est accepte en `PASS_WITH_RESERVES` pour la readiness locale/demo de la ruche jouable BEE-921 a BEE-940.

Rapport QA:

- `C:/projets/beekingdom/QA/QA_DEMO_075_BEE921_940_VALIDATION.md`
- Verdict: `QA_075_RESULT = PASS_WITH_RESERVES`

## Ce qui est valide

QA confirme que la boucle ruche locale/demo est suffisamment prouvee:

- ressources qui augmentent;
- collecte;
- amelioration batiment pending/completion/cout unique;
- entrainement queue/arrival;
- inspection armee locale;
- refus cause/recovery;
- boutons non muets;
- preservation BEE-905/BEE-910;
- non-claims serveur officiel/live;
- aucune carte monde;
- aucun BEE-881.

## Reserves restantes

Les reserves ne bloquent pas la suite locale/demo, mais elles bloquent toute declaration APK/device propre:

1. `PHYSICAL_DEVICE_PROOF = PENDING`
   - pas de preuve installation APK sur appareil;
   - pas de preuve lancement APK sur appareil;
   - pas de capture/video telephone portrait;
   - pas de capture/video tablette paysage.

2. APK traceability mismatch
   - APK courant: `C:/projets/beekingdomgame-master/Builds/Android/BeeKingdom.apk`
   - Last write time courant: `2026-07-12 10:26:31`
   - SHA256 courant QA: `5A4867C35C95F6621C0EA72B6A61BD9E42D87E8218CCAA7A61FA738B29889554`
   - Le manifeste DEMO-075 reference encore l'ancien APK du `2026-07-11`.

3. Pas de bundle capture/video player-facing DEMO-075.

## Action immediate

Builder-C doit corriger uniquement la tracabilite APK:

- recalculer taille/hash/date du APK courant;
- regenerer le manifeste APK/device DEMO-075 pour qu'il corresponde au fichier courant;
- ne pas changer gameplay/runtime;
- ne pas toucher la carte monde;
- ne pas creer/debloquer BEE-881;
- ne pas fermer la preuve physique device.

## Tache envoyee

Builder-C recoit une correction ciblee `BEE-921/924 APK traceability refresh`.

Livrable attendu:

- `C:/projets/beekingdom/prompts_codex/rapports/BuilderC_DEMO075_CurrentAPKTraceabilityFix_Report.md`
- manifeste APK/device rafraichi dans `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/`
- verdict `READY_FOR_DEMO_075_APK_TRACEABILITY_REFRESH = YES` ou `NO`

## Chaine suivante

Quand Builder-C termine:

- Demo-A doit relire le manifeste rafraichi et produire un addendum DEMO-075 si necessaire;
- QA-A doit ensuite valider uniquement la correction de tracabilite APK.

La preuve physique device restera pending jusqu'a reception de vraies captures/videos appareil.
