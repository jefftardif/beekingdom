# LivingHive — lecture active de la vitalité du couvain

Date : 21 juillet 2026

## Résultat produit

Après chaque observation d'une cohorte du chapitre 2, le joueur doit maintenant
comparer la nutrition et la stabilité avant d'accéder aux trois contrôles
d'incubation. La mesure la plus basse définit la priorité; une égalité donne la
priorité à la nutrition. Le panneau montre les deux valeurs et propose deux cibles
tactiles directes, en portrait comme en paysage.

Une réponse incorrecte ne consomme aucune ressource, n'ajoute aucun temps et ne
fait pas progresser l'étape. Un retour explique la règle et permet de réessayer.
Une réponse correcte déverrouille température, humidité et mouvement, puis le
choix du soin. La recommandation associe la gelée royale à une nutrition
prioritaire et la rotation hygiénique à une stabilité prioritaire, sans retirer
l'autre soin. Le joueur conserve donc une décision stratégique réelle après avoir
compris le diagnostic.

Cette extension ajoute une décision active par cohorte. Le chapitre 2 conserve
15 objectifs et 180 à 224 secondes de tâches chronométrées, mais passe de 28 à
30 interactions actives. Le chapitre 4 devient le seul plancher de l'Acte I à
28 interactions actives.

## Frontière mobile et serveur

- L'appareil rend les valeurs reconnues, les boutons, le retour pédagogique et la
  recommandation. Dans la preview, il garde seulement l'état de tutoriel local non
  officiel nécessaire au rendu.
- Le choix pédagogique ne modifie ni nutrition, ni stabilité, ni ressources, ni
  minuterie. Il n'existe aucune mutation hors ligne à rejouer.
- En production, nutrition, stabilité, révision, date UTC et opération active
  restent la propriété du serveur. Le mobile dérive seulement la recommandation
  visible à partir du dernier instantané reconnu.
- La progression tutorielle officielle et sa révision devront être persistées par
  le serveur; les lignes de preuve déclarent explicitement
  `tutorial_progress_revision` comme autorité future.
- Aucun nouvel endpoint économique ou de soin n'a été ouvert pour cette tranche.
  La lecture v6 reste derrière `BroodVitality:Enabled=false` en Production tant que
  le shell mobile et l'adaptateur HTTP ne sont pas raccordés.
- L'Intégrateur a confirmé cette frontière sans modifier le serveur dans
  `Docs/ProductionIntegration/Chapter2_BroodVitality_InterpretationContractAudit_2026-07-21.md`.
  Le futur contrat devra vérifier identité, appartenance, étape précédente,
  `expectedRevision`, `idempotencyKey`, ordre monotone et horodatage UTC serveur.

## Validation comportementale

La suite LivingHive couvre notamment :

- priorité nutrition avec une première erreur stabilité, puis correction sans
  coût, sans délai et sans accès prématuré aux contrôles;
- priorité stabilité avec recommandation de rotation hygiénique;
- deux cohortes, compteur total de réussites et compteur d'erreurs;
- conservation des deux soins après la recommandation;
- cibles de choix acceptant les entrées en portrait et en paysage;
- helpers de preuve adaptatifs lorsque des choix stratégiques antérieurs ont déjà
  changé l'équilibre nutrition/stabilité;
- métrique de rythme mise à jour à 30 interactions actives.

## Validation Unity

- Unity : `6000.5.3f1`.
- Compilation globale : 0 `error CS`, 0 `Compilation failed`.
- Suite LivingHive complète : marqueur
  `LivingHive manual collection checks passed.` dans
  `Artifacts/BroodVitalityInterpretationFinalF8.log`.
- Deux premières passes sont conservées comme historique non ratifié : la première
  a détecté l'attente de rythme encore fixée à 28; la seconde a détecté un helper
  supposant à tort que nutrition était toujours prioritaire. Les deux causes ont
  été corrigées avant la passe finale.
- La tentative `dotnet build` hors Unity n'est pas une preuve : elle s'est arrêtée
  avant compilation, faute d'accès au `NuGet.Config` du profil VM.
- Catalogues : 500 clés `fr-CA`, 500 clés `en-US`, aucun doublon; 16 clés nouvelles
  et alignées pour la lecture et la recommandation.
- Campagne visuelle : 22 PNG sur 22, manifeste de 22 entrées, aucune dimension
  inattendue et aucune exception du harnais dans
  `Artifacts/GuidedBroodIncubation`.

Preuves dédiées inspectées à résolution native :

- `Chapter2_VitalityAssessment_390x844.png` — 390 × 844, 377 246 octets,
  SHA-256 `b68eea56abbcef04059f1304b7d27e456dee1f97ad67d46e831a503b74f587d8`.
- `Chapter2_VitalityAssessment_1600x900.png` — 1600 × 900, 2 030 132 octets,
  SHA-256 `df12c0cc3b1446d20e343b48b21f1a110aa02425de4accc6cd8a77a0f5ebd26f`.

Les deux rendus montrent nutrition 73 %, stabilité 81 %, le titre, l'explication,
les deux choix et la fermeture sans texte coupé ni collision. Ces valeurs sont une
graine déterministe du harnais et ne prétendent pas être un état de production.

## Fondations préservées

- Scène canonique : 7 776 octets, horodatage `2026-07-17 17:11:05`, SHA-256
  `927fa2a719033270e8ad4bf66c719fad7a1414a08f9705d400d40a5de122b1b3`.
- Image de base LivingHive : 7 489 785 octets, horodatage
  `2026-07-13 11:10:51`, SHA-256
  `3c0e3b97e8e7ad76fc2c46a9342c4f9d7b03717591356251945c8f3f62b467f6`.
- Aucun changement au terrain 50 × 50 ni à ses images.
- Après validation : Unity 0, `dotnet` 0, `testhost` 0, `bee_backend` 0.

## Fichiers de la tranche

- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxGuidedBroodIncubationCapture.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxGuidedOpeningInstallationCapture.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

## Portes restantes

- Raccorder la session mobile, l'adaptateur HTTP et le cache protégé de vitalité.
- Persister la progression tutorielle du chapitre 2 avec révision et idempotence
  côté serveur avant de la considérer officielle.
- Garder toute mutation de soin fermée jusqu'à définition serveur des coûts,
  minuteries, préconditions et reçus idempotents.
- Valider .NET 8 x64 natif, SQL et staging mobile avant toute promotion serveur.
