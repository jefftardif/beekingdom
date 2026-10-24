# 56 — Lot P6 : ancrages des champs floraux sur la carte Wave6

Date : 21 octobre 2026
Statut : code QA dans la branche isolée feature/pollinisation-alpha-20261021.
Production SRVESDT : inchangée, flags OFF. Main Unity : inchangée.

## Architecture confirmée

Le serveur BeeKingdom existant porte déjà un catalogue FloralSites et un état
persistant partagé entre plusieurs ruches. Unity possède pour sa part un monde
Wave6 avec placement déterministe de ressources dans AddCoreSeededNodes, au sein
de WorldMapMmoFullscreenFoundationBootstrap.cs.

La carte ne reçoit PAS de nouvelles coordonnées inventées.

Deux propriétés de configuration **facultatives** sont ajoutées au catalogue
FloralSiteDefinition et propagées dans FloralSiteSnapshot :
- MapSurfaceId : identifiant exact de la surface de rendu existante.
- MapAnchorId : identifiant existant d'un noeud de ressource de cette surface.

Les deux champs doivent être vides ensemble ou valides ensemble. Un couple
surface + ancrage ne peut être déclaré qu'une fois dans le catalogue. Les sites
déjà sauvegardés ne sont pas migrés : ces propriétés sont uniquement des
métadonnées du catalogue serveur, pas des soldes ou des positions calculées
dans PlayerHiveState.

## Points d'ancrage vérifiés dans le code de la carte

La méthode AddCoreSeededNodes contient :
- res_pollen_core, ressource Pollen dans le chunk central du joueur ;
- res_nectar_core, ressource Nectar dans le même chunk central.

Leur position réelle est obtenue par RuntimePlacementPointAvoidingBearDen
selon la grille et son masque de placement actuel, plutôt qu'une coordonnée
XY supposée. Le démonstrateur de carte a la constante de surface
BK-DEMO-WORLD-WAVE6-LOCAL. Le nom WorldId du serveur reste un identifiant
indépendant au format canonique minuscule, pas cette constante d'interface.

La configuration QA illustrative, **non chargée automatiquement**, est
Docs/AI/Missions/Pollinisation/Fixtures/FloralSites.Wave6.QA.example.json.
Elle est désactivée par défaut et propose deux sites près des ancrages
existants. Elle n'est ni une annonce de géographie finale, ni un catalogue
commercial prêt pour l'Alpha publique.

## Implémentation sans refonte de terrain

- FloralSiteService.cs : valide l'ancrage configuré par le serveur et l'expose
  dans les lectures publiques de sites. Aucun débit de pollen supplémentaire.
- PollinationContracts.cs et PollinationClient.cs : affichent seulement des
  métadonnées connues et valident les paires / doublons avant projection UI.
- WorldMapMmoFullscreenFoundationBootstrap.Pollination.cs : nouveau fichier
  partial qui dessine des anneaux floraux non clignotants sur les noeuds réels
  quand la surface de l'API est exactement celle de la carte affichée.
- WorldMapMmoFullscreenFoundationBootstrap.cs : devient partial et comporte
  seulement les hooks de rendu et de sélection. Le code existant de terrain,
  streaming de chunks, ressources et combat n'est pas remplacé.
- PollinationWorldMapOverlay.cs : accepte le clic sur un marqueur, ouvre la
  fenêtre de pollinisation et sélectionne le site après réception du catalogue.
  Une lecture officielle est demandée à l'ouverture de la scène si le client
  et le serveur sont activés et l'utilisateur authentifié.

## Sécurité et compatibilité

- Feature flag client PollinationFeatureEnabled faux par défaut ;
  FloralSites.Enabled, CivilExpeditions.Enabled, CivilPopulation.Enabled,
  FloralAllianceProjects.Enabled restent aussi faux par défaut au serveur.
- Aucun noeud absent n'est remplacé par une coordonnée fictive.
- Une carte au MapSurfaceId différent ne dessine pas le marqueur du site.
- Les surfaces ou ancrages incomplets et les doublons sont refusés par le
  serveur, et à nouveau rejetés par le client.
- Le modèle FlowerPatch local ne débite ni ne crédite l'économie partagée.
- Les éléments existants restent intacts, la sélection des marqueurs ne
  s'exécute que lorsque le système officiel est configuré.
- Si un marqueur recouvre une ressource existante de la même zone, le clic
  sélectionne la fiche florale lorsque la fonctionnalité est active.
  Son harmonisation UX avec la récolte militaire historique devra être
  vérifiée en Editor avant activation.

## Contrôles effectués

- Tests ciblés HiveOperations FloralSiteServiceTests : 8 réussis, 0 échec.
- Tests ciblés API FloralSiteEndpointTests : 4 réussis, 0 échec.
- Test du client et contrôleur .NET : 36 assertions PASS.
- Compilation C# de l'adaptateur partiel de carte et des composants P5 dans
  un projet Unity 6000.5.3f1 minimal : code de sortie 0.
- QA PowerShell reproductible Validate-Wave6Anchors.ps1 : 2/2 ancrages
  réellement présents, surface exacte et tous les flags OFF (exit 0).
- Suite HiveOperations complète : 315/315 réussis.
- Suite API + SQL complète avec SQL LocalDB activé : **716/716 réussis**,
  aucun test ignoré. Total avec HiveOperations : **1 031/1 031 réussis**.
- Compilation Unity du projet entier : NON certifiée.
  Le lancement avec UPM a échoué par IPC après 30 s, sans atteindre la
  compilation complète. La compilation en -noUpm manque des résolutions de
  packages UI/TMP malgré leur cache local. Ne pas modifier la production ni
  fermer l'éditeur Unity officiel pour contourner ce défaut.

## Travaux suivants non déclarés terminés

1. Réparer ou fournir une résolution complète Unity Package Manager sur la
   copie QA, avec vraie compilation du projet et tests Editor / PlayMode.
2. Recetter l'ancrage et le clic dans la scène canonique réelle, sur petit
   écran et desktop, en conservant la navigation du tutoriel et du combat.
3. Obtenir le manifeste de surface officiel en production et les futurs
   sites/positions approuvés, sans employer la géographie du démonstrateur.
4. Finaliser l'interface premium via le même chrome Activités v4, en FR/EN ;
   l'IMGUI actuel n'est qu'une première intégration fonctionnelle.
5. Activer d'abord en staging après sauvegarde SQL, puis tester à plusieurs
   comptes et vérifier le rollback et la stabilité des marqueurs entre
   appareils.