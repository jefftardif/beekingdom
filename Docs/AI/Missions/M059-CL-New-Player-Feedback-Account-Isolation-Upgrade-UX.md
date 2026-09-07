# M059-CL — Premier retour d'un joueur externe : isolation de compte + UX d'amélioration

Date : 2026-09-07
Agent : Claude Code
Périmètre : `Environment2D5D_HiveMap_Test` uniquement. `Assets/Scenes/LivingHive.unity` n'a jamais été
ouverte. Aucune modification du terrain 50x50 ni de son package Resources.
Origine : premier test d'utilisabilité sur machine propre par un joueur qui n'avait jamais vu le jeu
(Alex). Numérotée M059 et non M057 pour éviter la collision avec le rapport de dérive SQL du même jour.

> **Note pour la direction : AUCUN défaut d'isolation serveur n'a été trouvé.** La section 2 en donne la
> preuve mécanique. Rien n'a été commité, poussé ni déployé.

---

## 1. Résumé

Trois livraisons, dans l'ordre de priorité demandé.

**Part 1 — le bug critique est réel, mais ce n'est PAS une fuite de données entre joueurs.** Les niveaux
impossibles vus par Alex (22, 24, 25, 27) ne sont les niveaux de personne : ce sont quatre constantes de
repli codées en dur dans le bac à sable de démonstration historique. La VUE COLONIE lisait directement
ce bac à sable au lieu de l'instantané serveur. Corrigé, avec une seconde faille d'isolation
authentique — locale, sur machine partagée — trouvée et fermée en chemin.

**Part 2 —** cliquer un bâtiment en cours d'amélioration ouvre désormais une fenêtre d'avancement
(niveau actuel → niveau visé, progression, temps restant, aide d'alliance réelle), sans toucher au
comportement déjà validé du clic de validation.

**Part 3 —** une barre de progression compacte s'affiche au-dessus du bâtiment en chantier dans la
ruche, alimentée par le minutage serveur, en complément du pulse bleu/cyan existant qui n'est pas
modifié.

---

## 2. Part 1 — diagnostic complet

### 2.1 Cause retenue

Sur la grille A–H de la mission : **A (données d'affichage codées en dur) et G (données de repli /
démonstration)**, confondues — c'est la même ligne de code. Plus, en second lieu, **F (persistance
locale périmée)** comme faille distincte et réelle, décrite en 2.4.

**Ne sont PAS en cause :** B (cache d'un autre joueur), C (état statique partagé venant d'un autre
compte), D (mauvaise résolution du joueur authentifié), E (mauvais `PlayerHiveState` récupéré),
H (problème d'isolation côté serveur).

### 2.2 Preuve — la correspondance numérique est exacte

Le résolveur de niveau du bac à sable local matérialise, pour tout bâtiment jamais rencontré, une
valeur inventée : 27 pour le Palais Royal, 25 pour la Réserve de miel, 24 pour la Caserne, et 22 pour
**tous les autres**. C'est exactement, valeur par valeur, la liste rapportée par Alex — dont le 22
répété plusieurs fois, qui est la signature du cas « tous les autres ».

Le chemin de données réel de la VUE COLONIE était donc :

```
compte authentifié → (ignoré)
                     ↓
    résolveur de niveau du bac à sable local → 27 / 25 / 24 / 22 → VUE COLONIE
```

L'identité authentifiée n'entrait nulle part dans la chaîne. Une ruche neuve, une ruche ancienne et
une absence totale de session produisaient rigoureusement le même affichage.

Point aggravant : le reste du jeu faisait déjà les choses correctement. Il existe depuis longtemps un
résolveur « serveur d'abord, bac à sable en repli » utilisé par les autres écrans de bâtiment ; la VUE
COLONIE était le seul écran joueur à court-circuiter cette autorité.

### 2.3 Preuve — l'isolation serveur est saine

La mission demandait d'arrêter et de signaler si le serveur avait pu renvoyer la ruche de Jeff pour
l'identité d'Alex. **Ce n'est pas le cas**, et c'est démontrable sans test en conditions réelles :

- l'identifiant de joueur utilisé par les trois points d'entrée d'amélioration de bâtiment
  (lecture, démarrage, validation) provient **exclusivement du jeton authentifié**. Il n'est jamais lu
  depuis l'URL ni depuis le corps de la requête ;
- l'état de ruche est stocké et relu sous une **clé composite (identifiant de joueur, identifiant de
  ruche)**, avec le joueur en première position de toutes les clauses de filtrage ;
- il n'existe aucun chemin joueur permettant de lire l'état d'un autre joueur.

**Un point mérite néanmoins l'attention de la direction, sans être un défaut.** La configuration client
embarque un identifiant de ruche **unique et identique pour toutes les installations**. Tous les
joueurs demandent donc littéralement « la ruche numéro X » — mais comme la clé de stockage est
composite, chacun n'atteint que sa propre ligne. La séparation repose donc entièrement sur le fait que
le serveur traite cet identifiant comme une simple seconde moitié de clé et jamais comme une
revendication de propriété. C'est le cas aujourd'hui, vérifié. C'est une fragilité de conception à
garder en tête si un joueur devait un jour posséder plusieurs ruches, ou si un futur point d'entrée
acceptait un identifiant de ruche sans le recouper avec le joueur. **Aucune action requise maintenant ;
aucun correctif serveur n'a été écrit ; rien n'est à déployer.**

### 2.4 Seconde faille, authentique celle-là : cache local partagé entre comptes

En traçant la chaîne, un vrai défaut d'isolation est apparu — local, pas serveur, mais réel.

Le cache de progression d'aperçu local (niveaux de repli, effectifs, abeilles championnes, paliers de
troupes) est stocké dans **un unique emplacement de préférences, partitionné par APPAREIL et jamais par
COMPTE** : sa clé de partition était un identifiant aléatoire généré une fois par installation. Deux
comptes utilisés sur la même machine **partageaient donc intégralement ce cache**.

S'y ajoutait un défaut de cycle de vie de la même famille que les fuites de drapeaux corrigées par
M056A-CL et M058-CL : les caches en mémoire du présentateur n'étaient **jamais purgés à la déconnexion
ni au changement de compte**. Une fois chargés, ils survivaient au changement de compte tant que le
processus vivait.

Les deux points sont corrigés (section 3.2). Ce n'est pas ce qu'Alex a vu — il était sur sa propre
machine — mais c'est exactement le scénario que la mission demandait de chercher, et il se serait
manifesté dès le premier test à deux comptes sur un même poste.

---

## 3. Corrections appliquées

### 3.1 VUE COLONIE suit désormais l'identité authentifiée

Un résolveur unique et sûr a été introduit et branché sur la liste des bâtiments comme sur la fiche de
détail. Sa règle, dans l'ordre :

1. session de jeu officielle branchée et niveau livré par le serveur → **le niveau serveur** ;
2. session officielle branchée mais instantané pas encore arrivé → **un tiret d'attente explicite**,
   jamais un nombre inventé ;
3. aucune session officielle (hors ligne / non connecté) → la valeur du bac à sable, mais l'écran
   **annonce alors clairement qu'il montre une démonstration**.

La ligne « prochain niveau » de la fiche de détail affichait le coût du bac à sable local à côté d'un
niveau serveur — incohérence corrigée : elle montre maintenant l'état réel du serveur pour ce bâtiment.

L'invariant demandé est donc tenu : **les données de la VUE COLONIE suivent l'identité authentifiée, et
jamais l'historique global du runtime.** Un joueur neuf voit le niveau 1 partout, qui est bien la valeur
que le serveur matérialise pour une ruche jamais améliorée.

### 3.2 Isolation et cycle de vie du cache local

- Le cache de progression locale porte désormais **le compte propriétaire**. Un cache appartenant à un
  autre compte est refusé et remplacé par un état vide.
- **Migration sans perte** : un cache écrit avant cette mission ne porte aucun compte ; il est *adopté*
  par le premier compte qui le lit, plutôt qu'effacé. La progression locale déjà accumulée sur la
  machine du CEO est donc préservée. Une fois adopté, il devient inaccessible aux autres comptes.
- Tout état de repli (vide, corrompu, mauvais profil, mauvais compte) est désormais **estampillé au
  compte courant dès sa création**. Sans cela, le tout premier cache écrit par un joueur l'aurait été
  sans compte, donc considéré comme hérité, donc adoptable par le compte suivant — la fuite se serait
  rouverte au premier enregistrement. *Ce point a été trouvé par un test qui échouait, pas par
  relecture.*
- La session de compte **purge maintenant explicitement** les caches en mémoire du présentateur à la
  configuration comme à la déconnexion, et referme la VUE COLONIE si elle était ouverte, pour qu'aucun
  chiffre du compte précédent ne reste à l'écran.
- En l'absence de session authentifiée, **le comportement antérieur est strictement inchangé**.

### 3.3 Part 2 — fenêtre « amélioration en cours »

Fenêtre compacte, centrée, bornée, adaptée au mobile comme au test Windows — pas un nouvel écran plein.
Elle affiche uniquement des faits serveur : bâtiment concerné, niveau actuel → niveau visé, barre de
progression et pourcentage, temps restant.

Actions : **uniquement l'Aide d'alliance**, c'est-à-dire exactement l'appel que l'écran Construction
effectue déjà pour cette même opération. **Aucun bouton « Accélérer »** : aucun point d'entrée serveur
ne raccourcit une opération de construction réelle, ce serait un faux bouton. Aucun coût ni durée
n'a été modifié.

La précédence de clic exigée est respectée, et surtout **le comportement déjà validé n'a pas été
touché** : le clic de validation en attente d'achèvement reste la propriété exclusive du composant
existant. Les deux crochets de clic cohabitent sans conflit possible parce qu'ils lisent deux états
mutuellement exclusifs du serveur, et que le serveur ne porte qu'un seul chantier pour toute la ruche.
La fenêtre se referme d'elle-même dès que l'opération quitte l'état « en cours ».

La fenêtre est enregistrée comme bloquant l'input du monde — sans quoi un clic destiné à son bouton
de fermeture aurait aussi atteint le bâtiment situé derrière elle. Elle ne dessine aucun sous-modal
par-dessus son propre contenu, la règle du 2026-09-03 ne s'y applique donc pas.

**Un risque de plantage a été trouvé et neutralisé en chemin** : l'action d'aide d'alliance
déréférence son contrôleur sans protection. Ce contrôleur relève d'une session distincte de celle de
la construction et peut être absent (hors ligne, joueur sans alliance). Appelée sans garde-fou depuis
la nouvelle fenêtre, elle aurait levé une exception à chaque image. L'appel est protégé.

### 3.4 Part 3 — barre de progression en espace monde

Barre compacte au-dessus du bâtiment en chantier, avec le temps restant lorsque le zoom la rend
lisible. Ses propriétés :

- valeur **exclusivement issue de l'opération serveur** (début, fin, horloge serveur projetée) —
  aucun minuteur client parallèle ;
- **le pulse bleu/cyan n'est ni retiré ni modifié.** Les deux signaux se complètent : le pulse dit
  « chantier ici », la barre dit « où en est le chantier » ;
- elle **disparaît d'elle-même** au passage en attente de validation, laissant l'indicateur
  d'achèvement officiel seul. L'asset d'achèvement approuvé n'a été ni redessiné, ni recoloré, ni
  remplacé ;
- elle **suit le bâtiment** au pan et au zoom en réutilisant la projection d'emprise écran déjà
  employée par le badge d'achèvement — pas de système de suivi parallèle, pas de dérive ;
- largeur **bornée** en pixels, donc ni minuscule ni géante aux extrêmes de zoom ;
- **elle ne peut pas devenir un bloqueur d'input invisible** : elle ne dessine aucun contrôle
  cliquable, et ce mode de rendu ne peut pas intercepter le raycast 3D des clics de bâtiment ;
- elle se **reconstruit** après aller-retour de scène ou reconnexion, puisqu'elle ne conserve aucun
  état propre et relit l'opération serveur à chaque image ;
- l'implémentation est **liée à l'identité du bâtiment en opération**, jamais à un bâtiment particulier
  codé en dur.

### 3.5 Amélioration Quality of Life du sprint (règle du 2026-08-04)

La VUE COLONIE porte désormais un **bandeau de provenance** : elle dit explicitement si les chiffres
affichés viennent du serveur (« ta ruche ») ou de la démonstration locale (« pas ta ruche »). C'est la
convention déjà retenue pour le CHAT ROYAL, et c'est précisément le piège dans lequel Alex est tombé :
croire lire sa ruche en regardant un bac à sable.

---

## 4. Preuves

### 4.1 Nouveaux tests

`Assets/BeeKingdom/Playground/Editor/ColonyViewAccountIsolationTests.cs` — **10/10**.

Couvrent : le niveau serveur remplace bien la valeur inventée et aucune des quatre valeurs rapportées
par Alex ne peut plus apparaître ; l'attente explicite plutôt qu'un nombre inventé ; le refus du cache
d'un autre compte sur le même appareil ; l'adoption sans perte d'un cache hérité ; l'absence de
régression quand aucun compte n'est connu ; la purge mémoire au changement de compte accompagnée de la
vérification qu'un second compte ne récupère pas la valeur écrite par le premier sur le même appareil ;
l'ouverture de la fenêtre d'avancement uniquement sur le bâtiment réellement en chantier ; la
non-substitution au comportement de validation ; la progression tirée du minutage serveur et son
extinction hors état « en cours » ; l'enregistrement puis la libération propre du blocage d'input.

### 4.2 Non-régression

| Suite | Résultat | Lecture |
|---|---|---|
| `ColonyViewAccountIsolationTests` | **10/10** | nouvelle |
| `SandboxLivingHiveManualCollectionTests` | **55/55** | couvre le codec de progression locale modifié |
| `HiveMapSceneReentryInputTests` | **6/6** | régression caméra de M056A non réintroduite |
| `BuildingInteractionControllerClickPriorityTests` | **10/10** | précédence de clic intacte |
| `SandboxLivingHiveUiStabilizationTests` | 20/22 | **identique à l'état documenté par M058-CL** |
| `SandboxLivingHiveBuildingUpgradeTests` | 9/10 | échec préexistant, voir ci-dessous |

Compilation Unity verte, aucune erreur console.

### 4.3 Échecs préexistants — honnêteté sur ce qui ne vient pas de moi

Un passage sur l'ensemble de l'espace de noms de test du bac à sable donne **22 échecs**. Aucun ne
relève de cette mission. Il s'agit d'attentes de tests devenues obsolètes : anciens libellés de
bâtiments (« Reserve miel » contre « Réserve de miel », « Administration » contre « Palais royal »),
chaînes de preuve de missions anciennes, et un avertissement de matériau en mode édition.

Pour les deux seuls échecs situés dans un domaine proche du mien, la démonstration est mécanique :

- les 2 échecs de `SandboxLivingHiveUiStabilizationTests` sont ceux, déjà documentés et analysés par
  M058-CL, du chemin Alliance — même nombre, mêmes tests ;
- l'échec de `SandboxLivingHiveBuildingUpgradeTests` porte sur une comparaison de durée mal écrite dans
  le test lui-même. **Les trois fichiers impliqués dans ce chemin de code sont strictement identiques
  à leur version de référence** — vérifié — donc l'échec ne peut pas provenir de cette mission.

**Je n'ai pas pu produire de référence d'exécution « avant/après » propre**, parce que cela aurait exigé
de mettre de côté temporairement le travail non commité d'autres sessions présentes dans l'arbre. Je
m'en suis abstenu délibérément. La démonstration ci-dessus repose donc sur la comparaison de fichiers et
sur la nature des assertions, pas sur une exécution de référence.

### 4.4 Limite majeure à lire avant de tester

**AUCUN test en Play Mode n'a été joué, et aucune capture d'écran n'a été produite.** Toutes les preuves
sont statiques (EditMode + analyse). Deux raisons : l'éditeur a déjà planté deux fois aujourd'hui sur
des sessions Play Mode prolongées, et l'outil de bascule en mode jeu n'était pas disponible dans cette
session. Par ailleurs, une vérification visuelle réelle des Parts 2 et 3 aurait exigé de démarrer une
véritable amélioration serveur authentifiée et d'attendre son minutage — exactement le type de session
longue à l'origine des blocages du jour.

Concrètement : **la logique est prouvée, le rendu ne l'est pas.** Les points à vérifier en priorité par
un œil humain sont listés en section 6.

---

## 5. Fichiers touchés

Modifiés :

- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/LocalPreviewHiveProgress.cs`
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json` *(cinq clés
  additives, dans les deux langues, validité JSON vérifiée)*
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs` — **⚠ ce fichier portait déjà des
  modifications non commitées d'une AUTRE session.** Mon ajout y est d'une seule ligne, strictement
  additif, et ne modifie aucun drapeau existant. Il était indispensable : c'est le point unique
  d'enregistrement des fenêtres qui bloquent l'input du monde, et sans lui la nouvelle fenêtre laissait
  les clics traverser jusqu'aux bâtiments.

Créés :

- `Assets/BeeKingdom/Playground/HiveMapBuildingUpgradeProgressBootstrap.cs`
- `Assets/BeeKingdom/Playground/Editor/ColonyViewAccountIsolationTests.cs`
- ce rapport.

**Non touchés, volontairement** : `BuildingActivityPulse.cs`, `HiveMapBuildingUpgradeVisualStateBootstrap.cs`
et les autres fichiers appartenant à des sessions en cours. Le nouveau composant est délibérément
séparé du composant de pulse afin de n'avoir à modifier ni le pulse bleu/cyan ni le comportement de
validation au clic.

**Rien n'a été commité, poussé ni déployé.** Aucun code serveur n'a été modifié.

---

## 6. Prochain test utilisateur

1. **VUE COLONIE, compte réel connecté** — ouvrir le Palais Royal puis la vue d'ensemble. Les niveaux
   doivent correspondre à la ruche réelle, et le bandeau doit indiquer « Données serveur · ta ruche ».
   Plus aucun 22 / 24 / 25 / 27 fantôme.
2. **Compte neuf** — les mêmes bâtiments doivent afficher le niveau 1.
3. **Bascule de compte sur la même machine** — se déconnecter, se reconnecter avec l'autre compte, et
   vérifier qu'aucun niveau ni effectif du compte précédent ne subsiste, y compris hors ligne.
4. **Amélioration en cours** — démarrer une amélioration, puis dans la ruche : une barre de progression
   doit apparaître au-dessus du bâtiment, le pulse bleu doit toujours être là, et cliquer le bâtiment
   doit ouvrir la fenêtre d'avancement et non la fenêtre ordinaire.
5. **Passage à « à valider »** — la barre doit disparaître, l'indicateur d'achèvement officiel prendre
   la main, et le clic sur le bâtiment valider l'amélioration comme avant.
6. **Aller-retour de scène** — ruche → carte du monde → ruche : chantier toujours actif, barre sur le
   bon bâtiment, progression cohérente, et surtout pan / zoom / clic de bâtiment toujours opérants.

---

## 6bis. Correctif de routage post-certification CEO (2026-09-07, même journée)

### Ce que le CEO a constaté en Play Mode réel

Réussi : la VUE COLONIE affiche bien les vrais niveaux, les valeurs fantômes 22/24/25/27 ont
disparu. L'état de chantier lui-même est réel — pendant que **Défense** est en amélioration, le
Palais Royal répond correctement « Un autre bâtiment occupe la file de construction ».

Échoué : **aucune barre de progression** au-dessus de Défense, et **cliquer Défense ouvrait sa
fenêtre ordinaire** (Fermer / Améliorer) au lieu de la fenêtre d'avancement.

### Cause exacte — une seule, pour les deux symptômes

`HiveMapBuildingUpgradeProgressBootstrap` **n'était pas câblé dans
`HiveMapRuntimeBootstrapInitializer`**. Son unique point d'entrée restait son propre
`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, qui ne se déclenche qu'une fois, sur la scène
active à l'instant où le Play Mode démarre — c'est-à-dire la scène de splash/login, qui ne commence
jamais par `Environment2D5D`. **Le composant n'a donc jamais existé dans la scène réelle.**

Conséquences mécaniques, exactement les deux symptômes rapportés :

- pas d'instance → pas de `OnGUI` → **aucune barre de progression monde** ;
- pas d'instance → `RegisterCompletionPreemption` jamais appelé → le crochet de préemption de clic
  n'existe pas → `DispatchClick` tombe directement dans `Selection.BuildingClicked`, dont
  `HiveMapUnsupportedBuildingBootstrap` est l'abonné pour Défense → **la fenêtre ordinaire s'ouvre**.

**Ce n'était donc NI un problème de source de données, NI un problème d'ordre de branchement, NI
une couverture partielle par type de bâtiment.** La logique de détection et le rendu étaient
corrects et le sont restés : `ActiveOfficialUpgradeHotspotIdForExternalHost()` lit bien la même et
unique autorité serveur (`HiveBuildingUpgradeScreenModel.ActiveOperation`) que le message « file
occupée » du Palais Royal — la seule différence légitime entre les deux est que la barre exige en
plus `Status == "running"`, ce qui est le comportement voulu.

**Troisième récidive du même défaut d'installation** : identique à M038B-CL (`HiveMapResearch-
Bootstrap`, `FtueTutorialBootstrap`) et à M049C-CL (`HiveMapResearchVisualStateBootstrap`, où le
CEO avait déjà signalé une recherche en cours sans pulse visible). C'est un piège structurel du
projet, pas une étourderie isolée.

### Correction

Une ligne d'installation ajoutée dans `HiveMapRuntimeBootstrapInitializer.InitializeAllBootstraps`,
juste après le bootstrap d'état visuel de construction. **Aucune réécriture de fonctionnalité :
ni la fenêtre, ni la barre, ni la détection, ni le pulse, ni la validation au clic n'ont été
touchés.** `HiveMapOverlayInputGateBootstrap.cs` n'a **pas** été modifié cette fois.

### Preuve de généricité — aucun sous-ensemble de bâtiments

Le chemin corrigé ne contient **aucune liste de bâtiments** : identité résolue par
`BuildingMappingTable` / `BuildingCatalog`, état lu sur l'opération serveur. Les 14 clés de
`BuildingLegacyKeys.All` correspondent exactement, une à une, aux 14 clés de `SupportedBuildings`
du client de construction, elles-mêmes miroir du catalogue serveur. Vérifié par exécution réelle
dans l'éditeur : **14/14 bâtiments** — Défense incluse — sont détectés comme « en chantier »,
ouvrent la fenêtre d'avancement, et n'ouvrent pas celle d'un autre bâtiment.

### Preuves

Nouveau `Assets/BeeKingdom/Playground/Editor/HiveMapUpgradeProgressWiringTests.cs` — **4/4**,
exécutés dans l'assembly réelle de l'éditeur :

| Test | Ce qu'il prouve |
|---|---|
| `UpgradeProgressBootstrapIsWiredIntoTheProductionSceneInstaller` | la cause exacte est fermée |
| `EveryBootstrapExposingInitializeForSceneIsCalledByTheProductionInstaller` | **filet générique** : plus aucun bootstrap `HiveMap*` exposant `InitializeForScene(Scene)` n'est absent de l'installeur (résultat : NONE). Empêche la 4e récidive, pour tout futur bootstrap |
| `RunningUpgradeIsDetectedForEveryRealBuilding` | 14/14 : détection, progression serveur (0,5 à mi-parcours) et ouverture de fenêtre, sans liste figée |
| `ClickIdentityRoundTripsForEveryRealBuilding` | l'aller-retour type de bâtiment ↔ clé serveur ne perd aucun des 14 bâtiments |

Sonde runtime dans l'éditeur : scène active `Environment2D5D_HiveMap_Test`, compilation verte,
aucune erreur console. Rien n'a été commité, poussé ni déployé.

### Limite restante

**Toujours aucun test en Play Mode.** Le lanceur de tests de l'éditeur était bloqué par une
exécution restée active d'une autre session (`d0ea457f-…`, en échec depuis 10:57) ; les tests ont
donc été exécutés par invocation directe des méthodes compilées plutôt que par le runner, et je me
suis abstenu d'entrer en Play Mode pour ne pas détruire le travail de cette autre session. Une
preuve visuelle complète exigerait de toute façon une amélioration serveur authentifiée réelle et
son minutage — c'est précisément le test que le CEO va rejouer.

---

## 7. Ouvert / à faire ensuite

1. Rejouer les Parts 2 et 3 en Play Mode réel avec une véritable amélioration serveur — c'est la seule
   preuve manquante.
2. Décider du sort de l'identifiant de ruche unique embarqué côté client (section 2.3). Sans danger
   aujourd'hui, structurellement fragile demain.
3. Les autres écrans hérités qui lisent encore directement le bac à sable local pour afficher un niveau
   n'ont pas été modifiés : ils appartiennent au rendu historique inaccessible depuis la ruche jouable
   actuelle. À reprendre si l'un d'eux redevenait atteignable.
4. Les 22 échecs de tests obsolètes de l'espace de noms du bac à sable méritent une passe de ménage
   dédiée : ils masquent aujourd'hui les vraies régressions.
