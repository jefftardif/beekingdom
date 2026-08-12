# Spawn Inspector P7 - UI Counter-Review

Date locale: 2026-07-15
Role: reviewer UI P7 Bee Kingdom
Portee: contre-revue documentaire du Spawn Inspector. Aucun controle Unity, log, image, APK, serveur ou ancien thread Codex n'a ete consulte.

## Sources autorisees

- `Docs/UIRelay/WorldMapSpawnInspector_UI_Spec.md`
- `Docs/WorldMapRuntimeEntitiesWave1/SpawnInspectorIntegration_Report.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/SpawnInspectorProof/SpawnInspectorProofReceipt.md`

## Regle de lecture des preuves

- `DECLARE`: le rapport d'integration et/ou le recu portent explicitement le resultat, sans preuve visuelle independante dans le perimetre de cette revue.
- `PARTIEL`: une partie du contrat est decrite ou mesuree, mais il manque des etats, champs ou vues requis.
- `NON_PROUVE`: l'exigence existe dans la spec, mais les deux documents d'execution ne fournissent pas de preuve exploitable.

Un libelle `PASS` dans le rapport ou le recu est conserve comme declaration de P7. Il n'est pas transforme en preuve visuelle par cette contre-revue.

## Verdict

Le dossier est coherent sur le socle de securite et d'execution: compilation Unity declaree PASS dans le rapport, Play Mode PASS, overlay diagnostic OFF au demarrage, generation locale, `server=false`, `official_gain=false` et regression P1-P6 PASS. Les hashes distincts et les compteurs 25/2/11/7 etayent aussi le test fonctionnel deterministe annonce.

La conformite UI complete de la spec P7 n'est toutefois pas prouvee. Le `Spawn inspector UI: PASS` est agrege, sans correspondance entre chaque critere et un etat visible. Aucune preuve autorisee ne montre les mises en page tablette/telephone, les transitions d'interaction, les etats de budget, les quatre exclusions visibles, le 50x50 logique, les dimensions tactiles ou l'accessibilite.

Decision: la contre-revue P7 est terminee et ne detecte pas de contradiction de securite bloquant l'entree en P8. P8 doit produire les preuves manquantes avant toute acceptation visuelle finale.

## Matrice de conformite

| ID | Exigence P7 | Element disponible | Verdict | Preuve P8 requise |
|---|---|---|---|---|
| UI-01 | Panneau `SPAWN INSPECTEUR`, badge local, overlay OFF/replie par defaut | Panneau et badge annonces; OFF confirme par rapport et recu | DECLARE | Premier frame apres reset sur les deux formats, puis panneau ouvert avec badge visible |
| UI-02 | Seed editable, version, changement sans regeneration automatique, erreur inline | Seed editable, `spawn_v1`, hashes A/B et determinisme annonces | PARTIEL | Sequence avant/apres edition, distribution inchangee avant action, erreur de seed invalide |
| UI-03 | `Regenerer apercu local`, `Jamais officiel`, timestamp, seed et monde cible | Bouton et mention non officielle annonces | PARTIEL | Etat avant action puis resultat 25x25 avec timestamp, seed appliquee et monde cible |
| UI-04 | Filtres famille/tier sans ecraser `LECTURE CARTE`; `Vue diag filtree` | Familles et tiers couverts par la generation selon le rapport | NON_PROUVE | Controles visibles, effet overlay, conservation des filtres carte et libelle de divergence |
| UI-05 | Entite cachee non cliquable; selection existante conservee avec `hors filtre` | Aucun detail d'interaction dans les preuves | NON_PROUVE | Selection avant filtrage, puis badge et opacite hors filtre; clic bloque sur entite cachee |
| UI-06 | Detail complet ruche, ressource, menace et exclusion | ID, famille, type/tier, chunk et coordonnee normalisee annonces | PARTIEL | Une fiche par famille avec coordonnee monde, seed source, etat spawn et champs specialises requis |
| UI-07 | Bandeau chunks/hives/resources/bestiary/cache et seuils OK, >=80%, depassement | 25/2/11/7 et `Density budgets: PASS` | PARTIEL | Les cinq compteurs, symboles, etats normal/ambre/rouge et texte `Budget depasse local` |
| UI-08 | Exclusions BearDen/eau/falaise/evenement par contours/hachures | Quatre familles annoncees; hits observes 0/0/0/25 | PARTIEL | Un cas visible et selectionnable pour chacune des quatre familles |
| UI-09 | Symboles, motifs de tier, selection, focus, contraste et monochrome | Aucune mesure ou vue fournie | NON_PROUVE | Etats couleur et monochrome, ratios de contraste et motifs non dependants de la couleur |
| UI-10 | Clavier, manette et tactile; cibles 44x44 et hitboxes marqueurs >=32x32 | Aucun parcours ou mesure fourni | NON_PROUVE | Parcours de focus, activation/repli, equivalence hover/focus et mesures des cibles |
| UI-11 | Tablette paysage: panneau droit, 320 px cible, hauteur <=38%, carte >=60% largeur | Aucune vue ni mesure fournie | NON_PROUVE | Capture plein viewport et releve geometrique selon le protocole P8 |
| UI-12 | Telephone portrait: tiroir <=36%, autres panneaux replies, carte >=55% hauteur | Aucune vue ni mesure fournie | NON_PROUVE | Capture plein viewport fermee/ouverte et releve geometrique selon le protocole P8 |
| UI-13 | Listes avec defilement interne, aucun modal/voile/collision | Aucun etat de debordement fourni | NON_PROUVE | Liste longue aux deux formats, carte encore manipulable et aucun chevauchement incoherent |
| UI-14 | Mode `50x50 logique`, `catalogue: 2500 coord.`, terrain non genere | Le rapport exclut le terrain 50x50 mais ne decrit pas l'etat UI logique | NON_PROUVE | Etat logique visible avec les deux mentions et sans art terrain 50x50 |
| UI-15 | Aucun controle de suppression, loot, gain officiel ou sauvegarde serveur | `server=false` et `official_gain=false` | PARTIEL | Inventaire visuel des actions disponibles dans le detail et le panneau |

## Ecarts et risques a lever

1. **Nommage du quatrieme compteur.** Le resultat observe emploie `threats`, tandis que le bandeau prescrit `bestiary`. P8 doit confirmer qu'il s'agit du meme budget et que le libelle UI attendu est present.
2. **Detail selection trop peu documente.** Le rapport ne cite que l'ID, la famille, le type/tier, le chunk et la coordonnee normalisee. Les coordonnees monde, la seed source, l'etat spawn et la plupart des champs specialises restent sans preuve.
3. **Couverture exclusions insuffisante.** Les hits BearDen/eau/falaise sont tous a zero. Le PASS agrege ne demontre ni leur rendu, ni leur selection, ni l'explication d'une absence de spawn.
4. **Budgets limites aux valeurs normales.** Les valeurs 25/2/11/7 ne montrent ni le seuil d'attention a 80%, ni un depassement, ni le compteur cache Wave5.
5. **Filtres non traces.** La couverture de familles et de tiers par le generateur ne prouve pas la presence des controles, leur isolation par rapport a `LECTURE CARTE` ou le comportement `hors filtre`.
6. **Responsive non trace.** Aucun viewport, aucune orientation, aucune dimension de panneau et aucun ratio de carte visible ne sont consignes.
7. **Accessibilite non tracee.** Aucun resultat ne couvre contraste, monochrome, motifs, ordre de focus, manette ou taille tactile.
8. **50x50 logique absent du recu.** L'absence de terrain 50x50 est conforme au perimetre, mais l'apercu logique et ses libelles ne sont pas attestes.

## Preuves minimales attendues en P8

- Deux series plein viewport: tablette paysage et telephone portrait.
- Etat initial apres reset, puis etat ouvert sans modal ni collision.
- Sequence seed modifiee -> aucune regeneration -> regeneration explicite.
- Seed invalide avec erreur inline et blocage limite a la regeneration.
- Familles et tiers filtres, divergence avec `LECTURE CARTE`, selection `hors filtre`.
- Une fiche complete pour ruche, ressource, menace et exclusion.
- Quatre exclusions reellement visibles, dont BearDen/eau/falaise avec hit non nul dans le scenario de preuve.
- Bandeau budget dans les trois etats: normal, attention et depassement.
- Mode 50x50 logique avec catalogue 2500 et terrain non genere.
- Mesures de panneau, carte visible, cibles tactiles, hitboxes et contrastes.
- Parcours clavier/manette et equivalence des informations hover/focus.
- Verification visuelle de l'absence d'actions officielles, persistantes, de loot ou de suppression.

## Interpretation des gates

`UI_P7_COUNTER_REVIEW=PASS` signifie que la contre-revue documentaire est complete et que les manques sont inventories. Il ne signifie pas que la conformite visuelle P7 est acquise.

`READY_FOR_P8_VISUAL_PROOF=YES` autorise la collecte P8 selon le protocole dedie. Toute preuve obligatoire absente doit etre notee `NOT_PROVEN`, jamais convertie en `PASS`.

UI_P7_COUNTER_REVIEW=PASS
READY_FOR_P8_VISUAL_PROOF=YES
