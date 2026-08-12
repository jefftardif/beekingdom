# LivingHive — Ronde quotidienne de la ruche

## Résultat joueur

Le menu `Quêtes` contient maintenant deux onglets tactiles : `Acte I` et `Ronde`. La ronde quotidienne valorise trois gestes utiles déjà présents dans la boucle LivingHive, sans ajouter d’attente artificielle :

1. collecter manuellement une production dans son bâtiment;
2. lancer une vraie opération de construction, formation ou recherche;
3. ouvrir `Sac & stocks`, puis naviguer vers un bâtiment avec `Voir`.

Chaque geste ne compte qu’après son résultat réel. Une fois les trois lignes terminées, le joueur doit réclamer explicitement la récompense de démonstration : **120 miel et 60 pollen**. Rien n’est crédité automatiquement. Une même journée locale de preview ne peut produire qu’un seul reçu de réclamation.

Avant validation, chaque ligne incomplète offre un raccourci `Aller` qui ouvre la destination exacte : bâtiment de collecte, menu Recherche ou registre `Sac & stocks`. Le raccourci ne valide jamais la ligne à la place du joueur.

## Expérience mobile

- Portrait 390x844 : le journal tient entre le HUD et le rail bas, avec les trois gestes, leur état, la récompense et le bouton de réclamation visibles sans défilement.
- Paysage 1600x900 : le panneau reste à droite; la ruche, les abeilles et la file Recherche réelle demeurent visibles.
- Les onglets, la fermeture et la réclamation ont des cibles d’au moins 44 px.
- Le bouton `Quêtes` conserve sa place dans les quatre entrées principales du portrait.
- Le badge `!` de Quêtes apparaît uniquement lorsque la récompense est réellement prête et disparaît après réclamation. Les anciens badges simulés de Mail, Alliance et Plus ont été supprimés.
- Le panneau annonce clairement `Aperçu local` et l’autorité officielle `serveur UTC`.
- Les catalogues `fr-CA` et `en-US` contiennent **726/726** clés uniques, sans doublon ni asymétrie.

## Frontière appareil / serveur

### Appareil

La démonstration locale conserve dans `PlayerPrefs` un journal versionné `v1` : jour UTC observé par l’appareil, masque des trois gestes, état réclamé et identifiant local de l’opération de réclamation. Ce journal permet de tester l’interface, le changement de journée et l’idempotence visuelle. Les soldes et la récompense qu’il manipule restent explicitement **locaux et non officiels**.

Dans le produit raccordé, l’appareil devra seulement conserver le dernier snapshot serveur reconnu, l’état d’affichage et une commande de réclamation en attente dans un stockage protégé et partitionné par joueur. Il ne pourra pas choisir le jour officiel, positionner lui-même un jalon, augmenter un solde ni décider qu’une récompense est acquise.

### Serveur

L’Intégrateur a ajouté un état persistant `HiveDailyRound` par joueur et ruche. Le serveur possède le jour UTC et n’accepte que trois faits vérifiés :

- une opération réellement arrivée au statut `Collected`;
- une opération non collectée dont `StartedAtUtc` appartient au jour UTC courant;
- une lecture explicite du snapshot autoritaire.

Une opération ancienne, inexistante ou déjà collectée ne peut pas servir de preuve de lancement. Lorsque les trois faits sont présents le même jour, `ClaimDailyRoundAsync` vérifie joueur, ruche, révision attendue, capacité et clé d’idempotence, puis crédite atomiquement 120 miel et 60 pollen. Une répétition identique du même jour rejoue le reçu; une charge contradictoire ou la réutilisation inter-journalière de la clé est distinguée par le hash incluant le jour UTC.

`HiveDailyRound:Enabled=false` reste fermé par défaut et en Production. Il n’existe encore aucun endpoint HTTP : session mobile, route snapshot, stockage protégé de l’outbox, réconciliation et staging restent des portes honnêtes. Rapport serveur : `Docs/ProductionIntegration/LivingHive_HiveDailyRound_Core_2026-07-22.md`.

## Validation

- Unity `6000.5.3f1`, suite LivingHive globale : sortie 0, marqueur `LivingHive manual collection checks passed.`, zéro `error CS`, journal `Artifacts/LivingHiveDailyRound_F8.log`.
- Le test dédié couvre les trois raccourcis exacts sans validation implicite, les trois événements réels, la répétition d’un geste, la réclamation unique, les montants exacts, la capacité, le reçu persistant, le redémarrage et le changement de jour.
- La compilation globale confirme aussi la disparition de `CS0234/CS0246` après le correctif isolé du thread Communication; aucun fichier Communication n’a été modifié par l’Architecte.
- Capture Unity : sortie 0, zéro `error CS`, journal `Artifacts/LivingHiveDailyRound_Capture.log`.
- `LivingHive_DailyRound_Portrait_390x844.png` : état 0/3, trois raccourcis `Aller` et aucun faux badge, SHA-256 `ebaa0868a9e99c10f833d6c52421e57a231eee2264ffabe3c9e0519309372db9`.
- `LivingHive_DailyRound_Landscape_1600x900.png` : état 3/3, récompense prête et seul badge Quêtes actionnable, SHA-256 `e70ffa10b61d1d39c3e8725897f2562be8e17fd0018223ef056b79aa11bf207d`.
- Manifeste : `Docs/Product/Evidence/LivingHiveDailyRound/LivingHiveDailyRound_CaptureManifest.md`.
- Serveur : HiveOperations **28/28**, rejet explicite d’une ancienne preuve de lancement, build Release 0 erreur; avertissement SqlClient préexistant seulement.
- Fin de validation : aucun processus Unity, dotnet ou testhost actif.
- La synchronisation finale a été tentée mais s’est arrêtée avant toute copie sur `Accès refusé` à `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun contournement ni écrasement; la copie locale et la liste exacte des fichiers font foi. Le dernier rapport lisible indique 0 conflit et 4 suppressions historiques en attente.

## Fondations protégées

- Scène canonique 50x50 : 7 776 octets, SHA-256 `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Image LivingHive : 7 489 785 octets, SHA-256 `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.
- Scène `LivingHive.unity` : 9 160 octets, SHA-256 `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Aucun terrain, tuile, image de carte, image de ruche ou scène n’a été modifié.

## Fichiers client exacts

- `Assets/BeeKingdom/Playground/LocalPreviewDailyRound.cs` et `.meta`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveDailyRoundCapture.cs` et `.meta`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- preuves et manifeste sous `Docs/Product/Evidence/LivingHiveDailyRound`

## Portes suivantes

- raccorder les trois faits aux transactions serveur réelles, puis exposer snapshot et claim authentifiés derrière le drapeau fermé;
- ajouter les contrats HTTP, les erreurs `game.*`, les tests de rejeu/conflit et la réconciliation par révision;
- stocker l’outbox mobile et le dernier snapshot dans un cache protégé, borné et partitionné par joueur;
- tester changement de compte, changement de journée pendant une reprise, perte réseau et capacité devenue insuffisante;
- valider SQL jetable, TLS/IIS, Android .NET 8 natif et staging avant toute activation.
