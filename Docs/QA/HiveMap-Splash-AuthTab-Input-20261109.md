# BeeKingdom — onglets Accueil / Connexion / Création inactifs (2026-11-09)

Branche isolée : `fix/hive-splash-auth-tabs-click-20261109`. Base : `e24c4a57`.

## Symptôme

Sur la scène `Environment2D5D_HiveMap_Test`, la page Ruche Prime affichait les trois onglets mais une pression sur « Connexion » ne changeait pas la page. L'utilisateur ne pouvait donc même pas atteindre les champs de connexion officielle.

## Correction

- `HiveMapSplashBootstrap.OnGUI` donne la priorité au panneau de connexion (`GUI.depth=-31000`), rétablit `GUI.enabled`, utilise une matrice non transformée puis restaure toutes les propriétés après le dessin. L'écran de chargement existant, à la priorité `-32750`, reste prioritaire quand il est actif.
- `HiveViewProductUiPresenter.SplashAuthTabRectsForProof` est la source de vérité géométrique des trois onglets. Les rects visuels et tactiles sont les mêmes.
- `TryNavigateSplashTabForProof` active la navigation dès l'événement `MouseDown`, y compris quand Unity a marqué l'événement comme `Used`. Il n'attend plus exclusivement la remontée `MouseUp` d'un `GUI.Button` invisible.
- Une transition d'onglet libère le contrôle souris/clavier IMGUI précédemment capturé. Le clic normal au clavier et au contrôleur reste assuré par le bouton existant.
- Suppression des messages fallacieux « Mode démo local » après le clic sur Connexion; le message reflète désormais la disponibilité réelle de la session.
- Une trace `[BK-Splash] Tab clicked: Login` est émise **uniquement dans l'éditeur** en cas de clic traité. Aucun secret ni identifiant n'est journalisé.

## Vérifications

- `dotnet build Assembly-CSharp.csproj` : **PASS**, zéro erreur C#.
- `dotnet build Assembly-CSharp-Editor.csproj` avec le nouveau test ajouté temporairement à son fichier projet généré : **PASS**, zéro erreur C#, projet généré restauré ensuite.
- Sept cas de test NUnit EditMode préparés dans `HiveSplashAuthTabPointerTests.cs` (écran téléphone, paysage et bureau; clic intérieur/extérieur; retour Accueil; protection lorsque la ruche est déjà ouverte). Leur **exécution dans Unity Test Runner reste à faire** : Unity est actuellement ouvert en mode interactif sur ce même dossier et un second éditeur ne doit pas le modifier en parallèle.
- Vérification publique de `/runtime/account-session-readiness` : endpoint réactif et paramètres de disponibilité des comptes positifs, mais **trois blocages déclarés côté production** (preuve de route, sauvegarde, approbation du retour arrière). Cela ne certifie pas une authentification réussie.

## Validation manuelle immédiate

Dans le Unity ouvert sur
`C:\BeeKingdom\staging\BK-HudChat-Reconcile-20261105` :

1. Quitter le mode Play, laisser Unity recompiler les scripts, puis relancer Play.
2. Sur Accueil, cliquer une fois sur l'onglet **Connexion** : les champs « Courriel du compte » et « Mot de passe » doivent apparaître dans la zone défilante.
3. Faire l'essai **Accueil → Connexion → Création → Connexion**, puis tester les boutons avec la souris.
4. Si l'onglet reste inactif, consulter la Console : présence/absence de `[BK-Splash] Tab clicked: Login` permettra de distinguer interception du clic et changement d'état.
5. Ne saisir aucun mot de passe dans le chat; saisir les identifiants directement dans le jeu.

Aucune configuration SRVESDT, donnée SQL, identifiant, ou branche `main` n'a été modifié dans ce correctif.
