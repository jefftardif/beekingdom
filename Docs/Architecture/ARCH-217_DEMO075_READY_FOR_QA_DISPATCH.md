# ARCH-217 - DEMO-075 Ready For QA Dispatch

Date: 2026-07-12

## Decision

Architecte valide le rapport DEMO-075 comme pret pour QA-A.

Statut Demo-A:

- Rapport: `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_Report.md`
- Verdict: `READY_FOR_QA_075 = YES`

## Ce qui est accepte pour QA

QA-A doit valider la readiness locale/demo de la ruche jouable BEE-921 a BEE-940:

- ressources qui augmentent;
- collecte locale;
- amelioration batiment avec pending, completion, cout reserve et cout depense une seule fois;
- entrainement troupes avec queue, arrival et compteur armee locale;
- inspection armee locale;
- refus avec cause et recovery;
- boutons non muets;
- menus permanents fixes;
- source action non trompeuse;
- preservation QA-074 BEE-905/BEE-910;
- non-claims serveur officiel/live;
- aucune carte monde;
- aucun BEE-881.

## Reserve obligatoire a maintenir

La preuve physique device n'est pas fermee.

QA-A ne doit pas transformer cette reserve en PASS complet si les artefacts reels suivants manquent:

- APK installe/lance sur appareil;
- preuve telephone portrait physique;
- preuve tablette paysage physique;
- captures ou video de device reel;
- verification tactile reelle des boutons et menus.

Verdict attendu possible: `PASS_WITH_RESERVES` si la boucle locale/demo est valide et que seule la preuve physique reste pending.

## Artefacts DEMO-075

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_Report.md`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_QAArtifactManifest.json`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_APKDeviceManifest.json`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_AppReadinessChecklist.json`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_BEE925_930_DailyHiveLoop_Manifest.md`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_BEE925_930_DailyHiveLoop_MachineReadableSummary.json`

## Chaine suivante

Si QA-A retourne:

- `PASS`: Architecte peut debloquer la suite Planner.
- `PASS_WITH_RESERVES`: Architecte decide si la reserve device devient une tache ciblee ou si la suite locale peut avancer.
- `BLOCKED`: Architecte relance uniquement le role responsable du blocage.
