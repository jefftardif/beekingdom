# LivingHive — contrat mobile de sortie au périmètre

## Résultat

La couche `BeeKingdom.Networking` possède maintenant un client mobile injectable
pour les contrats serveur `phase4-combat-squad-reservation-v1` et
`phase5-hive-perimeter-sortie-v1`. Il couvre la lecture, le commit et la
libération de la réservation, puis la lecture du tableau, le lancement, la
réclamation et le rappel d'une sortie.

Ce jalon ne prétend pas que la fonctionnalité est déjà jouable de bout en bout.
La surface LivingHive est maintenant visible sous
`Armée -> Préparer -> Voir sorties`, mais aucun transport HTTP de production,
aucun jeton officiel et aucun cache durable ne sont raccordés. Tant que le shell
de session officiel et les flags serveur sont fermés, son contrôleur par défaut
affiche explicitement `NotConfigured` et n'invente ni signal, ni statut, ni
récompense.

## Contrat et garde-fous

- toutes les routes sont construites avec l'identifiant canonique de la ruche;
- la clé d'idempotence et la révision restent celles fournies par l'appelant;
- les trois familles exactes sont `guardians`, `wingrunners` et `darters`;
- un brouillon local ne peut réserver ni quantité négative, ni famille inconnue,
  ni plus de 12 abeilles dans ce contrat initial;
- les versions de contrat, l'identité joueur/ruche, les révisions, le cycle UTC,
  les deux signaux, les risques, durées, seuils et récompenses sont validés avant
  publication au jeu;
- `SignalInstanceId` est recalculé depuis joueur + ruche + début de cycle + signal;
- `ServerTimeUtc` est exigé en UTC et sert de référence au délai relatif;
- une sortie active doit pointer vers le signal et la réservation du même snapshot;
- une réponse étrangère, altérée ou arithmétiquement incohérente est refusée avec
  `InvalidResponse`, jamais rendue comme un état officiel.

L'audit client a détecté que le snapshot serveur ne publiait pas la révision du
cycle lorsque la sortie était inactive. L'Intégrateur a ajouté `Revision` au
contrat Phase 5. La séquence réelle est maintenant prouvée : board `0`, premier
lancement `1`, réclamation `2`, nouvelle réservation, second signal lancé avec
`expectedRevision=2`, réponse `3`. Les tests vérifient aussi l'instance et la
réservation du second lancement.

## Frontière appareil / serveur

### Appareil

- référence volatile au jeton d'accès fourni par la session officielle;
- requête en cours et dernier snapshot retourné à l'appelant, uniquement en mémoire;
- rendu bilingue, navigation, sélection et brouillon local de composition;
- compte à rebours d'affichage dérivé de `EndsAtUtc`, sans autorité économique.

### Serveur

- compte, session et émission des jetons;
- roster, capacité et réservation d'escouade;
- révision, cycle UTC, identité des signaux et sortie active;
- admissibilité, idempotence, heure de fin, récompense, rappel et soldes.

Le client ne persiste ni access token, ni refresh token, ni snapshot dans cette
tranche. Il ne crédite jamais une récompense hors ligne et ne décide jamais que
le temps serveur est écoulé.

## Preuves

- harnais autonome client et présentation : **18/18** scénarios réussis;
- présentation isolée : états non configuré, réservation, lancement, sortie,
  réclamation et fin de cycle couverts sans horloge murale autoritaire;
- compilation alignée sur les assemblages Unity `netstandard2.1` : **0 erreur,
  0 avertissement**;
- compilation réelle des assemblages jeu et éditeur, présentateur et harnais de
  captures inclus : **0 erreur**; les 217 avertissements sont historiques;
- dix assertions Unity de navigation, routage, rectangles 390x844/1600x900 et
  localisation passent dans la suite F8 complète;
- F8 Unity : marqueur de succès, zéro `error CS`, fermeture propre;
- quatre captures dédiées exactes, inspectées et manifestées en SHA-256;
- catalogues `fr-CA` et `en-US` : **1006/1006** clés, zéro doublon, zéro
  asymétrie, dont 57 clés `perimeter_sortie.*` dans chaque langue;
- service serveur `HivePerimeterSortieTests` : **5/5**;
- HTTP serveur `HivePerimeterSortieEndpointTests` : **5/5**;
- fin de validation : aucun processus Unity, dotnet ou testhost conservé.

Le harnais est sous `Artifacts/HivePerimeterClientHarness`. Il compile les vrais
fichiers Asset et exécute les mêmes tests NUnit que l'assemblage Éditeur, sans
ouvrir Unity ni dépendre de sa licence.

## Fichiers client

- `Assets/BeeKingdom/Networking/HivePerimeterSortieClient.cs`
- `Assets/BeeKingdom/Networking/HivePerimeterSortieClient.cs.meta`
- `Assets/BeeKingdom/Tests/Editor/HivePerimeterSortieClientTests.cs`
- `Assets/BeeKingdom/Tests/Editor/HivePerimeterSortieClientTests.cs.meta`
- `Assets/BeeKingdom/Playground/HivePerimeterSortiePresentation.cs`
- `Assets/BeeKingdom/Playground/HivePerimeterSortiePresentation.cs.meta`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieTests.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieTests.cs.meta`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieCapture.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHivePerimeterSortieCapture.cs.meta`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`
- `Artifacts/HivePerimeterClientHarness/HivePerimeterClientHarness.csproj`
- `Artifacts/HivePerimeterClientHarness/HivePerimeterClientCompile.csproj`
- `Artifacts/HivePerimeterClientHarness/Program.cs`
- `Artifacts/HivePerimeterClientHarness/Directory.Build.props`
- `Artifacts/HivePerimeterClientHarness/NuGet.Config`

Le correctif serveur et ses preuves restent listés dans
`Docs/ProductionIntegration/LivingHive_Phase5_HivePerimeterSortie_Server_2026-07-22.md`.

## Portes encore fermées

1. fournir le vrai transport mobile authentifié et le cycle de session officiel;
2. décider d'un cache protégé, borné et partitionné avant toute reprise hors ligne;
3. injecter le contrôleur officiel depuis le shell mobile; le contrôleur par
   défaut doit rester `NotConfigured` jusque-là;
4. garder `CombatSquadReservation` et `HivePerimeterSortie` fermés jusqu'aux
   preuves Android/staging et à une autorisation de déploiement explicite.

Communication est resté entièrement gelé. Aucune scène, image LivingHive ou
image de terrain n'a été modifiée.

## Synchronisation et fondations protégées

La synchronisation normale finale tentée à `2026-07-22T18:09:35Z` a échoué avant toute
copie avec `Accès refusé` sur `\\DESKTOP-D3D29K7\BeeKingdomHost`. Le rapport
`.codex/vm-sync-last-report.txt` reste daté de `2026-07-22T02:57:51Z`, avec
0 conflit et 4 suppressions historiques en attente. Le travail demeure sur `C:`;
aucun accès direct à `Z:` ni remappage n'a été tenté.

- scène canonique 50x50 :
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive :
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive :
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

Les trois empreintes sont inchangées.
