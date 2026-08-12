# LivingHive — retour vivant de la vitalité du couvain

Date : 21 juillet 2026

## Résultat produit

La nurserie rend maintenant la nutrition et la stabilité du couvain directement
sur la ruche. Sept cellules larvaires respirent avec une intensité dérivée de la
valeur la plus faible, une fiche compacte classe l'état en `Soin requis`,
`À surveiller`, `Stable` ou `Florissant`, et un soin actif ajoute son temps restant
ainsi qu'un flux discret vers la chambre. La couche apparaît pendant le chapitre 2,
quand la nurserie est sélectionnée ou lorsqu'une opération la cible; elle ne laisse
aucun pictogramme permanent sur l'illustration.

Le mode mouvement réduit remplace toute pulsation et tout déplacement par une
composition fixe. La fiche est bornée hors du HUD et des rails sur téléphone et en
paysage. Elle ne reçoit aucun clic et ne change ni ressources, ni minuterie, ni
progression.

## Frontière mobile et serveur

- L'appareil rend le dernier instantané confirmé et peut le conserver dans un
  cache borné par joueur.
- Le cache ne devient jamais une autorité et n'accepte aucune mutation hors ligne.
- Le serveur possède nutrition, stabilité, révision, date UTC et opération active.
- La preview actuelle reste explicitement `local_preview_non_official` tant que le
  shell d'authentification mobile et l'adaptateur HTTP ne sont pas raccordés.
- Le serveur fournit maintenant une lecture authentifiée
  `GET /game/v1/hives/{hiveId}/brood/vitality`, encore fermée par
  `BroodVitality:Enabled=false` par défaut et en Production.
- Aucun endpoint de soin n'est ouvert à ce jalon; le mobile ne peut donc pas
  transformer son cache en mutation autoritaire.

## Validation serveur

- Modèle persistant v6, migration v5 vers v6 et préservation exacte de l'état.
- Repository/migration : 20/20, incluant valeurs absentes, bornes, révision,
  identité, types autorisés et horodatages UTC.
- HTTP : 2/2, incluant drapeau fermé sans lecture repository, session, identifiant,
  état non initialisé entièrement nullable, état exact et isolation entre deux
  joueurs.
- Suite serveur contemporaine : 255 réussis, 7 tests SQL ignorés, 262 total.
- Build Release : 0 erreur; deux avertissements `Microsoft.Data.SqlClient`
  préexistants. Exécution avec `DOTNET_ROLL_FORWARD=Major`, car le runtime .NET 8
  x64 natif n'est pas installé dans la VM après son redémarrage.
- Le candidat local courant précède le modèle v6 et reste non promouvable;
  aucun candidat, transfert, déploiement ou activation n'a été effectué.

## Validation Unity

- Unity : `6000.5.3f1`.
- Compilation globale : 0 `error CS`.
- Suite LivingHive complète : réussite dans
  `Artifacts/BroodVitalityFinalF8.log`.
- Catalogues : 484 clés `fr-CA`, 484 clés `en-US`, aucun doublon et sept nouvelles
  clés de vitalité dans chaque langue.
- Campagne visuelle : 20 PNG sur 20, manifeste écrit, 0 échec de capture et
  dimensions strictes dans `Artifacts/GuidedBroodIncubation`.

Preuves dédiées inspectées à résolution native :

- `Chapter2_NurseryVitalityCare_390x844.png` — 390 × 844, 373 203 octets,
  SHA-256 `c7e09252d93d74b928d94bcc5dbb12e702b3ff6b0855abc159454ff30e44bd5d`.
- `Chapter2_NurseryVitalityCare_1600x900.png` — 1600 × 900, 2 031 746 octets,
  SHA-256 `4dec38856bcd42ac1c49bd177f6e9d80b9695876f715d51edec689aaf2a3c763`.

Le scénario visuel déterministe emploie nutrition 73, stabilité 79 et une lecture
précise figée à 42 % uniquement dans le harnais. Ces valeurs ne prétendent pas être
un état de production. Les deux images montrent `Stable`, l'opération active et
11 secondes restantes, sans texte coupé ni collision avec les commandes tactiles.

## Fondations préservées

- Aucun changement à l'image de base LivingHive.
- Aucun changement au terrain, à ses images ou à la carte 50 × 50.
- La scène canonique reste à 7 776 octets, horodatée
  `2026-07-17 17:11:05`.
- Aucun processus Unity ou `dotnet` ne reste actif après validation.

## Fichiers de la tranche client

- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxGuidedBroodIncubationCapture.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

## Portes restantes

- Raccorder une session mobile de production et un cache protégé par joueur.
- Ajouter les mutations de soin seulement lorsque leurs coûts, minuteries et
  idempotence sont définis côté serveur.
- Garder `BroodVitality:Enabled=false` en Production jusqu'à ces raccordements.
- Valider le runtime .NET 8 x64 natif, SQL et le staging mobile avant toute
  promotion de candidat.
