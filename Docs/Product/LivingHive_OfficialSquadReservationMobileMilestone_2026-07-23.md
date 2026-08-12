# LivingHive — réservation officielle d’escouade mobile

Date : 2026-07-23

## Résultat

`Armée -> Préparer` ne s’arrête plus à un brouillon local lorsque la session
officielle est disponible. La composition Gardiennes/Voltigeuses/Lanceuses peut
être réservée puis libérée par le serveur sous le contrat
`phase4-combat-squad-reservation-v1`.

La réservation ne consomme aucun effectif et ne lance aucun combat. Une
réservation active verrouille les boutons `−`, `+` et `Suggérer`, puis donne
accès à `Libérer` et `Voir sorties`. Sans contrôleur officiel, l’aperçu local
historique reste clairement séparé.

## Frontière mobile / serveur

### Sur l’appareil

- rendu, navigation et brouillon de composition volatil ;
- dernier GET validé, chiffré et partitionné par joueur/ruche ;
- une commande commit ou release préparée dans l’outbox protégée avant le
  transport ;
- clé d’idempotence, route, révision attendue et charge canonique nécessaires à
  une reprise exacte ;
- reprise uniquement après le geste explicite `Vérifier la commande` ;
- aucun débit, effectif, identifiant de réservation, révision, heure
  d’acceptation ou succès inventé localement.

Le refresh, le redémarrage du contrôleur et l’ouverture de l’écran ne soumettent
jamais une mutation en attente. Une réponse ambiguë conserve la commande
protégée. Un conflit serveur définitif la retire et impose un nouveau refresh.
Le logout/changement de joueur ferme les contrôleurs avant la purge de la
partition locale.

### Sur le serveur

- identité joueur et ruche ;
- roster, disponible, réservé, capacité et révisions ;
- identifiant de réservation ;
- commit/release atomiques et persistants ;
- horloge UTC ;
- reçu public idempotent commit/release, sans `payloadHash` ni secret interne ;
- quantités du commit persistées pour un rejeu exact après release et
  reconstruction ;
- rétention bornée à 128 reçus avec éviction déterministe.

Un reçu historique peut donc être rejoué strictement à l’identique tandis que
le snapshot joint reflète un état serveur plus récent, par exemple une
réservation déjà libérée. Le client valide explicitement cette distinction.

## Interface

Le panneau `Préparer` présente l’état de la réservation officielle sans
dupliquer le panneau Sorties :

- lecture serveur, consultation hors ligne, mutation et résultat incertain ;
- `Réserver n/12`, `Libérer n`, `Vérifier la commande` ou `Actualiser` selon
  l’état ;
- cibles tactiles d’au moins 44 px en 390x844 et 1600x900 ;
- texte localisé dans les catalogues `fr-CA` et `en-US`.

Le bouton de réservation directe du panneau Sorties a été neutralisé : il
ramène maintenant à la composition. Toute réservation officielle passe ainsi
par le contrôleur protégé de `Préparer`.

## Preuves

- `HivePerimeterSortieClientTests` : 24/24 ;
- `SandboxLivingHiveOfficialSquadReservationTests` : 10/10 ;
- reprise après reconstruction sans auto-submit : verte ;
- catalogues : 1279/1279 clés, 0 doublon, 0 écart, dont 17 clés
  `formation_readiness.reservation.*` par langue ;
- compilation statique assemblage jeu : 0 erreur ;
- compilation statique assemblage éditeur : 0 erreur ;
- serveur `CombatSquadReservationTests` : 3/3 ;
- serveur `CombatSquadReservationEndpointTests` : 5/5 ;
- suite serveur net10 : 346 réussis, 0 échec, 8 SQL ignorés ;
- build serveur Release : 0 erreur, 1 avertissement
  `Microsoft.Data.SqlClient` préexistant ;
- Unity, dotnet et testhost : 0 processus à la fermeture.

Journaux autonomes :

- `Artifacts/SquadReservationClientHarness/SquadReservationClientHarness.log`
- `Artifacts/SquadReservationPresentationHarness/SquadReservationPresentationHarness.log`

La compilation statique réutilise les projets Unity générés. Le grand nombre
d’avertissements de références et de champs de désérialisation préexistants
n’est pas une preuve F8. Unity n’a volontairement pas été lancé pendant cette
tranche.

## Configuration fermée

`CombatSquadReservation:Enabled=false` reste fermé par défaut et en Production.
Aucun candidat serveur, transfert, activation ou déploiement n’a été produit.
Le build courant n’inclut toujours pas un environnement staging réellement
autorisé.

## Portes restantes

1. ouvrir `Assets/Scenes/LivingHive.unity` demain et laisser Unity compiler ;
2. exécuter F8 puis les tests Editor ciblés ;
3. inspecter le parcours portrait et paysage dans Game View ;
4. vérifier Android physique, Android Keystore, IL2CPP/AOT et TLS staging ;
5. valider DurableJson/SQL natif et multi-instance avant activation ;
6. fournir asset d’environnement, HiveId, comptes autorisés et flags ;
7. construire un nouveau candidat seulement après ces preuves ;
8. protéger de la même façon les mutations Phase 5 `launch`, `claim` et
   `recall`, qui restent une porte distincte avant activation des sorties.

## Inventaire exact de la tranche

- `Assets/BeeKingdom/Networking/HivePerimeterSortieClient.cs`
- `Assets/BeeKingdom/Playground/HiveOfficialSquadReservationPresentation.cs`
- `Assets/BeeKingdom/Playground/HiveOfficialSquadReservationPresentation.cs.meta`
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Tests/Editor/HivePerimeterSortieClientTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialSquadReservationTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialSquadReservationTests.cs.meta`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieTests.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- `Server/src/BeeKingdom.HiveOperations/CombatSquadReservationService.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/CombatSquadReservationTests.cs`
- `Server/tests/BeeKingdom.Tests/CombatSquadReservationEndpointTests.cs`
- `Artifacts/SquadReservationClientHarness/SquadReservationClientHarness.csproj`
- `Artifacts/SquadReservationClientHarness/Program.cs`
- `Artifacts/SquadReservationClientHarness/SquadReservationClientHarness.log`
- `Artifacts/SquadReservationPresentationHarness/SquadReservationPresentationHarness.csproj`
- `Artifacts/SquadReservationPresentationHarness/Program.cs`
- `Artifacts/SquadReservationPresentationHarness/SquadReservationPresentationHarness.log`
- `Artifacts/NuGet.Empty.Config`
- `Docs/ProductionIntegration/LivingHive_Phase4_CombatSquadReservation_FinalValidation_2026-07-23.md`
- `Docs/Product/LivingHive_OfficialSquadReservationMobileMilestone_2026-07-23.md`
- `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md`
- `Docs/VM/Codex_VM_Continuation.md`
- `Docs/Demos/LivingHive.md`

La scène canonique, la scène `LivingHive`, le terrain 50x50, ses images et
l’image de base de la ruche n’ont pas été modifiés :

- terrain canonique :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3` ;
- scène `LivingHive.unity` :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7` ;
- image de base :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

La synchronisation finale tentée le `2026-07-23T09:14:57Z` a échoué avant
toute copie : accès refusé à `\\DESKTOP-D3D29K7\BeeKingdomHost`. Aucun
contournement ni accès direct à `Z:` n’a été tenté. Le dernier rapport valide
reste daté du `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers l’hôte et
4 suppressions historiques en attente. La tranche demeure donc sur `C:`.
