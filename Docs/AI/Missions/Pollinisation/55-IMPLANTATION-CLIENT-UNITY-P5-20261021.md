# 55 — P5 : Client Unity et interface de pollinisation

Date : 21 octobre 2026. Branche : feature/pollinisation-alpha-20261021.

## Portée réalisée en branche isolée

**Code préparé et compilé isolément, pas encore fusionné ni déployé.**

- Assets/BeeKingdom/Networking/PollinationContracts.cs : modèles de transfert officiels de population civile, sites floraux, expéditions et projets d'alliance. Les noms de DTO et versions d'API correspondent au serveur .NET des Lots P1 à P4.
- Assets/BeeKingdom/Networking/PollinationClient.cs : appels authentifiés GET/POST vers les vraies routes, contrôles d'identité et de version, validation des stocks, bornes écologiques et progression, clé de commande unique et conservation au retry explicite, rafraîchissement de jeton une seule fois sans changement de joueur.
- Assets/BeeKingdom/Playground/PollinationPresentation.cs : projection de lecture et contrôleur de formulaire, taille d'essaim civil bornée aux disponibilités serveur, suivi d'expédition, création de projet, suppression des données privées à déconnexion et sans crédit économique local.
- Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.Pollination.cs : session officielle réutilisée, création et destruction du contrôleur au bon cycle de vie.
- Assets/BeeKingdom/Playground/PollinationWorldMapOverlay.cs : première interface plein écran opt-in FR/EN accessible depuis le monde déjà existant, onglets Champs / Essaims / Alliance, réserves et indices écologiques distincts, suivi et rapports serveur, bouton civil sans détournement des troupes.
- Assets/BeeKingdom/Networking/MobileAccountSessionRuntimeConfiguration.cs : booléen sérialisé pollinationFeatureEnabled **false par défaut**. Aucun joueur existant ne voit le nouveau bouton.
- Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs et WorldMapMmoFullscreenFoundationBootstrap.cs : accroches limitées à la session et à l'interception des clics lorsque le panneau est ouvert (y compris le bouton d'entrée). Aucun remplacement des chunks, tuiles, fleurs, bâtiments, ressources, cartes ou scènes.
- Assets/BeeKingdom/Playground/Editor/PollinationP5EditorTests.cs : quatre nouveaux tests Unity Editor déclarés, **pas encore exécutés dans le projet complet**.

## Invariants préservés

1. Aucune récolte Unity locale ne génère de pollen, score ou XP ; toutes les valeurs officielles proviennent de l'API serveur.
2. Une requête POST n'est **pas** réessayée automatiquement après une incertitude réseau ; le contrôleur retient une clé identique pour une éventuelle nouvelle confirmation du joueur.
3. Une réponse du mauvais joueur ou de la mauvaise ruche est rejetée, même si les autres champs sont cohérents.
4. Une demande dépassant le stock civil ou visant un site inconnu n'est pas envoyée.
5. Aucune disparition/recréation des boutons à chaque lecture : le modèle reste stable pendant un rafraîchissement. Le rafraîchissement n'accorde pas de ressource.
6. Le booléen client et les trois flags serveur restent désactivés par défaut.
7. Le basculement FR/EN lit BeeLocalization.CurrentLocale pendant l'affichage, sans créer de tables de traductions privées.
8. Le panneau est une couche au-dessus de la carte courante, sans modifier le terrain. La carte ignore ses interactions pendant que le panneau bloque la saisie.
9. Aucun marqueur floral n'est inventé : les coordonnées canoniques des sites ne sont pas encore présentes dans la réponse serveur. Le premier écran affiche les sites sous forme de liste officielle.

## Résultats de validation réellement exécutés

- dotnet run --project Server/ops/pollination-unity-client-qa/PollinationUnityClientQa.csproj : **33 assertions PASS** réparties entre contrats réseau (20) et contrôleur (13). Elles vérifient également la réutilisation de la clé après échec réseau, le refus d'une fausse récompense, les refresh d'authentification et la purge à déconnexion.
- Unity 6000.5.3f1, projet de compilation minimal et autonome utilisant des **copies exactes** des six nouveaux fichiers source avec adaptateurs de dépendance : **compilation terminée avec exit code 0**, vérifiée de nouveau après mise à jour du bouton d'entrée.
- Le code des scripts existants WorldMapMmoFullscreenFoundationBootstrap.cs et MobileAccountSessionRuntimeBootstrap.cs n'est pas compilé en totalité dans ce banc minimal, même si leurs accroches sont petites et délimitées. La compilation de tout BeeKingdom reste nécessaire.
- Les suites serveur SQL complètes validées au Lot P4 restent un jalon précédent : 313/313 HiveOperations et 716/716 API+SQL.
- Tous les tests Unity Editor ajoutés mais non lancés sont explicitement **en attente**.

## Environnement Unity : obstacle distinct du code P5

PCJEFF maintient l'éditeur Unity officiel ouvert sur C:\projets\beekingdomgame-master. Nous n'avons ni fermé l'éditeur officiel ni modifié ses fichiers.

Une importation en batchmode du worktree isolé a échoué car Unity Package Manager ne pouvait pas ouvrir son canal IPC au bout de 30 secondes. Un second essai avec -noUpm a pu importer le projet, mais le compilateur a signalé de nombreuses dépendances UI/TextMesh Pro introuvables, y compris dans des scripts **existants** du projet. Une copie autonome de Library/PackageCache depuis le projet officiel vers le worktree n'a pas suffi, puisque -noUpm empêche la résolution normale des packages.

Pour séparer ce défaut d'environnement de P5, un projet Unity minimal temporaire a été créé sous C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-p5-unity-compile-20261021, sans changer les sources du dépôt officiel. Cela prouve que les nouveaux composants GUI/API Unity compilent, **pas** que l'assemblage complet, toutes les scènes ou les assets premium sont validés.

## Qualité visuelle et décisions ouvertes

- Le panneau IMGUI est une première interface fonctionnelle sombre/dorée, FR/EN, pas encore la version UGUI/TMP finale sur la base exacte d'Activités v4. Une étape de finition visuelle, scroll, petits écrans et clics réels est requise après retour des packages Unity.
- Le marquage cartographique exige des positions de sites provenant du catalogue serveur officiellement validé ; aucune coordonnée factice n'est utilisée.
- Les projets collectifs de ce lot restent des états SQL P4, sous flag fermé ; l'UI n'en produit aucune simulation.
- Les animations de vol, trajectoires multi-sites, escorte, génétique, spécialisations, relais et diplomatie demeurent dans le backlog.
- Ne pas activer les flags ni fusionner dans main avant la compilation de tout le projet Unity, tests Editor/PlayMode et recette multijoueur avec SQL de staging distinct et sauvegarde vérifiée.

## Prochaine séquence prévue

1. Restaurer Unity Package Manager dans un worktree indépendant, compiler le projet intégral et exécuter les tests Editor P5 et les tests de régression de la carte.
2. Définir les positions et identités des premiers champs officiels depuis le vrai manifeste du monde (sans modifier les tuiles), puis afficher les marqueurs de lieux.
3. Adapter définitivement les écrans au chrome premium Activités v4 (UI Toolkit ou UGUI déjà en place), vérifier FR/EN, scroll, inertie des boutons et absence de clic traversant sur petits écrans.
4. Faire un essai Alpha avec plusieurs comptes sur la vraie API d'essai et la vraie base SQL avant toute ouverture en production.
## Protection du worktree après import Unity

L'importation en mode -noUpm a détecté des assets média absents dans ce checkout et
Unity a supprimé 5 527 métadonnées .meta sous Assets. Elles ont été restaurées
**uniquement dans le worktree P5** à partir de l'index Git en ciblant les seuls
chemins supprimés. Vérification : 0 suppression de fichiers suivis, 3 fichiers
source existants modifiés pour P5, 16 nouveaux fichiers P5, aucune texture,
scène ou métadonnée de l'ancien jeu enregistrée dans la livraison. L'éditeur
officiel n'a pas été fermé.

Le projet Unity de compilation minimal et la copie autonome du cache de paquets
résident hors du dépôt Git officiel, dans BK-Studio. Ce sont des outils QA
qui ne sont pas nécessaires pour les joueurs.
