# LivingHive — retour à la ruche

## Résultat joueur

LivingHive résume maintenant les opérations réellement présentes dans le journal local lorsque le joueur revient dans la ruche. Le panneau distingue deux états :

- une amélioration, une formation ou une recherche toujours active, avec le temps restant calculé depuis son échéance UTC;
- une opération arrivée à échéance pendant l’absence, conservée dans le résumé même après l’application idempotente de son résultat au premier rafraîchissement.

Ce second état corrige un défaut concret : le panneau de reprise pouvait être créé avant la complétion locale, puis devenir vide dès la première simulation. Le résumé est désormais un instantané borné des trois files restaurées et ne dépend plus de leur état après rafraîchissement.

Le bouton `Voir` ouvre la destination exacte de la première opération : bâtiment concerné, panneau Armée ou panneau Recherche. Cette navigation ne récolte rien, ne crédite aucune ressource et ne marque aucune tâche quotidienne à la place du joueur. La fermeture et `Voir` disposent chacun d’une cible de 44 px.

## Expérience mobile

- Portrait 390x844 : panneau sous le HUD, deux lignes lisibles, ruche encore visible et rail inférieur libre.
- Paysage 1600x900 : panneau à droite du rail des files; ruche, recherche active et navigation principale restent visibles.
- Le titre devient `À TON RETOUR / WHILE YOU WERE AWAY` dès qu’au moins une opération s’est terminée pendant l’absence; sinon le titre existant `FILES REPRISES / QUEUES RESUMED` est conservé.
- Les textes sont localisés en `fr-CA` et `en-US`; les catalogues comptent **735/735** clés uniques, sans doublon ni asymétrie.
- Le panneau annonce explicitement `Aperçu local · aucune récolte automatique`.

## Frontière appareil / serveur

### Appareil

La démonstration lit le journal local versionné `v2`, borné à trois files : amélioration, formation et recherche. Au chargement seulement, elle dérive un résumé de session avec type, destination, échéance et état actif/terminé pendant l’absence. Ce résumé pilote le rendu et la navigation. Il n’est pas une preuve officielle et ne peut ni créer une opération, ni changer son résultat, ni créditer une ressource.

Dans le produit raccordé, l’appareil conservera uniquement le dernier résumé serveur reconnu, sa révision, l’état d’affichage et une éventuelle commande en attente dans un cache protégé, borné et partitionné par joueur. Il recalculera un compte à rebours d’affichage, mais ne décidera jamais qu’une opération est terminée.

### Serveur

L’Intégrateur a ajouté `HiveOperationResumeSummaryFactory`, projection en lecture seule de `PlayerHiveState`. Elle sépare les opérations actives et collectées et expose exclusivement depuis l’état serveur :

- identifiant et type d’opération;
- destination;
- statut et résultat;
- révision de ruche;
- dates de début et de fin UTC.

Les recherches actives ou terminées sont incluses. Aucun identifiant ou statut fourni par le client n’est accepté. `HiveOperationResume:Enabled=false` reste fermé par défaut et en Production; aucune route HTTP, notification push, récolte automatique, population ou navigation serveur n’est inventée. Le futur raccordement devra vérifier session et appartenance joueur/ruche avant de lire cette projection.

Rapport serveur : `Docs/ProductionIntegration/LivingHive_OperationResumeContract_2026-07-22.md`.

## Validation

- Compilation complète `Assembly-CSharp-Editor.csproj` : **0 erreur**, 217 avertissements historiques.
- Cette compilation confirme aussi la disparition de `CS0234/CS0246` après le correctif Communication; aucun fichier Communication n’a été modifié par l’Architecte.
- Première passe F8 : compilation verte, puis échec honnête d’une assertion contenant une chaîne de test mal encodée. Le texte joueur réel était correctement rendu en Unicode; seule l’assertion a été corrigée.
- Passe F8 finale : sortie 0, marqueur `LivingHive manual collection checks passed.`, zéro `error CS`, zéro échec, journal `Artifacts/LivingHiveQueueReturn_F8.log`.
- Le nouveau test couvre résumé mixte actif/terminé, maintien après complétion, navigation exacte, absence de collecte, cibles tactiles, bornes portrait/paysage et présence des clés bilingues.
- Capture Unity : sortie 0, zéro `error CS`, journal `Artifacts/LivingHiveQueueReturn_Capture.log`.
- `LivingHive_QueueReturn_FR_Portrait_390x844.png` : SHA-256 `200dbf3c62957de26e98231f06442a8ee71a871d52e5984b862e083a15c60aec`.
- `LivingHive_QueueReturn_EN_Landscape_1600x900.png` : SHA-256 `076c8abce1e3436fe646c96e2fac2bbcf3b813f0d98409274d6d0bac25331c69`.
- Manifeste : `Docs/Product/Evidence/LivingHiveQueueReturn/LivingHiveQueueReturn_CaptureManifest.md`.
- Serveur : HiveOperations **29/29**, build Release **0 erreur**; avertissement SqlClient préexistant seulement.
- Fin de validation : aucun processus Unity, dotnet ou testhost actif.
- La synchronisation finale normale s’est arrêtée avant toute copie sur `Accès refusé` à `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun contournement par `Z:` ni élargissement de droits; le rapport lisible demeure daté de 02:57:51 UTC avec 0 conflit et 4 suppressions historiques en attente.

## Fondations protégées

- Scène canonique 50x50 : 7 776 octets, SHA-256 `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`.
- Image LivingHive : 7 489 785 octets, SHA-256 `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.
- Scène `LivingHive.unity` : 9 160 octets, SHA-256 `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`.
- Aucun terrain, tuile, image de carte, image de ruche ou scène n’a été modifié.

## Fichiers client exacts

- `Assets/BeeKingdom/Playground/LocalPreviewQueueJournal.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveManualCollectionTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveQueueReturnCapture.cs` et `.meta`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- preuves et manifeste sous `Docs/Product/Evidence/LivingHiveQueueReturn`

## Fichiers serveur exacts

- `Server/src/BeeKingdom.HiveOperations/HiveOperationResumeContract.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOperationResumeSummaryTests.cs`
- `Docs/ProductionIntegration/LivingHive_OperationResumeContract_2026-07-22.md`

## Portes suivantes

- exposer le résumé authentifié seulement lorsque le shell mobile, l’appartenance ruche et le transport sécurisé sont prêts;
- protéger et partitionner le dernier résumé reconnu par joueur;
- réconcilier les révisions et les opérations disparues ou remplacées;
- tester changement de compte, horloge appareil fausse, perte réseau et reprise après mise à jour d’application;
- valider SQL, TLS/IIS, Android .NET 8 natif et staging avant toute ouverture du drapeau.
