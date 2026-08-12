# LivingHive Chat — pont de cycle de session

Date : 2026-07-22  
Responsable : Communication  
État : **implémenté et testé**, branchement shell/staging non activé

## Résultat

Communication dispose maintenant d’un coordinateur injectable entre le futur shell mobile authentifié et `LivingHiveChatBootstrap`.

- l’activation exige un garde-fou officiel `CanSubmitLogin=true` ;
- une session en préparation ou non configurée est refusée avant composition du client ;
- la partition de stockage doit correspondre exactement au joueur authentifié ;
- une notification répétée du même joueur ne recrée ni contrôleur, ni connexion, ni polling ;
- le renouvellement du bearer reste fourni par la même source de session vivante ;
- une nouvelle liaison du même joueur n’est pas confondue avec l’ancienne : le fournisseur obsolète est fermé avant adoption de la nouvelle source de session ;
- un changement A→B ferme A avant d’activer B ;
- un logout annule une activation retardée et attend sa terminaison avant la purge ;
- une activation annulée vérifie l’annulation avant de publier le runtime ;
- un échec d’activation revient à un état propre et peut être retenté ;
- aucun bearer, mot de passe ou compte fictif n’est stocké par ce coordinateur.

## Frontière d’assemblage

La première version provisoire référençait directement `BeeKingdom.Networking.MobileAccountSessionGate`, ce qui a révélé CS0234/CS0246 dans l’assemblage `BeeKingdom.Gameplay`. Le correctif final supprime cette référence inverse.

Communication expose désormais :

- `IChatAccountSessionReadiness` : contrat minimal en lecture seule ;
- `DelegateChatAccountSessionReadiness` : adaptateur dynamique basé sur `Func<bool>` ;
- `LivingHiveChatSessionBinding` : ensemble cohérent options/session/store/protecteur/realtime/diagnostics ;
- `LivingHiveChatSessionCoordinator` : sérialisation, annulation et changement de compte ;
- `ILivingHiveChatBootstrap` : point d’injection testable.

Le futur shell peut donc adapter son `MobileAccountSessionGate.CanSubmitLogin` sans que Communication référence l’assemblage Networking. La compilation Unity globale après correctif est verte et les erreurs CS0234/CS0246 ont disparu.

## Fichiers exacts

Modifiés :

- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatBootstrap.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`
- `Docs/WorldMapCommunication/LivingHiveChat_SessionLifecycleBridge_Spec_2026-07-21.md`

Créé :

- `Docs/WorldMapCommunication/LivingHiveChat_SessionLifecycleBridge_2026-07-22.md`

Aucun présentateur partagé, catalogue de localisation, terrain, scène ou image LivingHive n’a été modifié.

## Preuves

Commande autonome :

`dotnet test CommunicationCompile.csproj --no-restore -v:minimal --logger "trx;LogFileName=LivingHiveChatSessionBridgeFinal145.trx"`

Résultat :

- 146 tests exécutés ;
- 146 réussis ;
- 0 échec, 0 erreur, 0 non exécuté ;
- TRX : `C:\Users\tardi\.codex\visualizations\2026\07\21\019f855a-7f5a-70e2-a104-e633cd421a43\TestResults\LivingHiveChatSessionBridgeFinal145.trx` ;
- preuve complémentaire de remplacement de liaison : `LivingHiveChatSessionBindingReplacement146.trx` ;
- fin de passe : Unity=0, dotnet=0, testhost=0.

Architecte a indépendamment confirmé la compilation Unity globale sous 6000.5.3f1 avec zéro `error CS` et la disparition de CS0234/CS0246.

## Porte produit restante

Le modèle Splash/Auth actuel reste honnêtement en préparation et ne crée pas encore de session officielle. Aucun code de production n’instancie donc le binding ni n’appelle `SessionAvailableAsync`. Le chat continue d’afficher `NotConfigured`; ce jalon ne prétend pas rendre l’authentification ou le chat live.

Le raccordement futur doit fournir une URL HTTPS autorisée, une source de session vivante, un stockage et un protecteur réels, puis appeler `SessionEndedAsync` au logout/changement de joueur/arrêt du shell.

Aucun secret, déploiement, transfert, activation ou synchronisation n’a été effectué.
