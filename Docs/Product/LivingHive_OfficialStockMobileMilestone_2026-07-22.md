# LivingHive — Sac & stocks officiel mobile

Date : 22 juillet 2026  
État : tranche hors Unity ratifiée; validation Unity et staging ouvertes.

## Résultat produit

`Sac & stocks` possède maintenant un mode officiel injecté par la session mobile.
Il lit `GET /game/v1/hives/{hiveId}/hive-stock` sous le contrat
`living-hive-stock-v1` et affiche uniquement les valeurs acceptées du snapshot
serveur :

- miel, cire et pollen avec montant disponible et capacité;
- population et capacité de population seulement si le serveur les possède;
- recherches terminées;
- opérations actives, sans reconstruire ni réengager leurs coûts sur le téléphone;
- révision et heure UTC du snapshot dans le modèle de présentation.

Le panneau conserve trois boutons `Voir` de 44 px qui ouvrent les bâtiments
existants. Cette navigation ne collecte rien. Lorsque le contrôleur officiel est
actif, elle ne marque pas non plus la tâche locale de la Ronde quotidienne : la
preuve officielle `SnapshotRead` appartient au serveur.

## Frontière appareil, cache et serveur

### Appareil

- rendu responsive et textes `fr-CA` / `en-US`;
- état transitoire du contrôleur;
- navigation vers les trois bâtiments;
- aucune création de stock, capacité, population ou engagement;
- aucune collecte et aucune mutation dans ce contrat.

### Cache protégé

- dernier GET validé, cloisonné par joueur, ruche, contrat et route;
- restauration seulement pour le joueur déjà connu;
- lecture seule explicite hors ligne;
- aucune file de mutation ni valeur locale de remplacement.

### Serveur

- identité joueur/ruche et cloisonnement de compte;
- révision, UTC, catalogue et contrat;
- trois stocks et leurs capacités;
- population lorsqu’un agrégat autoritaire existera;
- recherches terminées et opérations actives;
- preuve quotidienne `SnapshotRead` idempotente lorsque la Ronde officielle est
  activée;
- activation fermée par défaut et en Production.

Les montants de la démonstration locale ne sont jamais fusionnés avec le snapshot
officiel. Les productions en attente et leurs taux demeurent dans le contrat
officiel de production par bâtiment; ils ne sont pas recopiés dans ce snapshot
global afin d’éviter de combiner deux révisions différentes.

## Fermetures défensives

- identité, contrat, catalogue, révision et UTC validés avant publication;
- exactement les trois ressources attendues côté serveur;
- montants et capacités non négatifs, montant inférieur ou égal à la capacité;
- population fournie par paire ou explicitement absente;
- listes bornées à 64, identifiants uniques et jetons sûrs;
- types d’engagement limités à `BuildingUpgrade`, `Training`, `Production` et
  `Research`;
- dates UTC non nulles, début non futur, fin strictement après le début et durée
  maximale de 30 jours;
- une seule rotation de session après un rejet 401;
- cache ignoré s’il appartient à un autre joueur;
- réponse invalide refusée sans substituer une valeur de l’appareil.

## Interface et localisation

Le panneau officiel possède les états `NotConfigured`, `Loading`, `Ready`,
`OfflineReadOnly` et `Error`. Sans snapshot validé, il ne montre aucun nombre.
La population absente est indiquée comme non disponible dans le contrat actuel.
Les engagements actifs précisent que leurs montants sont déjà débités et
qu’aucun engagement n’est ajouté sur l’appareil.

Vingt-et-une clés `stock.official.*` sont ajoutées. Les catalogues contiennent
1 174 entrées chacun, sans doublon, valeur vide, divergence de clés ou divergence
de paramètres.

## Fichiers mobiles

- `Assets/BeeKingdom/Networking/HiveStockSnapshotClient.cs`
- `Assets/BeeKingdom/Playground/HiveStockPresentation.cs`
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Tests/Editor/HiveStockSnapshotClientTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialStockTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialStockCapture.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

## Validation

- harnais autonome du client mobile : 10/10 réussis;
- harnais autonome de présentation : 5/5 réussis;
- `BeeKingdom.Networking` : compilation sans erreur;
- `Assembly-CSharp` : compilation sans erreur;
- `BeeKingdom.Tests` : compilation sans erreur;
- `Assembly-CSharp-Editor` : compilation sans erreur;
- suite serveur `BeeKingdom.HiveOperations.Tests` : 51 réussis, 0 échec;
- replay ciblé `HiveStockSnapshotTests` : 2/2 réussis;
- replay HTTP `HiveStockEndpointTests` : 3/3 réussis — flag fermé avant
  authentification, session exigée, DTO camelCase activé et deux lectures
  strictement non mutantes;
- suite serveur globale net10.0 : 328 réussis, 0 échec et 8 SQL ignorés;
- tests Editor Stock : 7 scénarios ajoutés et compilés, exécution Unity non
  revendiquée;
- catalogues : 1 174 entrées par langue, 21 clés Stock, 0 doublon, 0 valeur
  vide, 0 divergence de clés et 0 divergence de paramètres.

Les inclusions ajoutées temporairement aux quatre `.csproj` générés par Unity
pour compiler les nouveaux fichiers ont été retirées après les preuves. Unity
les régénérera normalement au prochain refresh.

Le premier replay global avait honnêtement révélé une fixture activée sans état
Research valide : 327 réussis, 8 SQL ignorés et 1 échec
`EnabledReturnsAuthoritativeCamelCaseSnapshot`. La fixture a été corrigée, le
CS8602 du nouveau contrat supprimé, puis les replays ciblé et global ci-dessus
ont été exécutés. Seuls ces derniers résultats verts sont ratifiés.

## Portes restant ouvertes

- F8 Unity global et tests Editor dans l’instance de l’utilisateur;
- deux captures Unity honnêtes `NotConfigured`, FR 390x844 et EN 1600x900,
  puis inspection à résolution native;
- test d’une vraie session mobile autorisée contre TLS staging;
- Android Keystore physique, reprise hors ligne et changement de joueur;
- SQL jetable et persistance multi-instance;
- projection cohérente après une mutation Ronde quotidienne;
- activation du flag, candidat, transfert et déploiement;
- Ronde quotidienne officielle complète après ratification du snapshot Stocks.

La scène `Assets/Scenes/LivingHive.unity`, la scène terrain canonique, la carte
50x50, ses images et l’image de base de la ruche ne sont pas modifiées. Le
chantier Communication reste gelé et séparé.

Empreintes finales :

- scène terrain canonique :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène `LivingHive.unity` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base `background_hive.png` :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

La synchronisation de fin, tentée le 23 juillet à `04:14:35Z`, a échoué avant
toute copie avec `Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Le
dernier rapport reste daté du `2026-07-22T02:57:51Z` : 0 conflit, 0 copie VM
vers hôte et 4 suppressions historiques en attente. Aucun remappage, accès
direct `Z:` ou relâchement du bac à sable n’a été tenté; les changements
restent sur `C:`. État final : Unity=0, dotnet=0, testhost=0, bee_backend=0,
Java=0 et clang=0.
