# LivingHive — registre mobile Sac & stocks

## Résultat joueur

Le bouton `Sac`, auparavant factice, ouvre maintenant un registre tactile des ressources de la ruche. Le joueur voit pour le miel, la cire et le pollen :

- le stock disponible;
- la production qui attend encore une collecte manuelle dans son bâtiment;
- le montant déjà engagé dans une file construction, formation ou recherche.

Le panneau montre aussi la capacité globale et le total des engagements actifs. Chaque bouton `Voir` ouvre le bâtiment correspondant; il ne collecte jamais à la place du joueur. La boucle reste donc `Sac -> Voir -> bâtiment -> collecte manuelle`.

## Ergonomie mobile

- Portrait 390x844 : le registre tient entre le HUD et le rail bas; les trois ressources, la capacité et les engagements sont visibles sans défilement.
- Paysage 1600x900 : le panneau reste à droite et laisse la ruche, les abeilles et les trois files lisibles.
- Les boutons `Voir` et la fermeture mesurent 44 px.
- Une recherche active est utilisée dans les preuves pour confirmer le même engagement de 240 miel et 90 pollen dans le registre et dans la file Recherche.
- Les catalogues `fr-CA` et `en-US` contiennent **697/697** clés uniques, sans doublon ni asymétrie.

## Frontière appareil / serveur

### Appareil

Le panneau rend l’état local courant et peut, lors du futur raccordement, conserver au plus le dernier snapshot reconnu par joueur pour une consultation hors ligne. Il ne modifie aucun solde, ne réclame aucune récompense et ne fabrique aucune population. La production en attente reste une donnée de preview locale jusqu’à l’existence d’un agrégat serveur correspondant.

### Serveur

L’Intégrateur a ajouté `HiveStockSnapshotFactory`, qui projette uniquement l’état autoritaire `PlayerHiveState` vers un snapshot cloisonné joueur/ruche : révision, miel/cire/pollen et leurs capacités, recherches terminées et engagements actifs. Aucune valeur client n’est acceptée.

Population et capacité de population restent explicitement `null`, car aucun agrégat serveur fiable ne les représente encore. Le client ne doit donc jamais promouvoir ses compteurs locaux au rang de valeurs officielles.

`HiveStockSnapshot:Enabled=false` reste fermé par défaut et en Production. Aucune route HTTP, mutation, récompense, candidature ou mise en production n’est ouverte. Rapport serveur : `Docs/ProductionIntegration/LivingHive_HiveStockSnapshot_Core_2026-07-22.md`.

## Validation

- F8 Unity `6000.5.3f1` : sortie 0, marqueur `LivingHive manual collection checks passed.`, zéro `error CS`, journal `Artifacts/LivingHiveLedger_F8.log`.
- Le test dédié couvre l’autorité locale/officielle, les montants disponibles/en attente/engagés, la cible tactile de 44 px, la navigation exacte et l’absence de collecte directe.
- Capture Unity : sortie 0, zéro `error CS`, journal `Artifacts/LivingHiveLedger_Capture.log`.
- `LivingHive_Ledger_Portrait_390x844.png`, SHA-256 `40564fbce28fa67223f84ad6974cdaf52ea005fcbc2cd70fb2e7c0cf71b7b35d`.
- `LivingHive_Ledger_Landscape_1600x900.png`, SHA-256 `111672068f4c55ae2dd374d7415c5f0daab25811972b1082e59ce4090c128ad8`.
- Manifeste : `Docs/Product/Evidence/LivingHiveLedger/LivingHiveLedger_CaptureManifest.md`.
- Serveur : `HiveStockSnapshotTests` 1/1, HiveOperations 27/27, build Release 0 erreur; avertissement SqlClient préexistant seulement.
- Fin de validation : aucun processus Unity, dotnet ou testhost actif.

## Fondations protégées

- Scène canonique 50x50 : 7 776 octets, SHA-256 `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Image LivingHive : 7 489 785 octets, SHA-256 `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.
- Scène `LivingHive.unity` : 9 160 octets, SHA-256 `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Aucun terrain, tuile, image de carte, image de ruche ou scène n’a été modifié.

## Fichiers client exacts

- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveLedgerCapture.cs` et `.meta`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- preuves et manifeste sous `Docs/Product/Evidence/LivingHiveLedger`

## Portes suivantes

- raccorder session authentifiée et route de lecture derrière le drapeau fermé;
- protéger et partitionner le dernier snapshot mobile par joueur;
- réconcilier le cache par révision serveur avant tout affichage officiel;
- créer l’agrégat serveur de population avant d’afficher population/capacité comme officielles;
- couvrir HTTP, SQL, TLS/IIS et Android staging sous .NET 8 natif.
