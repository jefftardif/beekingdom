# Bee Kingdom - Contrat UI Skill Tree et handoff Builder-A

Date locale: 2026-07-15  
Statut: PREPARATION UI READY_FOR_BUILDER_A  
Portee: contrat UX, mapping du modele existant, plan d'integration Unity sans modification de scene

## 1. Verdict et garde de perimetre

Le contrat UI est pret pour une implementation code-driven par Builder-A. Cette publication est documentaire uniquement.

```text
UI_CONTRACT = READY
MODEL_CONTRACT_READ = PASS
BUILDER_A_HANDOFF = READY_FOR_REVIEW
SCENE_MODIFIED = NO
PNG_MODIFIED = NO
APK_BUILT = NO
SERVER_OR_REAL_DATA_MODIFIED = NO
RUNTIME_MODIFIED = NO
```

Le travail de cette passe ne modifie aucun fichier sous `Assets`, aucune scene Unity, aucun PNG, APK, serveur ou donnees reelles. Aucun nouveau test n'est ajoute: les tests Editor existants couvrent deja les invariants de progression necessaires a cette preparation.

## 2. Sources lues et invariants observes

Sources de verite lues:

- `Assets/BeeKingdom/Gameplay/Progression/PlayerSkillTree.cs`
- `Assets/BeeKingdom/Gameplay/Progression/PlayerSkillTreeUiModel.cs`
- `Docs/Architecture/PlayerSkillTree_Progression_Spec.md`
- `Assets/BeeKingdom/Tests/Editor/PlayerSkillTreeTests.cs`
- `Assets/BeeKingdom/Tests/Editor/PlayerSkillTreeUiModelTests.cs`

Invariants code qui doivent rester vrais dans l'UI:

| Invariant | Source | Contrat d'affichage |
|---|---|---|
| Niveau jouable 1..50 | `PlayerXpCurve`, `PlayerSkillState` | Afficher le niveau courant; refuser toute commande UI hors bornes. |
| Un point par niveau | `SkillPointsAwarded => Level` | Exposer points attribues, depenses et non depenses sans recalcul UI. |
| Classe verrouillee avant 10 | `ClassUnlockLevel = 10`, `TryChooseClass` | Garder le choix de classe visible mais inactif avant niveau 10. |
| Trois onglets | `PlayerSkillTreeView.Build` | Toujours afficher `Combat`, `Ressources / Evolution`, `Classe`. |
| Branche classe isolee | `BuildTab` filtre `ClassId` | Apres choix, ne montrer que les cinq noeuds de la classe courante. |
| Achat controle par le modele | `TryPurchase` | L'UI demande une mutation au modele, puis rebinde la vue. |
| Reset deterministe | `TryResetSkills` | Confirmation explicite, puis reconstruction complete de l'arbre. |
| Preview locale | `IsLocalPreview`, `TrySetLocal*` | Badge permanent et aucune formulation de progression officielle. |

Les tests lus confirment notamment: trois onglets au niveau 9 avec branche Classe verrouillee, cinq noeuds de classe pour une classe choisie, achat de `combat_foundation` qui ouvre `combat_command`, reset qui rend le budget initial et preview locale sans autorite officielle.

## 3. Objectif joueur

En ouvrant l'ecran, le joueur doit comprendre en moins de dix secondes:

1. son niveau, son XP et ses points;
2. quelles branches sont accessibles;
3. pourquoi un noeud est verrouille;
4. quel bonus le prochain rang apporte;
5. ce qui changera apres l'achat;
6. comment annuler une erreur par reset;
7. si l'ecran est une preview locale ou une progression officielle.

L'arbre est un ecran de progression fixe en espace UI. Il ne suit pas le pan/zoom de la WorldMap et ne peint aucune information dans le terrain, les tuiles ou les landmarks.

## 4. Structure de l'ecran

### 4.1 En-tete persistant

L'en-tete reste visible pour les trois onglets et contient:

- titre `Skill Tree` ou libelle localise equivalent;
- `Niveau 10 / 50`;
- barre XP avec `XP actuelle / XP requise`;
- `Points disponibles: N` quand ils sont depensables;
- `Points en reserve: N - verrouilles avant niveau 10` avant le niveau 10;
- classe active: `Neutral`, `RoyalGuard`, `Striker`, `Nurturer`, `Scout` ou `Alchemist`;
- badge `LOCAL - APERCU NON OFFICIEL` quand `IsLocalPreview` est vrai;
- bouton de fermeture/retour, avec focus et tooltip.

Le champ `SkillPointsAvailable` du modele UI correspond aux points non depenses. Avant le niveau 10 il ne doit pas etre affiche seul comme un budget achetable: la presentation doit le renommer en reserve verrouillee. Le calcul reste celui du modele.

Le modele actuel ne fournit pas `xp_total` ni `xp_to_next`. L'adaptateur UI doit les lire d'un resume de progression deja existant ou afficher un etat neutre explicite (`XP detaillee indisponible`) en mode developpement. Il ne doit jamais inventer une XP a partir du seul niveau.

### 4.2 Onglets

Ordre et libelles canoniques:

1. `Combat`
2. `Ressources / Evolution`
3. `Classe`

Regles:

- les trois onglets sont toujours rendus et atteignables au clavier, a la manette et au tactile;
- l'onglet actif est indique par un etat de selection, un texte et un contraste de bordure;
- le changement d'onglet conserve le scroll/zoom de chaque graphe pendant la session;
- les onglets Combat et Ressources / Evolution exposent leurs noeuds, mais ceux-ci restent verrouilles par niveau avant le niveau 10 car le catalogue courant leur attribue `RequiredLevel = 10`;
- l'onglet Classe affiche un verrou global avant le niveau 10 avec `Choisissez une classe au niveau 10 pour debloquer cette branche.`;
- a partir du niveau 10, si la classe est encore `Neutral`, l'onglet Classe affiche le choix de classe, pas une surface vide;
- apres choix, l'onglet Classe expose uniquement les noeuds de la classe choisie;
- `Neutral` n'est jamais une option de choix final.

### 4.3 Zone graphe

La zone centrale affiche un graphe lisible, avec:

- un noeud par `SkillId` du `SkillTreeTabView`;
- des connecteurs entre un noeud et chacun de ses prerequis;
- un sens de lecture haut-gauche vers bas-droite ou equivalent stable;
- une position fixe par `SkillId`, independante de l'ordre d'enumeration du dictionnaire;
- un zoom borne et un pan interne au graphe, sans effet sur la carte du monde;
- un bouton `Recentrer` qui remet le graphe de l'onglet actif a son cadrage de depart;
- un scroll interne sur mobile si la largeur du graphe depasse la zone utile.

Le modele ne porte pas les positions et la spec porte les prerequis mais pas de layout. Builder-A doit donc consommer une table de layout UI versionnee, separee des definitions de gameplay. Une position manquante est une erreur de validation en mode developpement; elle ne doit pas creer un noeud superpose ou hors cadre en retail.

### 4.4 Inspecteur de noeud

Un clic/tap/focus sur un noeud ouvre ou actualise l'inspecteur. Il contient:

- nom localise;
- description courte;
- arbre et classe associee;
- rang actuel et rang maximum, par exemple `Rang 1 / 3`;
- effet au rang actuel;
- effet du prochain rang;
- cout du prochain rang;
- prerequis, chacun avec son rang actuel et le rang requis;
- niveau requis;
- raison de verrouillage, si applicable;
- apercu du profil avant/apres achat;
- bouton d'achat explicite, ou etat `Achete`, `Maximum`, `Verrouille`.

Le bouton d'achat est absent ou desactive lorsque `CanPurchase` est faux. L'inspecteur doit toutefois expliquer l'etat: un bouton simplement gris sans raison est un echec UX.

## 5. Etats visuels des noeuds

Le rendu ne doit jamais dependre de la couleur seule. Chaque etat combine icone, libelle, contraste, motif et texte.

| Etat modele | Signal visuel minimum | Action |
|---|---|---|
| `LockedByLevel` | cadenas ferme, opacite reduite, badge niveau | Selection autorisee; achat interdit; afficher `Requiert le niveau X`. |
| `LockedByClass` | cadenas + badge de classe, motif diagonal | Selection autorisee; achat interdit; afficher classe requise. |
| `LockedByPrerequisite` | connecteur non alimente, icone chemin bloque | Selection autorisee; afficher chaque prerequis manquant. |
| `LockedByPrerequisite` + `Not enough skill points.` | icone point barre, compteur budget | Selection autorisee; afficher `Points insuffisants`, sans le presenter comme prerequis de graphe. |
| `Available` | contour accentue, point lumineux statique, libelle `Achetable` | Focus puis achat explicite. |
| `Purchased` | coche, remplissage actif, rang visible | Selection et apercu; achat du rang suivant si non max. |
| `Maxed` | embleme maximum, rang plein, connecteur final | Selection et apercu; aucune mutation d'achat. |

L'enum actuel ne distingue pas `LockedByPoints` de `LockedByPrerequisite`. L'adaptateur peut deriver le sous-etat d'affichage depuis `LockReason` ou un mapping de commande, mais ne doit pas changer la logique d'achat ni comparer des textes localises dans le runtime. La meilleure option est un mapping UI semantique local aux presentations.

### 5.1 Legende

La legende, toujours accessible depuis le graphe, reprend au minimum:

- verrou niveau;
- verrou classe;
- prerequis manquant;
- points insuffisants;
- achetable;
- achete;
- maximum.

Elle est compacte sur mobile et reste lisible en monochrome.

## 6. Verrouillage et choix de classe

### 6.1 Avant le niveau 10

- L'ecran est ouvrable et pedagogique.
- Les trois onglets sont visibles.
- Les points gagnes sont affiches comme `reserve verrouillee`.
- Les noeuds communs affichent leur niveau requis et restent non achetables.
- L'onglet Classe affiche un panneau verrouille avec l'appel `Atteignez le niveau 10 pour choisir une classe.`
- Toute tentative d'achat ou de choix retourne une erreur non destructive et conserve le focus sur l'element concerne.

### 6.2 Au niveau 10 sans classe

- L'ouverture de l'ecran met en avant le panneau `Choisir une classe`.
- Les cinq cartes autorisees sont visibles: RoyalGuard, Striker, Nurturer, Scout, Alchemist.
- Chaque carte montre son identite, deux ou trois effets d'orientation et le nombre de noeuds de branche.
- Une classe est preselectionnee uniquement pour le focus, jamais pour la selection effective.
- Le bouton `Confirmer la classe` reste desactive tant qu'aucune carte n'est selectionnee.
- La confirmation appelle `TryChooseClass` avec la classe choisie.
- Apres succes, le profil actif, la branche Classe et les points sont rebindees dans la meme frame UI logique.
- Apres echec, le message localise est inline et la selection reste modifiable.

### 6.3 Classe deja choisie

- La classe courante est visible dans l'en-tete et dans l'onglet Classe.
- Les noeuds d'une autre classe ne sont pas presentes dans la liste.
- Le changement de classe n'est pas declenche par le bouton Reset.
- En production, toute re-specialisation doit passer par un fournisseur de cout, cooldown et confirmation; ces donnees ne sont pas dans le modele lu et ne doivent pas etre simulees par l'UI.
- En preview locale, `TrySetLocalClass` est la voie de test explicite; elle remet les rangs a zero selon le modele et conserve le badge non officiel.

## 7. Achat et apercu

Flux nominal:

1. Le joueur selectionne un noeud.
2. L'inspecteur affiche le rang suivant, son effet et son cout.
3. Le joueur active `Acheter le rang X`.
4. L'UI bloque les activations repetees pendant le rebinding.
5. Le controleur appelle `TryPurchase(skillId, out error)`.
6. En cas de succes, la vue est reconstruite via `PlayerSkillTreeView.Build(state)`.
7. Le budget, le rang, les connecteurs, l'inspecteur et le profil actif sont actualises ensemble.
8. Un feedback court annonce le nouveau rang; le focus reste sur le noeud achete.

L'achat ne doit pas etre applique localement dans le composant visuel. Le modele reste l'autorite de validation. Le preview avant achat doit presenter le delta de bonus, pas promettre une valeur officielle si l'ecran est en local.

## 8. Reset et erreurs

### 8.1 Reset

Le bouton `Reset talents` est hors de la zone d'achat et toujours accompagne d'une icone et d'un texte.

- Ouverture: dialogue de confirmation avec nombre de rangs concernes et points rendus.
- Production: afficher uniquement cout et cooldown fournis par la couche de progression; sinon l'action est indisponible avec raison.
- Preview locale: afficher `Reset local gratuit - aucun gain officiel`, puis appeler `TryResetSkills`.
- Succes: rangs vides, points depenses a zero, points non depenses egaux aux points attribues, profil sans bonus de competences.
- Echec: aucun changement visuel optimiste; afficher l'erreur et conserver le noeud selectionne.
- Le reset ne change jamais de classe.

### 8.2 Table des erreurs

Les textes du modele sont des signaux fonctionnels. Ils doivent etre convertis en messages localises, courts et actionnables.

| Signal modele | Message UI | Action suggeree |
|---|---|---|
| `Class selection requires level 10.` | `Classe disponible au niveau 10.` | Afficher progression vers le niveau 10. |
| `Neutral cannot be selected as a level 10 class.` | `Choisissez une classe specialisee.` | Revenir aux cinq choix valides. |
| `Class is already selected; re-specialization is required.` | `Cette classe est deja active. Utilisez la re-specialisation.` | Ouvrir l'information de re-specialisation. |
| `Unknown skill.` | `Cette competence n'est plus disponible.` | Rebuild de la vue et focus sur l'onglet. |
| `Required level is not reached.` | `Niveau requis non atteint.` | Afficher le niveau requis. |
| `Skill does not belong to the selected class.` | `Cette competence appartient a une autre classe.` | Retirer le noeud parasite et rebinder. |
| `Choose a class before purchasing class skills.` | `Choisissez une classe avant d'acheter une competence de classe.` | Ouvrir le choix de classe. |
| `Skill is already at maximum rank.` | `Cette competence est au maximum.` | Afficher l'etat maximum. |
| `Prerequisites are not satisfied.` | `Completez les prerequis affiches.` | Focus sur le premier prerequis manquant. |
| `Not enough skill points.` | `Points disponibles insuffisants.` | Montrer points actuels et cout. |
| `Level override is available only in the local preview.` | `Cette commande est reservee a la preview locale.` | Ne pas afficher en retail. |
| `Class override requires level 10.` | `Le changement local exige le niveau 10.` | Afficher le verrou niveau. |

Une erreur de commande ne ferme pas l'ecran, ne consomme pas de point et ne change pas la classe.

## 9. Navigation et commandes

### 9.1 Clavier

- `Tab` / `Shift+Tab`: ordre de focus stable: fermeture, en-tete, onglets, graphe, inspecteur, reset.
- Fleches: deplacement spatial entre noeuds, puis onglet ou inspecteur si limite atteinte.
- `Enter` / `Space`: selection d'un noeud, activation d'un onglet ou activation d'une action non destructive.
- `Enter` sur `Acheter`: ouverture de la confirmation d'achat si le produit demande confirmation; second `Enter`: validation.
- `Escape`: ferme dialogue, puis inspecteur, puis ecran dans cet ordre.
- Focus visible obligatoire, jamais supprime par le zoom du graphe.

### 9.2 Manette

- D-pad ou stick gauche: navigation spatiale entre noeuds.
- LB/RB: onglet precedent/suivant.
- A: selection ou activation.
- X: achat depuis l'inspecteur quand disponible.
- B: retour/fermeture du sous-panneau puis de l'ecran.
- Y: ouverture de la legende; ne pas utiliser Y pour Reset.
- LT/RT ou stick droit: zoom du graphe si la plateforme expose ces commandes; sinon aucun raccourci destructif.
- Chaque focus manette annonce nom, rang, etat et raison de verrouillage.

### 9.3 Mobile

- Tap sur un noeud: selection et ouverture de l'inspecteur.
- Tap sur le bouton d'achat: action explicite; le tap sur le noeud n'achete jamais directement.
- Glisser dans la zone graphe: pan interne.
- Pincer: zoom interne borne; les gestes ne se propagent pas a la WorldMap.
- Double tap: recentrage du graphe uniquement si ce geste est disponible et annoncable; pas d'achat.
- Cibles tactiles: 44 x 44 dp minimum pour les commandes, hitbox de noeud 48 x 48 dp minimum.
- Les listes et dialogues defilent dans leur panneau; pas de defilement global qui ferait disparaitre l'action principale.

## 10. Accessibilite

- Contraste texte/panneau >= 4.5:1; composants et focus >= 3:1.
- Chaque etat couleur a un symbole et un texte.
- Les connecteurs de prerequis ont une variante de motif ou de largeur pour le monochrome.
- Texte redimensionnable jusqu'a 200 % sans chevauchement; aucune information critique en tooltip uniquement.
- Lecteur d'ecran: onglet, noeud, rang actuel/max, effet, cout, prerequis et raison sont exposes comme une phrase coherente.
- Les noeuds verrouilles restent navigables pour apprendre pourquoi ils sont bloques.
- Option mouvement reduit: supprimer pulse et transitions de zoom, conserver les changements d'etat.
- Option taille de cible: agrandir hitboxes sans changer la grille logique.
- Les icones seules ont un nom accessible et un tooltip au focus.
- Les textes de classe et d'effet passent par une table de localisation, sans afficher les identifiants bruts en retail.
- Le focus ne doit jamais etre perdu apres erreur, reset refuse ou rebinding.

## 11. Responsive

### Paysage tablette et desktop

- En-tete: 64 px environ, sans taille de police proportionnelle au viewport.
- Onglets: bande horizontale fixe, hauteur 52-60 px.
- Graphe: zone centrale dominante, inspector fixe a droite d'environ 320 px.
- Barre d'action: dans l'inspecteur ou en pied de panneau, toujours visible quand un noeud est selectionne.
- La carte peut rester visible en arriere-plan ou sous un voile leger non opaque; aucune UI fixe ne doit etre enfant du contenu pan/zoom de la carte.

### Portrait telephone

- En-tete compacte, XP et points empiles sur deux lignes si necessaire.
- Onglets en defilement horizontal avec le libelle complet `Ressources / Evolution` jamais coupe au milieu d'un mot.
- Graphe au premier plan avec une hauteur minimale de 48 % du viewport.
- Inspecteur en bottom sheet: replie par defaut, ouvert a environ 36 % du viewport, extensible sans masquer definitivement l'onglet et le graphe.
- Choix de classe en plein panneau mais sans texte hors cadre; une carte de classe par ligne si la largeur est insuffisante.
- Une seule confirmation ouverte a la fois.

### Points de rupture a verifier

- 1920 x 1200
- 1280 x 720
- 720 x 1280
- 390 x 844

Pour chaque viewport, valider absence de collision entre en-tete, onglets, graphe, inspector, bouton d'achat, reset et clavier virtuel.

## 12. Mapping modele -> UI

| Donnee | Utilisation UI | Regle |
|---|---|---|
| `PlayerSkillTreeView.PlayerClass` | classe active, badge, choix | Ne pas recalculer depuis les rangs. |
| `PlayerSkillTreeView.PlayerLevel` | niveau et verrous | Source unique du niveau affiche. |
| `PlayerSkillTreeView.SkillPointsAvailable` | reserve/budget | Libelle dependant du niveau; valeur jamais recalculee. |
| `Tabs[i].TreeId` | identite onglet | Ne pas indexer uniquement par position dans le code UI. |
| `Tabs[i].Title` | libelle de base | Peut passer par localisation, conserver la valeur canonique. |
| `Tabs[i].IsLocked` | verrou global | Ne pas rendre l'onglet inaccessible; rendre l'action interdite et explicable. |
| `Tabs[i].LockReason` | message global | Localiser sans le supprimer. |
| `Tabs[i].Nodes` | graphe | Filtrer uniquement par l'etat deja produit. |
| `Node.Definition.SkillId` | cle stable | Utiliser pour layout, localisation et commande d'achat. |
| `Definition.RequiredLevel` | niveau requis | Afficher au noeud et dans l'inspecteur. |
| `Definition.MaxRank` | rang maximum | Afficher dans le compteur de rang. |
| `Definition.CostPerRank` | cout | Afficher le cout du prochain rang. |
| `Definition.PrerequisiteSkillIds` | connecteurs et liste | Afficher chaque prerequis, y compris les branches multiples. |
| `Node.CurrentRank` | progression | 0 = non achete; valeur positive = rang actif. |
| `Node.Availability` | etat visuel | Mapper sans changer la decision. |
| `Node.CanPurchase` | disponibilite bouton | Condition finale d'activation, modele d'abord. |
| `Node.LockReason` | feedback de verrou | Toujours visible dans l'inspecteur. |
| `PlayerSkillState.BuildProfile()` | apercu bonus actif | Presenter un profil immuable; ne pas muter combat/economie depuis l'UI. |
| `PlayerSkillState.IsLocalPreview` | badge et garde | Desactiver tout wording officiel et toute persistance non prevue. |

### 12.1 Dependances UI a fournir hors runtime

Le code lu ne contient pas les champs narratifs de la spec (`display_key`, `description_key`, `exclusive_group`, `schema_version`) et ne contient pas de positions de graphe. Builder-A doit traiter ces elements comme des donnees de presentation versionnees:

- `skill_id -> nom localise`;
- `skill_id -> description localisee`;
- `skill_id -> icone et motif d'etat`;
- `skill_id -> position par onglet`;
- `effect_key -> format de valeur et texte d'apercu`;
- `skill_id -> prerequis affichables`.

Cette table ne doit pas redefinir les prerequis, les couts, les rangs ou la disponibilite. En cas de divergence, le modele gameplay gagne et l'ecran affiche une erreur de configuration en mode developpement.

## 13. Plan d'integration Unity pour Builder-A

Ce plan prepare une integration sans toucher a la scene dans cette passe. La cible est une surface UI code-driven instanciee a la demande sous le Canvas/overlay fixe deja utilise par l'application. Builder-A doit identifier le root reel au moment de l'implementation et ne pas supposer un nom de scene ou de GameObject non verifie.

### Phase A - Preflight en lecture seule

1. Verifier l'etat de travail et ne pas revert les changements d'autres agents.
2. Lire la scene active et le bootstrap sans les resauvegarder.
3. Identifier le Canvas espace ecran fixe, le routeur d'ouverture/fermeture et le fournisseur de progression local.
4. Verifier que l'arbre ne sera pas enfant du transform pan/zoom de la WorldMap.
5. Confirmer les quatre viewports de recette: 1920 x 1200, 1280 x 720, 720 x 1280, 390 x 844.
6. Arreter la passe si l'integration exige une modification de scene serialisee; demander alors une decision d'architecture plutot que d'editer la scene.

### Phase B - Adapter de presentation

1. Creer ou reutiliser un adaptateur UI qui recoit `PlayerSkillState` et produit `PlayerSkillTreeView.Build(state)`.
2. Ne dupliquer ni les verrous, ni le calcul de points, ni les prerequis dans les composants visuels.
3. Ajouter la table de presentation des noms, descriptions, icones et positions par `SkillId`.
4. Ajouter le resume XP en lecture seule si une source existante le fournit; ne pas le recalculer a partir de `PlayerLevel`.
5. Exposer un `UiBlockReason` semantique a partir de l'etat de vue, sans modifier l'enum runtime.
6. Ajouter le marqueur local derive de `IsLocalPreview`; en mode officiel, ne pas afficher le marqueur preview.

### Phase C - Shell et navigation

1. Instancier l'ecran uniquement a l'ouverture depuis le routeur UI existant.
2. Rendre en premier l'en-tete, les onglets, le graphe, l'inspecteur et les actions de fermeture/reset.
3. Ajouter les bindings clavier, manette et mobile du present contrat.
4. Isoler le pan/zoom du graphe de celui de la carte par un event boundary explicite.
5. Implementer le focus initial: onglet actif, puis premier noeud achetable ou premier noeud verrouille explicable.
6. Reappliquer le focus apres chaque rebind et chaque erreur.

### Phase D - Flux de classe

1. A niveau < 10, afficher l'onglet Classe verrouille et le message niveau.
2. A niveau >= 10 et classe `Neutral`, afficher le choix des cinq classes valides.
3. Confirmer par `TryChooseClass` puis reconstruire la vue.
4. Si la commande echoue, afficher l'erreur sans masquer les cartes.
5. Apres choix, filtrer la branche par `ClassId` fourni par le modele; ne jamais afficher une classe concurrente.
6. Garder la re-specialisation et le reset comme deux commandes separees.

### Phase E - Achat, reset et rebinding

1. Selectionner le noeud sans mutation.
2. Afficher apercu, prerequis, cout et raison.
3. Appeler `TryPurchase` uniquement depuis le bouton d'achat confirme.
4. Rebinder depuis `PlayerSkillTreeView.Build(state)` apres succes.
5. Appeler `TryResetSkills` seulement apres confirmation.
6. Recalculer le profil via `BuildProfile()` et afficher l'etat actif; ne pas ecrire directement dans les systemes Combat ou Ressources.
7. En preview locale, conserver une etiquette non officielle et ne declencher aucune persistence serveur, recompense, XP ou inventaire.

### Phase F - Verification Builder-A

Matrice minimale a retourner dans le rapport d'integration:

| Gate | Action | Resultat attendu |
|---|---|---|
| UI-SKILL-01 | Ouvrir au niveau 9 | Trois onglets; Classe verrouillee; points presentes comme reserve. |
| UI-SKILL-02 | Activer chaque onglet au niveau 9 | Aucun achat possible; raison visible. |
| UI-SKILL-03 | Passer au niveau 10 en preview | Choix de classe presente; Neutral non selectionnable. |
| UI-SKILL-04 | Choisir Scout | Cinq noeuds Scout, aucun noeud d'une autre classe. |
| UI-SKILL-05 | Selectionner un prerequis manquant | Etat et raison lisibles; bouton inactif. |
| UI-SKILL-06 | Acheter `combat_foundation` | Rang, points, connecteur et apercu mis a jour ensemble. |
| UI-SKILL-07 | Acheter sans points | Aucun changement; message points insuffisants. |
| UI-SKILL-08 | Reset local | Tous les rangs a zero; budget initial; profil vide. |
| UI-SKILL-09 | Erreur de commande | Focus conserve; aucun effet partiel. |
| UI-SKILL-10 | Clavier/manette/mobile | Navigation complete sans action destructive accidentelle. |
| UI-SKILL-11 | Accessibilite | Contraste, symboles non couleur, focus et libelles accessibles. |
| UI-SKILL-12 | Responsive | Aucun chevauchement aux quatre viewports. |
| UI-SKILL-13 | Carte | Pan/zoom de l'arbre ne deplace pas la WorldMap; UI fixe. |
| UI-SKILL-14 | Autorite | Preview visible comme locale; aucun gain officiel. |

Les preuves attendues sont un journal de verification, les resultats EditMode deja existants ou leur reprise ciblee, et des captures de validation produites par Builder-A lors de son integration. Cette preparation ne produit ni capture PNG ni APK.

### Phase G - Definition de fini et rollback

Integration acceptee seulement si:

- le contrat des trois onglets est respecte;
- les verrous et raisons sont visibles avant le niveau 10;
- le choix de classe est explicite et isole;
- les achats et resets passent par le modele;
- le profil actif est rebinde apres mutation;
- clavier, manette, tactile, accessibilite et responsive sont verifies;
- aucune scene n'a ete resauvegardee pour rendre l'UI disponible;
- les limites preview/non officielle sont preservees;
- le rapport Builder-A liste les fichiers runtime effectivement touches et leur justification.

Rollback: fermer le routeur de l'ecran et retirer l'instanciation code-driven; ne pas restaurer ou ecraser les changements d'autres agents et ne pas modifier la scene pour masquer un probleme.

## 14. Criteres d'acceptation UI finaux

- `Combat`, `Ressources / Evolution` et `Classe` sont visibles et navigables.
- Avant le niveau 10, aucun achat n'est possible et la reserve de points est clairement distinguee d'un budget depensable.
- Au niveau 10, une classe valide doit etre choisie avant tout achat de classe.
- Les cinq classes valides sont presentes; `Neutral` n'est pas une option finale.
- Chaque noeud expose rang, max rank, cout, niveau, prerequis, effet et etat.
- Les sept presentations d'etat sont distinguables en couleur et en monochrome.
- Un prerequis manque et un budget insuffisant ne sont jamais affiches comme la meme raison a l'utilisateur.
- L'achat et le reset donnent un feedback de succes ou d'erreur et ne produisent aucun effet partiel.
- Le reset est confirme, separe du chemin d'achat et ne change pas de classe.
- Le profil actif est visible et reconstruit depuis le modele.
- Les commandes clavier, manette et mobile sont completes et non destructives par defaut.
- Le focus, le contraste, les symboles et le lecteur d'ecran couvrent les etats verrouilles.
- Le graphe reste responsive aux viewports cibles sans masquer l'action principale.
- La preview locale est marquee et ne devient jamais un gain officiel.
- Aucun changement de scene, PNG, APK, serveur ou donnee reelle n'est requis par ce handoff.

## 15. Risques connus a traiter par Builder-A

| Risque | Parade obligatoire |
|---|---|
| `SkillPointsAvailable` semble achetable avant 10 | Libelle reserve verrouille et action globalement bloquee. |
| Branche Classe vide a niveau 10 avec `Neutral` | Afficher le choix de classe avant la liste de noeuds. |
| `LockedByPrerequisite` masque un manque de points | Sous-etat semantique local et message specifique. |
| Libelles spec absents des definitions | Table de presentation versionnee, jamais de raw `skill_id` en retail. |
| Layout absent du modele | Table de positions par ID, validation d'absence de chevauchement. |
| Achat double par tap rapide | Garde d'activation et rebind modele unique. |
| Reset confondu avec re-specialisation | Deux boutons, deux confirmations et deux contrats. |
| UI attachee au transform de la carte | Attachement au root Canvas/overlay fixe verifie en preflight. |
| Preview presentee comme officielle | Badge permanent, texte interdit et assertions d'autorite. |

### Verdict handoff

```text
PLAYER_SKILL_TREE_UI_CONTRACT = READY
BUILDER_A_SCENE_FREE_PLAN = READY
PRE_LEVEL_10_LOCK = SPECIFIED
LEVEL_10_CLASS_CHOICE = SPECIFIED
NODE_VISUAL_STATES = SPECIFIED
PURCHASE_RESET_ERRORS = SPECIFIED
INPUT_ACCESSIBILITY_RESPONSIVE = SPECIFIED
OFFICIAL_PROGRESSION_ACTIVATION = NOT_REQUESTED
```
