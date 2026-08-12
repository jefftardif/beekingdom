# LivingHive — localisation de la première session du chapitre 1

Date : 22 juillet 2026

Statut : réalisé et ratifié sous Unity 6000.5.3f1.

## Résultat produit

La première session du chapitre 1 ne dépend plus de libellés français codés en dur dans ses panneaux principaux. Le cadre commun du tutoriel, la collecte initiale, l'affectation de la relève, le second lot manuel, l'allocation du miel, les deux tournées du circuit, leur entretien, les contrôles de mise en service, la charte, le ravitaillement du couvain et leurs barres de progression utilisent maintenant les catalogues `fr-CA` et `en-US`.

Les choix, quantités, durées, coûts, récompenses, conditions d'idempotence et 29 interactions actives du chapitre restent inchangés. La collecte demeure manuelle et aucune automatisation, minuterie artificielle ou option payante n'a été ajoutée.

## Portée technique

- 71 entrées bilingues ajoutées: 4 pour le cadre partagé et 67 pour les panneaux et commandes du chapitre 1.
- Catalogues portés de 509 à 580 entrées uniques chacun.
- Parité stricte: 580 `fr-CA`, 580 `en-US`, zéro doublon, zéro clé présente dans une seule langue.
- Les 134 références `tutorial.chapter_01.*` et `tutorial.shell.*` actuellement utilisées par le présentateur existent dans les deux catalogues.
- Test éditeur ajouté pour les clés représentatives, la bascule de langue et le formatage dynamique de `CHAPITRE/CHAPTER`, du miel/honey et des quantités.
- Harnais visuel étendu avec deux preuves supplémentaires `en-US`: 390x844 et 1600x900. Chaque capture impose explicitement sa langue; les 52 preuves françaises existantes restent en `fr-CA`.

## Frontière mobile et serveur

Cette tranche est entièrement locale à l'appareil: choix de langue, résolution des textes, composition et mise en page. Elle ne crée aucune donnée économique ni progression officielle. Le serveur reste propriétaire de l'éligibilité, des ressources, de la progression et des reçus idempotents déjà définis; aucune route, configuration ou donnée `Server/` n'a été modifiée.

Le chantier Communication n'a pas été touché.

## Validation acquise

- Conversion JSON des deux catalogues réussie.
- 580/580 entrées, zéro doublon, zéro asymétrie, zéro référence localisée manquante dans le périmètre du chapitre 1 et du cadre partagé.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`: 0 erreur, 217 avertissements historiques du projet.
- F8 global et suite LivingHive: sortie 0, marqueur de réussite, zéro `error CS` et zéro `Compilation failed` dans `Artifacts/Chapter1LocalizationF8.log`.
- Campagne visuelle: sortie 0 et marqueur de réussite dans `Artifacts/Chapter1LocalizationCapture.log`.
- 54 PNG et 54 entrées de manifeste: 27 portraits 390x844, 27 paysages 1600x900, zéro dimension incorrecte.
- Preuve anglaise portrait: `Chapter1_ProductionChoice_en-US_390x844.png`, SHA-256 `C59E6726D8132D6E59787FB0684D7B85F31B9CE5083C2E4A2339B87746A30FB6`.
- Preuve anglaise paysage: `Chapter1_ProductionChoice_en-US_1600x900.png`, SHA-256 `9244AE78A24A4CF8D683F29051A6644560B29BF7170635AEBCF65B8C4FBD476E`.
- Inspection native: titre, explication, deux commandes, compteur d'acte et fermeture restent lisibles sans coupure ni collision dans les deux formats. `Chapter1_FoundationReward_390x844.png` confirme que la série française reste en français.
- Aucun éditeur Unity, `dotnet` ou `testhost` ne reste actif après les vérifications. Unity Hub et son client de licence restent ouverts dans la session utilisateur.

## Incident de licence et résolution

Deux tentatives lancées à l'intérieur du bac à sable ont été arrêtées avant compilation: le canal `LicenseClient-tardi` refusait la connexion et bouclait sur `com.unity.editor.headless not found`. Elles n'ont jamais été comptées comme preuves. La validation a ensuite été relancée hors du bac à sable, dans la même session utilisateur que Unity Hub; F8 puis les 54 captures ont terminé avec sortie 0.

## Fondations protégées

La scène canonique 50x50, ses tuiles et importeurs, le manifeste terrain, l'image de base LivingHive et les modules Communication n'ont pas été modifiés.

## Synchronisation

La tentative finale du 22 juillet à 04:30 UTC a été lancée par le script officiel, mais le bac à sable a refusé la lecture de `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun remappage, élargissement de droits ou accès direct à `Z:` n'a été utilisé. Les changements restent dans la copie locale C: et devront être synchronisés depuis la session utilisateur.
