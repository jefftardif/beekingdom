# LivingHive — recherche officielle et effets persistants

## Résultat

LivingHive possède maintenant une tranche mobile officielle sous le contrat
`living-hive-research-v1`. Le serveur est l’unique auteur du catalogue, des
coûts, des soldes, des durées, de l’heure UTC, de la file, des effets en points
de base, de la révision et des reçus. Le téléphone valide et présente ces
données; il ne débite, ne termine et n’applique jamais une étude localement.

Les deux études connues par l’interface sont `foraging_routes_i` et
`tempered_combs_i`. Leurs prix, durées et effets ne sont pas inscrits dans le
client. Les valeurs utilisées par les tests ne sont pas un équilibrage produit.
Le flag reste faux par défaut et en Production, et le catalogue HTTP live doit
rester vide tant qu’un catalogue économique n’a pas été ratifié.

## Frontière appareil / serveur

### Sur l’appareil

- rendu, localisation et interaction tactile;
- état transitoire du panneau et projection monotone du compte à rebours;
- access token en mémoire et refresh token destiné au coffre Android;
- dernier GET validé dans un cache protégé, borné et partitionné;
- clé d’idempotence stable conservée quand le résultat réseau d’un POST est
  incertain;
- lecture hors ligne explicitement sans mutation.

### Sur le serveur

- identité joueur/ruche et révision autoritaire;
- catalogue, coûts, durées, soldes et débit atomique;
- heure UTC, file unique et décision `awaiting_completion`;
- effets structurés `honeyProductionBonusBps` et
  `waxCapacityBonusBps`, persistance et application à la production;
- reçus idempotents et conflits de révision, clé, ressource, file ou échéance.

Les POST contiennent seulement `expectedRevision` et `idempotencyKey`. Le
mobile ne transmet aucun coût, effet, montant, délai, heure ou résultat faisant
autorité.

## Contrat mobile

- `GET /game/v1/hives/{hiveId}/research`;
- `POST /game/v1/hives/{hiveId}/research/{researchId}/start`;
- `POST /game/v1/hives/{hiveId}/research/{operationId}/complete`;
- une seule rotation de session après `401`, puis rejeu de la requête
  strictement identique;
- aucune répétition aveugle d’une mutation après panne réseau;
- acceptation d’un reçu de démarrage original avec un snapshot plus récent où
  l’étude est déjà terminée;
- complétion activée uniquement sur l’état serveur `awaiting_completion`;
- cache protégé utilisable seulement pour consulter le dernier état validé.

Le contrôleur est composé et détruit avec la session officielle dans
`MobileAccountSessionRuntimeBootstrap`. L’écran Recherche et la file latérale
utilisent l’état officiel lorsqu’il est injecté. Sans configuration, l’écran
affiche honnêtement `Serveur requis`; il ne montre ni coût, effet, succès ou
reçu inventé. La prévisualisation locale historique reste explicitement non
officielle.

## Validation autonome obtenue

- client réseau mobile : **13/13**;
- projection et contrôleur de panneau : **6/6**;
- compilation conjointe de `BeeKingdom.Networking`, `BeeKingdom.Tests`,
  `Assembly-CSharp` et `Assembly-CSharp-Editor` : **0 erreur**; les inclusions
  nécessaires dans les projets Unity générés ont ensuite été retirées;
- catalogues `fr-CA` et `en-US` : **1153 entrées chacun**, **38 clés Recherche
  officielle**, 0 doublon, 0 valeur vide, jeux de clés identiques et 0
  différence de paramètres de format;
- scène canonique : SHA-256
  `927FA2A719033270E8AD4BF66C719FAD7A1414A08F9705D400D40A5DE122B1B3`;
- scène LivingHive : SHA-256
  `ECCFE9AA81AE883317E4E951C8552DCEF1A156179F35480567466AB95A9708E7`;
- image de base LivingHive : SHA-256
  `3C0E3B97E8E7AD76FC2C46A9342C4F9D7B03717591356251945C8F3F62B467F6`.

La première livraison serveur n’a pas été ratifiée : les effets HTTP étaient
exposés à plat, la production testait seulement la présence d’une étude avec
des pourcentages codés en dur et le catalogue statique de test aurait été
exposé par la seule activation du flag. L’audit a fait corriger ces défauts.

État serveur final de cette tranche :

- DTO HTTP avec objet `effects` structuré;
- calcul hors ligne depuis les bps réellement persistés, validés entre 0 et
  10 000, sans pourcentage codé en dur;
- catalogue HTTP filtré par `LivingHiveResearch:Catalog`, vide par défaut et en
  Production; un identifiant absent est refusé avant mutation;
- validation 400 de la révision et de la clé sur start et complete;
- build Release : 0 erreur, 2 avertissements historiques;
- suite HTTP Recherche : **4/4**;
- suite serveur net10 : **325 réussis, 0 échec, 8 SQL ignorés**.

L’Architecte a rejoué indépendamment les binaires Release : **4/4** puis
**325/0/8**, TRX
`Server/tests/BeeKingdom.Tests/TestResults/LivingHiveResearchEndpointArchitectAudit.trx`
et
`Server/tests/BeeKingdom.Tests/TestResults/LivingHiveResearchFullArchitectAudit.trx`.
Une première tentative Debug a rencontré un refus d’écriture dans un cache
`obj` temporaire; aucun test n’a été compté sur cette tentative. Le rejeu
Release n’a laissé aucun échec.

Cette validation ne ratifie pas encore le staging : les preuves HTTP détaillées
start/complete, rejeu après complétion, conflits et effet historique avec bps
altérés restent explicitement ouvertes, tout comme SQL/TLS.

## Validation Unity encore ouverte

Unity est resté fermé pendant la tranche. F8, les huit assertions officielles
agrégées et les preuves visuelles ne sont donc pas encore ratifiés.

Le harnais `SandboxLivingHiveOfficialResearchCapture` est prêt pour exactement
deux preuves honnêtes `NotConfigured` :

- portrait français 390x844;
- paysage anglais 1600x900.

Il supprime les anciennes sorties avant exécution, vérifie les dimensions et
écrit un manifeste avec SHA-256. Il n’injecte aucun coût, effet, solde, succès,
reçu ou conversation fictive. À la prochaine ouverture Unity : laisser finir
l’import, confirmer zéro erreur C#, lancer F8, produire les captures Recherche
et Construction, puis inspecter chaque image à résolution native.

## Fichiers client principaux

- `Assets/BeeKingdom/Networking/HiveResearchClient.cs`;
- `Assets/BeeKingdom/Playground/HiveResearchPresentation.cs`;
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`;
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`;
- `Assets/BeeKingdom/Tests/Editor/HiveResearchClientTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialResearchTests.cs`;
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveOfficialResearchCapture.cs`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`;
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`;
- `Artifacts/ResearchClientHarness/`;
- `Artifacts/ResearchPresentationHarness/`.

Le détail serveur est dans
`Docs/ProductionIntegration/LivingHive_Research_ServerAuthority_2026-07-22.md`.

## Portes encore ouvertes

1. compléter les preuves HTTP de mutations et d’effets historiques avant toute
   ratification staging;
2. ratifier F8 et les deux captures Recherche, ainsi que la validation Unity
   Construction déjà en attente;
3. choisir, auditer et versionner le catalogue économique réel;
4. prouver JSON et SQL avec stockage jetable puis HTTPS/TLS staging;
5. configurer l’environnement mobile et le vrai `HiveId`;
6. valider deux joueurs, changement de compte, reprise réseau et Android
   Keystore sur appareil physique;
7. produire un paquet Android IL2CPP installable après le budget de contenu;
8. seulement ensuite ouvrir les flags, reconstruire un candidat, déployer et
   observer la télémétrie.

Communication est resté gelé. Aucun module, test, document ou ancrage chat n’a
été modifié. Aucun terrain 50x50, PNG de terrain, image de base ou scène n’a été
modifié.

## Synchronisation VM

Les synchronisations de début et de fin de tranche ont échoué avant toute copie
parce que le partage `\\DESKTOP-D3D29K7\BeeKingdomHost` était inaccessible.
La tentative finale date de `2026-07-23T03:23:08Z`; le rapport local demeure
daté du `2026-07-22T02:57:51Z`, avec 0 conflit, 0 copie VM vers hôte et 4
suppressions historiques en attente. Aucun conflit n’a été écrasé, aucun accès
direct à `Z:` et aucun relâchement du bac à sable n’ont été tentés. Les
changements restent sur la copie locale `C:` jusqu’à la prochaine
synchronisation autorisée.
