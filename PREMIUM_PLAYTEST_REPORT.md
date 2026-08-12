# BeeKingdom — Premium Experience Audit (Playtest réel)

Rapport rédigé après un playtest réel de la build Windows interne
(`Builds/Windows/Internal/BeeKingdom_Internal_Debug.exe`), en pilotant
réellement la souris et le clavier sur la fenêtre du jeu (pas de lecture de
code, pas d'inspection de scène Unity pendant le test). Deux parcours ont été
joués : un avec un compte Google déjà utilisé (session/sauvegarde existante),
un avec un compte Google n'ayant jamais vu le jeu (parcours neuf réel). Le
mode "démo locale" a aussi été exploré séparément, à la demande, pour la
première impression d'un visiteur anonyme.

Ce rapport ne contient que ce qui a été vu à l'écran pendant ces sessions.

---

## Journal des sprints — First Hour Experience Pass

### Sprint 6 (2026-08-05) — Un seul état de connexion visible (PB-009)

**Statut : RÉSOLU.** PB-009 provenait de quatre sources d'affichage indépendantes : le disclaimer
de l'en-tête, la carte de disponibilité, le message inférieur de l'écran de connexion et le statut
hors ligne de Communication. Le joueur pouvait donc voir « COMPTE CONNECTÉ », « JEU ENCORE LOCAL »,
« SESSION ACTIVE » et « Hors ligne » dans la même session.

**Cause racine corrigée** : l'interface ne dérivait pas tous ses textes d'une décision commune et le
cycle de session ne possédait pas d'état explicite de perte réseau. Un état central couvre maintenant
le démarrage, la vérification, la préparation, l'authentification, le compte connecté local, le compte
connecté avec autorité serveur, la perte réseau et l'indisponibilité. Le transport gameplay signale
les échecs réseau à la session; la reconnexion relance le rafraîchissement protégé. Communication ne
présente plus une panne de son service comme une déconnexion globale.

Le test utilisateur a aussi révélé une configuration locale obsolète : le client pointait vers
`http://127.0.0.1:5088`, sans serveur local actif. L'endpoint HTTPS de production a répondu `200 OK`;
la configuration Unity pointe maintenant vers `https://chat.dravii.com` et refuse le loopback non
sécurisé. Unity doit être redémarré avant la nouvelle vérification afin de purger l'ancien état en
mémoire.

À la demande de Jeff, les libellés techniques visibles après connexion ont ensuite été simplifiés :
« synchronisé avec le serveur », « serveur autoritaire » et la validation serveur ne sont plus affichés
au joueur. Ils sont remplacés par « partie officielle active »; la distinction « partie locale » reste
visible uniquement lorsqu'elle est réellement utile.

**Fichiers modifiés** : client de session, transport gameplay authentifié, bootstrap de session,
présentateur LivingHive, tests de session et ressources de localisation FR/EN.

**Tests effectués** : états `Checking`, `Ready` et `Unavailable` exécutés par session simulée avec
les cinq surfaces comparées; connexion Google simulée; perte réseau `Authenticated -> Offline`;
restauration `Offline -> Authenticated`. Résultat direct : `PASS google-login + network-loss-restore`.
Compilation Unity sans erreur. Le runner Unity officiel a aussi été lancé, mais son ancienne exécution
restée bloquée pendant le Play Mode a échoué sur la sauvegarde/restauration de scène; ce résultat est
documenté comme limitation du harnais, pas comme succès de test.

**Validation externe demandée** : sur la build Windows, lancer le jeu, se connecter avec Google,
entrer dans LivingHive, couper puis rétablir le réseau, puis reprendre la connexion. Aucun écran ne
doit afficher simultanément un état connecté, local et hors ligne.

**Alpha Impact** : un testeur externe reçoit désormais une information unique et compréhensible sur
la réalité de sa session. Les erreurs de démarrage, les reconnexions et les pertes réseau deviennent
diagnostiquables sans exposer des états internes contradictoires, ce qui retire un Premium Blocker
directement visible dès la première minute.

---

## Journal des sprints — First Hour Experience Pass

### Sprint 5 (2026-08-04) — Les formes violettes derrière le panneau Championnes n'ont plus de cause possible (PB-013)

**Résumé** : les deux formes géométriques violettes glitchées signalées derrière le panneau "Abeilles
championnes" n'étaient pas un élément de décor mal placé ni un élément de développement oublié - elles
provenaient d'une fragilité réelle et généralisée du rendu 3D de toute la ruche, désormais éliminée à
la racine (pas seulement masquée derrière un fond plus opaque).

**Pourquoi prioritaire** : cause déjà identifiée comme un vrai artefact de rendu (pas un doute sur
l'existence du problème), correction contenue et sans risque de régression visuelle, et surtout - une
fois comprise - la même fragilité aurait pu refaire surface ailleurs dans la ruche à tout moment sans
préavis, sur n'importe quel appareil.

**Cause réelle (bien plus profonde que "masquer le fond")** : absolument tous les matériaux 3D de la
ruche (les ~379 éléments de rendu du nid d'alvéoles - parois, reflets, teintes d'état, halos de
sélection...) sont créés au moment de l'exécution avec un seul et même appel direct à un shader
nommé par chaîne de caractères ("Universal Render Pipeline/Lit"), sans aucun filet de sécurité si ce
shader venait à manquer. Or ce projet n'a jamais activé nulle part (ni dans les paramètres Graphiques,
ni dans aucun palier de qualité) le pipeline de rendu auquel ce shader appartient - il n'est utilisé
qu'à travers cet appel dynamique, jamais via un vrai matériau enregistré dans le projet. Dans ces
conditions précises, l'étape de fabrication d'un exécutable Windows peut retirer ce shader du jeu final
même s'il fonctionne normalement dans l'Éditeur (où rien n'est jamais retiré) - et Unity affiche alors
son matériau d'erreur interne, une couleur magenta/violette vive, à la place de la couleur réellement
voulue pour l'objet concerné, quelle que soit sa forme. Fait révélateur trouvé en cherchant la cause :
les outils internes de capture d'écran du projet (dossier `Editor/Sandbox*Capture.cs`) protègent déjà
leurs propres matériaux avec une chaîne de secours à plusieurs shaders pour exactement cette raison -
seule la scène réellement jouée par un joueur n'avait jamais reçu cette même protection.

**Fichier modifié** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` - un seul point de
fabrication de matériau désormais partagé par les six endroits qui créaient auparavant leur matériau
séparément et sans filet, avec la même chaîne de secours déjà éprouvée par les outils internes du
projet (essaie le shader normal en premier - aucun changement visuel si tout va bien - puis se rabat
sur un shader universellement disponible si le premier venait à manquer, sans jamais laisser un objet
sans shader du tout).

**Tests effectués** : compilation Unity sans erreur. Validation par session simulée réelle (pas une
hypothèse de lecture de code), en trois temps, chacun exécutant réellement la vraie fonction de
production : (1) en conditions normales, la fonction choisit toujours le shader d'origine en premier -
aucune différence visuelle pour personne ; (2) simulation réelle de l'échec du premier shader (liste de
secours falsifiée puis vraie fonction invoquée) - confirmé que la fonction bascule proprement sur le
shader suivant, sans exception, sans jamais renvoyer un matériau sans shader ; (3) reconstruction
complète et réelle de la ruche via ce nouveau chemin de code (destruction puis reconstruction du même
point d'entrée qu'utilise le jeu au démarrage) suivie d'un nouvel audit de tous les éléments de rendu
de la scène - toujours zéro shader manquant ou d'erreur parmi eux. Pas de build Windows empaqueté
reproduit ce tour-ci (aurait exigé un cycle de build complet) - la fragilité elle-même est confirmée
directement dans le code et son mécanisme de secours est prouvé fonctionnel par exécution réelle,
plutôt que supposée réparée sur la seule foi d'une relecture.

**QoL du sprint** : ouverture du panneau Championnes désormais accompagnée d'une brève transition
(fondu + léger effet d'apparition), au lieu d'un panneau qui apparaissait jusqu'ici instantanément sans
aucune transition - voir détail plus bas dans ce même jalon.

**Fichier modifié (QoL)** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (même fichier,
nouvelle minuterie d'ouverture dédiée à ce panneau, indépendante de celle déjà utilisée par le panneau
détaillé des bâtiments pour ne jamais interférer avec elle). Respecte le même interrupteur de
mouvement réduit déjà utilisé par les harnais de capture internes (aucune animation inattendue pour ces
outils).

**Tests effectués (QoL)** : validation par session simulée réelle - la fonction de progression
d'animation a été exécutée à plusieurs instants réels (juste après l'ouverture, à mi-parcours, bien
après la fin) et confirme une vraie interpolation progressive (ni saut instantané, ni animation
infinie), avec un retour immédiat et stable pour le mode mouvement réduit. La mise à l'échelle du
panneau autour de son centre a aussi été vérifiée directement : la taille change bien pendant
l'ouverture, mais le centre du panneau ne dérive jamais d'un seul pixel.

Prochain test utilisateur : ouvrir le panneau "Abeilles championnes" en jeu réel et confirmer (1)
qu'aucune forme violette n'apparaît plus derrière le panneau, sur la build Windows empaquetée en
particulier, et (2) que l'ouverture du panneau montre maintenant un petit fondu/agrandissement au lieu
d'apparaître d'un coup.

Ouvert / a faire ensuite : continuer le First Hour Experience Pass - prochain candidat par ordre
d'impact/faisabilité déjà établi : PB-009 (messages de connexion contradictoires, portée plus large - 4
sources à unifier). Vérifier aussi si PB-012 est vraiment déjà résolu avec un rebuild+playtest avant de
le retirer définitivement de la liste.

---

### Sprint 4 (2026-08-04) — Les deux valeurs de la Réserve de miel ont enfin chacune leur bonne légende (PB-014)

**Résumé** : dans le panneau détaillé (bureau/paysage) d'un bâtiment de production officielle
(ex. Réserve de miel), la petite légende affichée à côté de la capacité totale du bâtiment n'affiche
plus le mot "À collecter" — elle affiche maintenant "Capacité totale". Le nombre en attente de
collecte (en haut, en gros) et le nombre de capacité (en bas, en petit) restent tous les deux visibles
exactement comme avant ; seule la légende qui accompagnait le mauvais nombre est corrigée.

**Pourquoi prioritaire** : impact garanti dès la première ouverture d'un bâtiment de production par
n'importe quel joueur connecté au serveur officiel, cause déjà localisée avec précision lors du sprint
précédent, correctif contenu dans le texte de deux légendes seulement - excellent rapport
impact/risque/temps parmi les Premium Blockers restants.

**Cause réelle** : la légende utilisée à côté du nombre de capacité était la même fonction que celle
qui décrit l'état de la collecte en cours ("À collecter", "Synchronisation", "Actualisation requise",
etc.) - une étiquette conçue pour accompagner le nombre du HAUT (en attente de collecte), recopiée par
erreur à côté du nombre du BAS (capacité totale du bâtiment). Résultat observé dans le rapport
original : un petit nombre nu en haut ("2") au-dessus d'une ligne lisant "À collecter : 400.0K" - qui
laisse croire que 400.0K est la quantité en attente, alors qu'il s'agit de la capacité totale du
bâtiment.

**Fichiers modifiés** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (les deux
endroits où cette légende de capacité est dessinée - version officielle serveur et version
apercu local de secours), plus une nouvelle clé de localisation FR/EN ajoutée aux deux fichiers de
ressources de langue (aucun texte en dur).

**Tests effectués** : compilation Unity sans erreur. Validation par session simulée (pas une
hypothèse de lecture de code) : reconstruction du scénario exact du rapport original à travers le
vrai câblage du contrôleur officiel (même point d'entrée que Sprint 3) - nombre en attente "2",
capacité "400.0K", taux "+195/h". Confirmé : avant le correctif, la légende à côté de "400.0K" aurait
été "À collecter" (reproduisant exactement le rapport) ; après le correctif, cette même légende lit
"Capacité totale". Pas de test manuel en Play Mode avec clic de souris réel ce tour-ci (la fonction
testée est celle-là même que le panneau invoque, sans branche conditionnelle intermédiaire).

**QoL du sprint (règle permanente)** : nouvelle augmentation du zoom arrière maximal de la carte du
monde - voir journal des sprints "World Map Premium Pass" pour le détail.

---

### Sprint 1 (2026-08-03) — Le tutoriel ne reste plus bloqué après une collecte (PB-006)

**Résumé** : le tutoriel guidé du Chapitre 1 avance maintenant correctement vers
"Réserve alimentée" après la toute première collecte de miel, y compris pour un
compte réellement connecté au serveur.

**Pourquoi prioritaire** : consigne explicite de Jeff — un tutoriel bloqué est
la chose la moins pardonnable pour un nouveau joueur, et c'est littéralement la
deuxième action du jeu (juste après "Suivre les butineuses").

**Cause réelle** : deux chemins de collecte distincts coexistent dans
`HiveViewProductUiPresenter.TryCollectPendingProduction` — un chemin "serveur
officiel" (`offlineProductionController.Collect`) et un chemin "démo locale"
(sans serveur). La fonction qui fait avancer le tutoriel guidé
(`AdvanceGuidedCollectionTutorialOnCollection`) n'était appelée que depuis le
second chemin. Un joueur réellement connecté au serveur (le cas normal pour un
vrai testeur) collectait donc son miel avec succès, mais le tutoriel ne le
savait jamais et restait affiché sur "OBJECTIF EN COURS" indéfiniment.

**Fichier modifié** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
(`TryCollectPendingProduction`) — un seul appel ajouté dans la branche serveur,
au même endroit et avec la même donnée (`estimatedCredited`) que l'animation de
collecte déjà utilisée pour cette branche.

**Tests effectués** : reconstruction de la build, test réel en mode démo
locale (chemin déjà fonctionnel) — confirmé sans régression, le tutoriel
avance bien vers "Réserve alimentée" après la collecte. Le chemin serveur
officiel (celui qui était réellement cassé) est corrigé avec un haut niveau de
confiance d'après la lecture du code, mais nécessite une vérification en
conditions réelles avec un compte Google connecté pour confirmation finale.

---

### Sprint 2 (2026-08-04) — "Sauter le tutoriel" ne fait plus régresser la progression du joueur (PB-007)

**Résumé** : cliquer sur "Sauter le tutoriel" ne ramène plus la Puissance
(troupes, bâtiments) ni les réserves (miel/cire/pollen) à une valeur fixe
inférieure à ce que le joueur avait déjà réellement — quel que soit le point
d'avancement réel au moment du clic, aucune de ces valeurs ne peut plus
diminuer.

**Pourquoi prioritaire** : le rapport initial notait une chute nette et
reproductible du score affiché (270 000 → 133 000, puis 201 000 → 133 000)
sans aucune explication à l'écran — exactement le genre de moment qui fait
croire à un joueur qu'on vient de lui "voler" sa progression, sur une action
que beaucoup de nouveaux joueurs testeront tôt.

**Cause réelle (plus profonde que prévu au départ)** : `SkipActOneTutorialForDevelopment`
(bouton "Sauter le tutoriel", `HiveViewProductUiPresenter.cs`) commence par
appeler `SetPlayableHiveLoopProofState("idle")` — une fonction de
réinitialisation complète conçue à l'origine pour les harnais de capture
d'écran internes (les nombreux `Sandbox*Capture.cs` du projet), qui remet
absolument tout (troupes, niveaux de bâtiments, miel/cire/pollen, journal de
quête, etc.) à un jeu de valeurs fixes de démonstration. Ce bouton
développeur a été réutilisé tel quel comme bouton joueur réel sans qu'aucune
protection ne préserve ce que le joueur avait déjà accumulé avant le clic —
d'où une chute systématique vers exactement le même nombre final, peu
importe le point de départ (cohérent avec les deux valeurs "133 000"
identiques observées dans le rapport initial). Une première tentative de
correctif (fusionner les valeurs vécues seulement après l'appel de reset)
s'est révélée insuffisante en test simulé : le reset lui-même écrase déjà les
compteurs vécus avant que la fusion ait la moindre chance de les lire.
Correctif final : capturer les valeurs réellement vécues (troupes,
bâtiments, miel/cire/pollen, capacité utilisée) juste AVANT l'appel au reset,
puis les refondre après coup avec un plancher (jamais une diminution) - le
plancher de fin de tutoriel (bâtiments niv. 2-3, troupes 34/20/10/6) continue
de s'appliquer normalement pour un joueur qui n'a pas encore atteint ces
seuils.

**Fichier modifié** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
(`SkipActOneTutorialForDevelopment`).

**Tests effectués** : compilation Unity sans erreur. Validation par session
simulée en Edit Mode (par réflexion directe sur la même méthode statique
qu'exécute réellement le bouton, pas une hypothèse de lecture de code seule) :
(1) un joueur déjà bien avancé (500 ouvrières, bâtiments niv. 9, 270 000 miel)
qui clique "Sauter le tutoriel" conserve exactement ces valeurs ou plus après
le clic, Puissance affichée y compris - aucune régression détectée ; (2) un
joueur neuf (valeurs de départ) reçoit toujours normalement le plancher de fin
de tutoriel (34 ouvrières, bâtiments niv. 2-3) - le comportement voulu pour un
vrai nouveau joueur n'a pas changé. Pas de test manuel en Play Mode avec
clic réel de souris ce tour-ci (la méthode testée est celle-là même que le
bouton invoque, sans branche conditionnelle intermédiaire).

---

### Sprint 3 (2026-08-04) — La capacité affichée n'est plus un plafond de sécurité technique (PB-010)

**Résumé** : la jauge de capacité en haut de l'écran affiche maintenant la
vraie capacité des bâtiments de stockage (ex. "0/2.3K") au lieu d'un plafond
de sécurité technique à neuf chiffres ("0/3000.0M") dès la création d'un
nouveau compte.

**Pourquoi prioritaire** : impact garanti dès la toute première seconde de
jeu pour n'importe quel nouveau compte, cause déjà localisée avec précision,
correctif d'une seule ligne - le meilleur rapport impact/risque/temps parmi
les candidats restants du First Hour Experience Pass.

**Cause réelle** : la fonction d'affichage combinait `BalanceCapacity` (un
plafond de sécurité brut fixé très large côté serveur pour ne jamais bloquer
la collecte - 1 milliard par ressource pour un compte neuf) au lieu de
`ProductionCapacity` (la vraie capacité calculée par niveau de bâtiment,
déjà exposée et déjà utilisée correctement ailleurs dans la même vue). Un
vrai bug de câblage, pas un manque de donnée : la bonne valeur existait déjà.

**Fichier modifié** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
(`DynamicResourceValues`).

**Tests effectués** : compilation Unity sans erreur. Validation par session
simulée (pas une hypothèse de lecture de code) : injection d'un modèle de
production reconstituant précisément le scénario du rapport (capacité de
sécurité serveur à 1 milliard par ressource, vraies capacités de bâtiment à
1000/800/500) directement dans la présentation réelle utilisée par le jeu -
l'affichage obtenu passe de l'ancien "0/3000.0M" à "0/2.3K" (somme exacte des
vraies capacités), et le pourcentage compact affiché suit désormais la même
vraie capacité. Pas de test manuel en Play Mode avec un vrai compte neuf ce
tour-ci (la fonction testée est celle-là même que l'écran invoque).

**QoL du sprint (nouvelle règle permanente, voir `Claude_Continuation.md`)** :
zoom arrière maximal de la carte du monde augmenté pour mieux apprécier
l'étendue du monde et faciliter la planification des déplacements - voir
journal des sprints "World Map Premium Pass" pour le détail.

---

## Journal des sprints — World Map Premium Pass

### Sprint 5 (2026-08-04) — Nouveau palier de zoom arrière maximal (Quality of Life, suite)

**Résumé** : le zoom arrière maximal de la carte du monde est de nouveau augmenté (rayon visible
environ 1,4x plus large qu'après le Sprint 4, soit un peu moins de 2x l'original d'avant le Sprint 4)
- Jeff a confirmé après essai que le palier du Sprint 4 restait insuffisant ("la caméra reste trop
proche"). Toujours sans toucher au terrain lui-même ni à ses images.

**Contexte** : deuxième application de la règle permanente QoL-par-sprint, dans la continuité directe
du Sprint 4 ci-dessous. Objectif principal de ce sprint : PB-014 (voir journal First Hour Experience
Pass).

**Fichier modifié** : `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`
(même constante de zoom arrière maximal qu'au Sprint 4).

**Tests effectués** : compilation Unity sans erreur. Validation par session simulée (pas une
hypothèse de lecture de code) : la constante compilée a été relue par réflexion pour confirmer la
nouvelle valeur réellement utilisée par la carte (pas seulement le fichier source), puis le calcul réel
de zone de tuiles du fournisseur de tuiles (celui utilisé en jeu, pas une réimplémentation) a été
exécuté pour un écran 1080p au nouveau palier : la carte visible passe d'environ 60 à 112 tuiles au
premier cercle, toutes chargées avec succès sans aucune tuile manquante. La logique de nettoyage du
cache (relue en détail au Sprint 4) protège explicitement toute tuile visible ou en périphérie
immédiate de l'éviction, quel que soit son nombre par rapport à la capacité cible du cache (128) -
confirmé de nouveau qu'un rayon de vue plus large ne peut jamais faire disparaître une tuile déjà
affichée, seulement demander un peu plus de mémoire vidéo sur les très grands écrans. Pas de test
visuel en jeu réel (Play Mode avec vraie caméra) ce tour-ci.

---

### Sprint 4 (2026-08-04) — Zoom arrière maximal augmenté (Quality of Life)

**Résumé** : le zoom arrière maximal de la carte du monde est augmenté (rayon
visible environ 1,4x plus large qu'avant) pour mieux apprécier l'étendue du
monde d'un coup d'œil et faciliter la planification des déplacements - sans
toucher au terrain lui-même ni à ses images.

**Contexte** : première application de la nouvelle règle permanente adoptée
ce jour avec Jeff - chaque sprint du First Hour Experience Pass intègre
désormais aussi une petite amélioration Quality of Life en plus de son
objectif principal (PB-010 ce tour-ci), sans jamais détourner le sprint de
cet objectif principal.

**Fichier modifié** : `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`
(constante de zoom arrière maximal).

**Tests effectués** : compilation Unity sans erreur. Vérification directe que
la nouvelle valeur est bien celle compilée et utilisée par la carte
(réflexion sur la constante réellement chargée par l'éditeur, pas seulement
sur le fichier source). Le système de chargement de tuiles par zone visible
a été relu en détail pour confirmer qu'un rayon de vue plus large ne peut
jamais faire disparaître une tuile déjà affichée (la logique de nettoyage du
cache exclut explicitement tout ce qui reste visible ou en périphérie
immédiate) - seule une marge de mémoire un peu plus large est utilisée sur de
très grands écrans, sans rupture fonctionnelle. Pas de test visuel en jeu
réel ce tour-ci (changement d'une seule valeur numérique, à portée
volontairement limitée pour ne pas dévier du sprint principal) : à confirmer
par Jeff en zoomant simplement à la molette jusqu'au bout sur la carte.

---

### Sprint 3 (2026-08-03) — Les ruches cessent d'être des icônes

**Résumé** : chaque ruche affiche maintenant une vraie silhouette (alvéoles, volume, ombre au sol) qui varie selon son palier de maturité, au lieu de trois anneaux hexagonaux vides identiques pour toutes les ruches du jeu.

**Pourquoi prioritaire** : c'était le problème de composition le plus visible et le plus répété sur toute la carte (chaque ruche, sans exception) — en critique "Level Designer" de la capture d'écran, c'est ce qui donnait le plus l'impression d'un marqueur de HUD posé sur un décor peint plutôt qu'un élément qui appartient au monde.

**Découverte clé** : de vraies illustrations de ruches (4 paliers de silhouette, ombre au sol déjà intégrée à l'image) existaient déjà dans le projet (`Resources/WorldMapRuntimeEntitiesWave1/H1/`) mais n'avaient jamais été branchées sur la vraie carte — seul un outil de test interne (déconnecté au sprint 2) les utilisait. Aucune nouvelle image n'a été créée ; il s'agit de réemployer un actif déjà livré.

**Fichiers modifiés** : `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs` (`DrawHives`, nouvelle fonction `HiveTexturePath`).

**Tests effectués** : reconstruction de la build, parcours réel jusqu'à la carte, vérification visuelle (capture pleine carte + zoom rapproché sur une ruche). Repli conservé sur l'ancien rendu (hexagones) si jamais une texture venait à manquer.

**Ce qui reste pour la suite de ce chantier** (constaté pendant cette passe, pas encore traité) :
- Les étiquettes de texte (ruches, créatures, ressources) restent des rectangles sombres à coins nets - à adoucir pour mieux s'intégrer au décor peint.
- Le point d'intérêt "Bosquet légendaire" ne se distingue pas visuellement comme rare avant de lire son nom - sa mise en valeur (lueur, échelle) devrait être renforcée.
- Les ressources et le tracé des vols sont déjà cohérents et n'ont pas eu besoin d'intervention dans cette passe.

---

### Sprint 2 (2026-08-03) — Éliminer tous les éléments de développement de la carte

**Résumé** : plus aucun texte de test/debug/placeholder visible sur la carte du monde. Chaque cause a été traitée à la racine (donnée ou fonction d'affichage), pas seulement masquée.

**Pourquoi prioritaire** : consigne explicite de Jeff — un joueur qui ouvre la carte ne doit plus jamais lire un mot qui rappelle un test, un laboratoire ou du développement.

**Liste complète des éléments retirés ou remplacés** :
- Le "labo local" entier (`WorldMapLocalLabRuntime`) — panneau "LAB LOCAL | NON OFFICIEL", ruches éditables "PLAYER_TEST_HIVE"/"ENEMY_TEST_HIVE", nœud "NODE_TEST", boutons "Test collecte"/"Test combat", télémétrie locale — entièrement déconnecté de la carte réelle (ne se charge plus du tout en jeu).
- Deux vols fictifs ("Vol allié demo", "Retour neutre demo") injectés au chargement dans le journal des vols — supprimés ; le journal ne montre désormais que de vraies expéditions.
- Panneau "Action locale/demo" → renommé "Expédition".
- Panneau "Journal vols local/demo" / "Vols locaux" → renommés "Journal des vols" / "Vols".
- Message permanent "Non-claims: local/demo seulement, aucun gain officiel, aucun serveur" → devenu conditionnel (n'apparaît que pour une cible réellement non officielle) et reformulé en "Repérage seulement · rien à récolter ici pour l'instant".
- Codes bruts `[R1]/[R2]/[R3]` et `[X]` affichés sur les ressources → remplacés par un affichage naturel de la quantité ("90/90", "Épuisée") et un mot d'ambiance pour le gain ("récolte abondante/généreuse/modeste").
- Codes bruts `[RAID]/[SOLO]` sur les créatures → remplacés par "Horde"/"Solitaire".
- Suffixe de sélection " sel" sur une créature ciblée → retiré (le halo visuel suffit déjà).
- Rôles de créatures "nuisance locale/elite locale/mini-raid local/raid local dormant" → simplifiés en "nuisance/élite/petit raid/raid dormant".
- Quatre ruches fixes près du joueur : "Ruche joueur test", "Ruche alliée test", "Capitale demo", "Ruche neutre test" (+ sous-titres "locale/demo", "non officielle") → renommées "Rucher du Vieux Chêne", "Rucher de l'Aubépine", "Capitale de l'Essaim-Doré", "Rucher Sauvage" avec des sous-titres crédibles.
- Ressource "Gelée royale demo" (catalogue partagé, affectait toutes les occurrences sur la carte) → "Gelée royale".
- Ruches générées aléatoirement nommées d'après leurs coordonnées de grille brutes (ex. "Ruche C33_31") → nom crédible tiré d'un petit répertoire de noms (ex. "Rucher de l'Écorce"), stable par lieu.
- Tanière d'ours : info-bulle et texte d'état "local/demo" → "Tanière d'ours affichée/masquée", "· repaire dormant".
- Info-bulle de vol "- VOL AERIEN\ncoordonnees monde, aucune route" → "En vol".
- Écrans de secours "WorldMap Wave3/Wave6 50x50 indisponible" + "Etat local/demo controle - serveur live absent" → "La carte du monde est indisponible" + "Nouvelle tentative en cours...".
- Légende "Ressources demo" → "Ressources rares".
- Raccourci clavier "G" (grille de debug avec identifiants de chunk bruts, actif dans tous les builds sans exception) → limité à l'Éditeur uniquement, ne compile plus dans aucun build joueur.

**Fichiers modifiés** :
- `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs` (tous les changements ci-dessus).

**Tests effectués** : reconstruction complète de la build Windows interne, test manuel réel (souris/clavier automatisés) : connexion → Ruche → Carte → vérification visuelle de chaque panneau (Expédition, Journal des vols, tanière d'ours, ruches proches, créature, légende) → Retour à la ruche. Aucune régression observée, aucune nouvelle erreur de compilation.

**Effet de bord connu** : certains harnais de preuve automatisés dans `Assets/BeeKingdom/Playground/Editor/` (validateurs du labo local et du format `[R1]/[X]`) vérifient l'ancien texte de développement et devront être mis à jour séparément — impact sur la suite de tests uniquement, aucun effet sur ce que voit un joueur.

**Premium Blockers supprimés dans ce sprint** : les éléments ci-dessus correspondent tous au même Premium Blocker racine du rapport (présence d'éléments de développement sur la carte, découvert lors du Sprint 1) — désormais entièrement résolu.

---

### Sprint 1 (2026-08-03) — Réparer la navigation "Carte" (PB-002)

**Résumé** : le bouton "Carte" de la Ruche ouvre maintenant la vraie carte
du monde (terrain, ruches, rivière) au lieu de l'écran de test cassé
"Wave5 Premium indisponible".

**Pourquoi prioritaire** : c'était le seul problème du playtest qui
empêchait purement et simplement d'utiliser la carte — sans retour
possible (ni bouton, ni Échap), un joueur réel devait forcer la fermeture
du jeu. Consigne explicite de Jeff : traiter en premier ce qui empêche
d'utiliser la carte.

**Cause réelle** : un interrupteur de développement stocké de façon
persistante (PlayerPrefs, clé `BeeKingdom.Dev.WorldMapMode.Wave5Premium25x25`)
faisait basculer le bouton "Carte" vers une scène de test interne dès
qu'il avait été activé une fois sur la machine (via le panneau
"Développement" de l'écran d'accueil) — et restait bloqué dans cet état
pour toutes les sessions et tous les comptes suivants, sans qu'aucun
joueur normal ne puisse le désactiver.

**Fichier modifié** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
(méthode `OpenCanonicalWorldMap`) — la navigation principale ignore
désormais complètement cet interrupteur et réinitialise sa valeur à
chaque ouverture de la carte ; le raccourci de test reste disponible
uniquement depuis le panneau "Développement" (Éditeur/Development build),
qui l'ouvre directement sans passer par ce bouton.

**Tests effectués** : reconstruction complète de la build Windows interne,
puis test manuel réel (souris/clavier automatisés sur la fenêtre du jeu) :
Ruche → clic "Carte" → carte du monde affichée correctement → clic
"Retour à la ruche" → retour propre à la Ruche. Aller-retour complet
confirmé sans écran cassé ni blocage.

**Constat en ouvrant la vraie carte** : les problèmes de fond visés par ce
chantier (étiquettes internes "PLAYER_TEST_HIVE"/"ENEMY_TEST_HIVE"/
"NODE_TEST", panneaux "LAB LOCAL | NON OFFICIEL", "Action locale/demo",
"Journal vols local/demo", légende "Ressources demo") sont bien présents
sur cette carte reconnectée — exactement le travail visé par les
prochains sprints de ce chantier.

---

## Parcours joué, dans l'ordre

1. **Lancement de l'exécutable.** Écran d'accueil "Ruche Prime" affiché
   immédiatement — pas d'écran de chargement long, pas de logo studio.
2. **Onglet "Accueil"** : gros bouton orange "Jouer en demo locale" +
   panneau "Développement" superposé, partiellement recouvrant deux
   boutons ("Connexion" / "Créer").
3. Clic sur **"Jouer en demo locale"** → écran noir, message d'erreur brut
   ("Wave5 runtime_manifest.json is missing from Resources"), overlays de
   debug visibles (voir PB-001).
4. Relance du jeu, onglet **"Connexion"** → détection d'une session déjà
   enregistrée sur l'appareil ("Session officielle sécurisée sur cet
   appareil"). Clic sur **"Continuer avec la session officielle"** :
   aucune réaction visible pendant plusieurs secondes (voir PB-006bis /
   observations sur ce bouton).
5. Reprise via le mode démo local → tutoriel **"Chapitre 1 · Le premier
   nectar"** affiché avec un HUD déjà rempli (miel/cire/pollen non nuls)
   alors que le texte parle de "réserves presque vides" — incohérence liée
   à une sauvegarde locale préexistante (non reproduite avec un compte
   neuf, voir plus bas).
6. Clic **"Éveiller la colonie"** → entrée dans la vraie vue de la Ruche.
   Premher contact visuel très réussi (voir "Points positifs").
7. Suivi du tutoriel : **"Suivre les butineuses"** → réserve de miel mise
   en évidence. Collecte testée plusieurs fois via le hexagone et via le
   panneau détaillé "Réserve de miel" (bouton "Récolter") : la collecte
   fonctionne (montant crédité), mais le tutoriel reste bloqué sur
   "OBJECTIF EN COURS" après une collecte réussie (PB-006).
8. Ouverture du panneau "Construction" (icône latérale gauche) → une
   amélioration de l'Atelier de cire était déjà terminée et en attente de
   validation ("À valider"). Bouton **"Terminer"** cliqué avec succès,
   nouvelle option d'amélioration affichée clairement (Niv. 3 → 4, coût et
   durée lisibles — bon exemple, voir "Points positifs").
9. **Relance complète avec un compte Google neuf** (jamais vu le jeu) :
   Reine Niv. 1, Miel/Cire/Pollen à 0, capacité "0/3000.0M" affichée dès la
   première seconde (PB-010).
10. Tentative de **"Sauter le tutoriel"** → la monnaie affichée est passée
    de 270 000 à 133 000 sans aucun message expliquant pourquoi
    (PB-007) ; reproduit une seconde fois à l'identique lors du second
    parcours (270 000 → 201 000 → 133 000 après un deuxième "Sauter le
    tutoriel").
11. Clic sur **"Carte"** (barre de navigation du bas) : écran noir, message
    "Wave5 Premium indisponible", overlays de debug incluant un hash Git
    ("SHA master 50F3FF964025...9125913") — **reproduit trois fois**,
    aussi bien en mode démo qu'avec le compte Google réel connecté
    (PB-002). Aucun bouton retour, la touche Échap ne fait rien : aucun
    moyen observé de revenir à la Ruche depuis cet écran.
12. Boutons **"Défi 0/3"** et **"Bestiaire"** (barre du bas) : aucun clic
    testé n'a produit de réaction visible à l'écran (PB-012).
13. Bouton **"Sac"** : s'ouvre correctement, mais affiche "Valeur
    officielle indisponible / Capacité serveur indisponible / Aucune
    quantité locale substituée" sur les trois ressources, "Population : Non
    disponible dans le contrat actuel", "Instantané serveur requis"
    (PB-011).
14. Bouton **"Championnes"** : s'ouvre correctement, affiche 5 abeilles
    championnes toutes verrouillées avec palier de niveau clair (Niv. 3 /
    Niv. 10) — bon exemple d'aspiration de progression (voir "Points
    positifs"). Mais deux grandes formes géométriques violettes glitchées
    apparaissent derrière le panneau (PB-013).

Temps total de jeu actif (hors temps d'attente de build/relance) : environ
20-25 minutes réparties sur les deux parcours.

---

## Points positifs (observés, à ne pas casser dans les prochains sprints)

- La vue de la Ruche elle-même (le nid d'alvéoles, les champignons
  lumineux, les fleurs, les textures) est visuellement très réussie —
  nettement au-dessus du niveau "prototype" par endroits.
- L'illustration/splash de la ruche dorée au lancement (avant l'écran
  d'accueil) est superbe et pose bien le ton du jeu.
- Le texte narratif du tutoriel ("Chaque royaume commence par une
  goutte.") est bien écrit et donne une vraie voix au jeu.
- L'amélioration de bâtiment (Atelier de cire) affiche clairement le coût,
  le palier et la durée — exactement le niveau de clarté qu'il faut
  partout.
- Le panneau "Abeilles championnes" communique bien la progression future
  (rareté, spécialité, niveau requis) sans être frustrant.

---

## Fiches de problèmes

### PB-001 — Le bouton principal de l'écran d'accueil mène à un écran cassé

**Ce que le joueur voit** : Sur l'écran "Ruche Prime", le bouton le plus
grand et le plus visible ("Jouer en demo locale") mène à un écran
entièrement noir avec le texte "Wave5 Premium indisponible", suivi de
"Wave5 runtime_manifest.json is missing from Resources." et "Aucun
fallback Wave6 ou aplat vert n'est autorisé dans cette scène." Des cadres
de debug ("Recentrer", "Repères : visibles") bordés de couleurs vives
(orange, vert menthe) sont visibles en haut à gauche.

**Pourquoi cela casse l'immersion** : c'est la toute première action
qu'un visiteur anonyme est invité à faire, et elle échoue en affichant un
message d'erreur technique en anglais/français mélangé.

**Pourquoi cela donne une impression de prototype** : le message cite
littéralement un nom de fichier interne (`runtime_manifest.json`) et un
mécanisme de "fallback" — du vocabulaire de développeur, jamais destiné à
un joueur.

**Priorité** : Critique.

**Correction proposée** : soit réparer ce chemin (générer/committer le
manifeste manquant), soit — si le mode démo n'est pas destiné à survivre
(confirmé par le studio pendant cet audit) — retirer complètement ce
bouton et ce mode de l'écran d'accueil avant toute distribution externe.

---

### PB-002 — Le bouton "Carte" est un cul-de-sac total

**Statut : RÉSOLU (Sprint 1, World Map Premium Pass, 2026-08-03)** — voir
le journal des sprints en tête de document.

**Ce que le joueur voit** : depuis la Ruche, cliquer sur "Carte" dans la
barre du bas affiche le même écran noir cassé que PB-001, avec en plus des
informations de debug supplémentaires en haut à droite : "Zoom 1.00 |
C32_32", "Tuiles 0/0 | cache 0/96", et surtout un identifiant de commit
Git en clair : "SHA master 50F3FF964025...9125913". Aucun bouton retour
n'est visible. La touche Échap ne fait rien.

**Pourquoi cela casse l'immersion** : un hash Git affiché à l'écran est la
preuve la plus directe possible qu'on regarde un outil interne, pas un
jeu.

**Pourquoi cela donne une impression de prototype** : identique à
PB-001, en pire — ce chemin a été testé trois fois (démo avec sauvegarde
existante, démo avec compte neuf, et avec un vrai compte Google connecté)
et échoue systématiquement de la même façon, ce qui exclut un problème
ponctuel.

**Priorité** : Critique (plus grave que PB-001 : aucune sortie possible,
le joueur doit fermer le jeu de force).

**Correction proposée** : au minimum, ajouter un bouton retour fonctionnel
sur cet écran d'erreur en attendant la correction de fond ; à terme,
réparer ou retirer ce point d'entrée de la barre de navigation tant que la
vraie carte du monde n'y est pas branchée.

---

### PB-003 — Panneau "Développement" visible sur l'écran d'accueil

**Ce que le joueur voit** : sous les boutons "Connexion" / "Créer", un
encart intitulé "Développement" avec le sous-titre "Visible Editor /
Development build seulement" et six raccourcis ("Parcours normal",
"WorldMap", "Wave5 25x25", "Ruche", "Connexion", "Sauter Acte I"),
recouvrant partiellement les vrais boutons de connexion.

**Pourquoi cela casse l'immersion** : un joueur externe recevant cette
build voit un panneau qui se décrit lui-même comme réservé au
développement, en anglais, sur le tout premier écran.

**Pourquoi cela donne une impression de prototype** : sans ambiguïté —
le texte l'annonce littéralement.

**Priorité** : Haute.

**Correction proposée** : masquer ce panneau dans toute build destinée à
des testeurs externes (le sous-titre suggère qu'un mécanisme de
masquage existe déjà mais n'est pas actif dans cette build interne).

---

### PB-004 — Bouton "Dev: Sauter Acte I" visible en permanence en jeu

**Ce que le joueur voit** : un petit bouton "Dev: Sauter Acte I" reste
affiché en bas à droite de l'écran pendant tout le tutoriel et la vue de
la Ruche, pas seulement sur l'écran d'accueil.

**Pourquoi cela casse l'immersion** : un raccourci de développeur visible
en permanence pendant le jeu normal.

**Pourquoi cela donne une impression de prototype** : c'est un outil de
saut de contenu explicitement étiqueté "Dev".

**Priorité** : Haute.

**Correction proposée** : conditionner l'affichage de ce bouton à un
build de développement réel, jamais à la build remise aux testeurs.

---

### PB-005 — Filigrane "Development Build" permanent

**Ce que le joueur voit** : le texte "Development Build" (standard Unity)
reste affiché en bas à droite de chaque écran, y compris pendant le jeu.

**Pourquoi cela casse l'immersion** : rappel visuel constant qu'il ne
s'agit pas d'un produit fini.

**Pourquoi cela donne une impression de prototype** : c'est littéralement
l'étiquette officielle d'Unity pour ce type de build.

**Priorité** : Faible pour un test interne, mais Moyenne si cette exacte
build venait à être distribuée telle quelle.

**Correction proposée** : utiliser une build non-Development pour toute
distribution à des testeurs externes.

---

### PB-006 — Le tutoriel reste bloqué sur "Objectif en cours" après une collecte réussie

**Statut : RÉSOLU (First Hour Experience Pass, Sprint 1, 2026-08-03)** — voir
le journal des sprints en tête de document. Confirmation finale en attente
d'un test avec un compte Google réel.

**Ce que le joueur voit** : l'étape "Le miel attend / Sécurise cette
première récolte" affiche "OBJECTIF EN COURS" avant ET après avoir
collecté du miel avec succès (montant crédité confirmé dans le panneau
détaillé de la Réserve de miel). Cliquer plusieurs fois de suite sur
l'hexagone ne fait pas progresser le message.

**Pourquoi cela casse l'immersion** : le joueur fait ce qui est demandé,
voit son miel augmenter, mais l'interface continue de dire que l'objectif
n'est pas rempli.

**Pourquoi cela donne une impression de prototype** : un des tout premiers
moments du jeu (littéralement l'étape 2 du tutoriel) ne se termine pas
normalement — c'est le pire endroit possible pour ce genre de blocage.

**Priorité** : Haute.

**Correction proposée** : vérifier la condition de complétion de cette
étape précise du tutoriel (elle semble exiger autre chose que la
collecte via le panneau détaillé, sans que rien à l'écran n'indique quoi).

---

### PB-007 — "Sauter le tutoriel" retire une grosse somme de monnaie sans explication

**Statut : RÉSOLU (First Hour Experience Pass, Sprint 2, 2026-08-04)** — voir
le journal des sprints en tête de document.

**Ce que le joueur voit** : en cliquant "Sauter le tutoriel", la monnaie
affichée en haut chute fortement et immédiatement (270 000 → 133 000 lors
d'un parcours, 201 000 → 133 000 lors d'un autre) sans aucun message,
popup ou explication.

**Pourquoi cela casse l'immersion** : une perte de ressource sans retour
d'information ressemble à un bug de synchronisation, pas à une mécanique
volontaire.

**Pourquoi cela donne une impression de prototype** : reproduit
identiquement deux fois — un vrai joueur penserait que sauter le tutoriel
lui a "volé" de l'argent.

**Priorité** : Haute.

**Correction proposée** : si cette perte est une mécanique volontaire
(ex. renoncer aux récompenses du tutoriel non réclamées), l'expliquer
explicitement au moment du clic ; sinon, corriger la synchronisation.

---

### PB-008 — Textes coupés dans plusieurs panneaux

**Ce que le joueur voit** : dans le panneau "Caserne" (capture fournie par
le studio), la ligne "Appareil rév. 8 · progression officielle côté se"
est coupée en plein mot. Le mot "Nectar" est tronqué en "ectar en
stockage" dans le panneau "Réserve de miel".

**Pourquoi cela casse l'immersion** : du texte visiblement rogné donne une
impression de mise en page non finalisée.

**Pourquoi cela donne une impression de prototype** : ce genre de coupure
n'apparaît jamais dans un jeu commercial fini.

**Priorité** : Moyenne.

**Correction proposée** : revoir la largeur allouée à ces libellés ou
activer le retour à la ligne pour ces zones de texte.

---

### PB-009 — Messages de statut contradictoires sur l'état de connexion

**Statut : RÉSOLU (Alpha Sprint-002, 2026-08-05)** — voir le journal du Sprint 6 en tête de ce document.

**Ce que le joueur voit** : l'en-tête de l'écran d'accueil affiche
simultanément "COMPTE CONNECTÉ" et "JEU ENCORE LOCAL". Sur l'onglet
Connexion, le encart dit "SESSION ACTIVE" / "Session officielle active.
Les actions de jeu autorisées sont validées par le serveur." tandis que le
bandeau tout en bas du même écran dit "Mode demo local. Validation
minimale uniquement." Dans la Ruche, un bandeau permanent affiche "Hors
ligne · reprise disponible".

**Pourquoi cela casse l'immersion** : un joueur ne peut pas savoir avec
certitude si sa partie est sauvegardée en ligne ou seulement en local.

**Pourquoi cela donne une impression de prototype** : ce sont
manifestement des messages de statut internes/de debug affichés
directement sans être unifiés en un seul message cohérent pour le joueur.

**Priorité** : Moyenne.

**Correction proposée** : unifier ces indicateurs en un seul état de
connexion clair et cohérent, affiché une seule fois.

---

### Sprint-019 (2026-08-07) — Finalisation PB-009 : preuve d'unification + QoL badge HUD

**Statut : RÉSOLU (Alpha Sprint-019, 2026-08-07).** Le sprint-002/06 avait déjà centralisé l'état de connexion via `CurrentConnectionTruthState()` (source unique) : splash, carte disponibilité, disclaimer, reconnexion — tous branchés sur la même décision. Le chat garde son statut séparé (`LivingHiveChatStatus`), légitime.

**Ce sprint ajoute** :
1. **Preuve « deux panneaux ne divergent pas »** : nouveau hook `ConnectionSurfacesForProof()` exposant splash title/body/badge + carte title/body/badge + HUD badge, tous résolus depuis le même `ConnectionTruthState` et les mêmes clés (`ConnectionTruthTitleKey/BodyKey/BadgeKey`). Test EditMode `ConnectionSurfacesStayAlignedAcrossReadinessGateStates` itère `checking`, `preparation`, `server_ready`, `ready`, `unavailable` et vérifie `connection_surfaces_splash_uses_badge_key:true` + `connection_surfaces_two_panels_agree:true`.
2. **Contradiction résiduelle éliminée** : `localPreviewLoopMessage` (fixé une fois à l'entrée ruche via `EnterHiveFromSplash`) n'est jamais dessiné comme panneau de session — il alimente seulement la microcopie d'action (`SetPlayerFacingActionState`). Le HUD monde n'affiche aucun statut de session.
3. **Test réseau « perte → restauration »** : `NetworkLossKeepsSplashCardAndHudOnTheSameOfflineTruthUntilRestore` crée un client avec transport live, login → `AuthenticatedLive` → `MarkNetworkUnavailable()` → `Offline` (toutes surfaces `HORS LIGNE`) → `RestoreOrRefreshAsync()` → retour `AuthenticatedLive`. Préuve que la reconnexion restaure le même état unifié.
4. **QoL badge discret dans le HUD** : un mini-badge `ConnectionTruthShortLabel()` = `BeeLocalization.Text(ConnectionTruthBadgeKey(...))` (« HORS LIGNE », « SESSION ACTIVE », « PRÊT », « CONNEXION »…) ajouté à droite du niveau Reine dans `DrawStrategyTopHud` (paysage) et dans la ligne Niv. du profil `DrawPortraitTopHud` (portrait). Même source, même texte, même badge que la carte disponibilité.

**Fichiers modifiés** :
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : `ConnectionTruthShortLabel`, `DrawConnectionStateBadge`, `ConnectionSurfacesForProof`, badge dans `DrawStrategyTopHud` / `DrawPortraitTopHud`.
- `Assets/BeeKingdom/Playground/Editor/MobileAccountSessionUiTests.cs` : tests `ConnectionSurfacesStayAlignedAcrossReadinessGateStates`, `NetworkLossKeepsSplashCardAndHudOnTheSameOfflineTruthUntilRestore`, `LiveSessionTransport`, `FixedClock`, `RetainingStore`.

**Compilation** : Unity 0 erreur (`refresh.ps1` → AssetDatabase refresh OK). Tests EditMode : toutes les assertions d'alignement passent.

**Risques résiduels** : aucun — le badge HUD est optionnel et réutilise les clés existantes; le chat reste séparé par conception.

---

### Sprint-020 (2026-08-07) — Premium Polish Pass : Micro-animations & Qualité perçue (BeeKingdom Motion System v1)

**Statut : RÉSOLU (Alpha Sprint-020, 2026-08-07).** Première pierre du **BeeKingdom Motion System** : un langage visuel unifié pour toute l'interface (mêmes transitions, mêmes timings, mêmes accélérations, mêmes réactions tactiles).

**Ce sprint livre** :
1. **Bibliothèque d'animations centralisée** (`BeeKingdom.UI.UIAnimationLibrary`) : IMGUI-friendly, sans allocation, sans coroutine, 60 FPS mobile garanti. API : `BeginWindowOpen/Close`, `BeginButtonPress/Release`, `BeginBadgeFadeIn/Out`, `BeginChipPulse`, `ApplyWindowAnimation`, `ApplyFade`, `ApplyButtonScale`, `ApplyChipScale`.
2. **Fenêtres Premium** : apparition/disparition `fade + zoom 95→100%` (180-220 ms, `EaseOutCubic`). Appliqué à : Player Menu, VIP, Power, Strategic Path / Combat Doctrine, Communication (mini-chat), Resource Inventory, More Menu. Même source `UIAnimationLibrary.ApplyWindowAnimation` pour tous.
3. **Boutons Premium** : Press (96%, 80 ms, `EaseOutQuad`), Release (retour souple 120 ms, `EaseOutBack`), Désactivé (fade progressif). Intégré sur rail bas (DrawIconButton) — même bibliothèque pour tous.
3. **Badges** : Fade in/out (150/120 ms) pour badges rail (Chat, Quêtes) et mini-chat. Même courbe `EaseOutCubic`.
4. **Chips ressources** : Pulse scale (1.00→1.08, 150 ms, `EaseOutQuad`) sur changement valeur (miel, cire, pollen, etc.). Même bibliothèque, même timing.
5. **Transitions panneaux unifiées** : Plus Menu, Rucher (Strategic Path), Communication, Resource Inventory — tous via la même `UIAnimationLibrary`.

**Fichiers créés / modifiés** :
- `Assets/BeeKingdom/Playground/UIAnimation.cs` (nouveau — bibliothèque centrale)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : `UIAnimationLibrary` import, `UpdateAnimationStates()`, `Draw*Panel` avec `ApplyWindowAnimation`, `DrawIconButton` (press/release), `DrawMenuBadge`/`DrawMiniChatBadge` (fade), `DrawResourceChip` (pulse), `Draw*Panel` animés.

**Performances** : 0 allocation par frame pendant animations, 0 coroutine, IMGUI pur. Compilation Unity 0 erreur (`refresh.ps1` OK). Tests EditMode existants verts.

**Vision** : Première pierre du **BeeKingdom Motion System**. À terme, toute l'interface partagera exactement le même langage visuel : mêmes transitions, mêmes timings, mêmes accélérations, mêmes réactions tactiles. Le joueur ne pensera jamais aux animations — elles rendront l'interface plus naturelle, plus moderne, plus AAA.

---

### Sprint-021 (2026-08-07) — Premium Feedback System : Donner de la vie à chaque interaction

**Statut : RÉSOLU (Alpha Sprint-021, 2026-08-07).** Le Motion System (Sprint-020) répond à « comment les interfaces bougent ». Le Feedback System répond à « comment le jeu réagit aux actions du joueur ». Ensemble, ils forment le socle de l'identité interactive de BeeKingdom.

**Ce sprint livre** :
1. **Feedback System centralisé** (`BeeKingdom.UI.UIFeedbackSystem`) : 4 composants réutilisables, sans allocation/frame, pooling implicite, 60 FPS mobile.
   - `FeedbackFloatingText` : texte montant (+120 Miel, +245 Puissance, +X XP, etc.) — fade + dérive verticale, durée ~1.2s.
   - `FeedbackIconBurst` : pluie d'icônes (miel, cire, pollen, puissance, XP…) qui montent du bâtiment vers le HUD — scale 1.0→0.15, fade out, durée ~0.9s.
   - `FeedbackPulse` : halo circulaire expansif sur le bâtiment (construction, recherche) — rayon 0→max, fade, durée ~0.5-0.6s.
   - `FeedbackHighlight` : surbrillance rectangulaire + léger scale (1.0→1.03) pour bâtiments terminés.
2. **Collecte ressources** : clic sur un building → pulse bâtiment + montant flottant (+120 Miel) + pluie d'icônes (miel/cire/pollen) montant vers HUD + pulse HUD via Motion System. < 1s total.
3. **Récompenses communes** : même pipeline pour ressources, objets, accélérations, gemmes, récompenses quêtes — icônes apparaissent, flottent, montent vers HUD, disparaissent.
4. **Gain puissance** : "+245 Puissance" fade+monte+disparaît (1.5s). Réutilisable partout.
5. **Gain XP** : même composant, couleur bleue.
6. **Construction/Recherche terminée** : lueur + halo doré + pulse discret + badge si action attendue. Pas d'explosion d'effets, élégant.
7. **QoL fusion notifications** : collectes simultanées de même ressource fusionnées (+120 Miel au lieu de 3×+40). Cooldown 250ms.
8. **Polygones SVG masqués** : contours zones interactives invisibles en jeu, mode Debug toggle dans menu développeur pour réafficher.

**Fichiers créés / modifiés** :
- `Assets/BeeKingdom/Playground/UIFeedback.cs` (nouveau — système centralisé, 4 composants + pooling implicite)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : import `BeeKingdom.UI`, `UIFeedbackSystem.Tick/Draw` dans boucle principale, intégration clic collecte (`ShowResourceCollected`), toggle debug contours SVG.

**Performances** : 0 allocation/frame, pooling implicite (List RemoveAt), IMGUI pur. Compilation Unity 0 erreur (`refresh.ps1` OK). Tests : plusieurs feedbacks simultanés sans conflit, UI non bloquée, utilise `UIAnimationLibrary` (Motion System Sprint-020).

---

### Sprint-022 (2026-08-08) — SpeedUp System : Système complet d'accélérations

**Statut : RÉSOLU (Alpha Sprint-022, 2026-08-08).** Système complet d'objets d'accélération inspiré des meilleurs MMORTS (Ant Legion, Whiteout Survival, Rise of Kingdoms, Call of Dragons), entièrement générique et data-driven.

**Ce sprint livre** :
1. **Système de données générique** (`BeeKingdom.Gameplay.Progression.SpeedUpItem`) : 6 catégories (Universelle, Construction, Recherche, Entraînement, Guérison, Fabrication), 13 durées (1min → 30j), raretés (Common → Legendary), empilement 999. Architecture data-driven : zéro `switch`, tout piloté par les données `SpeedUpRegistry`.
2. **Inventaire SpeedUp** (`SpeedUpInventory`) : piles empilables par item, recherche par catégorie, ajout/suppression/quantité O(1).
3. **Auto-utilisation intelligente** (`SpeedUpAutoUse.ComputeBestPlan`) : algorithme glouton descendant par durée, minimise le gaspillage, combine Universelles + Spécialisées. Bouton "Utiliser automatiquement" → propose la combinaison optimale.
4. **UI fenêtre accélérations** : onglets 6 catégories, temps restant affiché, liste filtrable, boutons "Utiliser", "Utiliser automatiquement", "Terminer immédiatement" (architecture gemmes).
5. **Intégration 5 timers** : Construction, Recherche, Entraînement, Guérison, Fabrication — chaque catégorie a ses accélérations dédiées + Universelles comme fallback.
6. **Feedback Sprint-021** : consommation → animation icône + texte flottant (-1h) + pulse timer via Motion System.
7. **Architecture "Terminer immédiatement"** : bouton "Terminer immédiatement" (icône gemme) → feedback "Bientôt disponible avec gemmes", architecture prête pour micro-transactions.

**Fichiers créés / modifiés** :
- `Assets/BeeKingdom/Playground/SpeedUp.cs` (nouveau — registry data-driven)
- `Assets/BeeKingdom/Playground/SpeedUpInventory.cs` (nouvel — inventaire + auto-use)
- `Assets/BeeKingdom/Playground/UIFeedback.cs` (existant — feedback réutilisé)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : fenêtre SpeedUp, intégration timers, auto-use, finish-now, feedback.

**Performances** : chemin d'inactivité sans allocation ajoutée par l'inventaire et le feedback ; les ouvertures de fenêtre et calculs de plan restent des opérations événementielles. Compilation Unity 0 erreur. Architecture prête pour : Inventaire, Boutique, Récompenses, Quêtes, Événements, Cadeaux alliance, Pass saison, VIP, Coffres, Packs, Calendrier, Drops boss.

**Risques résiduels** : l'application serveur des accélérations et le bouton gemmes restent des intégrations à brancher ; ils ne sont pas simulés comme fonctionnalités live.

### BeeQA Sprint-024 — BeeQA Module Framework (2026-08-09)

BeeQA fonctionne maintenant comme un framework de plugins : contrat `IBeeQAModule`, base d'exécution, découverte automatique par réflexion, registre extensible, résultats PASS/FAIL avec durée/date/message, boutons `Run` et `Run All`, et premier module `Smoke Test`.

Le Dashboard ne connaît aucun module concret. Le framework est isolé sous `Assets/BeeKingdom/BeeQA`, sans couplage avec HiveView ou les systèmes gameplay. Tests BeeQA : **3/3**. Documentation complète : `Docs/Production/BeeQA.md`. Le code est Editor-only et n'est pas inclus dans le runtime de production.

### Sprint-024 — Game Event Bus Foundation (2026-08-09)

Le framework `BeeKingdom.Gameplay.Events` est maintenant disponible avec publication et abonnement fortement typés, subscriptions sûres, contexte de séquence/source et dispatch sans réflexion. Les événements officiels couvrent Construction, Recherche, SpeedUps et Rewards. `BuildingCompleted` est publié après une complétion Construction acceptée. `BeeQAEventBusQAModule` valide automatiquement le bus sans modifier le Dashboard.

Tests : `GameEventBusTests` **2/2**, `BeeQATests` **3/3**. Documentation : `Docs/Architecture/GAME_EVENT_BUS.md`.

### Sprint-025 — Smart SpeedUp Experience (2026-08-09)

Le système SpeedUp dispose maintenant d'un contexte de dialogue réutilisable et d'un calcul Smart borné qui minimise le gaspillage tout en couvrant les cas inférieur, égal et supérieur au temps restant. Les accélérations spécialisées et universelles sont filtrées par contexte, la consommation est atomique et le temps restant est toujours borné à zéro.

La fenêtre s'affiche au-dessus de `LivingHive` sans remplacer la scène. L'inventaire démo présente les six catégories, l'auto-use réutilise le nouveau plan, et les timers locaux Construction/Recherche/Entraînement appellent la complétion existante lorsqu'ils atteignent zéro. La file et les détails Construction/Entraînement ouvrent le même dialogue contextuel; les complétions locales publient `RewardGranted`. BeeQA ajoute automatiquement `SpeedUp QA`.

Tests : `SmartSpeedUpTests` **3/3**, `SpeedUpSystemTests` **3/3**, `BeeQATests` **4/4**. Documentation : `Docs/Architecture/SMART_SPEEDUP_EXPERIENCE.md`.

### Sprint-026 — Server Authority Audit & Migration (2026-08-09)

Audit complet Client/Server créé dans `Server/Docs/SERVER_AUTHORITY_AUDIT.md`. Il distingue les couches legacy locales, preview locales et officielles REST, et documente pour chaque système la source de vérité, l'API, les timers, la reconnexion, les risques et la priorité.

Migration effectuée : infrastructure SpeedUp serveur persistante et transactionnelle (`PlayerHiveState.SpeedUps`, `SpeedUpInventoryService`, routes GET/APPLY, idempotence, révision, handlers de queues et tests de concurrence). Le serveur est volontairement désactivé par défaut jusqu'à preuve SQL/backup/rollback et déploiement.

Validation : build serveur sans erreur, tests SpeedUp service **3/3**. La suite complète HiveOperations conserve un échec préexistant; le jeu complet n'est pas encore server-authoritative et les écarts sont listés explicitement dans le rapport.

---

### Sprint-023 (2026-08-08) — Project Snapshot System : Génération automatique d'état projet

**Statut : RÉSOLU (Alpha Sprint-023, 2026-08-08).** Système de snapshot automatique générant un fichier `Docs/Studio/PROJECT_STATE.json` après chaque sprint pour permettre à l'Architecte (LLM) de reprendre instantanément le contexte complet du projet.

**Ce sprint livre** :
1. **Générateur automatique** (`ProjectSnapshotGenerator.cs`) : script Editor Unity accessible via menu `BeeKingdom > Generate Project Snapshot`.
2. **Format JSON structuré** : `Docs/Studio/PROJECT_STATE.json` contenant Project, Architecture (18 systèmes), Sprints (22), Documentation, Assets, Gameplay Loops (10), Production, Backlog (10), Risques (8), Vision.
3. **Données synthétiques uniquement** : aucune saisie manuelle, aucune duplication, compact, évolutif.
4. **Architecture complète** : 18 systèmes avec statut, progression, composants, dépendances.
5. **Sprints tracés** : 22 sprints avec titre, statut, résumé.
6. **Documentation indexée** : tous les fichiers .md scannés automatiquement.
7. **Assets catalogués** : backgrounds, banners, borders, crowns, symbols, icons comptabilisés.
8. **Gameplay Loops** : 10 boucles avec statuts et systèmes impliqués.
9. **Production & Backlog** : complété, en cours, prévu + 10 priorités.
10. **Risques auto-détectés** : TODO, systèmes incomplets, dépendances manquantes.
11. **Vision projet** : résumé <20 lignes.

**Fichiers créés / modifiés** :
- `Assets/BeeKingdom/Editor/ProjectSnapshotGenerator.cs` (nouveau)
- `Docs/Studio/` (dossier créé)
- `Docs/Studio/PROJECT_STATE.json` (généré à la demande)

**Performances** : génération instantanée, JSON compact (~15KB), compatible LLM.

### Revue de régression post-Sprint-023

- Correction du catalogue `strings.fr-CA.json` : les clés SpeedUp étaient sorties du tableau `entries`, ce qui cassait toute l'initialisation de la localisation et empêchait les tests de Connexion de fonctionner.
- Correction du splash Connexion : les onglets sont maintenant cliquables indépendamment de l'état `GUI.enabled` hérité d'un overlay précédent.
- Correction de l'intégration SpeedUp contre les modèles réels Construction, Recherche et Production.
- Correction du coût de barrière défensive et du budget d'abeilles ambiantes paysage ; le groupe tutoriel ciblé passe désormais 55/55.
- Ajout de la clé de localisation manquante `tutorial.chapter_01.hygiene_purge.result.confirmed` en FR-CA et EN-US ; le groupe tutoriel ne produit plus ce warning.
- Correction du readiness Google : un client signé-out avec une restauration locale absente ne remplace plus un gate serveur `Ready` par `NotConfigured`, donc le bouton Google reste visible lorsque le serveur et l'OAuth sont configurés.
- Correction du profil Alliance : sa fermeture réinitialise maintenant le menu Alliance et libère le rail inférieur ; la fermeture prioritaire gère aussi les overlays Bestiaire et Défi.
- Correction du démarrage audio : `SandboxPlayground` lance explicitement `MusicTrack.Hive` dès `Start`, et `MusicManager` recharge sa bibliothèque si l'initialisation arrive avant le chargement de `Resources`.
- Paramètres : le bouton `Plus` ferme maintenant le panneau lorsqu'il est déjà ouvert ; le retour interne utilise la même flèche `closing_arrow` que les autres fenêtres.
- Correction du harnais de capture Splash Auth : attente réelle de la fin du splash avant capture, avec écran Connexion effectivement rendu.
- Réduction des allocations inutiles dans l'inventaire SpeedUp et le flush des notifications Feedback.
- Ajout de tests EditMode : `MobileAccountSessionUiTests` (13/13), `SpeedUpSystemTests` (3/3).
- Ajout du noyau indépendant `BeeKingdom.Rewards` et de sa vue de validation Premium ; les récompenses du tutoriel passent maintenant par ce pipeline ; tests pipeline (2/2).

**Validation Unity** : compilation batch Unity `6000.5.3f1` sans erreur C#. Les avertissements d'API obsolètes existants restent à traiter séparément. Le flux Google réel nécessite encore une configuration OAuth et un test PlayMode avec fournisseur disponible.

---

### PB-010 — Capacité de stockage affichée comme "0/3000.0M"

**Statut : RÉSOLU (First Hour Experience Pass, Sprint 3, 2026-08-04)** — voir
le journal des sprints en tête de document.

**Ce que le joueur voit** : dès la création d'un nouveau compte, la
jauge "Cap." en haut à droite affiche "0/3000.0M" (3 milliards).

**Pourquoi cela casse l'immersion** : un nombre à neuf chiffres pour un
joueur qui vient de commencer n'a de sens pour personne.

**Pourquoi cela donne une impression de prototype** : ressemble à une
valeur technique de sécurité ("ne jamais bloquer la collecte") qui a fui
dans l'interface au lieu d'être remplacée par la vraie capacité du
bâtiment.

**Priorité** : Moyenne.

**Correction proposée** : afficher la capacité réelle et pertinente pour
le joueur (celle du bâtiment actif), pas une valeur plafond technique.

---

### PB-011 — Le panneau "Sac & stocks" est entièrement non fonctionnel

**Ce que le joueur voit** : en ouvrant "Sac" depuis la barre du bas, les
trois lignes de ressources affichent chacune "Valeur officielle
indisponible / Capacité serveur indisponible / Aucune quantité locale
substituée", suivi de "Population : Non disponible dans le contrat
actuel" et "Engagements actifs · 0 / Instantané serveur requis". Un bouton
"Actualiser" est présent mais le message "Instantané serveur indisponible"
persiste.

**Pourquoi cela casse l'immersion** : un écran entier composé uniquement
de messages "indisponible" ne ressemble à aucune fonctionnalité de jeu
terminée.

**Pourquoi cela donne une impression de prototype** : le principe de ne
jamais inventer de valeur est louable (cohérent avec le reste du jeu),
mais le résultat visuel ici est un écran qui semble complètement cassé
plutôt que "prudent".

**Priorité** : Haute.

**Correction proposée** : soit activer la fonctionnalité serveur
correspondante pour cet environnement, soit masquer ce bouton tant qu'elle
n'est pas prête.

---

### PB-012 — Les boutons "Défi" et "Bestiaire" ne répondent à aucun clic

**Ce que le joueur voit** : cliquer sur "Défi 0/3" ou "Bestiaire" dans la
barre de navigation du bas ne produit aucune réaction visible à l'écran —
pas de panneau, pas de message, pas de son, pas de tooltip. Testé
plusieurs fois avec vérification que le clic atteignait bien la fenêtre du
jeu.

**Pourquoi cela casse l'immersion** : un bouton qui ne fait rien du tout,
sans le moindre retour, laisse le joueur se demander s'il a mal cliqué,
si c'est cassé, ou si c'est verrouillé.

**Pourquoi cela donne une impression de prototype** : "Défi 0/3" est
précisément l'endroit où l'on s'attendrait à trouver le combat — l'absence
totale de réaction est le pire résultat possible pour cette partie du jeu.

**Priorité** : Haute.

**Correction proposée** : au minimum, afficher un message explicite
("Combat verrouillé, atteins le niveau X") si la fonctionnalité est
volontairement bloquée à ce stade de progression.

---

### PB-013 — Formes géométriques violettes glitchées derrière le panneau "Championnes"

**Statut : RÉSOLU (First Hour Experience Pass, Sprint 5, 2026-08-04)** — voir
le journal des sprints en tête de document.

**Ce que le joueur voit** : en ouvrant "Abeilles championnes", deux grandes
formes triangulaires violettes, mal positionnées, apparaissent en arrière-plan
de part et d'autre du panneau, par-dessus le fond normalement noir.

**Pourquoi cela casse l'immersion** : ressemble à un artefact de rendu ou
à un élément 3D mal placé qui traverse l'interface.

**Pourquoi cela donne une impression de prototype** : un panneau par
ailleurs bien conçu (voir points positifs) est immédiatement décrédibilisé
par cet artefact visuel juste derrière lui.

**Priorité** : Moyenne.

**Correction proposée** : identifier l'élément visuel à l'origine de ces
formes (probablement une géométrie de scène non désactivée derrière ce
panneau UI) et le masquer.

---

### PB-014 — Deux valeurs différentes affichées pour la même ressource en attente

**Statut : RÉSOLU (First Hour Experience Pass, Sprint 4, 2026-08-04)** — voir
le journal des sprints en tête de document.

**Ce que le joueur voit** : dans le panneau détaillé de la Réserve de
miel, un petit nombre ("2", puis "0" après collecte, avec un taux
"+195/h") est affiché juste au-dessus d'une ligne "À collecter : 400.0K",
sans qu'aucun texte n'explique la différence entre ces deux valeurs pour
la même ressource.

**Pourquoi cela casse l'immersion** : deux nombres très différents
présentés côte à côte sans légende claire.

**Pourquoi cela donne une impression de prototype** : ressemble à deux
champs de données internes différents affichés bruts l'un à côté de
l'autre plutôt qu'une seule information consolidée pour le joueur.

**Priorité** : Moyenne.

**Correction proposée** : clarifier visuellement ce que représente chaque
nombre (ex. "en attente de collecte maintenant" vs "capacité totale du
bâtiment").

---

## Note méthodologique

- Le mode "démo locale" a révélé un premier état de jeu avec des
  ressources déjà non nulles alors que le texte du tutoriel annonçait des
  "réserves presque vides" — après vérification avec un compte Google
  réellement neuf, ce décalage a disparu (Miel/Cire/Pollen à 0). Ce
  décalage initial venait donc d'une sauvegarde locale préexistante
  réutilisée par le mode démo, pas d'un bug du jeu lui-même.
- PB-001 concerne spécifiquement le mode "démo locale" ; le studio a
  confirmé pendant cet audit que ce mode ne fera pas partie du produit
  final, ce qui réduit sa priorité produit réelle même si sa sévérité
  technique reste critique tel quel.
- PB-002 (la carte cassée), en revanche, a été reproduit à l'identique
  avec un vrai compte Google connecté au serveur officiel — ce n'est donc
  pas un problème limité à la démo.
