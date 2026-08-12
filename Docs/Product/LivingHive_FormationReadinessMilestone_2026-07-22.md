# LivingHive — préparation honnête d’escouade

> Statut historique : ce jalon de lecture est maintenant prolongé par
> `LivingHive_DoctrineRecruitmentMilestone_2026-07-22.md`. Le roster doctrinal
> séparé et la formation de Caserne remplacent la porte « non reconnue », sans
> convertir les effectifs historiques.

Date : 22 juillet 2026

## Résultat produit

Le panneau `Armée` possède maintenant une entrée tactile `Préparer`. Elle ouvre
un brouillon mobile qui relie l’enseignement du triangle de doctrine aux
effectifs réellement connus, sans transformer les anciens rôles en nouvelles
familles.

- `Gardiennes` est la seule correspondance locale prouvée et préremplit le
  brouillon avec le nombre réellement présent dans l’aperçu local.
- `Voltigeuses` et `Lanceuses` restent visibles mais verrouillées comme
  `Non reconnue / Not recorded`.
- Les `Soldats` et `Éclaireuses` historiques sont affichés dans une réserve
  `hors doctrine`.
- Aucun mapping `Soldats -> darters` ou `Éclaireuses -> wingrunners` n’existe.
- Choisir une menace affiche avantage, exposition ou neutralité sans calculer
  puissance, dégâts, pertes, victoire ou récompense.
- Le retour `Armée` efface la menace et le brouillon volatile; aucune commande
  officielle n’est proposée.

Le panneau normal d’Armée a aussi reçu une hauteur mobile cohérente : les trois
types historiques, l’action d’entraînement, `Préparer`, le motif de blocage et
la progression tiennent dans la surface 390x844. Toutes les commandes de la
préparation font au moins 44 px.

## Frontière appareil / serveur

### Appareil

- rendu, langue et geste tactile;
- lecture de l’aperçu local persistant déjà existant;
- correspondance locale stricte `Gardiennes -> guardians`;
- famille inspectée et menace dans un brouillon volatil en mémoire;
- aucun cache de formation, aucune outbox et aucune mutation de gameplay.

Le nombre affiché reste explicitement un aperçu local non officiel. Il ne peut
pas être présenté comme disponibilité de combat autoritaire.

### Serveur

L’Intégrateur a préparé
`GET /game/v1/hives/{hiveId}/combat/formation-readiness`. L’audit confirme que
`PlayerHiveState` ne persiste encore aucun roster classé par doctrine. La réponse
honnête est donc :

- `contractVersion=phase4-combat-formation-readiness-v1`;
- `doctrineCatalogVersion=phase4-combat-v1`;
- `availabilityStatus=not_recorded`;
- `families={}` — jamais trois faux zéros;
- rôles historiques `Soldats`, `Gardiennes`, `Eclaireuses` explicitement non
  classifiés.

`CombatFormationReadiness:Enabled=false` reste la valeur par défaut et en
Production. Le drapeau faux renvoie `503 game.unavailable` avant
authentification ou lecture. La route ne mute rien. Effectifs admissibles,
révision, composition officielle et futur envoi restent serveur.

Rapport serveur :
`Docs/ProductionIntegration/LivingHive_Phase4_CombatFormationReadiness_Server_2026-07-22.md`.

## Validation

- Unity 6000.5.3f1, suite LivingHive globale : 104 contrôles, sortie 0,
  marqueur `LivingHive manual collection checks passed`, zéro `error CS` et
  zéro `Compilation failed` dans
  `Artifacts/LivingHiveFormationReadiness_F8_Ratified.log`.
- Assemblage jeu : 0 erreur, 117 avertissements historiques.
- Assemblage éditeur et tests : 0 erreur, 100 avertissements historiques.
- Tests ajoutés : projection stricte, compte Gardiennes nul, refus des deux
  familles non mappées, frontière appareil/serveur, layout portrait/paysage,
  cycle de vie et 33 clés de localisation.
- Catalogues : 935/935 entrées uniques, strictement alignées entre `fr-CA` et
  `en-US`, dont 33 clés `formation_readiness.*` par langue.
- Serveur : 40/40 tests HiveOperations et build Release sans erreur;
  avertissement SqlClient préexistant. Les tests HTTP WebApplicationFactory,
  SQL et staging restent à ouvrir.
- Capture Unity finale : sortie 0, huit PNG aux dimensions exactes et marqueur
  `LivingHive strategic path proofs captured` dans
  `Artifacts/LivingHiveFormationReadiness_Capture_Ratified.log`.
- Processus finaux : Unity 0, dotnet 0, testhost 0.

## Preuves visuelles

Les deux nouvelles preuves ont été inspectées à résolution native :

- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_FormationReadiness_GuardiansVsDarters_FR_390x844.png`
  — 390x844, SHA-256
  `4569350aaafde847d1a31fb358150c368af30408e296e4ab708f7cff22d865ae`;
- `Docs/Product/Evidence/LivingHiveStrategicPath/LivingHive_FormationReadiness_GuardiansVsWingrunners_EN_1600x900.png`
  — 1600x900, SHA-256
  `fd9ccde04dab42dc04267afc62322b07ac47490e372408d3a30b80b7dcde03dd`.

Le portrait montre huit Gardiennes locales contre des Lanceuses, les deux
familles non reconnues, la réserve historique et l’autorité serveur sans texte
tronqué. Le paysage montre la même escouade exposée aux Wingrunners. Le manifeste
des huit preuves et leurs empreintes courantes est
`Docs/Product/Evidence/LivingHiveStrategicPath/LivingHiveStrategicPath_CaptureManifest.md`.

## Fondations protégées

- Carte canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Scène `LivingHive` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Image de base `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Les trois empreintes sont inchangées. Aucun fichier Communication n’a été
modifié; Communication est resté totalement gelé.

## Limites et prochaine porte

- Il n’existe encore aucun effectif doctrinal autoritaire côté serveur.
- Les Gardiennes ne sont donc préremplies que dans le brouillon local; le
  serveur les conserve lui-même dans la liste non classifiée tant que le modèle
  durable n’est pas étendu.
- Voltigeuses et Lanceuses n’ont ni entraînement, ni population, ni commit
  officiel.
- Aucun endpoint de composition ou d’envoi n’est préparé.
- La route de lecture doit encore recevoir ses tests HTTP, puis un vrai roster
  durable versionné avant raccordement au shell mobile.

La prochaine tranche recommandée est la création serveur-first du roster
doctrinal : migration durable, sources d’entraînement explicites pour
Voltigeuses/Lanceuses, snapshot authentifié et cache mobile du dernier snapshot
acquitté. Aucune composition officielle ne doit être activée avant cette étape.

## Fichiers de la tranche

Client :

- `Assets/BeeKingdom/Playground/HiveFormationReadinessPresentation.cs` et `.meta`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveFormationReadinessTests.cs` et `.meta`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveStrategicPathCapture.cs`;
- catalogues `strings.fr-CA.json` et `strings.en-US.json`;
- huit preuves et leur manifeste sous
  `Docs/Product/Evidence/LivingHiveStrategicPath`.

Serveur :

- `Server/src/BeeKingdom.HiveOperations/CombatFormationReadiness.cs`;
- `Server/tests/BeeKingdom.HiveOperations.Tests/CombatFormationReadinessTests.cs`;
- `Server/src/BeeKingdom.Server/Program.cs`;
- `Server/src/BeeKingdom.Server/appsettings.json`;
- `Server/src/BeeKingdom.Server/appsettings.Production.json`;
- `Docs/ProductionIntegration/LivingHive_Phase4_CombatFormationReadiness_Server_2026-07-22.md`.

## Vérification manuelle recommandée

Ouvrir `Assets/Scenes/LivingHive.unity`, entrer en Play/Game et fermer
l’introduction. Toucher `Armée`, puis `Préparer`. Vérifier que seules les
Gardiennes affichent un nombre local; toucher Voltigeuses puis Lanceuses et
confirmer qu’aucune sélection n’est créée. Choisir successivement les trois
menaces, puis revenir par `Armée` et rouvrir `Préparer` : la menace doit être
effacée. Entraînement, stocks, files, progression, voie stratégique et effectifs
doivent rester inchangés.

## Synchronisation VM

La synchronisation officielle de fin tentée à `2026-07-22T12:00:00Z` a échoué
avant toute copie : `Test-Path` reçoit `Accès refusé` sur
`\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` demeure daté de `2026-07-22T02:57:51Z`, avec
0 conflit et 4 suppressions historiques en attente. Aucun accès direct à `Z:`,
remappage ou contournement du bac à sable n’a été tenté; cette tranche reste sur
la copie locale `C:` jusqu’à la synchronisation utilisateur.
