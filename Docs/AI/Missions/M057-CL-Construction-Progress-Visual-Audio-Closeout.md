# M057-CL — Ferme le cycle chantier : contour Alliance, polish bleu/cyan, SFX de fin de chantier

Date : 2026-09-07
Agent : Claude Code
Scene de test : `Environment2D5D_HiveMap_Test` (jamais `LivingHive.unity`)
Statut : **CLOSED / PASS** — verifie par 31 tests EditMode verts + retest CEO reel en
Play Mode (visuel ET audio). Rien n'a ete pousse.

---

## 1. Perimetre de la mission

Trois demandes successives du CEO, dans la meme session, sur le meme fil "chantier de
batiment" :

1. **Correctif fonctionnel** : `ClosePremiumScreensInPriorityOrderCore` (Echap / retour
   Android / fermeture globale) ne liberait que le profil de membre d'alliance, pas le
   menu Alliance lui-meme — meme famille de defaut que M056A, deja repere par M058-CL
   comme limitation connue mais non corrige a l'epoque ("hors de mon perimetre" selon
   son propre rapport). Corrige ici.
2. **Coherence visuelle finale** : la barre de progression du chantier (en espace
   monde ET dans la fenetre "Amelioration en cours") utilisait encore le remplissage
   jaune/orange generique alors que le reste du langage visuel "construction" (contour
   de silhouette du batiment) est bleu/cyan depuis M059-CL.
3. **Integration audio finale** : jouer le SFX officiel `upgrade.mp3` (choisi et
   installe par le CEO) exactement quand une amelioration est **validee par le
   serveur** — jamais au clic, jamais en cas d'echec, une seule fois, pour n'importe
   quel batiment.

## 2. Correctif fonctionnel — fuite du menu Alliance a la fermeture globale

`ClosePremiumScreensInPriorityOrderCore` fermait le profil de membre
(`CloseAllianceMemberProfile` -> `ReleaseAllianceScreenOverlays`) mais retournait
`true` **avant** d'atteindre la branche `activeHiveMenu == HiveMenuMode.Alliance` un
peu plus bas dans la meme fonction, qui est la seule a liberer
`activeMainMenuId`/`activeHiveMenu`. Resultat : `PremiumUiBlocksWorldInput()` restait
vrai indefiniment apres un Echap/retour global depuis un profil de membre — la camera
HiveMap ne repondait plus, sans aucun ecran visible a fermer.

Correctif : les trois overlays propres a l'ecran Alliance (profil, panneau d'action,
tiroir de chat) et la sortie du menu Alliance lui-meme sont maintenant liberes en un
seul geste dans cette branche. Le bouton "Retour" **interne** a l'ecran Alliance
(`CloseAllianceMemberProfile`, appele depuis `DrawAllianceHeadquartersScreen`) n'est
**pas** touche : il continue a ne fermer que le profil et a laisser le Centre
d'Alliance ouvert, comportement voulu et protege depuis M058-CL.

## 3. Polish visuel — meme bleu/cyan que le contour de construction

- Nouvelle texture procedurale dediee `progress-fill-construction` (degrade
  `(0.16, 0.55, 0.92)` -> `(0.55, 0.92, 1)`), la meme teinte que
  `BuildingActivityPulse.Construction.Color`. **Separee** de `progress-fill`
  (jaune/orange), toujours utilisee telle quelle par la production, la recherche, le
  recrutement, etc. — aucun autre ecran n'a change de couleur.
- `DrawConstructionWorldProgressBar` (nouveau helper prive) : fond sombre partage
  inchange + remplissage bleu/cyan. Utilise par la barre en espace monde ET par la
  fenetre "Amelioration en cours" (`DrawUpgradeProgressOverlayForExternalHost`) — seule
  la couleur du remplissage change dans cette fenetre, position/timing/pourcentage
  intacts.
- `DrawOutlinedWorldLabel` (nouveau helper prive) : le temps restant au-dessus du
  batiment est desormais en gras, avec une taille qui suit la largeur de la barre
  (donc le zoom camera) au lieu d'un 10px fixe, et un contour sombre simule (8
  passages decales d'un pixel) pour rester lisible sur un decor de ruche clair comme
  sombre. Aucune police BeeKingdom dediee (Cinzel) n'est chargeable en IMGUI dans ce
  fichier — verifie : elle n'existe que comme asset TextMeshPro pour le Canvas uGUI du
  menu du bas, jamais comme `UnityEngine.Font` legacy nulle part dans
  `HiveViewProductUiPresenter.cs`. Introduire un nouveau pipeline de chargement de
  police pour une seule etiquette etait hors de proportion : la police IMGUI existante
  est conservee, seulement agrandie/grasse/contouree.

## 4. Integration audio — SFX de fin de chantier

- `Assets/Audio/SFX/UI/upgrade.mp3` (depose par le CEO) deplace vers
  `Assets/Audio/Resources/upgrade.mp3` (GUID preserve via `AssetDatabase.MoveAsset`)
  pour suivre la meme convention d'auto-chargement que `troop_ready`/`collect_troop`
  dans `AudioManager`.
- `AudioManager.PlayBuildingUpgradeComplete()` (nouvelle methode) : passe par
  `PlaySound` -> `sfxSource.PlayOneShot`, donc respecte integralement le volume SFX et
  le mute existants. Aucun `AudioManager` parallele.
- Nouveau `BuildingUpgradeCompletionSfxBootstrap` : ecoute l'evenement
  `BuildingCompleted` du `GameEventBus` partage. Ce signal est publie **uniquement**
  par `HiveBuildingUpgradePanelController.CompleteCoreAsync`
  (`HiveBuildingUpgradePresentation.cs`) **apres** une reponse serveur reussie —
  jamais sur les branches d'erreur (`HivePerimeterClientException`,
  `OperationCanceledException`, exception generique), jamais au demarrage d'une
  amelioration, une seule fois par completion reelle (la boucle de retry ne republie
  qu'a la sortie reussie). Generique par construction : `BuildingCompleted` transporte
  le `BuildingId` reel, donc fonctionne pour n'importe quel batiment sans liste figee.
- Volontairement un simple abonnement evenementiel plutot qu'un appel direct depuis le
  controleur de presentation : `HiveBuildingUpgradePanelController` est teste en
  EditMode sans scene Unity (`BuildingUpgradeFrameworkTests`,
  `HiveMapUpgradeProgressWiringTests`...) et ne doit jamais dependre d'un singleton
  MonoBehaviour audio.
- Cable dans `HiveMapRuntimeBootstrapInitializer.InitializeAllBootstraps` — piege deja
  documente trois fois dans ce meme fichier (M038B-CL, M049C-CL, M059-CL) : un
  bootstrap qui ne compte que sur son propre `[RuntimeInitializeOnLoadMethod]`
  n'existe jamais reellement dans la scene de jeu, seulement sur la scene active au
  demarrage du Play Mode (jamais `Environment2D5D*`).

## 5. Tests EditMode — 31/31 verts

| Suite | Resultat |
|---|---|
| `HiveMapUpgradeProgressWiringTests` | 4/4 — couvre le cablage `InitializeForScene` (reflexion sur tous les bootstraps `HiveMap*`, y compris `BuildingUpgradeCompletionSfxBootstrap` indirectement via l'installeur) et la detection generique du chantier en cours pour les 14 batiments reels |
| `SandboxLivingHiveUiStabilizationTests` | 22/22 — inclut les deux tests corriges par la section 2 (`WorldInputIsBlockedWhileAnyFullScreenIsOpen`, `ClosingAllianceProfileReleasesCapturedGuiControls`) |
| `GameEventBusTests` | 2/2 — non-regression du bus d'evenements partage utilise par le SFX |
| `BuildingUpgradeFrameworkTests` | 3/3 — non-regression du modele de presentation qui publie `BuildingCompleted` |

Compilation Unity verifiee propre (0 erreur) apres chaque etape. Un hang de l'Editeur
Unity (processus non-repondant, ~7 minutes) est survenu apres la premiere passe de
tests ; l'utilisateur a demande un redemarrage explicite du processus (relance
identique via `-projectPath`), apres quoi les 4 suites ont ete rejouees integralement
et sont restees vertes.

**Retest CEO reel (Play Mode, `Environment2D5D_HiveMap_Test`)** : confirme PASS,
incluant explicitement le SFX `upgrade.mp3` a la validation reelle d'une amelioration
de batiment — pas seulement les assertions EditMode statiques ci-dessus.

## 6. Fichiers commites (M057 uniquement)

Le checkout principal (`C:\projets\beekingdomgame-master`, celui sur lequel tourne
l'Editeur Unity connecte via MCP) contenait, avant ce commit, un arbre de travail non
commite melant plusieurs missions independantes (M059-CL "chantier + routage de clic",
mais aussi une isolation de compte separee, la Vue Colonie, des tests Profil Joueur,
la localisation du chat, une police, etc.). Seuls les fichiers reellement nes de cette
mission — plus les fichiers dont ils dependent directement pour compiler
(`RegisterCompletionPreemption`/`HiveMapBuildingUpgradeProgressBootstrap`/
`HiveMapResearchVisualStateBootstrap`/`BuildingActivityPulse`, deja references par
l'installeur commite mais jamais ajoutes a git) — ont ete retenus. Le reste du travail
non lie present dans l'arbre a ete **preserve tel quel, non commite**.

Voir le message de commit pour la liste exacte des fichiers.

## 7. Prochain test utilisateur (deja effectue par le CEO)

Dans `Environment2D5D_HiveMap_Test`, en Play Mode : lancer une amelioration, observer
la barre bleu/cyan au-dessus du batiment ET dans la fenetre "Amelioration en cours",
puis valider le chantier termine et entendre `upgrade.mp3`. **Confirme PASS par le
CEO.**
