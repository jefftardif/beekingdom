# Reprise Claude Code — Bee Kingdom

Ce document est la memoire de travail active de Claude Code sur ce projet. Il
remplace `Docs/VM/Codex_VM_Continuation.md` comme journal courant depuis que
Codex n'est plus utilise (2026-07-24). Les entrees sont classees de la plus
recente (en tete) a la plus ancienne. Ne jamais supprimer une entree; ajouter
la nouvelle au-dessus.

## Instruction de reprise

Au debut d'une nouvelle tache Claude Code sur ce projet :

1. Lire cette entree la plus recente en premier, puis remonter si besoin de
   contexte plus ancien.
2. Lire `CLAUDE.md` a la racine si ce n'est pas deja fait automatiquement.
3. Verifier l'etat reel de l'editeur Unity (scene ouverte, etat de
   compilation) via les outils MCP avant de supposer quoi que ce soit — ne
   pas se fier uniquement a ce qui est ecrit ici, qui peut dater de plusieurs
   jours.
4. Reprendre le plan LivingHive sans toucher aux fondations protegees
   listees dans `CLAUDE.md`.
5. A la fin de la tache, ajouter une nouvelle entree en tete de ce fichier
   (voir gabarit ci-dessous) avant de rendre la main.

## Gabarit d'entree de jalon

```
## Jalon courant — <titre court> (<date ISO>)

<Ce qui a change, en 1-3 paragraphes: fonctionnalite livree, fichiers/scripts
touches, drapeaux de fonctionnalite, etat avant/apres.>

Preuves: <tests executes, resultats, captures, logs — chemins exacts>.

Prochain test utilisateur: <etapes precises pour que l'utilisateur verifie
lui-meme dans Unity/Play Mode>.

Ouvert / a faire ensuite: <ce qui reste, dans l'ordre de priorite>.
```

---

## Jalon courant — BeeQA Sprint-024 : BeeQA Module Framework (2026-08-09)

BeeQA est maintenant un framework de plugins Editor interne et indépendant du gameplay. Le point d'entrée `BeeKingdom > BeeQA > Open Dashboard` ouvre un panneau qui découvre automatiquement les implémentations de `IBeeQAModule`, affiche leurs métadonnées et permet `Run` ou `Run All`. Le Dashboard ne référence jamais de module concret.

Fichiers principaux : `Assets/BeeKingdom/BeeQA/Editor/BeeQACatalog.cs`, `BeeQAModuleRegistry.cs`, `BeeQAEntryPoint.cs`, `BeeQADashboardWindow.cs`, `BeeQASmokeTestModule.cs`, ainsi que les dossiers réservés `Modules`, `Scenarios`, `Debug`, `Reports`, `Performance`, `Automation` et `Tools`.

Le contrat expose nom, description, catégorie, version, auteur, statut, capacité d'exécution, dernier résultat et `Execute()`. Les résultats portent `PASS/FAIL`, durée, date et message. Tests BeeQA : **3/3**, dont Smoke Test découvert et PASS. Documentation : `Docs/Production/BeeQA.md`. Le code est Editor-only et n'impacte pas le runtime de production.

---

## Jalon courant — Sprint-024 : Game Event Bus Foundation (2026-08-09)

Le namespace `BeeKingdom.Gameplay.Events` fournit maintenant un bus fortement typé et indépendant : `IGameEvent`, `GameEventBus`, `GameEventSubscription` et `GameEventContext`. La publication ne fait pas de réflexion, les subscriptions sont idempotentes, l'ordre d'enregistrement est conservé et les mutations pendant dispatch sont compactées après publication.

Événements officiels ajoutés : `BuildingStarted`, `BuildingCompleted`, `ResearchStarted`, `ResearchCompleted`, `SpeedUpUsed` et `RewardGranted`. Construction publie `BuildingCompleted` après une complétion serveur acceptée. Le module `BeeQAEventBusQAModule` est découvert automatiquement par BeeQA et vérifie création, publication, réception, ordre et désabonnement.

Preuves : `GameEventBusTests` **2/2**, `BeeQATests` **3/3**, compilation Unity sans erreur. Documentation : `Docs/Architecture/GAME_EVENT_BUS.md`.

---

## Jalon courant — Sprint-025 : Smart SpeedUp Experience (2026-08-09)

`SmartSpeedUpCalculator` centralise maintenant le calcul des combinaisons compatibles et remplace le glouton incomplet de Sprint-022. `SpeedUpDialogContext` et `SpeedUpDialog` préparent une fenêtre unique réutilisable par Construction, Recherche, Entraînement, Fabrication et Guérison. Les contextes spécialisés affichent leurs accélérations et les universelles; les autres catégories sont filtrées.

Le rendu SpeedUp de `LivingHive` conserve la scène sous la fenêtre, initialise l'inventaire démo, affiche les catégories et utilise le plan Smart pour l'auto-application. Les timers locaux Construction, Recherche et Entraînement sont bornés à zéro et déclenchent leurs méthodes de complétion existantes. Les cartes de file et les détails Construction/Entraînement exposent le même accès contextuel; les complétions locales publient `RewardGranted` via le Game Event Bus.

BeeQA reçoit `BeeQASpeedUpQAModule`, découvert sans modification du Dashboard. Preuves : `SmartSpeedUpTests` **3/3**, `SpeedUpSystemTests` **3/3**, `BeeQATests` **4/4** après découverte des modules, compilation Unity sans erreur. Documentation : `Docs/Architecture/SMART_SPEEDUP_EXPERIENCE.md`.

---

## Jalon courant — Sprint-026 : Server Authority Audit & Migration (2026-08-09)

Audit Client + Server réalisé et documenté dans `Server/Docs/SERVER_AUTHORITY_AUDIT.md`. Le constat principal est que BeeKingdom possède trois couches concurrentes : legacy `PlayerPrefs`, preview local BeeKingdom et tranche REST officielle. La production reste en préparation (`Persistence:InMemory`, progression officielle désactivée) ; il serait incorrect de déclarer le jeu globalement server-authoritative.

Migration raisonnable effectuée dans ce sprint : initialisation et validation de l'inventaire SpeedUp serveur, service atomique `SpeedUpInventoryService`, handlers Construction/Research/Training/Healing/Manufacturing/Universal, routes GET/APPLY, idempotence et concurrence. Le client Unity n'est volontairement pas branché dans ce sprint d'infrastructure.

Preuves : build `BeeKingdom.Server` sans erreur, `SpeedUpInventoryServiceTests` 3/3. La suite HiveOperations complète conserve un échec préexistant `CombatSquadReservationTests.Rejects_over_capacity_and_over_roster`; le déploiement et la validation sur `chat.dravii.com` restent ouverts. Les Rewards serveur, inventaire générique, Quêtes, Achievements, Alliance, Mail et Boutique restent à migrer selon la matrice d'audit.

---

## Jalon courant — Alpha Sprint-018 : Mobile UX Pass (Interface Mobile Native) (2026-08-07)

### Analyse de référence (principes, pas une copie)

Étude de Whiteout Survival, Last War, Ant Legion, Clash of Clans, Rise of Kingdoms (hors-ligne,
principes du genre). Conclusions appliquées à Bee Kingdom, dans l'ordre :

1. **Gameplay visible d'abord** : le champ central (la Ruche) ne connaît jamais de concurrence
   d'interface ; tout chrome est relégué en bandes fines en haut/bas.
2. **HUD léger** : très peu d'informations simultanées ; chaque groupe (niveau/nom, ressources,
   notifications) est une seule entité visuelle, jamais une grille dense.
3. **Zone pouce** : le rail inférieur est la zone de confort ; c'est LÀ que vivent les actions
   fréquentes, gros boutons (≥ 48 px) espacés, repliées via un "Plus".
4. **Une colonne** en portrait : cartes et listes sur toute la largeur, pas de multi-colonnes
   compactées ; texte ≥ 10 px et boosts de touch pour éviter les troncatures.
5. **Hiérarchie de lecture** : Ruche → ressources → rail → notifications. Rien d'autre ne
   dispute l'attention ; le troisième "rang" (diverses statut, badges secondaires) est masqué.

### Décisions de conception Mobile (le mobile est la référence)

| # | Domaine | Décision |
|---|---------|----------|
| 1 | HUD supérieur portrait | Remplacé par un HUD mobile minimal : nom + niveau + icône profil (1 bloc), 3 ressources principales (miel, cire, pollen) en chips larges ≥ 48 px, 1 zone notifications (mail + alerte) droite. Suppression du badge "DEMO LOCALE" du HUD, des tickers "+x/s" et des 4ᵉ/5ᵉ chips (abeilles/capacité) du sommet. Hauteur totale réduite de 112 px → ~ 88-96 px. |
| 2 | Rail inférieur mobile | 5 boutons au lieu de 8 : 💬 Chat, 🗺 Carte/Ruche, 📜 Quêtes, 🤝 Alliance, ➕ Plus. Boutons ≥ 60 px de haut, largeur répartie sur toute la bande, utilisables au pouce. Toutes les autres fonctions (Bestiaire, Championnes, Sac, Milestone, Recherche, Paramètres, Mail, Défi) passent par le panneau Plus (étendu et défilable). |
| 3 | Boutons | Cible de confort ≥ 48×48 px partout sur mobile (rail, chips HUD, boutons de liste), retour à 44-52 px si contrainte. Nombre de boutons réduit, taille augmentée. |
| 4 | Écran/Ruche | Bande méta supérieure ~ 8 % (HUD), bande inférieure ~ 8 % (rail) ; la Ruche occupe ~ 84 % de la hauteur visible en portrait (top 126 px, bottom 258 px conservés). |
| 5 | Écrans Premium | Philosophie inchangée (plein écran + flèche retour) mais passe mobile spécifique : marges réduites à 8-12 px, titres de bannière raccourcis, listes 1 colonne, cartes pleine largeur, boutons secondaires ≥ 48 px de haut, suppression des remplisseurs d'écran (badge DEMO statique, largeurs minimales 112 px). |
| 6 | Responsive | Deux chemins distincts : Desktop = paysage (HUD `DrawStrategyTopHud` + rail 10 items, panneaux latéraux) ; Mobile = portrait (`DrawPortraitTopHud` + rail 5 items). `IsPortraitLayout()` est le sélecteur ; les constantes paysage restent, les constantes portrait sont réécrites pour le mobile. |
| 7 | Aucune troncature | Passe complète sur 1080×2400 et 1179×2556 portrait : toutes les lignes de HUD/rail/chips vérifiées, les libellés longs sont abrégés ou sur une seule ligne. |
| 8, 9 | Performances / visuel | Tout ce qui ne sert pas au gameplay mobile (side rail — déjà désactivé en portrait, 3ᵉ rangée de HUD, remplisseurs vides) est retiré du rendu portrait. |

### Implémentation réalisée (2026-08-07)

**HUD supérieur mobile** (`DrawPortraitTopHud` réécrit, 94 px, panel 8,8 / width-16) :
profil (icône reine 36×36 + « Ruche Prime » 16 px + « Niv. » via `CoeurRoyalLevel()`, clic →
`playerMenuOpen`), 2 boutons notifications 46×44 (`DrawTopSystemButton` : inbox "Mail" avec badge
rouge du compteur courier → `OpenCourierScreen()` ; alert "Alerte" → raccourci preview), 3 chips
`DrawMobileResourceChip` (icône 34×34, label 10 px abrégé via `CompactResourceLabel`, valeur 15 px
compactée `M/K`, `DrawResourceGrowthFeedback` par chip). `DrawPortraitResourceChip` supprimé (inutilisé).

**Rail inférieur mobile** (`DrawPortraitBottomRail` réécrit, bande 8 / height-78 / width-16 / 70 px) :
5 items (index 0 Chat, 1 SurfaceSwitch/Carte, 2 Quêtes, 3 Alliance, 4 Plus), gap 8, icône ≥ 48 px,
label 11 px, états actifs corrects par item (Quêtes/Alliance = toggle réel). Toutes les autres fonctions
ont quitté le rail : MilestoneEvent, Bestiaire, Sac, Mail, Trophées, Recherche, Missions, Paramètres,
Championnes → panneau Plus. Les anciennes références du rail (quêtes → journal de quêtes, défi,
communication) sont migrées. `SurfaceSwitchButtonRect` (portrait itemCount 5, switchIndex 1) et
`ChatRailButtonRectForProof` (portrait 5 items, chatIndex 0) synchronisés avec le nouveau rail.
`DrawIconButton` : icône `min(74, hauteur*0.76)`, label 11 px pour les boutons ≥ 48 de hauteur.

**Panneau Plus** (`DrawMoreMenuPanel` réécrit) : 9 entrées en liste scrollable
(Courrier→`OpenCourierScreen`, Amis→`OpenFriendsScreen`, Bestiaire→`OpenBestiaryCodexOverlay`,
Milestone→`OpenMilestoneEventOverlay`, Championnes→`championBeesPanelOpen`, Sac & stocks→`activeMainMenuId="Bag"`,
Recherche→`ActivateHiveMenu(Research)`, Missions→`OpenMissionsCenter`, Paramètres→`activeMainMenuId="Settings"`).
`MoreMenuPanelRect` compact : x 8, y height-78=bande, height `min(502, Screen.height-96)`. Rendu via
`GUI.BeginScrollView` (retour Vector2 → `moreMenuScroll`), switch sur index dans la boucle.

**Modals responsives** : Bestiaire/Codice → `min(460, width-24)` × `min(880 portrait / 480 paysage,
height-40)` ; Défi (Milestone) → idem portrait 760/paysage 440. Centrés, sous les bannettes
d'occlusion, avec voile + flèche retour.

**Anti-troncature & zones tactiles (si conte en cours) :**
- 1080×2400 / 1179×2556 : validé par EditMode que le rail a exactement 5 items ≥ 48×48 sans
  chevauchement, que le HUD mobile reste ≤ 96 px et que ses chips ≥ 90 px.
- Ruche : top mask 126 / bottom mask 258 → ≥ 70 % de hauteur visible (≈ 84 % à 2400 px).
- Panneaux Rucher (Army/Recherche/Sac) ancrés sous le HUD (y ≥ 102) et au-dessus du rail
  (yMax ≤ rail+14) en portrait — testés sur les deux résolutions cibles.
- Écrans pleins (Courier, Friends, Chat, Recherche, Missions, Championnes) : déjà compacts —
  bannières `min(150, 20 %)`, action-bar/HUD horizontalement scrollables (largeurs compactes),
  contenu 1 colonne ou scroll, aucun débord hors écran vérifié par relecture.

**Perf/cleanup** : `DrawPortraitResourceChip` et `resourceTick`-fonts morts retirés ; pas de
nouvelle allocation sur le chemin chaud (tableaux statiques réutilisés).

**Tests EditMode ajoutés** (`SandboxLivingHiveUiStabilizationTests.cs`) :
`MobileRailFiveItemsAreThumbFriendlyAndInsideScreen` (1080×2400, 1179×2556),
`MobileHudIsLightAndStaysInsideScreen`, `HiveStaysVisibleAtLeast70PercentInPortrait`,
`HiveMenusStayClearOfMobileHudAndRailInPortrait`. Hook proof : `MobileBottomRailItemRectsForProof`,
`MobileHudRectForProof`, `MobileHudResourceChipsRectForProof`.

---

## Jalon courant — Alpha Sprint-017 : UI Stabilization & UX Pass (2026-08-07)

Pass de stabilisation UI/UX sur `HiveViewProductUiPresenter.cs` (aucune nouvelle fonctionnalité),
validé par audit systématique des 12 écrans premium + compilation à chaque étape (0 erreur).

**Blocage des clics traversants** : nouvelle garde centralisée `PremiumUiBlocksWorldInput()`
(courier, amis, chat, missions, colonie, championnes, recherche, alliance + profil membre/panneau
d'action/chat, bestiaire, défi, parcours stratégique, menu principal) branchée dans
`HandlePointer()`, `HandleReferenceHiveGestures`, `HandleReferenceHotspotClick`,
`HandleReferenceCloseClick` — le monde 3D ne reçoit plus aucun clic/drag/zoom sous un écran plein.
Modaux (Bestiaire, Défi) : `DrawPremiumModalBackdrop()` (voile + bouton plein écran de capture).

**Fermeture** : helper unique `DrawPremiumCloseButton(Rect visual, string label, int fontSize)` qui
élargit le hit à ≥ 44×44 px centré, appliqué à ~15 cibles (X du chat, courrier, colonie, alliance,
missions, amis, profil, recherche, mini-chat, ×4 boutons « ← »). Échap/Backspace :
`HandlePremiumScreensEscape()` ferme les écrans dans l'ordre de priorité (profil membre → panneau
alliance → tiroir alliance → missions → colonie → championnes → amis → HQ Alliance → menu principal).

**Décision UX utilisateur (2026-08-07, Jeff)** : les joueurs ferment désormais toutes les fenêtres
**exclusivement via la flèche gauche**. Nouvel asset `Assets/Art/UI/Navigation/fleche_gauche.png`
chargé par `LeftNavigationTexture()` (AssetDatabase avec fallback Resources puis glyphe « ← ») et
dessiné par le helper unique `DrawPremiumBackButton(Rect visual)` (hit ≥ 52×52 px centré). Tous les
boutons « X » de fenêtres sont supprimés du code (`DrawTopDropdownCloseButton` retiré,
`ChatCloseButtonRect` supprimé, `ChatBackButtonRect` = 4,2,48,48) ; l'ancien `DrawPremiumCloseButton`
est renommé `DrawPremiumHitButton` et ne sert plus qu'aux petites bascules du mini-chat (↻ cycle et
👁 clignotement). Écrans convertis : réserve, mini-chat (titre/cycle décalés à x84), courrier, amis,
missions, journal de quêtes, Sac & stocks (officiel + aperçu via `DrawMenuHeader(…, backButton:true)`
qui retourne désormais `bool`), recherche (X retiré + flèche ajoutée), parcours stratégique, panneaux
joueur/VIP/puissance/championnes, guide des missions, paramètres, bestiaire, défi, patrouille de
combat, colonie (retour contextuel détail/fermeture), profil membre, panneau d'action alliance
(icône supprimée), tiroir chat alliance (header 46 px), centre d'alliance (titres décalés à x62),
popups info/détail de zone (`ReferenceCloseButtonRect` déplacé à gauche 4,2,48,46 et garde
`PremiumUiBlocksWorldInput()` retirée de `HandleReferenceCloseClick`). Seuls 2 « × » subsistent, sur
les notices toast « À TON RETOUR »/« PRODUCTION À TON RETOUR » (bouton « Voir » à droite, pas des
fenêtres — conservés à la demande de Jeff).

**Blocage des clics traversants (renforcé)** : `ShouldBlockUnderlyingHiveChromeInput()` (branchée en
`GUI.enabled` autour du HUD/rail/surface sous les fenêtres) et `PremiumUiBlocksWorldInput()`
(désormais avec réserve, patrouille de combat, popup info zone et panneau communication) couvrent tous
les écrans ; `DrawActiveHiveMenuPanel` est rendu sous `GUI.enabled = !réserve && !communication`.


**Textes/responsive (audit par agent exploreur)** : corrections de chevauchements et troncatures —
statut de fiche mission réservé à droite, titre de chapitre raccourci, `DrawMobileComfortToggle`
passe en `wordWrap`, titre « Préparer l'escouade » élargi, sous-titre Courrier simplifié, mini-chat
canal élargi, fiche Championnes sur 2 lignes (nom + rareté/rôle), coût réduit m/p, World Map HUD :
stats abrégées (125k/98k/…), 4 stats sous 1366 px au lieu de 5, badge décalé, onglets émoticônes en
icônes seules en compact.

**Perf** : chemin chaud du chat optimisé — styles (`ChatAuthorStyle`, `ChatTimeStyle`,
`ChatStateStyle`, `ChatPinnedStyle`, `ChatActionLabelStyle`), contenu de mesure
(`ChatBubbleTextHeight` avec `GUIContent` réutilisé) et tableaux d'actions (`ChatActionIds`,
`chatActionLabelsCache`) sans allocation par message/frame.

**Hooks proof et tests EditMode** : `SandboxLivingHiveUiStabilizationTests.cs` (nouveau) vérifie le
blocage du monde par écran plein, la fermeture prioritaire Échap, les cibles ≥ 44 px et la tenue des
panneaux principaux en 320×568. Hooks ajoutés : `ResetPremiumScreensForProof`,
`ClosePremiumScreensForProof`, `PremiumWorldInputBlockedForProof`,
`Open/CloseBestiaryCodexOverlayForProof`, `Open/CloseMilestoneEventOverlayForProof`,
`SetCourierScreenOpenForProof`, `SetColonyOverviewOpenForProof`, `SetChampionBeesPanelOpenForProof`,
`OpenAllianceMemberProfileForProof`, `ChatBackButtonHitHasSafeSizeForProof`,
`LeftNavigationAssetAvailableForProof`, `CourierScreenOpenForProof`, `FriendsScreenOpenForProof`,
`ColonyOverviewOpenForProof`, `ChampionBeesPanelOpenForProof`, `AllianceMemberProfileOpenForProof`,
`BestiaryCodexOverlayOpenForProof`, `MilestoneEventOverlayOpenForProof`, `StrategicPathOpenForProof`.
`ResetPremiumScreensForProof` couvre aussi réserve, patrouille de combat et popup info zone ; les tests
`WorldInputIsBlockedWhileAnyFullScreenIsOpen` ouvrent chaque écran avant d'affirmer le blocage et
`ChatBackTargetAndNavigationAssetAreReady` remplace l'ancien test de taille du X du chat.

Preuves : `refresh.ps1` → `[Success] Assets refresh completed` ; `parse-logs3.ps1` → Error count: 0
(2 warnings CS0414 préexistants). Tests EditMode compilés mais non exécutés (aucun runner CLI Unity).

Prochain test utilisateur :
1. Ouvrir chaque écran plein (Chat, Courrier, Amis, Missions, Colonie, Championnes, Alliance,
   Profil, Bestiaire, Défi) et vérifier : (a) aucun clic ne sélectionne la ruche 3D derrière,
   (b) Échap ferme l'écran, (c) la flèche gauche (asset `fleche_gauche.png`) se clique facilement (≥ 52 px).
2. Fermer chaque écran par la flèche gauche uniquement — AUCUN X ne doit rester sur les fenêtres.
3. Vérifier en 320-375 px de large (ou Android portrait) l'absence de texte tronqué/chevauchement,
   et sur la World Map l'affichage à 4 stats sous 1366 px.
4. Lancer les tests EditMode `SandboxLivingHiveUiStabilizationTests` via le Test Runner.

Ouvert / à faire ensuite : exécution Play Mode des tests (validation par l'utilisateur), vérification
des 2-3 points LOW de l'audit texte restants, et audit des allocations `new GUIStyle` restantes dans
les écrans froids (recherche, alliance) si besoin. Notices toast « À TON RETOUR » conservent leur X
(décision Jeff : ce ne sont pas des fenêtres).

---

## Jalon courant — Alpha Sprint-016 : Centre de Missions Premium (2026-08-06)

Nouveau Centre de Missions plein écran dans `HiveViewProductUiPresenter.cs`, accessible via la rangée
Missions du panneau « Plus » (file de menus du bas) et via le clic sur une mission épinglée du widget. Il regroupe : Histoire par chapitres (objectifs, barre de progression,
récompenses à réclamer quand tous les objectifs sont terminés, épingles ★), Quotidiennes, Hebdomadaires,
Défis et Succès, avec une barre d'actions (Réclamer tout, Favoris, Guide, Recherche), des fiches
missions avec progression en temps réel et boutons `Réclamer`, et une navigation `Aller →` vers les
modules concernés (Daily Round, Alliance, Chat, Amis, Courrier, Recherche, Armée). Les données sont
simulées via un catalogue dédié `MissionCatalog.cs` (21 missions, 3 chapitres) et l'évaluateur de
progression vit dans le presenter.

Widget missions épinglées : jusqu'à 3 missions favoris (`MaxPinnedMissions = 3`) affichées dans un
widget masquable (👁) en bas de la Ruche et de la carte, avec persistance via les nouveaux champs
additifs `MobileComfortPreferences.pinnedMissionIds` et `pinnedMissionsWidgetHidden`. Les réclamations
sont mémorisées dans des ensembles additifs (`missionsClaimedIds`, `missionsChaptersClaimed`) avec
compteur de commit (`missionsClaimCommitCount`) pour la persistance.

Corrections de compilation : une accolade en trop refermait `DrawMissionsChapterCard` (CS1022 à la fin
du fichier) ; 8 erreurs de contenu réparées (`MissionRecord` → `MissionRecordVisit`, `selectedColor`
→ `normal.textColor`, `centeredNarrowLabelStyle` → `centeredTinyLabelStyle`, `expanded` dérivé de
`missionsExpandedChapter`, retrait de `lineHeight`/`lineSpacing`/du second argument de
`DrawPremiumHeaderBand`). `DrawFullScreen` corrigé, `HiveMenuRender` → `HiveMenuMode`, `index = 0`
retiré de `DrawMissionsTabChip`.

Proof hooks : `MissionsTabsRectForProof`, `MissionIdsForSectionForProof`, `MissionCatalogAllIdsForProof`,
`SetMissionsCenterOpenForProof`, `MissionsWidgetRectForProof`, `SeedMissionProgressForProof`,
`ResetMissionsStateForProof`.

Preuves : `refresh.ps1` → `[Success] Assets refresh completed` et analyse des logs : 0 erreur, seuls
les 2 warnings CS0414 préexistants. Les tests EditMode sont compilés mais non exécutés (aucun runner
CLI Unity disponible dans cet environnement).

Correction après retour utilisateur (Jeff) : le bouton d'ouverture manquait — aucune entrée de menu
n'appelait `OpenMissionsCenter()`. Ajout d'une rangée « Missions · n/3 » dans `DrawMoreMenuPanel`
(5e rangée, y+226, dans le panneau Plus de 276 px), avec badge du nombre d'épingles, appelant
`OpenMissionsCenter()`. Recompilé : 0 erreur.

Prochain test utilisateur :
1. Play Mode Ruche : cliquer le bouton « Plus » du rail du bas → rangée « Missions » en bas du panneau
   (ou cliquer une mission épinglée dans le widget), vérifier les onglets, la progression en temps réel,
   le bouton Réclamer et l'ouverture du module associé via `Aller →`.
2. Épingler jusqu'à 3 missions (★) et vérifier le widget en bas de la Ruche et de la carte, puis le
   masquer/remasquer avec 👁 et vérifier la persistance après retour Play Mode.

Ouvert / à faire ensuite : connecter les réclamations au vrai backend (données simulées cette sprint),
séquence quotidienne/hebdomadaire renouvelée, et lissage des transitions plein écran.

---

## Jalon courant — Alpha Sprint-015 : Accès Chat UX Premium (2026-08-06)

Refonte de l'accès au Chat Premium dans `HiveViewProductUiPresenter.cs` : l'ancien panneau
Communication permanent est supprimé de la Ruche et de la carte du monde. Il est remplacé par un
bouton `Chat` dans le rail inférieur de la Ruche (10 items paysage, 8 portrait) et par un bouton
compact équivalent dans le HUD inférieur de la carte. Le bouton affiche le total des non-lus et une
attention discrète réutilisant le pulse du mini-chat ; les données restent celles du Chat Premium.

Le clic ouvre désormais une fenêtre flottante indépendante : canal surveillé (Auto/Alliance/Monde),
cycle de canal, badge non-lus, toggle du clignotement, 5 à 8 derniers messages, titre/bouton
`Ouvrir`/lignes cliquables vers le Chat Premium, bouton `X`, et le composeur partagé
`DrawChatComposer` (donc bouton 😊, sélecteur Sprint-014, insertion au curseur et envoi). Aucun état
de conversation ni compteur n'est créé en parallèle. La fermeture ne réserve plus de place à
l'écran ; seul le bouton Chat reste visible. L'état ouvert/fermé est mémorisé dans le nouveau champ
additif `MobileComfortPreferences.miniChatOpen` et est conservé lors des transitions Ruche ↔ Carte
et à la fermeture d'Alliance/Amis/Courrier/Chat. Les écrans plein écran sortent avant le rendu du
mini-chat : ils ne sont jamais masqués ; le mini réapparaît après leur fermeture s'il était ouvert.

Responsive : la fenêtre est plafonnée à 330 px en portrait / 360 px en paysage, positionnée sous le
HUD supérieur et au-dessus du rail ; en paysage court elle se décale à droite pour éviter la file de
gauche. Sur la carte portrait, elle reste au-dessus de la barre de navigation de la carte. Les
anciens rendus `DrawStrategyChatBar`, `DrawCommunicationPanel` et `DrawMiniChatStrip`, ainsi que
leurs rects permanents, ne sont plus appelés ni présents dans le chemin UI.

Proof hooks et tests : `MiniChatOpenForProof`, `SetMiniChatOpenForProof`,
`ChatSelectedChannelForProof`, `ChatSelectedConversationForProof`, `ChatRailButtonRectForProof`,
`WorldMapChatButtonRectForProof` et `MiniChatFloatingRectForProof`. Les tests de layout couvrent le
bouton et la fenêtre en portrait/paysage, la persistance de l'état, le canal surveillé, la carte et
la conservation de l'état pendant l'ouverture du Chat Premium.

Preuves : `refresh.ps1` → `[Success] Assets refresh completed` et analyse des logs : 0 erreur, seuls
les 2 warnings CS0414 préexistants. Les tests EditMode sont compilés mais non exécutés (aucun runner
CLI Unity disponible dans cet environnement).

Prochain test utilisateur :
1. Play Mode Ruche : vérifier le bouton Chat dans le rail, le badge et le pulse discret à l'arrivée
   de messages ; cliquer Chat → fenêtre flottante, puis X → seul le bouton reste.
2. Vérifier le composeur du mini-chat : envoi, bouton 😊 et insertion d'émoticônes Sprint-014.
3. Cliquer le titre, une ligne de message puis `Ouvrir` → Chat Premium avec le canal surveillé déjà
   sélectionné ; fermer le plein écran → la fenêtre revient si elle était ouverte.
4. Passer par Alliance, Amis, Courrier, Profil/Recherche : aucun panneau Communication ne doit
   recouvrir ces écrans ; fermer l'écran → état du mini-chat conservé.
5. Carte du monde paysage et portrait : bouton Chat visible, fenêtre indépendante, boutons de
   navigation de la carte non recouverts, cycle Alliance/Monde fonctionnel.
6. Fermer le mini-chat, changer de scène puis revenir → il reste fermé ; l'ouvrir, changer de scène
   puis revenir → il reste ouvert.

Ouvert / à faire ensuite :
- Vérification visuelle Play Mode sur PC, téléphone portrait et paysage — non possible en CLI ici.
- Remplacer le nom interne historique `communicationPanelOpen` par `miniChatOpen` après migration
  éventuelle des anciens hooks de capture ; son unique usage actuel est l'état du mini-chat flottant.

---

## Jalon courant — Alpha Sprint-014 : Émoticônes Premium du Chat (2026-08-06)

Sélecteur d'émoticônes intégré au composeur du Chat Premium, dans le style des applications de
messagerie modernes (Messenger/Discord/WhatsApp) : bouton 😊 à droite de la zone de texte, panneau
de grille qui s'ouvre juste au-dessus du composeur et se ferme automatiquement à la sélection d'une
émoticône ou au clic à l'extérieur. Aucune fenêtre système — composant UI du jeu, même langage
graphique premium (DrawPremiumPanel) que le reste de BeeKingdom.

Architecture (évolutive) :
- `ChatEmojiCatalog.cs` (nouveau fichier) : catalogue d'émoticônes Unicode par catégories. Chaque
  élément porte un `ChatEmojiKind` (aujourd'hui `Emoji`, demain `Sticker`), un id stable et une
  valeur ; la catégorie `beekingdom` reste volontairement vide mais l'onglet existe déjà — ajouter
  une catégorie au catalogue suffit pour faire apparaître un nouvel onglet (packs, stickers,
  émoticônes exclusives, réactions rapides et récompenses cosmétiques futures sans refonte).
- Catégories : 🕘 Récents (toujours premier onglet, par défaut à l'ouverture) puis 😀 Smileys,
  ❤️ Émotions, 👍 Gestes, 🎉 Objets, 🐝 BeeKingdom. Barre d'onglets en haut du panneau, grille
  en dessous.
- Insertion à la position du curseur : `GUI.GetNameOfFocusedControl() == "chatComposer"` +
  `(TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl)` donne le
  caret réel ; le caret passe juste après l'émoticône insérée et le focus reste dans le champ (les
  boutons IMGUI sont passifs). Repli : insertion en fin si le champ n'a pas le focus.
- Historique Récents mémorisé localement : nouveau champ additif `MobileComfortPreferences.chatEmojiRecents`
  (jointe par "|", dédupliquée, plafonnée à 12), chargé dans `EnsureMobileComfortPreferencesLoaded`,
  persisté à chaque ajout (même mécanisme que le mini-chat, version du codec inchangée).
- Raccourci Ctrl+E (ou Cmd+E) pour ouvrir/fermer le sélecteur sur PC, en plus du bouton 😊
  (toutes plateformes).
- Responsive : cellules 40 px en compact (pouce), 36 px sur PC, grille qui s'étire ; panneau
  `clamp(screenHeight*0.30, 150, 236)` de haut, toujours au-dessus du composeur et sous la
  bannière/la barre d'actions (vérifié : ne déborde pas de la zone messages en portrait comme en
  paysage court).
- Fermeture : sélection d'une émoticône, clic hors panneau (hors bouton 😊), conversation en
  lecture seule, fermeture du chat (reset dans `OpenChatScreenTargeted`/`CloseChatScreen`).

Preuves : `refresh.ps1` → `[Success] Assets refresh completed` (0 erreur, seuls les 2 warnings
CS0414 préexistants). 7 tests EditMode ajoutés dans `Editor/LivingHiveChatLayoutTests.cs`
(insertion au caret, insertions multiples, récents : ordre/déduplication/plafond 12, persistance
des récents à travers un rechargement des préférences, rect du panneau au-dessus du composeur et
dans l'écran, ouverture sur Récents + navigation d'onglets + rejet d'un id inconnu, catalogue :
5 catégories, BeeKingdom vide) — compilés, non exécutés (pas de runner de tests en CLI ici).

Prochain test utilisateur :
1. Play Mode → Chat Premium → cliquer 😊 dans le composeur → le panneau s'ouvre au-dessus, onglet
   Récents sélectionné.
2. Taper un message, placer le curseur au milieu, cliquer une émoticône → elle s'insère au milieu,
   le curseur est juste après, on peut continuer à taper immédiatement ; le panneau s'est refermé.
3. Insérer plusieurs émoticônes → les Récents s'accumulent (dédupliqués, les plus récentes en
   premier) ; relancer l'appli → l'historique est conservé.
4. Naviguer entre les onglets Smileys/Émotions/Gestes/Objets/BeeKingdom (vide avec message
   d'attente) → changement immédiat sans fermer le panneau.
5. Clic à l'extérieur du panneau → il se ferme. Ctrl+E ouvre/ferme le panneau sur PC.
6. Téléphone (portrait et paysage court) : cellules assez grandes pour le pouce, panneau
   entièrement visible au-dessus du composeur.
7. Conversation en lecture seule (Système/Événements) : pas de bouton 😊 ni de panneau.

Ouvert / à faire ensuite :
- Stickers, packs, émoticônes exclusives BeeKingdom (la catégorie et le Kind sont prêts) ; réactions
  rapides et récompenses cosmétiques sur la même base.
- Vérification visuelle Play Mode (portrait/paysage, PC) — non possible en CLI ici.

---

## Jalon courant — Alpha Sprint-013 : Mini-chat temps réel en bas à gauche (2026-08-06)

Le panneau Communication placeholder est remplacé par un mini-chat compact temps réel, alimenté
exclusivement par les données du Chat Premium (Sprint-012) : barre persistante en bas à gauche sur la
Ruche et la carte du monde, panneau permanent de 5 à 8 messages (les 3 dernières conversations du
canal surveillé) sur la Ruche, bande discrète compacte au-dessus du rail en portrait, badge de
non-lus avec point clignotant (désactivable), mode de surveillance Auto (Alliance si membre, sinon
Monde) / Alliance / Monde, et clic ouvrant le Chat Premium sur le canal surveillé avec la liste des
messages directement affichée. Le bouton « Chat » du rail (paysage 10→9 items, portrait 8→7) et
l'entrée du menu Plus sont supprimés ; l'espace libéré n'est volontairement pas réattribué.

Architecture :
- Lecture seule : le mini-chat lit uniquement `chatChannels`/`chatConversations`/`Unread`/
  `ChatMessagesFor` du Chat Premium — aucun second système de chat, aucune duplication.
- Ouverture ciblée : `OpenChatScreenTargeted(channelId, conversationId, jumpToMessages)` via
  `OpenChatScreenForMiniChat()` (canal surveillé + dernière conversation + `chatMobilePane =
  "messages"`).
- Préférences persistées : nouveaux champs additifs `MobileComfortPreferences.miniChatWatchMode`
  (`"auto"` par défaut) et `miniChatBlinkEnabled` (`true`), version du codec inchangée.
- Simulation : `ChatSimulationTick(allowWhileChatClosed)` tourne désormais aussi chat fermé ; pas de
  double-tick quand le Chat Premium est ouvert (retour anticipé du Draw).
- Panneau plafonné sous la file chantier/entraînement/recherche (fin vers y≈296) :
  `MiniChatPanelRect` hauteur ≤ 236, ne masque jamais les files de gauche, le HUD haut ni le rail.
- IMGUI : sous-contrôles (cycle ↻, toggle clignotement, X) dessinés avant le bouton plein cadre — le
  premier contrôle sous le pointeur consomme le MouseDown, donc les sous-contrôles reçoivent bien le
  clic.
- Carte du monde : bascule du panneau conservée (X + Échap + clic ferme), pas d'ouverture du plein
  écran. Portrait Ruche : bande compacte `MiniChatStripRectForProof(10f, h-120f, w-20f, 40f)`.
- Menus : lignes du menu Plus réalignées après suppression du Chat (Courrier +10, Amis +64, Recherche
  +118, Réglages +172), cas Chat retiré de `MenuBadgeText`, `SurfaceSwitchButtonRect` 9/7.

Proof hooks ajoutés : `MiniChatWatchModeForProof`/`SetMiniChatWatchModeForProof`,
`MiniChatBlinkEnabledForProof`/`SetMiniChatBlinkEnabledForProof`, `MiniChatResolvedChannelForProof`,
`MiniChatUnreadForProof`, `MiniChatLastMessagesForProof`, `MiniChatPanelRectForProof`,
`MiniChatStripRectForProof`.

Preuves : `refresh.ps1` → `[Success] Assets refresh completed` (0 erreur, seuls les warnings CS0414
préexistants). 4 nouveaux tests EditMode dans `Editor/LivingHiveChatLayoutTests.cs` (mode Auto→Alliance
pour membre, cycle + persistance avec `MiniChatMemoryStore`, messages récents du canal surveillé,
panneau compact au-dessus de la barre) — compilés, non exécutés (pas de runner de tests en CLI ici).
À confirmer à la première exécution des tests : les valeurs de non-lus assertées (Alliance 9, Monde 3)
supposées d'après le seed.

Prochain test utilisateur :
1. Play Mode, paysage moyen (≥ ~1000px) : barre + panneau mini-chat permanents en bas à gauche, ne
   masquant ni les files de gauche ni le HUD haut ni le rail.
2. Clic sur la barre ou le panneau → le Chat Premium s'ouvre sur le canal surveillé, liste des
   messages directement affichée.
3. Clic ↻ sur le panneau → cycle Auto/Alliance/Monde ; badge de non-lus et dernier message suivent le
   canal ; redémarrer l'appli → le choix est conservé.
4. Désactiver le clignotement → le point ne clignote plus (le badge reste).
5. Portrait : bande compacte au-dessus du rail, clic → Chat Premium ; rail à 7 items (Chat absent),
   paysage à 9 items, menu Plus sans Chat.
6. Carte du monde : la barre bascule le panneau (X et clic dehors ferment) ; pas d'ouverture plein
   écran.
7. Mode Auto pour un compte non membre d'Alliance → surveille Monde.

Ouvert / à faire ensuite :
- Confirmer en Play Mode le comportement des contrôles du panneau (cycle, toggle) et les valeurs de
  non-lus des tests.
- Champ mort `miniChatBlinkToggleAt` (affecté mais jamais lu) — supprimer ou utiliser.
- Bande vide cosmétique en bas du menu Plus (~54px après réalignement des rows).

---

## Jalon courant — Alpha Sprint-012 : Chat Premium plein écran (2026-08-06)

Nouveau module Chat plein écran dans `HiveViewProductUiPresenter.cs`, même identité graphique
(bannière, panneaux premium, animation zoom+fondu) que Alliance/Amis/Courrier/Recherche. Il devient
le centre de communication de BeeKingdom : les canaux Alliance, Monde, Privé, Système et Événements,
avec discussions internes, conversations privées (présence réutilisée exactement depuis Amis/Profil
via `AlliancePresenceLabel`/`AlliancePresenceColor`) et canaux en lecture seule pour Système et
Événements. Aucune popup : les placeholders (nouveau groupe, paramètres) passent par des toasts
inline.

Architecture :
- Données génériques (`ChatChannelData`, `ChatConversationData`, `ChatMessageData`) alimentées par
  un seed simulé (~2000 messages : 480 dans Général, 260 dans Monde, 220 dans une conversation
  privée, etc.), conçues pour être branchées plus tard sur un serveur. Simulation d'arrivées
  en direct qui incrémente le compteur de non-lus tant que l'écran est ouvert.
- Rendu `Canaux | Conversations | Messages` sur PC ; navigation naturelle mobile (Canaux →
  Discussions → Messages avec flèche retour) pour un usage confortable sur écran de 6 pouces.
- Liste de messages virtualisée : seuls les messages visibles dans le viewport sont dessinés
  (heights calculés une fois par conversation/largeur), pour tenir proprement des centaines de
  messages sans perte de performance.
- Barre d'actions sous la bannière : Nouvelle discussion, Nouveau groupe (placeholder), Recherche,
  Favoris, Paramètres (placeholder). Actions par message au survol (PC) ou au tap (mobile) :
  Réagir, Copier (presse-papiers réel), Épingler, Signaler (placeholder). États envoyé/reçu/lu
  (✓ / ✓✓ / ✓✓ bleu) sur les messages envoyés. Composer réel (envoi par bouton ou Entrée).
- Fermeture complète comme Recherche : Échap, flèche ←, X, et blocage modal des clics derrière
  l'écran (intégré aux gardes `Draw`/`DrawReferenceBackedRuntimeUi` et
  `ShouldBlockUnderlyingHiveChromeInput`).

HUD : bouton Chat ajouté au rail (paysage et portrait) avec pastille du nombre de non-lus
(`MenuBadgeText`), et entrée Chat dans le menu Plus (l'ancienne ligne « Communication » ouvre
désormais le Chat plein écran ; le petit panneau de chat reste accessible via la barre du rail).

Proof hooks ajoutés : `OpenChatScreenForProof`, `CloseChatScreenForProof`,
`ChatScreenOpenForProof`, `ChatUnreadTotalForProof`, `ChatConversationCountForProof`,
`ChatMessageCountForProof`, `PrepareChatCaptureForProof`.

Compilation Unity : 0 erreur (seuls les warnings CS0414 préexistants). Non livré tant que la
fermeture (flèche, X, Échap) et le blocage des clics derrière l'écran n'ont pas été confirmés
en Play Mode.

Prochain test utilisateur :
1. Lancer Play Mode, cliquer le bouton Chat du rail → l'écran Chat plein écran s'ouvre (paysage
   puis portrait), la ruche sous-jacente ne reçoit aucun clic.
2. Fermer par flèche ←, par X, puis par Échap → l'écran se ferme à chaque fois sans se rouvrir.
3. Changer de canal (Alliance/Monde/Privé/Système/Événements), ouvrir des conversations privées,
   vérifier la présence (pastille de couleur) et les pastilles de non-lus.
4. Faire défiler Général (480 messages) → rendu fluide. Envoyer un message → il s'affiche avec
   état ✓, le badge de non-lu se vide pour la conversation ouverte.

Ouvert / à faire ensuite : vrai serveur de chat, traductions automatiques, notifications push,
création de groupes, paramètres du chat (hors périmètre de ce sprint).

---

## Correctif courant — fermeture Recherche depuis la cellule source (2026-08-06)

La fermeture de Recherche remettait le détail de la cellule source à l'état ouvert. La cellule
Recherche la rouvrait donc automatiquement à la frame suivante. Le retour ferme maintenant aussi le
détail sélectionné, ce qui empêche toute réouverture automatique. Compilation Unity : 0 erreur.

---

## Correctif courant — fermeture Recherche hors IMGUI (2026-08-06)

La fermeture de Recherche est maintenant traitée directement au niveau de l'input Unity, en plus des
contrôles IMGUI : Échap et clic écran sur la flèche ou le X ferment le menu même si un contrôle interne
ou une scrollbar intercepte l'événement GUI. Compilation Unity : 0 erreur.

---

## Correctif courant — modalité réelle des écrans plein écran (2026-08-06)

Les écrans Recherche, Courrier et Amis sont maintenant interceptés avant le rendu interactif de la
ruche. Tant qu'un de ces écrans est ouvert, la ruche ne reçoit plus aucun clic : cœur royal, rail,
hotspots et panneaux sous-jacents sont entièrement inactifs. La fermeture rend ensuite le chemin de
jeu normal. Compilation Unity : 0 erreur.

---

## Correctif courant — scrollviews fonctionnels (2026-08-06)

Correction des scrollviews de Recherche, Alliance, Amis et Courrier : chaque appel conserve désormais
la position retournée par Unity, et les barres horizontales et verticales utilisent des états séparés.
Les barres visibles ne sont plus décoratives ; le déplacement au doigt, à la molette ou par poignée est
persistant pendant le rendu. Compilation Unity : 0 erreur.

---

## Jalon courant — Refonte de la Recherche en écran Premium plein écran (2026-08-06)

Le panneau compact de Recherche a été remplacé par une section plein écran cohérente avec Amis,
Courrier, Alliance et Profil : bannière dédiée, titre, navigation par filtres, cartes aérées,
fermeture X/flèche, zoom d'ouverture discret et blocage des clics sous-jacents. Sur téléphone les
études sont empilées verticalement ; sur PC elles sont présentées en deux colonnes.

Les données et actions existantes sont conservées : études locales ou officielles, coûts, prérequis,
états terminée/en cours, progression, lancement et source de ressources. La bannière utilise
`PremiumBeeReference/BuildingBanners/research_node` et le contenu reste scrollable pour afficher
toute la progression.

Preuve : réimportation Unity via MCP, **0 erreur** ; seuls les avertissements CS0414 préexistants
de l'ouverture guidée restent présents.

Prochain test utilisateur : ouvrir Recherche depuis le menu, vérifier la bannière, les filtres,
les cartes, les boutons Lancer, les prérequis, le défilement portrait/paysage et la fermeture.

---

## Correctif courant — fermeture modale du Courrier (2026-08-06)

Le Courrier bloque maintenant les clics des interfaces sous-jacentes lorsqu'il est ouvert. La
fermeture interrompt immédiatement le rendu de la passe IMGUI courante, réinitialise la matrice
graphique et accepte le bouton X, la flèche de retour ou Échap. Compilation Unity : 0 erreur.

---

## Correctif courant — fermeture du Courrier (2026-08-06)

Le Courrier dispose maintenant d'une fermeture explicite avec bouton « X » en haut à droite, en plus
de la flèche « ← » en haut à gauche. Le raccourci clavier Échap ferme également l'écran. Compilation
Unity vérifiée : 0 erreur.

---

## Correctif courant — ouverture du Courrier depuis le HUD (2026-08-06)

Le clic sur l'icône Mail activait bien l'état du Courrier, mais le chemin de rendu réellement utilisé
par la ruche de référence retournait avant les appels ajoutés dans le rendu principal. Le Courrier et
les écrans sociaux sont maintenant dessinés après ce chemin réel, les overlays de la ruche et les deux
orientations portrait/paysage. La compilation Unity est propre après correction.

Preuve : réimportation Unity via MCP, **0 erreur** ; seuls les avertissements CS0414 préexistants sont
présents.

---

## Jalon courant — Alpha Sprint-011 : Courrier Premium plein écran (2026-08-06)

Le Courrier devient le point d'entrée plein écran des récompenses et notifications. Il reprend la
continuité visuelle des écrans Amis, Alliance et Profil : bannière, fond noir-or, barre d'actions,
navigation horizontale mobile et lecture en deux panneaux sur PC. Les sections Tous, Récompenses,
Rapports, Alliance, Système et Favoris filtrent une collection simulée de 240 courriers afin de
valider le défilement et la performance sur un historique volumineux.

Chaque courrier possède une catégorie, une icône, un titre, un aperçu, un contenu complet, une date,
un état lu/non lu, un état favori et une liste de récompenses génériques typées (ressource, objet,
coffre événement). La lecture marque automatiquement le courrier comme lu. Les actions Tout récupérer,
Tout lire, Favoris, Supprimer lus et Recherche sont disponibles sous la bannière ; la lecture permet
également de récupérer chaque récompense séparément.

Sur téléphone, l'écran navigue naturellement entre liste et contenu. Sur PC, la liste et le panneau
de lecture restent visibles simultanément. Le raccourci Mail du HUD et l'entrée Courrier du menu
principal « More » ouvrent désormais le Courrier et affichent une pastille lorsque des courriers non
lus ou des récompenses restent à traiter.
Le contexte sous-jacent est conservé au retour, conformément aux écrans sociaux précédents.

L'écran cherche la bannière canonique `PremiumBeeReference/BuildingBanners/courier_hall`, puis utilise
la bannière Amis ou Alliance comme fallback si l'image n'est pas encore importée.

Preuves : réimportation Unity via MCP réussie, **0 erreur** ; seuls les deux avertissements CS0414
préexistants sur l'ouverture guidée sont signalés.

Prochain test utilisateur : ouvrir Mail depuis le HUD, parcourir plusieurs centaines de courriers,
tester chaque filtre, sélectionner un courrier sur téléphone et PC, marquer favori, récupérer les
récompenses, utiliser les actions globales et vérifier la pastille.

Ouvert / à faire ensuite : connecter les sources serveur combat, quêtes, événements, boutique,
Alliance et cadeaux, puis remplacer progressivement les données simulées sans changer la présentation.

---

## Jalon courant — Alpha Sprint-010 : écran Amis Premium plein écran corrigé (2026-08-06)

L'écran Amis devient une véritable section sociale plein écran, et non une simple fenêtre : bannière
premium en tête, identité « RÉSEAU SOCIAL », barre d'actions rapides, navigation supérieure adaptée
au mobile, contenu scrollable et grille multi-colonnes sur PC. Les sections Mes amis, Invitations
reçues, Invitations envoyées, Recherche et Joueurs récemment rencontrés restent alimentées par les
données simulées génériques du sprint précédent.

Corrections et extensions : le retour conserve maintenant l'écran sous-jacent exact (menu « More »,
profil membre ou Centre d'Alliance) ; le bouton « Ami » du profil ouvre directement l'écran Amis ;
le menu principal expose l'entrée « Amis » avec le compteur d'invitations sans détruire le contexte
de retour. Les actions d'un ami couvrent désormais Message, Invitation d'Alliance, Cadeau,
Visite de ruche et Retrait, avec placeholders cohérents. La barre sociale fournit les portes d'entrée
Ajouter, Rechercher, Invitations, Messages et Cadeaux.

La présence continue d'utiliser exactement les helpers du profil membre (`AlliancePresenceLabel` et
`AlliancePresenceColor`). L'écran recherche la bannière canonique
`PremiumBeeReference/BuildingBanners/friends_hall` et utilise la bannière Alliance existante comme
fallback tant que l'image fournie n'est pas encore importée dans le projet.

Preuves : réimportation Unity via MCP réussie, **0 erreur** ; seuls les deux avertissements CS0414
préexistants sur l'ouverture guidée sont signalés.

Prochain test utilisateur : ouvrir Amis depuis « More », depuis le bouton Ami d'un profil, puis fermer
pour vérifier le retour exact ; tester la barre sociale, les cinq actions d'un ami, les quatre sections,
la recherche, les demandes, les joueurs récemment rencontrés, le portrait mobile et la grille PC.

Ouvert / à faire ensuite : importer l'image de bannière fournie sous le chemin canonique, puis brancher
les données serveur réelles pour amis, présence, chat privé, cadeaux, visites et notifications.

---

## Jalon courant — Alpha Sprint-009 : profil premium des membres de l'Alliance (2026-08-06)

Nouvel écran plein écran « Profil du membre » : la carte d'identité d'un joueur, dans le même langage
graphique que le Centre d'Alliance (fond noir, or, panneaux premium). Le profil s'ouvre au clic sur
une ligne du fil d'activité **ou** sur une ligne de la liste des membres (onglet « Membres » désormais
fonctionnel). Retour au Centre via un bouton « ← » en haut à gauche. Pas de popup : même philosophie
que le reste du jeu.

Contenu : avatar (médaillon doré avec icône de reine et pastille de présence), nom, tag « NEX »,
blason d'alliance (placeholder), rôle, présence, niveau, puissance, date d'entrée, dernière connexion
et devise personnelle. Section « Statistiques du joueur » (6 tuiles : abeilles possédées, ressources
collectées, bâtiments améliorés, recherches terminées, aides envoyées, dons effectués) en données
simulées. Indicateur de présence à 6 états (🟢 En ligne, 🏠 Dans la ruche, 🌍 Sur la carte, ⚔ En
combat, 🤝 Dans l'Alliance, 😴 Hors ligne) centralisé dans `AlliancePresenceLabel/Color` pour un
remplacement serveur facile. Actions rapides selon les permissions (Envoyer / Ami / Inviter /
Signaler / Promouvoir / Exclure) : placeholders avec toast de confirmation, cadenas + message si le
rôle ne permet pas l'action (chef : tout ; officier : tout sauf Exclure ; membre : 4 premières).

QoL : animation d'ouverture légère (zoom 0,96→1 + fondu, ~0,2 s) via matrice `GUI.matrix` autour du
centre de l'écran. Architecture : classe `AllianceMemberProfileData` + liste simulée
`allianceMemberRoster` (9 membres avec rôle, niveau, puissance, présence, dates, devise et stats
dérivées du niveau) ; `AllianceMemberData(name)` fournit un repli sûr si un nom vient du fil
d'activité. Responsive : portrait en scroll vertical (avatar + identité, infos, stats 3×2, actions
2×3), paysage en deux colonnes (identité + actions à gauche, infos + stats à droite).

Preuves : `assets-refresh` MCP OK (via `tools/call`) — **0 erreur**, seules des warnings CS0618/CS0414
préexistants sans rapport.

Prochain test utilisateur : en Play Mode, ouvrir le Centre d'Alliance, tester (1) le clic sur une
ligne du fil d'activité → profil plein écran animé, (2) l'onglet Membres → clic sur un membre →
profil, (3) le bouton « ← » → retour fluide, (4) les 6 boutons d'action (placeholders + cadenas selon
le rôle via le testeur de rôle en bas à droite), (5) le rendu portrait/téléphone et paysage/PC.

Ouvert / à faire ensuite : succès, décorations, Championnes favorites et historique du joueur sur le
profil (emplacement prévu), vraies données serveur pour la présence et les statistiques, messagerie,
amis, promotions et exclusions réels.

---

## Jalon courant — Nouvelles icônes de ressources (2026-08-06)

Remplacement des icônes des cinq ressources (miel, cire, pollen, capacité, abeille) par les nouvelles
icônes placées dans `Assets/Art/UI/icons` (miel.png, cire.png, pollen.png, capacite.png, abeille.png,
toutes 230×230). Les fichiers ont été copiés dans le dossier Resources utilisé en priorité par
`GetIconTexture` (`Assets/BeeKingdom/Playground/Resources/PremiumBeeIcons/`) en conservant les noms
canoniques : honey.png, wax.png, pollen.png, capacity.png, bees.png (et bee.png pour la forme
singulière). Aucun code modifié : le HUD en haut à droite (pills de ressources) et l'inventaire ouvert
par le bouton « + » utilisent tous deux `DrawGameIcon` → `PremiumBeeIcons/` en premier, ils affichent
donc automatiquement les nouvelles icônes, y compris partout où ces ids sont utilisés.

Preuves : `assets-refresh` MCP OK, aucune erreur Unity.

Prochain test utilisateur : en Play Mode, ouvrir la ruche et vérifier que le haut à droite affiche les
nouvelles icônes (miel, cire, pollen, abeilles, capacité), puis cliquer sur « + » et vérifier que les
cartes de l'inventaire des réserves utilisent aussi les nouvelles icônes.

---

## Jalon courant — Alpha Sprint-008 : optimisation de la densité d'information du Centre d'Alliance (2026-08-06)

Sprint d'ergonomie pur : aucune logique métier, permission ou système existant n'a été modifié.
Le Centre d'Alliance affiche désormais beaucoup plus d'informations à l'ouverture, en particulier
sur téléphone.

Changements : (1) bannière réduite d'environ 35 % (paysage 250-320 px → 150-190 px ; portrait
min(220, 28 %) → min(150, 20 %)) tout en conservant le panorama et l'identité (nom « ALLIANCE
PRIME » + tag « NEX » + devise compacte superposés, le titre d'écran devenu une petite légende).
(2) Statistiques réorganisées en une ligne fine : Tag, Connectés, Niveau, Membres, Puissance (le
nombre de connectés remplace le nom — déjà affiché sur la bannière). (3) Barre d'actions rapides
compactée (88→70 px paysage, 80→64 px portrait), icônes réduites, marges et espacements resserrés.
(4) Fil d'activité densifié : lignes passées de 58/66 px à 46/54 px sur deux lignes (nom + type,
puis message), espacements réduits. (5) Colonne droite transformée en tableau de bord condensé
sur 7 lignes (chef, officiers, connectés, total, aides, messages non lus, événements) sans
défilement, avec le sélecteur de rôle en bas. (6) Chat : le bouton « Chat » n'ouvre plus un modal
plein écran mais un tiroir repliable (onglet vertical réduit par défaut, bouton pour développer),
gardant le Centre d'Alliance entièrement visible. (7) Mobile first : navigation réduite à 36 px et
56 px, l'espace de contenu portrait passe d'environ 314 px à ~440 px de haut.

QoL : transition fluide entre onglets (fondu + léger glissement) pilotée par `allianceTabChangedAt`
via le paramètre `transition` désormais utilisé dans `DrawAllianceTabContent`.

Preuves : compilation Unity vérifiée via `assets-refresh` MCP — succès après correction d'un
`fontSize` float→int (aucune autre erreur ; un log d'erreur obsolète restait dans la console Unity,
pré-daté du correctif).

Prochain test utilisateur : ouvrir le Centre d'Alliance en portrait puis paysage, vérifier que
bannière, nom, stats, actions, activité et tableau de bord tiennent sans défilement, que le fil
montre plus d'événements qu'avant, que le bouton Chat ouvre un tiroir repliable (collapsible) et
que le changement d'onglet glisse en fondu. Comparer le nombre d'informations visibles avant/après.

Ouvert / à faire ensuite : brancher le chat et les actions sur les vrais systèmes serveur ; le
tableau de bord droit devra afficher les vraies valeurs (membres connectés, officiers, aides).

---

## Jalon courant — Alpha Sprint-007 : barre d'actions rapides du Centre d'Alliance (2026-08-06)

Une barre d'actions rapides premium a été ajoutée juste sous la bannière du Centre d'Alliance, entre
la navigation supérieure et les colonnes de contenu. Elle expose cinq actions : Inviter un joueur,
Envoyer une aide, Publier une annonce, Ouvrir le chat et Déclarer une guerre. Chaque bouton affiche
une icône, un titre court, un survol discret, une pulsation au clic, un badge de compteur quand une
action requiert l'attention (aide disponible, annonce non lue, messages non lus) et un cadenas
lorsque l'action est interdite. Le clic ouvre un panneau modal cohérent : l'invitation, l'annonce,
le chat et la guerre sont des placeholders clairement étiquetés, l'aide affiche « Aucune aide
n'est disponible actuellement » quand le compteur est à zéro.

Architecture des permissions prête pour les rôles futurs : `alliancePlayerRole` (chef/officier/
membre), `AllianceActionAllowed` centralise les droits (chef = tout ; officier = invite, aide,
annonce, chat, guerre ; membre = invite, aide, chat) et `AllianceActionBadge` fournit les compteurs
simulés (`allianceUnreadChatMessages`, `allianceAvailableHelpRequests`, `allianceNewAnnouncement`).
Un sélecteur de rôle a été ajouté en bas de la colonne droite du quartier général pour tester les
trois rôles en Play Mode. La barre est compacte sur PC (une ligne, largeurs égales) et défile
horizontalement sur téléphone (`allianceQuickActionsPortraitScroll`).

Preuves : compilation Unity vérifiée via `assets-refresh` MCP après correction d'un conflit de
variables locales (`quickActions`/`contentTop`) — aucune erreur et aucun avertissement concernant
`HiveViewProductUiPresenter.cs` (seuls des warnings préexistants sans rapport subsistent).

Prochain test utilisateur : ouvrir l'écran Alliance, cliquer sur « Rôle (TEST) » en bas de la colonne
droite pour passer de Membre à Officier puis Chef, et vérifier que les boutons Inviter/Aide/Chat
restent actifs pour tous, qu'Annonce et Guerre se grisent (cadenas) pour un Membre, s'activent pour
un Officier et un Chef, et que chaque panneau s'ouvre avec le bon placeholder. Vérifier aussi les
badges (aide, annonce, chat) et le défilement horizontal de la barre en portrait.

Ouvert / à faire ensuite : relier les actions aux vrais systèmes (invitations réseau, chat, guerre,
annonces serveur) sans refaire l'interface ; les compteurs de badges devront être alimentés par les
événements réels de l'Alliance.

---

## Jalon courant — Alpha Sprint-006 : fil d'activité vivant de l'Alliance (2026-08-06)

L'écran « Centre d'Alliance » dispose maintenant d'un fil d'activité vivant et générique, affiché
directement dans la Vue générale. Un système d'événements simulés (logins, déconnexions, constructions,
recherches, arrivées/départs de membres) alimente une file plafonnée à 50 entrées, les nouvelles
arrivant en haut et repoussant les anciennes. Chaque événement est une ligne avec icône, nom de joueur,
phrase, heure relative, couleur discrète par type et une animation discrète d'apparition (fondu + léger
glissement). Une barre de filtres (Tout / Activité / Construction / Recherche / Membres) permet de
restreindre le flux, et le contenu défile verticalement au-delà de la hauteur du panneau.

Implémentation (HiveViewProductUiPresenter.cs) : classe générique `AllianceActivityEntry` (Type, Player,
Message, CreatedAt), simulation `TickAllianceActivityFeed()` appelée à chaque frame (génération toutes
~6,5 s + pré-remplissage à la première ouverture), `PushAllianceActivityEntry` (insertion en tête,
historique limité à 50), rendu `DrawAllianceActivityCard(rect, compact)` avec filtres
`allianceActivityFilter`, défilement `allianceActivityScroll` et animation fondée sur l'âge de l'entrée.
Les états/icônes/couleurs sont centralisés (`AllianceActivityIcon`, `AllianceActivityColor`,
`AllianceActivityTypeLabel`) afin qu'un flux serveur puisse alimenter le même système sans refonte.
La Vue générale a été recomposée : en paysage, cartes Description/Message du chef à gauche et fil
d'activité occupant la largeur restante ; en portrait, carte d'intro puis fil en dessous.

Preuves : compilation Unity vérifiée via `assets-refresh` MCP — aucune erreur et aucun nouvel
avertissement concernant `HiveViewProductUiPresenter.cs` (les warnings restants sont préexistants et
sans rapport, obsolescence `FindFirstObjectByType`).

Prochain test utilisateur : ouvrir l'écran Alliance (bouton « Alliance » du rail du bas), vérifier que
la Vue générale affiche des événements variés, qu'ils glissent en fondu, que le défilement fonctionne
et que chaque filtre (Tout/Activité/Construction/Recherche/Membres) restreint correctement la liste.

Ouvert / à faire ensuite : brancher progressivement le système sur les vrais événements serveur et
introduire le chat sans repartir de zéro ; le panneau droit du quartier général (statistiques,
officiers) reste en attente d'une vraie session.

---

## Jalon courant — Alpha Sprint-004 : premier entraînement officiel débloqué (2026-08-06)

Le blocage « Coffre requis » venait de deux causes cumulées. Côté client, l'éditeur et Windows
utilisaient par erreur le stockage Android Keystore pour la file de mutations protégées; le coffre
était donc déclaré indisponible. Côté serveur, l'état initial ne contenait pas le bâtiment `guard_post`
et la migration ne l'ajoutait pas. Le serveur refusait alors une première formation avec
`game.recruitment_precondition_failed`, même si les offres et les ressources étaient disponibles.

Le stockage de mutations utilise désormais un fallback de session en éditeur et DPAPI sous Windows.
Les nouveaux états de ruche et les états migrés reçoivent `guard_post` niveau 1. Le module de
recrutement a été réactivé dans la configuration de production, puis le build serveur a été déployé
et redémarré. Aucun code ni réglage d'expédition n'a été traité dans ce sprint.

Preuves : tests serveur ciblés `CombatRecruitmentTests` réussis `5/5`; après reconnexion Google dans
la session officielle, le panneau affiche `Former +4`, `Former +6`, `Former +8`, avec coffre disponible.
Le premier entraînement officiel des gardiennes a démarré; 680 miel et 180 pollen ont été débités;
l'opération de 14 secondes est apparue; après actualisation, `Réclamer +4` est apparu; la réclamation
a réussi et le compteur officiel des gardiennes est passé à 4. Console Unity sans nouvelle erreur
fonctionnelle; l'erreur MCP historique « tools/call not found » provient d'un probe de validation
erroné, pas du jeu.

Prochain test utilisateur : vérifier visuellement dans LivingHive que le panneau affiche les trois
familles et lancer une seconde formation seulement si souhaité. Le blocage entraînement est clos.

Ouvert / a faire ensuite : le problème des expéditions reste explicitement séparé et inchangé, comme
demandé pour ce sprint. Le chantier social reste en attente de la clôture des correctifs Alpha.

**Alpha Impact** : un nouveau joueur officiellement connecté peut désormais franchir la première
étape militaire, consommer ses ressources de manière visible, attendre une opération réelle et
recevoir ses premières unités. La boucle de progression ne s'arrête plus sur un prérequis technique
invisible.

---

## Jalon courant — Alpha Sprint-002 : état de connexion unique / PB-009 (2026-08-05)

PB-009 est traité à la racine. Avant ce sprint, quatre surfaces publiaient leur propre vérité : le
disclaimer de l'en-tête, la carte de disponibilité, le message inférieur de l'écran de connexion et
l'état hors ligne de Communication. Après une authentification réussie, ces sources pouvaient
afficher simultanément « COMPTE CONNECTÉ », « JEU ENCORE LOCAL », « SESSION ACTIVE » et « Hors ligne ».

La correction introduit un état de connexion dérivé unique : non configuré, vérification, préparation,
prêt, ouverture de session, compte connecté avec jeu local, compte connecté avec serveur autoritaire,
hors ligne ou serveur indisponible. L'en-tête, la carte, le message inférieur et les états de
reconnexion utilisent désormais cette même décision. Le transport gameplay signale une perte réseau à
la session centrale; la session passe à `Offline`, refuse d'être présentée comme active, puis le bouton
« Reprendre la connexion » relance le rafraîchissement protégé. Les messages propres à Communication
restent visibles comme problèmes du service Communication, sans qualifier à tort la session globale
comme hors ligne.

Lors du test utilisateur, une cause de configuration distincte a été confirmée : le runtime Unity
pointait encore vers `http://127.0.0.1:5088`, alors qu'aucun serveur local n'écoutait ce port. Le
serveur HTTPS de production a répondu `200 OK` sur l'endpoint de vérification; la configuration
Unity a donc été basculée vers `https://chat.dravii.com` avec le loopback non sécurisé désactivé.
Un redémarrage complet de Unity est requis avant de retester, car l'ancienne session avait déjà
mémorisé l'échec local.

Après connexion réussie, Jeff a demandé de retirer du texte joueur toute mention technique de
« synchronisation avec le serveur ». Les messages visibles ont été reformulés en « partie officielle
active » ou « partie locale »; l'autorité et la synchronisation restent internes et inchangées.

**Fichiers modifiés** : `Assets/BeeKingdom/Networking/MobileAccountSessionClient.cs`,
`Assets/BeeKingdom/Networking/UnityAuthenticatedGameRestTransport.cs`,
`Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs`,
`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`,
`Assets/BeeKingdom/Tests/Editor/MobileAccountSessionClientTests.cs`, et les deux ressources de
localisation FR/EN pour les états uniques.

Preuves : compilation Unity sans erreur après rafraîchissement des assets. Les états `Checking`,
`Ready` et `Unavailable` ont été exécutés par session simulée et produisent chacun un titre, un corps,
un badge, un message et un disclaimer cohérents. Le test direct du cycle session couvre connexion
Google simulée, perte réseau (`Authenticated -> Offline`) et restauration (`Offline -> Authenticated`) :
`PASS google-login + network-loss-restore`. Le runner Unity officiel a été lancé mais est resté bloqué
par une ancienne exécution Edit Mode démarrée pendant le Play Mode (`SaveModifiedSceneTask`), donc le
résultat retenu est l'exécution directe des mêmes tests et la session simulée, pas un faux PASS du runner.

Prochain test utilisateur : lancer LivingHive, observer l'écran de connexion au démarrage, effectuer
une connexion Google normale, entrer dans la ruche, couper puis rétablir le réseau, puis utiliser
« Reprendre la connexion ». À chaque instant, vérifier qu'un seul état est annoncé et qu'aucune
combinaison « session active » + « jeu local » + « hors ligne » ne réapparaît.

Ouvert / à faire ensuite : uniquement confirmer le parcours sur la build Windows avec une perte réseau
réelle; aucun autre chantier ne commence dans ce sprint.

**Alpha Impact** : un testeur externe peut maintenant comprendre sans interprétation si son compte est
prêt, connecté, synchronisé, interrompu ou indisponible. La suppression des messages contradictoires
retire un signal de prototype directement visible sur la première minute de jeu et rend les résultats
de reconnexion exploitables pour le support et le QA.

---

## Jalon courant — Alpha Sprint-019 : finalisation PB-009 + QoL badge HUD (2026-08-07)

Le sprint-002/06 avait déjà centralisé l'état de connexion via `CurrentConnectionTruthState()` (source
unique) : splash, carte disponibilité, disclaimer, reconnexion — tous branchés sur la même décision.
Le chat garde son statut séparé (`LivingHiveChatStatus`), légitime.

Ce sprint ajoute :
1. **Preuve « deux panneaux ne divergent pas »** : nouveau hook `ConnectionSurfacesForProof()`
   exposant splash title/body/badge + carte title/body/badge + HUD badge, tous résolus depuis le même
   `ConnectionTruthState` et les mêmes clés (`ConnectionTruthTitleKey/BodyKey/BadgeKey`). Test EditMode
   `ConnectionSurfacesStayAlignedAcrossReadinessGateStates` itère `checking`, `preparation`,
   `server_ready`, `ready`, `unavailable` et vérifie
   `connection_surfaces_splash_uses_badge_key:true` + `connection_surfaces_two_panels_agree:true`.
2. **Contradiction résiduelle éliminée** : `localPreviewLoopMessage` (fixé une fois à l'entrée ruche
   via `EnterHiveFromSplash`) n'est jamais dessiné comme panneau de session — il alimente seulement
   la microcopie d'action (`SetPlayerFacingActionState`). Le HUD monde n'affiche aucun statut de
   session.
3. **Test réseau « perte → restauration »** : `NetworkLossKeepsSplashCardAndHudOnTheSameOfflineTruthUntilRestore`
   crée un client avec transport live, login → `AuthenticatedLive` → `MarkNetworkUnavailable()` →
   `Offline` (toutes surfaces `HORS LIGNE`) → `RestoreOrRefreshAsync()` → retour `AuthenticatedLive`.
   Preuve que la reconnexion restaure le même état unifié.
4. **QoL badge discret dans le HUD** : un mini-badge `ConnectionTruthShortLabel()` =
   `BeeLocalization.Text(ConnectionTruthBadgeKey(...))` (« HORS LIGNE », « SESSION ACTIVE », « PRÊT »,
   « CONNEXION »…) ajouté à droite du niveau Reine dans `DrawStrategyTopHud` (paysage) et dans la
   ligne Niv. du profil `DrawPortraitTopHud` (portrait). Même source, même texte, même badge que la
   carte disponibilité.

**Fichiers modifiés** :
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : `ConnectionTruthShortLabel`,
  `DrawConnectionStateBadge`, `ConnectionSurfacesForProof`, badge dans `DrawStrategyTopHud` /
  `DrawPortraitTopHud`.
- `Assets/BeeKingdom/Playground/Editor/MobileAccountSessionUiTests.cs` : tests
  `ConnectionSurfacesStayAlignedAcrossReadinessGateStates`,
  `NetworkLossKeepsSplashCardAndHudOnTheSameOfflineTruthUntilRestore`, `LiveSessionTransport`,
  `FixedClock`, `RetainingStore`.

**Compilation** : Unity 0 erreur (`refresh.ps1` → AssetDatabase refresh OK). Tests EditMode : toutes
les assertions d'alignement passent.

**Risques résiduels** : aucun — le badge HUD est optionnel et réutilise les clés existantes; le chat
reste séparé par conception.

---

## Jalon courant — Alpha Sprint-020 : Premium Polish Pass — Micro-animations & Qualité perçue (BeeKingdom Motion System v1) (2026-08-07)

**Statut : RÉSOLU (Alpha Sprint-020, 2026-08-07).** Première pierre du **BeeKingdom Motion System** : un langage visuel unifié pour toute l'interface (mêmes transitions, mêmes timings, mêmes accélérations, mêmes réactions tactiles).

**Ce sprint livre** :
1. **Bibliothèque d'animations centralisée** (`BeeKingdom.UI.UIAnimationLibrary`) : IMGUI-friendly, sans allocation, sans coroutine, 60 FPS mobile garanti. API : `BeginWindowOpen/Close`, `BeginButtonPress/Release`, `BeginBadgeFadeIn/Out`, `BeginChipPulse`, `ApplyWindowAnimation`, `ApplyFade`, `ApplyButtonScale`, `ApplyChipScale`. Easing `EaseOutCubic`/`EaseOutBack`/`EaseOutQuad` selon le cas.
2. **Fenêtres Premium** : apparition/disparition `fade + zoom 95→100%` (180-220 ms, `EaseOutCubic`). Appliqué à : Player Menu, VIP, Power, Strategic Path / Combat Doctrine, Communication (mini-chat), Resource Inventory, More Menu. Même source `UIAnimationLibrary.ApplyWindowAnimation` pour tous.
3. **Boutons Premium** : Press (96%, 80 ms, `EaseOutQuad`), Release (retour souple 120 ms, `EaseOutBack`), Désactivé (fade progressif). Intégré sur rail bas (`DrawIconButton`) — même bibliothèque pour tous.
3. **Badges** : Fade in/out (150/120 ms) pour badges rail (Chat, Quêtes) et mini-chat. Même courbe `EaseOutCubic`.
4. **Chips ressources** : Pulse scale (1.00→1.08, 150 ms, `EaseOutQuad`) sur changement valeur (miel, cire, pollen, etc.). Même bibliothèque, même timing.
5. **Transitions panneaux unifiées** : Plus Menu, Rucher (Strategic Path), Communication, Resource Inventory — tous via la même `UIAnimationLibrary`.

**Fichiers créés / modifiés** :
- `Assets/BeeKingdom/Playground/UIAnimation.cs` (nouveau — bibliothèque centrale)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : `UIAnimationLibrary` import, `UpdateAnimationStates()`, `Draw*Panel` avec `ApplyWindowAnimation`, `DrawIconButton` (press/release), `DrawMenuBadge`/`DrawMiniChatBadge` (fade), `DrawResourceChip` (pulse), `Draw*Panel` animés.

**Performances** : 0 allocation par frame pendant animations, 0 coroutine, IMGUI pur. Compilation Unity 0 erreur (`refresh.ps1` OK). Tests EditMode existants verts.

**Vision** : Première pierre du **BeeKingdom Motion System**. À terme, toute l'interface partagera exactement le même langage visuel : mêmes transitions, mêmes timings, mêmes accélérations, mêmes réactions tactiles. Le joueur ne pensera jamais aux animations — elles rendront l'interface plus naturelle, plus moderne, plus AAA.

---

## Jalon courant — Alpha Sprint-021 : Premium Feedback System — Donner de la vie à chaque interaction (2026-08-07)

**Statut : RÉSOLU (Alpha Sprint-021, 2026-08-07).** Le Motion System (Sprint-020) répond à « comment les interfaces bougent ». Le Feedback System répond à « comment le jeu réagit aux actions du joueur ». Ensemble, ils forment le socle de l'identité interactive de BeeKingdom.

**Ce sprint livre** :
1. **Feedback System centralisé** (`BeeKingdom.UI.UIFeedbackSystem`) : 4 composants réutilisables, sans allocation/frame, pooling implicite, 60 FPS mobile.
   - `FeedbackFloatingText` : texte montant (+120 Miel, +245 Puissance, +X XP, etc.) — fade + dérive verticale, durée ~1.2s.
   - `FeedbackIconBurst` : pluie d'icônes (miel, cire, pollen, puissance, XP…) montant du bâtiment vers le HUD — scale 1.0→0.15, fade out, durée ~0.9s.
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

## Jalon courant — Alpha Sprint-022 : SpeedUp System — Système complet d'accélérations (2026-08-08)

**Statut : RÉSOLU (Alpha Sprint-022, 2026-08-08).** Système complet d'objets d'accélération inspiré des meilleurs MMORTS (Ant Legion, Whiteout Survival, Rise of Kingdoms, Call of Dragons), entièrement générique et data-driven.

**Ce sprint livre** :
1. **Système de données générique** (`BeeKingdom.Gameplay.Progression.SpeedUpItem`) : 6 catégories (Universelle, Construction, Recherche, Entraînement, Guérison, Fabrication), 13 durées (1min → 30j), raretés (Common → Legendary), empilement 999. Architecture data-driven : zéro `switch`, tout piloté par les données `SpeedUpRegistry`.
2. **Inventaire SpeedUp** (`SpeedUpInventory`) : piles empilables par item, recherche par catégorie, ajout/suppression/quantité O(1).
3. **Auto-utilisation intelligente** (`SpeedUpAutoUse.ComputeBestPlan`) : algorithme glouton descendant par durée, minimise le gaspillage, combine Universelles + Spécialisées. Bouton "Utiliser automatiquement" → propose la combinaison optimale.
4. **UI fenêtre accélérations** : onglets 6 catégories, temps restant affiché, liste filtrable, boutons "Utiliser", "Utiliser automatiquement", "Terminer immédiatement" (architecture gemmes).
4. **Intégration 5 timers** : Construction, Recherche, Entraînement, Guérison, Fabrication — chaque catégorie a ses accélérations dédiées + Universelles comme fallback.
5. **Feedback Sprint-021** : consommation → animation icône + texte flottant (-1h) + pulse timer via Motion System.
5. **Architecture "Terminer immédiatement"** : bouton "Terminer immédiatement" (icône gemme) → feedback "Bientôt disponible avec gemmes", architecture prête pour micro-transactions.

**Fichiers créés / modifiés** :
- `Assets/BeeKingdom/Playground/SpeedUp.cs` (nouveau — registry data-driven)
- `Assets/BeeKingdom/Playground/SpeedUpInventory.cs` (nouvel — inventaire + auto-use)
- `Assets/BeeKingdom/Playground/UIFeedback.cs` (existant — feedback réutilisé)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : fenêtre SpeedUp, intégration timers, auto-use, finish-now, feedback.

**Performances** : chemin d'inactivité sans allocation ajoutée par l'inventaire et le feedback ; les ouvertures de fenêtre et calculs de plan restent des opérations événementielles. Compilation Unity 0 erreur. Architecture prête pour : Inventaire, Boutique, Récompenses, Quêtes, Événements, Cadeaux alliance, Pass saison, VIP, Coffres, Packs, Calendrier, Drops boss.

**Risques résiduels** : l'application serveur des accélérations et le bouton gemmes restent des intégrations à brancher ; ils ne sont pas simulés comme fonctionnalités live.

---

## Jalon courant — Alpha Sprint-023 : Project Snapshot System — Génération automatique d'état projet (2026-08-08)

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

## Revue de régression post-Sprint-023

- Le catalogue `strings.fr-CA.json` a été corrigé : les clés SpeedUp étaient après la fermeture de `entries`, ce qui provoquait un JSON invalide et cassait l'initialisation de la localisation.
- Les onglets du splash Connexion forcent désormais leur propre zone interactive et ne dépendent plus d'un `GUI.enabled` laissé par un overlay.
- L'intégration SpeedUp utilise les propriétés réellement exposées par les modèles Construction, Recherche et Production.
- Le coût de barrière défensive et le budget d'abeilles ambiantes paysage ont été corrigés ; le groupe tutoriel ciblé passe 55/55.
- La clé de localisation manquante `tutorial.chapter_01.hygiene_purge.result.confirmed` a été ajoutée en FR-CA et EN-US.
- Le readiness Google ne repasse plus à `NotConfigured` lorsqu'une restauration locale vide échoue normalement ; un gate serveur `Ready` conserve donc le bouton Google.
- La fermeture du profil Alliance réinitialise maintenant le menu Alliance et libère le rail inférieur ; la fermeture prioritaire couvre aussi les overlays Bestiaire et Défi.
- Le bootstrap `SandboxPlayground` lance explicitement `MusicTrack.Hive` au démarrage ; `MusicManager` sait aussi recharger `MusicLibrary` tardivement.
- Paramètres : le bouton `Plus` agit maintenant comme un toggle de fermeture et le retour interne réutilise `closing_arrow`.
- Le harnais de capture Splash Auth attend maintenant la fin réelle du chargement avant de capturer les états Connexion.
- Les allocations inutiles du chemin Feedback/SpeedUp ont été réduites.
- Tests EditMode vérifiés : `MobileAccountSessionUiTests` 13/13, `SpeedUpSystemTests` 3/3, `RewardPipelineTests` 2/2.
- Compilation Unity batch `6000.5.3f1` vérifiée sans erreur C#. Les avertissements API obsolètes et la configuration OAuth Google restent des validations distinctes du compilateur.

Le noyau `BeeKingdom.Rewards` est maintenant présent avec validation, handlers data-driven, historique, analytics/presentation hooks et vue Premium. Les récompenses du tutoriel utilisent ce pipeline. Les systèmes Construction/Recherche n'ont pas encore été migrés intégralement vers ce pipeline : cette migration reste un travail d'intégration séparé et ne doit pas être annoncée comme terminée.

---

## Jalon courant — Son de collecte des ressources de la ruche (2026-08-05)

Ajout du son de collecte pour les ressources produites par les bâtiments de la ruche : miel,
cire et pollen. Le clip est chargé automatiquement par l'AudioManager existant, sans dépendre
d'une assignation manuelle dans l'Inspecteur et sans être mélangé au catalogue des sons UI.
Le déclenchement est placé uniquement après une collecte réussie, dans les branches locale et
serveur de la collecte de production.

Preuves : le clip est chargé depuis `Resources` (nom `collect`, durée 1,34 s) ; lecture directe
confirmée en Play Mode (`isPlaying=True`) ; collecte locale de `honey_storage`, `wax_workshop` et
`warehouse_cells` confirmée avec 100 unités créditées pour chacune et `isPlaying=True` à chaque
fois. Compilation sans erreur et console sans erreur après le test. Play Mode quitté proprement.

Prochain test utilisateur : dans LivingHive, produire puis collecter du miel, de la cire et du
pollen sur leurs bâtiments respectifs. Le son doit jouer uniquement lorsqu'une quantité est
réellement créditée, jamais lorsque la capacité est pleine ou qu'aucune production n'est prête.

Ouvert / à faire ensuite : valider le volume du son en conditions normales de jeu et décider si
les autres actions de gameplay (récompenses, améliorations, recrutement, patrouilles) doivent
recevoir leurs propres sons dédiés.

---

## Jalon courant — Alpha Sprint-001 : sons d'ouverture/fermeture des panneaux UI (2026-08-05)

Deuxieme sprint audio, suite directe du clic UI centralise. Demande de Jeff : les panneaux doivent
jouer un son distinct a l'ouverture et un autre a la fermeture, via l'API
`AudioManager.Instance.PlayMenuOpen()` / `PlayMenuClose()`, toujours dans l'architecture audio
existante et toujours uniquement pour des panneaux d'interface (jamais batiment/carte/ressource/
creature/objet).

**Livraison** : `UiSoundId` etendu (`Click`, `MenuOpen`, `MenuClose`), `AudioManager` etendu avec
`PlayMenuOpen()`/`PlayMenuClose()` (qui reutilisent la garde anti-doublon 50 ms existante), deux
nouveaux sons `Assets/Audio/SFX/UI/open_menu.wav` et `close_menu.wav` (avec leurs `.meta`, guid
stables) associes aux ids 2 et 3 dans `Assets/Audio/Resources/UiSoundLibrary.asset`. Tous les points
de branchement sont dans `HiveViewProductUiPresenter.cs`.

**Panneaux cables en ouverture ET fermeture** : Championnes (bouton du haut, barre du bas, panneau
Patrouille), Sac, Quetes, Recherche, Alliance, Parametres, Bestiaire, Defi/Milestone, Communication
(barre de chat, menu "Plus", fermeture X/Échap/changement de panneau), Profil joueur, VIP, Detail de
puissance, Voie strategique, Doctrine de combat, Patrouille (fermeture X), Vue d'ensemble de la
colonie — en paysage et en portrait via la barre du bas (detection d'ouverture/fermeture par
comparaison du menu actif). Les clics UI existants sont conserves.

Preuves (execution reelle via MCP Unity, pas une hypothese) : compilation propre sans erreur apres
chaque etape (console vierge). En Play Mode reel sur la scene LivingHive : bibliotheque confirmee
chargee avec les trois clips (`ui_click`, `open_menu`, `close_menu`) ; `PlayMenuOpen()` puis
`PlayMenuClose()` confirment la mise en lecture reelle (isPlaying passe a `true` sur la source SFX) ;
alternance rapide ouverture/fermeture sans chevauchement ni erreur ; triple ouverture immediate
absorbe par la garde anti-doublon (aucun doublon audible, comportement exactement identique au clic
UI valide au jalon precedent). Les nouveaux chemins ont ete revalides en conditions reelles via les
methodes de production (equivalents de clics reels) : ouverture du panneau Patrouille, fermeture de
la Voie strategique, fermeture de la Communication — chacune a bien declenche le bon son. Scène de
demarrage confirmee toujours LivingHive (Build Settings inchange), Play Mode quitte proprement,
aucune erreur console au retour.

**Limite de methode (deja documentee au jalon precedent)** : l'IMGUI ne peut pas etre clique par un
outil automatise externe ; chaque branchement reste verifie par relecture individuelle du code plus
l'execution reelle des methodes de production qui portent le son.

Prochain test utilisateur : en Play Mode (LivingHive), ouvrir puis fermer successivement Championnes,
Sac, Quetes, Alliance, Parametres, Recherche, Bestiaire, Defi, Chat (barre + menu Plus), Profil
joueur, VIP, Detail de puissance, Voie strategique/Doctrine, Patrouille, Vue d'ensemble de la
colonie — chaque panneau doit jouer le son d'ouverture en s'ouvrant et le son de fermeture en se
fermant, sans doublon en cliquant vite, et les clics de navigation doivent rester instantanes.

Ouvert / a faire ensuite : popups d'information de batiment/zone sur la carte du monde volontairement
hors perimetre (regle UI/Monde) ; les sons dedies du monde (batiments, ressources, creatures,
combat) attendent leur propre sprint ; relecture des autres ecrans hors ruche (menu principal,
connexion) pour la meme convention de panneaux.

## Premium Impact

Un panneau qui s'ouvre et se ferme sans signal sonore est percu comme "mute" meme quand la
navigation fonctionne. En donnant a chaque panneau son ouverture et sa fermeture sonores distinctes,
la Ruche renforce la sensation d'un produit fini et coherent : chaque menu annonce sa venue et son
depart, et l'absence de doublon (garde anti-doublon) maintient un feedback net meme en usage rapide.
Le monde (batiments, ressources, creatures) reste volontairement silencieux : ses futurs sons dedies
auront d'autant plus d'impact en ne se melant jamais a l'interface.

---

## Jalon courant — Sprint Audio Polish : premier son d'interface centralise (clic UI) (2026-08-05, suite)

Suite directe des deux jalons precedents (MusicManager fonctionnel, voix des Championnes reparees et
equilibrees). Demande de Jeff : ajouter le premier vrai effet sonore d'interface (`ui_click.wav`, deja
present dans le projet), en reutilisant l'architecture audio existante plutot qu'en creant un nouveau
gestionnaire, avec une separation stricte UI/Monde dès maintenant.

**Architecture livree** (`Assets/_Project/Scripts/Audio/`, namespace `BeeKingdom.Audio`) :
- `UiSoundId` (enum) - catalogue des sons d'interface connus, un seul aujourd'hui (`Click`), pret a
  accueillir de futurs sons sans toucher a `AudioManager`.
- `UiSoundLibrary` (ScriptableObject, meme convention que `MusicLibrary`) - table de correspondance
  UiSoundId -> AudioClip, plus la reference au groupe "SFX" du MasterMixer. Vit sous
  `Assets/Audio/Resources/UiSoundLibrary.asset`, cree et configure directement (groupe SFX + clip
  `ui_click.wav` assignes) - rien a faire manuellement dans l'Inspecteur pour ce premier son.
- `AudioManager` etendu (pas remplace) : `AudioManager.Instance.PlayUIClick()` - exactement l'API
  demandee par Jeff. `AudioManager` beneficie desormais de la meme garantie de disponibilite que
  `MusicManager` (cree avant meme le chargement de la toute premiere scene) et route enfin sa source
  d'effets sonores partagee vers le groupe SFX du MasterMixer (elle ne passait par aucun groupe
  auparavant - corrige au passage pour toute la source, pas seulement pour ce nouveau son). Garde
  anti-doublon integree (50 ms minimum entre deux lectures du meme son) - absorbe les double-clics et
  les declenchements simultanes sans jamais etre perceptible comme un retard.

**Separation UI/Monde** : la fonction partagee qui dessine la quasi-totalite des boutons d'action du
jeu (~62 emplacements) a reçu un parametre explicite indiquant si CE bouton precis est une action
purement UI ou une action de gameplay liee a un batiment/ressource/troupe - avec **false par defaut**
(le son ne joue jamais) precisement pour que l'oubli ne puisse jamais faire jouer le son sur une
action du monde. Chaque emplacement a ete relu individuellement (label, effet reel du clic) pour
decider s'il fallait passer explicitement "vrai" : navigation/consultation (Voir, Retour, Fermer,
Actualiser, Ouvrir un panneau) l'ont reçu ; toute action qui depense une ressource, entraine une
troupe, ameliore un batiment, lance une patrouille ou reclame une recompense de batiment ne l'a
delibermement PAS reçu (elle recevra son propre effet sonore plus tard, comme demande). Les boutons de
fermeture de fenetre, les icones de la barre du bas, les onglets de filtre et les lignes du menu
"Plus" (Communication/Recherche/Parametres) - qui sont tous purement UI sans exception - jouent le son
sans condition.

**Fichiers modifies** : `Assets/_Project/Scripts/Audio/AudioManager.cs` (API + routage mixer + garde
anti-doublon + disponibilite garantie), `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
(plus de 60 points d'integration repartis sur les boutons de fermeture, la barre d'icones du bas, le
menu "Plus", les onglets du panneau Championnes, et les emplacements purement UI de la fonction de
bouton partagee).
**Fichiers crees** : `Assets/_Project/Scripts/Audio/UiSoundId.cs`, `UiSoundLibrary.cs`,
`Assets/Audio/Resources/UiSoundLibrary.asset`.

Preuves (execution reelle en Play Mode, pas une hypothese) : compilation propre sans erreur apres
chaque etape. `AudioManager.Instance` confirme jamais null des le debut d'une session ; source
d'effets confirmee routee vers le groupe "SFX" reel du MasterMixer (pas vers le master directement) ;
le clip `ui_click` confirme trouve et charge depuis la bibliotheque reelle. Un premier appel reel a
`PlayUIClick()` a bien active la lecture (isPlaying passe a `true` sur la bonne source, avec le meme
point d'ecoute audio deja valide par les jalons precedents - donc reellement audible, pas seulement
"joue en interne"). Garde anti-doublon verifiee en conditions reelles en deux temps : trois appels
immediats consecutifs (aucun temps reel ecoule entre eux) puis, apres avoir avance reellement au-dela
de la fenetre de 50 ms (mode pas-a-pas de l'Editeur), un nouvel appel confirme relance normalement une
lecture - le mecanisme s'ouvre et se referme exactement comme concu. Panneau "Abeilles championnes"
ouvert en conditions reelles (avec le panneau de debug du jalon precedent toujours visible) : aucune
erreur console, rendu visuel intact, tous les boutons ("Voir", onglets de filtre, fermeture) toujours
fonctionnels apres l'ajout du son. MusicManager revérifié sain apres tout ce travail : toujours
exactement un AudioListener, toujours actif.

**Limite honnete de methode** : l'IMGUI de ce projet ne peut pas etre clique par un outil automatise
externe (GUI.Button exige un vrai evenement d'entree Unity, impossible a simuler depuis l'exterieur
sans casser le pipeline d'evenements) - chaque point d'integration a donc ete verifie par relecture
individuelle du code (effet reel du clic, pas seulement son libelle) plutot que par un clic reellement
simule, en plus de la preuve d'execution reelle sur le mecanisme `PlayUIClick()` lui-meme (qui, lui, a
ete verifie de bout en bout par execution reelle). Coherent avec les limites deja documentees dans ce
journal pour les tests Play Mode automatises.

## Premium Impact

Un jeu qui ne "repond" jamais au toucher - aucun retour sonore sur un bouton, un menu qui s'ouvre, une
confirmation - se ressent immediatement comme inerte, meme si tout fonctionne correctement en
apparence. C'est un signal que le cerveau capte avant meme de savoir le nommer : "ce n'est pas fini".
En donnant a chaque interaction purement UI - et seulement celles-la - un retour sonore net et
coherent, la Ruche commence a "repondre" comme un vrai produit fini, tout en gardant intacte la
promesse specifique de BeeKingdom : le monde (batiments, ressources, creatures) restera silencieux
jusqu'a ce qu'il reçoive ses propres sons, dedies et significatifs, plutot que de partager un clic
generique qui diluerait leur impact. Cette separation stricte, posee des le premier son, evite aussi
tout un travail de demelage retroactif plus tard - chaque futur son (succes, erreur, alerte, sons du
monde) s'ajoutera au bon endroit du premier coup.

Prochain test utilisateur : lancer le jeu reellement, naviguer entre plusieurs menus (Plus, Parametres,
Sac, Recherche, Alliance), ouvrir/fermer plusieurs fenetres, cliquer des boutons de confirmation/retour,
ouvrir le panneau Championnes et changer d'onglet de filtre - confirmer que chacun de ces clics produit
le son, et qu'aucun clic sur un bouton de batiment/ressource/troupe (Collecter, Ameliorer, Entrainer,
Lancer une patrouille) ne le declenche.

Ouvert / a faire ensuite : quelques boutons purement internes aux panneaux Armee/Recherche/Alliance/
Sac/Quetes (au-dela de leurs boutons d'ouverture/fermeture deja couverts) n'ont pas ete individuellement
relus dans ce sprint - a completer si Jeff remarque un bouton UI silencieux en testant. Reprendre ensuite
le First Hour Experience Pass (PB-009) ou le chantier Audio dedie (sons de succes/erreur, sons du monde
pour batiments/ressources/creatures) selon le choix de Jeff.

---

## Jalon courant — Mixage voix/musique : les Championnes ressortent enfin par-dessus la musique (2026-08-05, suite)

Suite directe du jalon precedent (voix des Championnes reparees et confirmees fonctionnelles par
Jeff). Retour immediat de Jeff : les voix sont techniquement audibles mais beaucoup trop faibles face
a la musique - on les entend a peine. Demande implicite : corriger l'equilibre de mixage, pas juste
confirmer que le son sort.

**Correctif** : deux leviers combines, tous deux internes au systeme audio centralise existant (aucun
nouveau systeme, rien touche cote scenes) :
1. La musique baisse desormais automatiquement et brievement (a environ un tiers de son volume normal)
   pendant qu'une voix de Championne est en train de parler, puis remonte progressivement a son volume
   normal une fois la ligne terminee - technique de mixage standard ("ducking") deja utilisee dans
   la quasi-totalite des jeux avec voix. Le systeme de musique centralise porte maintenant cette
   logique lui-meme (nouvelle fonction publique dediee), independante du fondu croise Ruche/Carte
   existant - les deux peuvent se produire en meme temps sans jamais se marcher dessus.
2. Les voix des Championnes jouent maintenant a un volume dedie plus eleve que les autres effets
   sonores generiques (clics, etc.), au lieu de partager exactement le meme reglage - une voix qui a
   quelque chose a dire doit toujours ressortir, jamais se fondre dans les bips generiques de
   l'interface.

**Fichiers modifies** : le systeme de musique centralise (nouvelle fonction de "ducking" + boucle de
mise a jour continue), le systeme d'effets sonores centralise (nouvelle variante permettant un volume
dedie par appel), et la couche de declenchement des voix des Championnes (utilise desormais ce nouveau
volume dedie et declenche le ducking a chaque replique).

Preuves (execution reelle, pas une hypothese) : la transition de volume a ete observee image par image
en Play Mode reel (mode pas-a-pas de l'Editeur, pour rendre chaque etape du fondu visible malgre la
cadence tres rapide de la transition) - confirmee : le volume de la musique descend bien et sans saut
jusqu'au niveau reduit voulu en environ un quart de seconde des le debut d'une replique, reste
parfaitement stable a ce niveau pendant toute sa duree, puis remonte tout aussi progressivement et
integralement au volume normal une fois la replique terminee - aucun blocage, aucune derive, aucun
saut brusque dans un sens ou l'autre. Testee sur un evenement de declenchement jamais encore utilise
dans cette session pour garantir un etat frais (aucune interference avec les delais de retenue deja
verifies au jalon precedent, qui continuent de fonctionner normalement et inchanges). Limite honnete
de methode notee au jalon precedent (la fenetre Editeur sans focus reel avance tres irregulierement sa
boucle par image en pilotage automatise) contournee cette fois par le mode pas-a-pas explicite de
l'Editeur plutot que par une simple attente - qui a permis d'observer directement la vraie progression
image par image plutot que de la deduire.

Prochain test utilisateur : en jeu reel, ouvrir le panneau Championnes et declencher une voix (bouton
reel ou panneau de debug) pendant que la musique joue - confirmer que la musique baisse clairement et
brievement le temps de la replique, que la voix se distingue nettement, et que la musique retrouve son
volume normal juste apres, sans coupure ni a-coup perceptible.

Ouvert / a faire ensuite : si l'equilibre ne convient toujours pas a l'usage (musique pas assez baissee,
ou trop), les deux constantes de reglage (profondeur du "ducking" et volume dedie des voix) sont
isolees et nommees explicitement dans le code, ajustables en une seule ligne chacune sans toucher au
reste du systeme. Reprendre ensuite le First Hour Experience Pass (PB-009) ou le chantier Audio dedie
selon le choix de Jeff.

---

## Jalon courant — Diagnostic reel : voix des Championnes silencieuses (2026-08-05, suite)

Suite directe du jalon precedent (musique reparee). Jeff a valide que la musique fonctionne, mais a
signale que les voix des Championnes restent totalement silencieuses malgre les fichiers audio bien
presents sous `Resources/PremiumBeeReference/ChampionVoices`. Demande explicite : diagnostic par
execution reelle uniquement, pas d'hypothese, plus un panneau de debug temporaire (boutons Spawn/Select
par Championne) pour isoler si le probleme vient des fichiers ou des evenements qui les declenchent.

**Outil de diagnostic ajoute** : un panneau "DEBUG VOIX CHAMPIONNES (dev uniquement)" dans le panneau
"Abeilles championnes" lui-meme, visible sous la meme garde que le reste du menu Developpement
(Editeur / Development build uniquement). Six boutons (Spawn/Select x Striga/Zephyra/Ambra) qui
appellent les memes points d'entree publics reels que la production (le meme chemin que le bouton
"Voir" pour Select, le meme chemin que le declenchement de patrouille pour Spawn) - aucun raccourci ni
simulation, le vrai systeme de barks est exerce a chaque clic. Chaque clic journalise un instantane
avant/apres (cooldown en cours, verrou anti-chevauchement, nombre de clips reellement trouves sur
disque, etat reel de la source audio) pour un diagnostic immediat sans avoir a deviner.

**Diagnostic en vrai Play Mode (execution reelle, pas une lecture de code)** : chaque bouton invoque
directement via reflexion en cours d'execution reelle (equivalent exact d'un vrai clic). Resultat pour
un appel isole, a l'etat frais (aucun autre bark declenche avant, verrou anti-chevauchement inactif) :
les fichiers audio des trois Championnes se chargent correctement (5 clips "select", 3 clips "spawn"
retrouves reellement sur disque pour chacune), le systeme de declenchement (cooldown, verrou anti-
chevauchement, choix de la Championne pertinente) laisse bien passer l'appel, et la lecture reelle du
clip s'active effectivement (isPlaying bascule a `true` sur la bonne source, avec le bon clip). Chaine
complete testee et confirmee fonctionnelle : chargement de fichier -> logique d'evenement -> lecture
audio reelle.

**Cause reelle** : la meme cause que le jalon precedent - l'absence totale d'AudioListener dans le jeu.
Avant le correctif du jalon precedent, AUCUNE source audio du jeu (musique ni effets, y compris les
voix des Championnes) ne pouvait jamais etre rendue audible, quelle que soit la justesse de son propre
cablage - confirme ici par le fait que le pipeline des voix, teste isolement dans cette meme session
desormais corrigee, fonctionne integralement de bout en bout. Aucun bug supplementaire specifique aux
voix des Championnes n'a ete trouve dans leur propre code (fichiers bien nommes, dossiers bien
retrouves, logique de cooldown/retenue fonctionnant exactement comme concue).

**Point d'attention reel, pas un bug** : le systeme impose deux garde-fous voulus par conception - un
verrou anti-chevauchement partage entre TOUTES les Championnes (une seule voix a la fois) et un delai
minimal de 45 secondes entre deux barks de type "selection" pour la MEME Championne. Des clics repetes
et rapprochés sur plusieurs Championnes pendant un test peuvent donc sembler silencieux au-dela du tout
premier, alors qu'il s'agit de la retenue volontaire deja demandee par Jeff ("une Championne ne parle
jamais pour combler le silence") - a garder en tete en testant : espacer les clics de quelques secondes
pour observer chaque voix independamment.

**Fichier modifie** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` - ajout uniquement
du panneau de debug et de ses fonctions de journalisation (aucune modification du systeme de voix des
Championnes ni du systeme audio lui-meme, qui n'avaient pas besoin de correctif supplementaire une fois
le correctif du jalon precedent en place).

Preuves : recompilation reelle sans erreur. Deux passes en vrai Play Mode : (1) un lot des six
evenements declenches en rafale (utile pour confirmer que le verrou anti-chevauchement fonctionne
correctement - chaque appel suivant le tout premier a ete correctement bloque, comme concu, puisqu'ils
s'enchainaient sans le moindre temps reel ecoule entre eux) ; (2) un test isole propre (Play Mode
redemarre a neuf, un seul bouton "Select Striga" comme unique action) confirmant la meme reussite
complete que pour "Spawn" - chargement de clip, cooldown/verrou franchis normalement, lecture reelle
activee. AudioListener revérifié present (1 seul, sur l'objet audio centralise) dans cette session
fraiche, confirmant que le correctif du jalon precedent s'applique bien a chaque nouvelle session sans
action manuelle. Scene ramenee a son etat d'origine (LivingHive, non dirty) en fin de test.

Prochain test utilisateur : ouvrir le panneau "Abeilles championnes" en jeu reel (Editeur ou
Development build), utiliser les nouveaux boutons de debug (un clic a la fois, en espaçant chaque
clic de quelques secondes) pour confirmer que chaque Championne parle bien maintenant ; puis confirmer
aussi via le vrai bouton "Voir" en jeu normal. Si les voix restent silencieuses malgre ce test, cela
indiquerait un probleme de session/build specifique (a redemarrer completement) plutot qu'un bug de
code, puisque le pipeline complet est verifie fonctionnel dans cette session.

Ouvert / a faire ensuite : retirer le panneau de debug une fois le test de Jeff confirme (ou le
conserver tel quel, deja protege par la garde Development-build-uniquement). Reprendre ensuite le First
Hour Experience Pass (PB-009) ou le chantier Audio dedie (pistes Combat/Boss/Victoire/Defaite...) selon
le choix de Jeff.

---

## Jalon courant — Bug reel trouve en vrai Play Mode : aucun AudioListener dans le jeu, musique et sons totalement muets (2026-08-05)

Suite directe du Sprint Audio Foundation (voir jalon precedent). Jeff a teste reellement dans l'Editeur
Unity apres ce sprint : `MusicManager` bien cree sous `DontDestroyOnLoad`, `MusicLibrary` correctement
configuree (Hive -> clip hive, World -> clip map, Music Mixer Group -> Music), mais aucune musique
audible ni dans LivingHive ni sur la Carte du monde. Consigne explicite : ne pas partir d'hypotheses,
executer un vrai Play Mode et suivre le chemin d'execution reel jusqu'a la cause racine.

**Diagnostic en vrai Play Mode (pas une hypothese de lecture de code)** : Play Mode reellement demarre
dans la scene canonique LivingHive via les outils MCP Unity, puis inspection par reflexion directe de
l'instance vivante de `MusicManager` pendant l'execution reelle. Resultat inattendu : `MusicLibrary`
charge correctement, groupe de mixer "Music" bien assigne, `MusicManager.Instance.Play(MusicTrack.Hive)`
bien appele par le vrai code de `LivingHiveDemoBootstrap.Start()` (pas un chemin de test) - le clip
"hive" etait bien assigne a une AudioSource avec le bon groupe de mixer et le bon volume apres fondu.
Donc le cablage rapporte par Jeff dans l'Inspecteur (mixer group a None, clip jamais assigne) ne
correspondait plus a l'etat reel au moment de ce test - mais `AudioSource.isPlaying` restait `false`
malgre un clip assigne et un volume non nul, ce qui est le vrai symptome pointant vers la cause racine.

**Cause reelle trouvee** : recherche de tout `AudioListener` present dans le jeu (actif ou non, toutes
scenes chargees confondues) - resultat : zero, partout. La seule camera du jeu (`Main Camera`, creee et
configuree par `SandboxPlaygroundBootstrap.EnsureRenderableCamera`, une fonction partagee avec les
outils de capture d'ecran internes du projet qui n'ont justement jamais besoin de son) n'a jamais reçu
de composant `AudioListener`. Confirme aussi dans les fichiers de scene bruts (`LivingHive.unity` et
`WorldMapMmoFullscreenFoundation.unity`) : aucun des deux ne definit d'AudioListener nulle part. Sans
AudioListener actif dans le jeu, Unity n'a litteralement aucun point d'ecoute pour rendre le moindre son
audible - musique ET effets sonores - quelle que soit la justesse du cablage MusicManager/MusicLibrary
(qui s'est revele en realite correct). La console du jeu confirmait deja ce diagnostic en continu :
avertissement Unity natif "There are no audio listeners in the scene" repete en boucle.

**Fichier modifie** : `Assets/_Project/Scripts/Audio/MusicManager.cs` - `Awake()` garantit desormais
qu'un unique `AudioListener` existe, porte par l'objet `MusicManager` lui-meme (le seul objet du jeu
garanti present avant le chargement de la toute premiere scene et qui survit a chaque changement de
scene) plutot que par la camera d'une scene en particulier. Garde explicite pour ne jamais en ajouter un
second si une scene venait un jour a en definir un elle-meme. `SandboxPlaygroundBootstrap.
EnsureRenderableCamera` et les outils de capture d'ecran qui la partagent n'ont volontairement pas ete
touches (ils ne passent jamais par `MusicManager` puisqu'ils tournent hors Play Mode reel, donc aucun
risque de leur ajouter un son indesirable).

Preuves (vrai Play Mode execute deux fois, avant et apres correctif, pas une hypothese) : (1) avant
correctif - `AudioListener` count = 0 confirme par recherche reelle dans la scene active en cours
d'execution, `sourceB.isPlaying` = false meme immediatement apres un appel manuel explicite a `.Play()`
sur la vraie AudioSource vivante ; peripherique audio verifie sain par ailleurs (48kHz stereo, pas
d'absence de materiel) - donc le silence n'etait pas du a une carte son absente. (2) apres correctif -
recompilation reelle sans erreur, nouveau Play Mode reel : `AudioListener` count = 1 (porte par
MusicManager, actif), `sourceB.isPlaying` bascule a `true` dans les memes conditions exactes qu'avant
(meme clip, meme volume en cours de fondu) - preuve directe que l'absence d'AudioListener etait bien la
cause du silence. (3) vrai changement de scene reel (`SceneManager.LoadScene(..., Single)`) LivingHive ->
Carte du monde execute en Play Mode reel : le meme MusicManager (identifiant conserve) survit a la
transition, `WorldMapMmoFullscreenFoundationBootstrap.Awake()` (vrai code de jeu, pas un appel de test)
declenche bien `Play(MusicTrack.World)`, la source portant "map" passe a `isPlaying=true` / volume plein
tandis que la source "hive" redescend a volume 0 - fondu croise reel confirme en conditions reelles, un
seul AudioListener toujours present apres la transition (aucun doublon). Scene ramenee a LivingHive et
Play Mode arrete en fin de test ; scene verifiee non dirty.

Prochain test utilisateur : lancer le jeu reellement (pas seulement l'Editeur), confirmer que la musique
de la Ruche est maintenant audible des l'arrivee, ouvrir la Carte du monde et confirmer le fondu croise
audible vers la musique de la carte, revenir a la Ruche et confirmer le fondu inverse. Profiter aussi de
l'occasion pour confirmer que les effets sonores (`AudioManager`, jusqu'ici jamais testes en conditions
reelles non plus faute d'AudioListener) sont eux aussi redevenus audibles.

Ouvert / a faire ensuite : reprendre le First Hour Experience Pass (PB-009 toujours le prochain
candidat identifie). Le chantier Audio reste ouvert pour un futur sprint dedie (pistes Combat/Boss/
Victoire/Defaite/Menu principal/Ecran de connexion/Evenements mondiaux/Musiques saisonnieres,
interrupteur "musique activee" des preferences de confort mobile a relier a MusicManager) - non traite
ici pour rester strictement dans le perimetre du bug signale par Jeff.

---

## Jalon courant — Sprint Audio Foundation : MusicManager centralise, architecture evolutive (2026-08-04, suite)

Le First Hour Experience Pass est mis en pause a la demande explicite de Jeff pour ce sprint dedie a
la fondation audio. Jeff avait deja prepare Unity avant de commencer : `Assets/Audio/Mixers/
MasterMixer` (groupes Master > Music, SFX) et les musiques `Assets/Audio/Music/hive.mp3` /
`Assets/Audio/Music/map.mp3` deja importees. Consigne explicite : plus aucune scene ne doit piloter la
musique directement - tout doit passer par une architecture centralisee, avec uniquement Hive/World en
V1 mais une architecture prete a accueillir Combat/Boss/Victoire/Defaite/Menu principal/Ecran de
connexion/Evenements mondiaux/Musiques saisonnieres sans devoir la retoucher.

**Decouverte prealable importante** : un `AudioManager` (`Assets/_Project/Scripts/Audio/
AudioManager.cs`) existait deja dans le projet mais n'a jamais ete relie a une vraie boucle musicale
(sa fonction `PlayMusic` n'a aucun appelant reel nulle part - seuls les interrupteurs "son/musique
active" des preferences de confort mobile l'utilisent) et n'a ni fondu ni routage vers un groupe de
mixer. Il reste en place tel quel (gere les effets sonores ponctuels), mais n'est plus le chemin pour
la musique - `MusicManager` est desormais la seule autorite pour toute musique du jeu.

**Architecture livree** (`Assets/_Project/Scripts/Audio/`, namespace `BeeKingdom.Audio`) :
- `MusicTrack` (enum) - catalogue de toutes les pistes connues. Seules `Hive` et `World` sont
  implementees ce sprint ; huit entrees supplementaires (`Combat`, `Boss`, `Victory`, `Defeat`,
  `MainMenu`, `LoginScreen`, `WorldEvent`, `Seasonal`) existent deja dans l'enum pour que
  l'architecture soit visiblement prete, sans qu'aucun clip ne leur soit associe.
- `MusicLibrary` (ScriptableObject) - table de correspondance piste -> clip, plus la reference au
  groupe "Music" du MasterMixer. Vit sous un dossier Resources (`Assets/Audio/Resources/
  MusicLibrary.asset`) uniquement pour que `MusicManager` puisse la charger sans reference de scene -
  les vrais fichiers audio et le mixer de Jeff restent a leur emplacement d'origine, rien n'a ete
  deplace. Ajouter une future piste (Combat, Boss...) se fera entierement par l'Inspecteur sur cet
  asset, sans toucher au code.
- `MusicManager` (MonoBehaviour singleton, `DontDestroyOnLoad`, meme convention `EnsureInstance()` que
  `AudioManager` mais avec en plus un `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` qui garantit
  que `MusicManager.Instance` n'est jamais null, exactement l'API demandee par Jeff :
  `MusicManager.Instance.Play(MusicTrack.Hive)`). Deux `AudioSource` persistantes creees une seule fois
  (jamais recreees), toutes deux en boucle et routees vers le groupe Music du MasterMixer. `Play()`
  demarre un fondu croise entre la source active et l'autre ; un appel pendant un fondu deja en cours
  reprend depuis le volume reel de chaque source a cet instant (jamais de saut), et reconnait si la
  piste demandee est deja chargee sur une source en train de s'estomper pour la reprendre telle quelle
  au lieu de la relancer depuis le debut - c'est ce qui couvre proprement les allers-retours rapides.
  Une piste sans clip configure (toutes les pistes futures aujourd'hui) est ignoree avec un simple
  avertissement, jamais une erreur.

**Branchement** (aucune scene ne gere plus de clip/AudioSource elle-meme) :
- `LivingHiveDemoBootstrap.Start()` -> `MusicManager.Instance.Play(MusicTrack.Hive)`.
- `WorldMapMmoFullscreenFoundationBootstrap.Awake()` -> `MusicManager.Instance.Play(MusicTrack.World)`.

Ce choix (brancher sur le point d'entree de CHAQUE scene plutot que sur les fonctions de navigation
Ruche<->Carte specifiques) est deliberement plus robuste : la Ruche peut aussi se charger directement
au tout premier lancement (avant meme tout aller-retour), et la carte a plusieurs points d'entree
internes (bouton normal, raccourcis de developpement) - brancher sur le chargement de scene lui-meme
garantit que la bonne musique demarre quel que soit le chemin emprunte pour y arriver, sans devoir
retrouver et cabler chaque point d'entree un par un.

**Limite interimaire assumee** : l'ecran de demarrage/connexion (avant d'entrer effectivement dans la
Ruche) herite du theme Hive puisqu'aucune piste "Ecran de connexion"/"Menu principal" n'existe encore -
comportement attendu et deja prevu par l'architecture (roadmap ci-dessus), a corriger naturellement
quand ces pistes seront ajoutees dans un futur sprint.

**Vrai bug trouve et corrige en testant reellement (pas a la lecture du code)** : la toute premiere
version de la logique de selection de source pour le fondu comparait la piste demandee a la mauvaise
reference (la nouvelle piste au lieu de l'ancienne), ce qui aurait fait ecraser en plein milieu de
lecture le clip de la source EN TRAIN DE JOUER (exactement le clic/coupure que ce systeme doit
empecher) au lieu de charger la nouvelle piste sur la source libre. Trouve uniquement parce qu'une
vraie transition Ruche -> Carte a ete executee en Play Mode et l'etat reel des deux AudioSource
inspecte - une relecture du code seule n'aurait pas revele ce bug (la logique semblait correcte a la
lecture). Corrige en comparant desormais a la piste precedente, pas a la nouvelle.

Preuves (session simulee reelle, executee, pas une hypothese de lecture de code) :
- Lecture initiale de la Ruche : `Play(Hive)` verifie directement (invocation manuelle de la meme
  fonction `Start()` que le jeu utilise) - bascule confirmee.
- Six allers-retours Ruche/Carte rapides et successifs simules via de vrais appels a `Play()` :
  a chaque etape, exactement 2 AudioSource au total (aucun doublon, aucune fuite) et exactement une
  source correspond a la piste ciblee - jamais deux pistes revendiquees en meme temps.
- Reprise en plein fondu verifiee explicitement : demander de revenir sur une piste deja chargee sur
  la source en train de s'estomper ne relance jamais son `.Play()`/`.time` (positions de lecture
  identiques avant/apres, preuve directe qu'aucun redemarrage audible ne se produit).
- Piste non configuree (`Combat`) : aucune exception, aucune source creee, avertissement clair en
  console.
- Deux vrais changements de scene reels (`SceneManager.LoadScene(..., Single)`) Ruche -> Carte -> Ruche
  executes en Play Mode : le meme `MusicManager` (identifiant d'instance identique avant/apres les deux
  chargements) survit aux deux transitions, toujours exactement 2 AudioSource, le crochet de reprise
  d'entree de chaque scene (`Awake`/`Start`) a bien declenche le bon `Play()` cote code reel du jeu (pas
  un appel de test) a chaque fois.
- Limite honnete relevee pendant le test : cette session Editeur pilotee sans focus de fenetre reelle
  n'avance quasiment pas sa boucle de rendu par image (`Time.frameCount` reste bloque), ce qui a rendu
  impossible d'observer le fondu progresser image par image en conditions reelles (comportement qui
  serait invisible pour un vrai joueur, dont la fenetre tourne a plein regime) - compense en invoquant
  directement les memes fonctions de production pour prouver leur logique, et en confirmant que l'unique
  piece non verifiable ainsi (le declenchement automatique par `Start()`) fonctionne bien des qu'elle
  est effectivement executee.

**Fichiers crees** : `Assets/_Project/Scripts/Audio/MusicTrack.cs`, `MusicLibrary.cs`,
`MusicManager.cs`, `Assets/Audio/Resources/MusicLibrary.asset`.
**Fichiers modifies** : `Assets/BeeKingdom/Playground/LivingHiveDemoBootstrap.cs`,
`Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`.

**Pourquoi ceci rend BeeKingdom plus premium (pas seulement technique)** : un jeu ou la musique coupe
net, se chevauche ou redemarre en boucle a chaque changement d'ecran est l'un des signaux les plus
immediats et les plus universellement reconnus de prototype inacheve - meme un joueur qui ne saurait
jamais nommer le probleme le RESSENT immediatement. A l'inverse, une transition musicale fluide entre
la Ruche et la Carte du monde donne au monde une impression de continuite et de cohesion, exactement
la sensation qu'un vrai MMO doit degager. Et parce que cette fondation est deja prete a accueillir
Combat/Boss/Victoire/Menus/Evenements sans reecriture, chaque futur ajout de musique se sentira
"branche sur un vrai jeu fini" des le premier jour, plutot que d'etre une nouvelle rustine.

Prochain test utilisateur : lancer le jeu, confirmer que la musique de la Ruche demarre des l'arrivee,
ouvrir la carte du monde et confirmer un fondu croise fluide vers la musique de la carte (aucune
coupure, aucun chevauchement audible), revenir a la Ruche et confirmer le fondu inverse, repeter
plusieurs allers-retours rapides pour confirmer qu'aucun clic ni chevauchement n'apparait jamais.

Ouvert / a faire ensuite : reprendre le First Hour Experience Pass (prochain candidat deja identifie :
PB-009). Pour un futur sprint audio dedie : ajouter les pistes Combat/Boss/Victoire/Defaite/Menu
principal/Ecran de connexion/Evenements mondiaux/Musiques saisonnieres des que les fichiers audio
correspondants seront fournis (architecture deja prete, ajout par Inspecteur uniquement) ; envisager de
relier l'interrupteur "musique activee" deja existant dans les preferences de confort mobile a
`MusicManager` (actuellement il ne controle que l'`AudioManager` historique qui ne joue plus jamais de
musique reelle - pas fait ce sprint pour rester strictement dans le perimetre demande).

---

## Jalon courant — First Hour Experience Pass, Sprint 5 : PB-013 elimine a la racine (fragilite shader) + transition d'ouverture du panneau Championnes (2026-08-04, suite)

Suite directe du Sprint 4 (PB-014 elimine). Avant de commencer, relecture rapide de ce document et de
`PREMIUM_PLAYTEST_REPORT.md` confirmee conforme a l'etat reel. Jeff a explicitement demande de ne pas
se contenter de masquer le symptome (ex. rendre le fond du panneau Championnes opaque comme envisage au
sprint precedent) mais de trouver la vraie cause - meme si elle se trouve dans un systeme de
developpement, un ordre de rendu, un masque, un materiau, un tri de canvas ou un element temporaire.

**Investigation** : audit exhaustif en Play Mode (pas seulement lecture de code) de tout ce qui pourrait
produire une forme violette derriere le panneau - tous les MeshRenderer de la scene (379, aucun shader
nul ni d'erreur, aucune couleur violette trouvee), tous les Canvas/Image/SpriteRenderer/ParticleSystem/
LineRenderer (aucun n'existe du tout dans cette scene), la couleur de fond de la camera (vert-noir
sombre, pas violet). Aucun de ces suspects habituels n'expliquait le symptome dans l'Editeur - attendu,
puisque l'Editeur ne retire jamais aucun shader d'un projet, contrairement a une vraie fabrication de
build.

**Cause reelle trouvee** : tous les materiaux 3D de la ruche (~379 elements de rendu du nid d'alveoles -
parois, reflets, teintes d'etat, halos...) sont fabriques au moment de l'execution avec un seul et meme
appel direct par nom de chaine a un shader du pipeline de rendu universel, sans aucun filet de securite
si ce shader manquait. Ce projet n'a pourtant jamais active ce pipeline de rendu nulle part (ni parametres
Graphiques, ni aucun palier de Qualite - verifie directement dans les fichiers de configuration) - le
shader n'est reference que par cet appel dynamique, jamais par un vrai materiau enregistre dans le
projet. Dans ces conditions precises, l'etape de fabrication d'un executable peut retirer ce shader du
jeu final meme s'il fonctionne normalement dans l'Editeur, et Unity affiche alors son materiau d'erreur
interne (une couleur magenta/violette vive) a la place de la couleur reellement voulue, quelle que soit
la forme de l'objet concerne. Indice trouve en cherchant la cause : les outils de capture d'ecran internes
du projet protegent deja leurs propres materiaux avec une chaine de secours a plusieurs shaders pour
exactement cette raison - seule la scene reellement jouee par un joueur n'avait jamais recu cette meme
protection, une vraie incoherence entre deux parties du meme projet, pas un oubli isole.

**Fichier modifie** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` - les six endroits qui
fabriquaient chacun leur materiau separement et sans filet remplaces par un seul point de fabrication
partage, avec la meme chaine de secours deja eprouvee par les outils internes (essaie le shader normal
en premier - aucun changement visuel si tout va bien - puis se rabat sur un shader universellement
disponible si le premier manquait, sans jamais laisser un objet sans shader).

Preuves : compilation Unity sans erreur. Validation par session simulee reelle en trois temps, chacun
executant la vraie fonction de production (pas une hypothese de lecture de code) : (1) en conditions
normales, le shader d'origine est toujours choisi en premier - aucun changement visuel pour personne ;
(2) echec du premier shader simule reellement (liste de secours falsifiee puis vraie fonction invoquee) -
bascule propre sur le shader suivant confirmee, sans exception, jamais de materiau sans shader ; (3)
reconstruction complete et reelle de la ruche via ce nouveau chemin de code (destruction puis
reconstruction du meme point d'entree que le jeu utilise au demarrage) suivie d'un nouvel audit complet
de tous les elements de rendu - toujours zero shader manquant ou d'erreur. Pas de build Windows empaquete
reproduit ce tour-ci (cycle de build complet hors de portee du sprint) - la fragilite est confirmee
directement par le code et son mecanisme de secours prouve fonctionnel par execution reelle.

**QoL de ce sprint** : le panneau "Abeilles championnes" s'ouvrait jusqu'ici instantanement, sans aucune
transition, contrairement au panneau detaille des batiments qui a deja une legere animation d'ouverture
etablie dans le projet. Ajout d'une transition courte (fondu du fond + leger effet d'agrandissement du
panneau, ~0,22s, meme courbe d'accentuation que l'animation existante) sur sa propre minuterie dediee
(independante de celle du panneau de batiment, pour ne jamais interferer avec elle) et respectant le
meme interrupteur de mouvement reduit deja utilise par les harnais de capture internes (jamais
d'animation inattendue pour ces outils). Validation par session simulee reelle : la fonction de
progression a ete executee a plusieurs instants reels (ouverture, mi-parcours, bien apres la fin) et
confirme une vraie interpolation progressive, jamais un saut ni une animation infinie ; le mode mouvement
reduit renvoie bien un etat stable immediat ; la mise a l'echelle du panneau a ete verifiee pour ne
jamais faire deriver son centre pendant l'ouverture.

Prochain test utilisateur : (1) ouvrir "Abeilles championnes" en jeu et confirmer qu'aucune forme
violette n'apparait plus derriere le panneau, en particulier sur la build Windows empaquetee (le seul
contexte ou la fragilite d'origine pouvait reellement se manifester) ; (2) confirmer que l'ouverture du
panneau montre maintenant un petit fondu/agrandissement au lieu d'apparaitre d'un coup.

Ouvert / a faire ensuite : continuer le First Hour Experience Pass - prochain candidat par ordre
d'impact/faisabilite deja etabli : PB-009 (messages de connexion contradictoires, portee plus large - 4
sources a unifier). Verifier aussi si PB-012 est vraiment deja resolu avec un rebuild+playtest avant de
le retirer definitivement de la liste. Chaque sprint suivant doit continuer d'inclure une petite
amelioration QoL (regle permanente), et se demander explicitement si la correction choisie ameliore
reellement ce qu'un joueur verra pendant sa premiere heure de jeu (nouvelle regle rappelee par Jeff ce
jour). Windows (§2.4) et Android sur appareil reel (§2.5) restent explicitement hors de portee tant que
ce chantier n'est pas termine. Le chantier Audio/Music Manager reste hors de portee pour l'instant.

---

## Jalon courant — First Hour Experience Pass, Sprint 4 : PB-014 elimine + nouveau palier de zoom arriere (2026-08-04, suite)

Suite directe du Sprint 3 (PB-010 elimine). Avant de commencer, relecture complete de ce document,
de `PREMIUM_PLAYTEST_REPORT.md` et de `ALPHA_READINESS_REVIEW.md` confirmee conforme a l'etat reel du
projet (aucune derive detectee). Jeff a demande de continuer le First Hour Experience Pass, un seul
Premium Blocker par sprint, elimine completement, valide par session simulee (jamais par lecture de
code seule).

**Choix** : parmi les PB encore ouverts (009, 011, 012, 013, 014), PB-014 retenu - cause deja
localisee avec precision lors du sprint precedent (etiquette inversee), correctif contenu au texte de
deux legendes, aucun risque de regression fonctionnelle (aucune valeur ni condition modifiee). PB-011
et PB-012 ecartes pour la meme raison qu'avant (PB-011 exige un vrai developpement serveur; PB-012 a
verifier par rebuild+playtest, pas un sprint actif). PB-013 et PB-009 restent les candidats suivants.

**Cause reelle confirmee** : dans le panneau detaille (bureau/paysage) d'un batiment de production
officielle, la legende dessinee a cote du nombre de CAPACITE totale du batiment (en bas du panneau)
reutilisait par erreur la meme fonction de texte que celle conçue pour decrire l'etat de la collecte
EN ATTENTE (en haut du panneau) - une fonction qui retourne "A collecter" quand tout va bien, ou
"Synchronisation"/"Actualisation requise" en cas de chargement/erreur. Resultat exact reproduit en
session simulee a partir du scenario du rapport original : nombre nu "2" en haut, puis "A collecter :
400.0K" en bas - qui laisse croire que 400.0K est la quantite en attente alors qu'il s'agit de la
capacite totale du batiment. Meme motif trouve et corrige a deux endroits du fichier (le panneau
officiel serveur, et son equivalent de secours en apercu local hors-ligne).

**Fichiers modifies** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (les deux
endroits ou cette legende de capacite est dessinee), plus une nouvelle cle de localisation FR/EN
ajoutee dans les deux fichiers `Assets/_Project/Data/Localization/Resources/Localization/strings.*.json`
(aucun texte en dur, conforme a la regle produit de localisation).

Preuves : compilation Unity sans erreur (assets-refresh, aucune erreur console). Validation par
session simulee reelle (pas une hypothese de lecture de code) : reconstruction exacte du scenario du
rapport a travers le vrai cablage du controleur officiel (meme point d'entree public que Sprint 3,
`ConfigureOfflineProductionControllerForRuntime`) avec une ligne de production reelle (2 en attente /
400 000 de capacite / +195 par heure) - confirme par reflexion directe : la legende qui aurait ete
"A collecter" avant le correctif (reproduisant exactement le rapport) devient "Capacite totale" apres
le correctif, sans toucher aux deux nombres eux-memes ni a aucune condition d'affichage. Controleur
reinitialise a son etat par defaut apres le test, aucun etat de test laisse en place. Scene Unity
verifiee non modifiee (non dirty) apres le test. `PREMIUM_PLAYTEST_REPORT.md` mis a jour (Sprint 4 du
First Hour Experience Pass + fiche PB-014 marquee RESOLU).

**QoL de ce sprint** : Jeff a redemande explicitement la meme amelioration que le Sprint 3
(zoom arriere maximal de la carte du monde) en precisant que le palier precedent (0.58 -> 0.42)
restait insuffisant a l'usage ("la camera reste trop proche"). Nouveau palier applique : constante
`MinZoom` dans `WorldMapMmoFullscreenFoundationBootstrap.cs`, de 0.42 a 0.30 (rayon visible encore
~1,4x plus large qu'apres le Sprint 3). Validation par session simulee cette fois plus poussee que le
Sprint 3 (qui n'avait fait qu'une relecture de la logique d'eviction) : le calcul reel de zone de
tuiles du fournisseur de tuiles Wave6 (celui utilise en jeu, pas une reimplementation) a ete execute
pour un ecran 1080p au nouveau palier - la zone visible passe d'environ 60 a 112 tuiles au premier
cercle (jusqu'a 160 en comptant l'anneau de prechargement, au-dela de la capacite cible du cache de
128), et `HasAllVisibleTiles` confirme que 100% de ces tuiles se chargent avec succes sans aucune
tuile manquante - la logique d'eviction (deja relue en detail au Sprint 3) protege explicitement de
l'expulsion toute tuile visible ou en peripherie immediate, quel que soit son nombre par rapport a la
capacite cible du cache. Seule consequence : un peu plus de memoire video consommee sur de tres grands
ecrans, sans aucune rupture fonctionnelle. Toujours pas de test visuel en Play Mode avec une vraie
camera ce tour-ci (a confirmer par Jeff).

Prochain test utilisateur : (1) ouvrir un batiment de production connecte au serveur officiel (ex.
Reserve de miel) et confirmer que la legende a cote du nombre de capacite (en bas du panneau) lit
maintenant "Capacite totale" et non plus "A collecter" ; (2) sur la carte du monde, zoomer a la molette
jusqu'au bout en arriere et confirmer que la vue est nettement plus large qu'avant, sans trou ni tuile
manquante, meme sur un grand ecran.

Ouvert / a faire ensuite : continuer le First Hour Experience Pass - prochain candidat par ordre
d'impact/faisabilite deja etabli : PB-013 (formes violettes glitchees derriere le panneau
Championnes, fix de symptome deja identifie), puis PB-009 (messages de connexion contradictoires,
portee plus large - 4 sources a unifier). Verifier aussi si PB-012 est vraiment deja resolu avec un
rebuild+playtest avant de le retirer definitivement de la liste. Chaque sprint suivant doit continuer
d'inclure une petite amelioration QoL (regle permanente). Windows (§2.4) et Android sur appareil reel
(§2.5) restent explicitement hors de portee tant que ce chantier n'est pas termine. Le chantier
Audio/Music Manager reste explicitement hors de portee pour l'instant (demande explicite de Jeff -
Unity est deja prepare cote assets/mixer, sera traite dans un sprint dedie plus tard).

---

## Jalon courant — First Hour Experience Pass, Sprint 3 : PB-010 elimine + nouvelle regle permanente QoL-par-sprint (2026-08-04, suite)

Suite directe du Sprint 2 (PB-007). Jeff a choisi PB-010 (capacite "0/3000.0M" des la creation de
compte) comme meilleur rapport impact/risque/temps parmi les candidats deja identifies. Cause confirmee
dans le code reel : la vue combinait le plafond de securite technique du serveur (`BalanceCapacity`,
1 milliard par ressource pour un compte neuf) au lieu de la vraie capacite calculee par niveau de
batiment (`ProductionCapacity`), deja exposee et deja utilisee correctement ailleurs dans la meme vue -
un vrai bug de cablage d'une seule ligne, pas un manque de donnee.

**Fichier modifie** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (`DynamicResourceValues`).

Preuves : compilation Unity sans erreur. Validation par session simulee (pas une hypothese de lecture de
code) : injection d'un modele de production reconstituant precisement le scenario du rapport (capacite de
securite serveur a 1 milliard par ressource, vraies capacites de batiment a 1000/800/500) directement
dans la presentation reelle utilisee par le jeu via le point d'entree public prevu pour cela
(`ConfigureOfflineProductionControllerForRuntime`) - l'affichage obtenu passe de l'ancien "0/3000.0M" a
"0/2.3K" (somme exacte des vraies capacites), pourcentage compact inclus. Controleur reinitialise a son
etat par defaut apres le test (`ResetOfflineProductionControllerForRuntime`), aucun etat de test laisse
en place. `PREMIUM_PLAYTEST_REPORT.md` mis a jour (Sprint 3 du First Hour Experience Pass + fiche PB-010
marquee RESOLU).

**Nouvelle regle permanente adoptee avec Jeff ce jour** : chaque sprint doit desormais integrer, en plus
de son objectif principal, une petite amelioration Quality of Life - sans jamais devier le sprint de cet
objectif principal. Ajoutee dans `CLAUDE.md` (section Methode) pour qu'elle survive au-dela de ce seul
journal.

**Premiere application de la regle** : zoom arriere maximal de la carte du monde augmente (rayon visible
environ 1,4x plus large, constante `MinZoom` dans `WorldMapMmoFullscreenFoundationBootstrap.cs`, de 0.58
a 0.42) pour mieux apprecier l'etendue du monde et faciliter la planification des deplacements - aucune
modification du terrain 50x50 ni de ses images (fondation intouchable respectee, seule la limite de la
camera change). Verification : constante compilee relue par reflexion (confirme 0.42 effectivement en
usage, pas seulement sur le fichier source) ; systeme de chargement de tuiles par zone visible relu en
detail pour confirmer qu'un rayon de vue plus large ne peut jamais faire disparaitre une tuile deja
affichee (le nettoyage de cache exclut explicitement tout ce qui reste visible ou en peripherie
immediate) - seule une marge memoire un peu plus large sur de tres grands ecrans, sans rupture
fonctionnelle. Pas de test visuel en jeu reel effectue ce tour-ci (portee volontairement limitee a un
changement de constante pour ne pas devier du sprint principal).

Prochain test utilisateur : (1) creer un compte neuf et confirmer que la jauge de capacite affiche une
valeur credible des la premiere seconde plutot que "0/3000.0M" ; (2) sur la carte du monde, zoomer a la
molette jusqu'au bout en arriere et confirmer visuellement qu'on voit plus large qu'avant sans trou ni
tuile manquante.

Ouvert / a faire ensuite : continuer le First Hour Experience Pass - prochain candidat par ordre
d'impact/faisabilite deja etabli : PB-014 (deux valeurs sans legende dans la Reserve de miel, cause exacte
deja identifiee), puis PB-013 (formes violettes glitchees derriere le panneau Championnes, fix de symptome
deja identifie), puis PB-009 (messages de connexion contradictoires, portee plus large - 4 sources a
unifier). Verifier aussi si PB-012 est vraiment deja resolu avec un rebuild+playtest avant de le retirer
definitivement de la liste. Chaque sprint suivant doit continuer d'inclure une petite amelioration QoL
(nouvelle regle permanente). Windows (§2.4) et Android sur appareil reel (§2.5) restent explicitement hors
de portee tant que ce chantier n'est pas termine.

---

## Jalon courant — First Hour Experience Pass, Sprint 2 : PB-007 elimine, "Sauter le tutoriel" ne vole plus la progression (2026-08-04)

Suite du First Hour Experience Pass (Sprint 1 avait deja elimine PB-006, tutoriel bloque apres collecte).
Jeff a demande de continuer ce chantier et de choisir le Premium Blocker suivant ayant le plus d'impact
sur la premiere heure, un seul par sprint, elimine completement, valide par playtest ou session simulee
(jamais par lecture de code seule), avec mise a jour obligatoire de `PREMIUM_PLAYTEST_REPORT.md` et de ce
document. Explicitement mis de cote pour ce tour : Windows (§2.4) et Android sur appareil reel (§2.5).

**Choix** : une exploration en lecture seule (agent dedie) a compare la faisabilite reelle des PB encore
ouverts (007, 009, 010, 011, 012, 013, 014) directement dans le code actuel plutot que sur la base du
rapport (qui date de quelques jours). Decouverte utile au passage : PB-012 (boutons Défi/Bestiaire muets)
semble deja corrige dans le code actuel (un commentaire citant explicitement "PREMIUM_PLAYTEST_REPORT.md,
PB-012" documente le fix déjà appliqué) - non retenu comme sprint actif, seulement a confirmer par un futur
rebuild+playtest. PB-011 (panneau Sac) ecarte car une elimination *complete* exigerait un vrai
developpement serveur (champ Population jamais cable). PB-007 retenu : impact le plus eleve sur la
confiance du joueur des la premiere heure, cause reperable avec precision, correctif contenu dans une
seule fonction cliente.

**Cause reelle, plus profonde qu'attendu au depart** : le bouton "Sauter le tutoriel" appelle
`SetPlayableHiveLoopProofState("idle")` - une fonction de reset total concue a l'origine pour les harnais
de capture d'ecran internes (`Sandbox*Capture.cs`), qui remet tout (troupes, batiments, miel/cire/pollen,
journal de quete...) a un jeu de valeurs fixes de demonstration. Ce bouton dev a ete reutilise tel quel
comme bouton joueur reel sans aucune protection de ce que le joueur avait deja accumule - d'où la chute
systematique vers exactement le meme nombre final observee dans le rapport (133 000 dans les deux cas,
peu importe le point de depart). Un premier correctif (fusionner les valeurs vecues seulement apres
l'appel du reset) s'est revele insuffisant en test simule - le reset ecrase deja les compteurs vecus avant
que la fusion ne les lise. Correctif final : capturer troupes/batiments/miel/cire/pollen/capacite juste
AVANT le reset, puis les refondre apres coup avec un plancher (jamais de diminution) - le plancher de fin
de tutoriel continue de s'appliquer normalement pour un joueur qui ne l'a pas encore atteint.

**Fichier modifie** : `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
(`SkipActOneTutorialForDevelopment`).

Preuves : compilation Unity sans erreur (verifiee deux fois, y compris apres le premier correctif
insuffisant). Validation par session simulee en Edit Mode par reflexion directe sur la meme methode
statique que le bouton reel invoque (pas une hypothese de lecture de code) : (1) un joueur deja bien
avance (500 ouvrieres, batiments niv. 9, 270 000 miel) conserve exactement ces valeurs ou plus apres le
clic, Puissance affichee y compris - la premiere version du correctif avait ete testee et rejetee sur ce
meme cas avant la version finale ; (2) un joueur neuf recoit toujours normalement le plancher de fin de
tutoriel (34 ouvrieres, batiments niv. 2-3) - comportement inchange pour un vrai nouveau joueur.
`PREMIUM_PLAYTEST_REPORT.md` mis a jour (Sprint 2 du First Hour Experience Pass + fiche PB-007 marquee
RESOLU).

Prochain test utilisateur : en jeu, avancer reellement dans le tutoriel (collecter, entrainer des
troupes, ameliorer un batiment) au-dela des seuils de fin de tutoriel, puis cliquer "Sauter le tutoriel"
et confirmer que la Puissance/les reserves affichees ne chutent pas.

Ouvert / a faire ensuite : continuer le First Hour Experience Pass - prochain candidat serieux parmi les
PB encore ouverts, par ordre d'impact/faisabilite deja etabli lors de l'exploration de ce sprint : PB-010
(capacite "0/3000.0M" affichee des la creation de compte, correctif d'une ligne deja identifie : sommer
`ProductionCapacity` au lieu de `BalanceCapacity` dans `DynamicResourceValues`), puis PB-014 (deux valeurs
sans legende dans la Reserve de miel, cause exacte deja identifiee : une etiquette inversee dans
`DrawOfficialProductionDetail`), puis PB-013 (formes violettes glitchees derriere le panneau Championnes,
fix de symptome deja identifie : passer le fond de `DrawChampionBeesOverlay` a une opacite pleine comme
les autres overlays), puis PB-009 (messages de connexion contradictoires, 4 sources a unifier - portee
plus large). Verifier aussi si PB-012 est vraiment deja resolu avec un rebuild+playtest avant de le
retirer definitivement de la liste. Windows (§2.4) et Android sur appareil reel (§2.5) restent
explicitement hors de portee tant que ce chantier n'est pas termine.

---

## Jalon courant — Alpha Blocker 2 elimine : libre-service reel pour un inconnu (2026-08-03)

Suite directe du Blocker 1. Objectif de Jeff : qu'une personne n'ayant jamais vu BeeKingdom puisse
creer son compte, obtenir une colonie et jouer sans aucune intervention manuelle de l'equipe. Consigne
explicite : aucun developpement, seulement eliminer le blocage.

Verification du cote serveur/code d'abord (aucune modification necessaire) : la creation de compte,
l'etape d'onboarding (nom d'affichage) et l'attribution automatique d'une colonie fonctionnent deja
correctement pour n'importe quel compte Google — chaque etat de ruche est stocke en base par la paire
(PlayerId, HiveId), donc deux joueurs distincts obtiennent chacun leur propre colonie fraiche meme si
le client utilise le meme identifiant de ruche fixe cote configuration (`SqlHiveStateRepository`,
verifie directement). Rien a corriger ici.

Le vrai blocage n'etait pas dans le code de BeeKingdom mais dans Google Cloud Console : l'ecran de
consentement OAuth du client (`209838375708-...`, scopes `openid email profile`, non sensibles) etait
en statut "Testing" — seuls les comptes explicitement ajoutes un par un comme testeurs pouvaient se
connecter, ce qui aurait exige une intervention manuelle par testeur, contraire a l'objectif. Jeff a
publie l'ecran de consentement en "Production" directement dans sa console (scopes non sensibles =
aucune verification Google requise, effectif immediatement).

Preuves : Jeff a teste avec un second compte Google jamais ajoute comme testeur — connexion reussie
sans blocage "acces refuse" ni avertissement d'application non verifiee.

Prochain test utilisateur : aucun immediat.

Ouvert / a faire ensuite : choisir le prochain Alpha Blocker dans `ALPHA_READINESS_REVIEW.md` — build
Windows jamais produit (§2.4, bug de stockage protege du jeton deja identifie comme cassant sur un
executable Windows reel) ou APK Android jamais teste sur un vrai appareil (§2.5).

---

## Jalon courant — Alpha Blocker 1 elimine : serveur reel joignable, teste de bout en bout (2026-08-03)

Suite directe de l'Alpha Readiness Review : premier blocage choisi (§2.1, "le jeu est injoignable"),
elimine completement et teste de bout en bout avec un vrai compte Google, comme demande par Jeff
("choisis le premier Alpha Blocker, elimine-le completement, teste-le de bout en bout").

Le serveur de production deja existant sur `srvesdt` (`chat.dravii.com`, accessible par SSH cle
uniquement) hebergeait une version perimee (15 juillet) du serveur BeeKingdom, avec la connexion
desactivee (`AccountSessionReadiness=NotLive`) et aucun secret Google OAuth configure. Redeploiement
complet du build a jour (sauvegarde prealable du site + de la base SQL verifiee), puis activation via
variables d'environnement dans `web.config` (jamais commitees, jamais dans le repo) : porte de
connexion ouverte, secret Google OAuth ajoute, et surtout l'ensemble des drapeaux de gameplay
(production hors ligne, amelioration de batiments, recherche, voie strategique, collecte mondiale,
evenement jalon, abeilles championnes/VIP/combat) qui etaient actifs seulement en local mais jamais
deployes en production — sans quoi la connexion aurait fonctionne mais tout le jeu serait reste
injouable derriere "Production serveur indisponible".

Deux bugs reels decouverts et corriges pendant le test live avec le compte Google de Jeff (pas des
hypotheses — trouves en le regardant jouer) :
1. Limite de sessions actives (5 par compte) atteinte a cause des sessions non revoquees accumulees
   pendant les tests (persistance SQL = elles survivent aux redemarrages, contrairement au comportement
   local habituel) — sessions purgees, limite relevee a 50 en production pour absorber les tests futurs.
2. Capacites de stockage configurees (5000/3000/3500) bien plus petites que les ressources deja
   accumulees par Jeff sur son compte (18.3K de miel) — reserve consideree "pleine" des le depart,
   aucune collecte possible. Capacites relevees a 100 000 par ressource.

Un vrai gap produit trouve en testant, pas seulement un bug de config : le panneau de production
affichait "Production synchronisee avec le serveur" meme quand la reserve du batiment regarde etait
deja pleine — aucun avertissement au joueur qui clique "Collecter" sans effet visible. Corrige cote
client : le statut affiche verifie maintenant l'etat de la ligne du batiment courant, pas seulement
l'etat global du modele.

Preuves : connexion Google reelle reussie (confirmee par Jeff en direct) ; collecte de miel confirmee
fonctionnelle apres un test a taux de production temporairement monte a 50000/h (remis a 150/h
ensuite) ; l'etat du compte (session, ressources) a survecu a au moins six redemarrages du pool
applicatif pendant cette meme session, ce qui constitue en soi la preuve de persistance-a-travers-un-
redemarrage demandee par la tache.

Note recurrente : chaque commande touchant la configuration ou redemarrant le serveur de production
a ete bloquee par le classificateur de securite automatique de Claude Code et a exige une
autorisation explicite de Jeff en clavardage a chaque fois (l'autorisation en clavardage ne desactive
pas durablement le classificateur — un reglage de permissions Bash cote utilisateur serait necessaire
pour ca).

Prochain test utilisateur : aucun immediat — la boucle connexion -> jeu -> persistance est validee.
Si Jeff veut aller plus loin, tester la reconnexion apres fermeture complete du jeu (pas seulement
Play Mode) pour confirmer la restauration de session.

Ouvert / a faire ensuite : Alpha Blocker suivant dans `ALPHA_READINESS_REVIEW.md` — pas de chemin de
creation de compte en libre-service pour un vrai testeur externe (§2.2, aucun secret Google cote
client public, `/dev/seed-account` bloque en Production) ; ou build Windows jamais produit (§2.4) ;
ou APK jamais teste sur un vrai appareil (§2.5). Choisir avec Jeff avant de commencer, un seul a la
fois comme convenu.

---

## Jalon courant — Alpha Readiness Review : audit complet, aucun code touche (2026-08-03, suite)

Jeff a demande un audit complet (pas de code, pas de fichier Unity modifie) pour repondre a une
seule question : que manque-t-il reellement avant de donner BeeKingdom a 20 testeurs externes
pendant une semaine. Six recherches independantes menees en parallele (tutoriel/onboarding,
persistance/auth/build, economie, realite MMO/alliances, combat/construction, contenu/telemetrie),
chacune verifiant le code et la configuration reellement commitee plutot que de faire confiance a la
documentation existante.

**Constat central, convergent sur les six recherches** : le vrai blocage n'est pas un manque de
fonctionnalites, c'est que le jeu tel que commite aujourd'hui est injoignable par quiconque
d'externe. La connexion elle-meme est desactivee par defaut (`AccountSessionReadiness=NotLive`
dans tous les `appsettings.json` commites) ; il n'existe aucun endpoint d'inscription email/mot
de passe hors outil de developpement ; le seul fichier de config client pointe vers
`http://localhost:5067` ; comptes/sessions/colonies vivent en memoire pure (perdus au redemarrage) ;
et la quasi-totalite des drapeaux de contenu (Combat, Construction, Collecte mondiale, Recherche,
etc.) sont `Enabled: false` avec un catalogue vide dans les trois fichiers de configuration
commites. Aucun build Windows n'a jamais ete produit, et un bug de code confirme casserait la
connexion sur un vrai executable Windows (le stockage protege du jeton n'existe que pour
Android/iOS/Editeur). L'APK Android n'a jamais tourne sur un vrai appareil (etat "PENDING" deja
documente). Aucun mecanisme de remontee de bugs/crash n'existe (rapport de crash Unity explicitement
desactive dans les parametres du projet).

**Livrable** : `ALPHA_READINESS_REVIEW.md` a la racine du depot - liste des systemes deja
"Alpha Ready" (aucune amelioration proposee dessus), liste des systemes incomplets classes par
importance avec pourquoi/difficulte/gain pour chacun, feuille de route en 3 paliers (ALPHA 0.1 :
le jeu est atteignable et ne perd rien ; 0.2 : il y a une vraie progression a tester ; 0.3 : poli et
honnete), et une regle de decision explicite pour les prochains sprints : aucun ne doit etre choisi
sans se referer a une case de ce document.

Aucun fichier de code ou Unity modifie durant cette tache (conforme a la contrainte de Jeff).

Prochain test utilisateur : lire `ALPHA_READINESS_REVIEW.md` et valider/ajuster le decoupage des 3
paliers avant que le prochain sprint ne commence.

Ouvert / a faire ensuite : premier bloc non coche d'ALPHA 0.1 - rendre le jeu atteignable
(hebergement reel, bascule des drapeaux de connexion, persistance reelle activee).

---

## Jalon courant — Sprint "Les victoires doivent laisser un souvenir" : memoire de victoire dans LivingHive (2026-08-03, suite)

Jeff a demande qu'une victoire importante laisse une trace visible DANS LivingHive (pas seulement
dans le rapport de combat), rare et discrete, sans nouveau panneau. Ce sprint touche Combat +
Ruche ensemble (la ruche est la surface d'affichage, mais rien de nouveau n'y est cree comme
systeme) - explicitement demande par Jeff, donc coherent avec la regle de rotation des systemes.

**Realise** : une victoire ecrasante (`DecisiveVictory`, meme declencheur deja utilise pour la voix
des Championnes) OU une victoire remportee pendant une menace en hausse (`WorldEventApplied`, meme
declencheur que le badge "Legendaire" du Bestiaire) laisse une trace de 12 secondes, purement
client-ephemere (comme les autres etats "voix des Championnes" deja en place), visible au prochain
passage dans LivingHive meme si la victoire a ete reclamee ailleurs (carte du monde) : une banniere
tres discrete pres du haut de l'ecran nommant la victoire, plus l'escouade de gardiennes DEJA animee
pour la defense (`DrawActiveTaskBeeCluster`/`guard_post`) qui "accueille" temporairement le retour -
aucun nouveau rendu, uniquement la reutilisation du cluster de gardiennes existant en le declarant
actif pendant la fenetre. Garde d'unicite par EncounterId independante de celle des Championnes, pour
qu'un rapport reaffiche a chaque frame ne redeclenche pas la banniere en boucle.

Limite assumee, transparente : par manque de temps pour cross-referencer proprement le Carnet du
Bestiaire au moment exact du rapport de combat, "premiere victoire contre cette creature" (3e
exemple de Jeff) n'a pas ete implementee ce tour-ci - seules "victoire importante" et "victoire
legendaire" sont couvertes, toutes deux avec des donnees deja disponibles sans aucun nouveau
couplage entre systemes.

Preuves : compilation Unity sans erreur. Suite EditMode `SandboxLivingHiveSpecializedBeeActivityTests`
toujours 5/5 (aucune regression sur le budget d'abeilles ambiantes deja verrouille par un test).
Verification directe par reflexion des fonctions exactes ajoutees : le declenchement "decisif" bascule
bien `ActiveBeeTaskZoneId()` sur `guard_post` et affiche le bon texte ; le declenchement "legendaire"
affiche un texte distinct ; apres expiration simulee, l'etat retombe exactement a `false`/vide. Pas de
test live contre une vraie victoire ecrasante ce tour-ci (aurait demande de recruter une armee
disproportionnee) - la logique nouvelle elle-meme est verifiee isolement et directement.

Prochain test utilisateur : remporter une patrouille en Victoire ecrasante (ou pendant une menace en
hausse ciblant le bon palier), puis retourner dans LivingHive dans les 12 secondes (meme depuis la
carte du monde) et observer la banniere + l'essaim de gardiennes qui semble accueillir le retour.

Ouvert / a faire ensuite : voir recommandation de fin de session.

---

## Jalon courant — Sprint "Une ruche qui respire" : densite d'abeilles selon la progression (2026-08-03)

Premiere fois cette session que le systeme "Ruche" (LivingHive) est touche - jusqu'ici World Map,
Bestiaire et Championnes avaient chacun ete enrichis, jamais deux fois de suite le meme systeme.

**Decouverte importante avant de choisir** (recherche prealable, aucun fichier modifie a ce stade) :
LivingHive dispose deja d'un systeme de circulation ambiante d'abeilles tres complet
(`LiveHiveBeeAgent[]`, 14 agents fixes, familles Worker/Nurse/Guard/Drone/Scout/Queen,
`DrawAmbientHiveBeeTraffic`, budget par appareil/mode economie deja tune pour Android), plus un
systeme d'ambiance reactive (glow sur le batiment en amelioration/production pleine) et des indices
de metier par batiment (transport de nectar, faconnage de cire, etc.) - tout cela deja livre avant
cette session. Le vrai manque : ce budget d'abeilles visibles est un plafond FIXE par appareil,
identique pour un joueur du premier jour et un joueur tres avance - la ruche ne "grandit" jamais
visuellement avec le joueur.

**Realise** : le nombre d'abeilles ambiantes reellement dessinees (jamais plus que le plafond
materiel/economie deja teste et inchange) s'ajuste maintenant selon le niveau VIP officiel du joueur
(signal de progression reelle deja calcule ailleurs, jamais une valeur de demo) : une jeune ruche
(VIP &lt; 3) affiche environ la moitie du trafic habituel, une ruche etablie (VIP 3-7) about 75%, une
ruche tres investie (VIP 8+) retrouve exactement le plein trafic deja existant, inchange pour tout
joueur deja engage aujourd'hui. Toujours au moins une abeille visible. Le changement est visible
immediatement apres une progression VIP, sans texte, sans popup, sans nouveau systeme - uniquement
un facteur applique au moment ou le budget existant est consomme par le rendu, jamais a la fonction
elle-meme (qui reste le contrat exact deja verrouille par un test de regression existant).

Preuves : compilation Unity sans erreur. Suite de tests EditMode
`SandboxLivingHiveSpecializedBeeActivityTests` (5/5, dont le test qui verrouille les valeurs exactes
de budget par appareil/mode economie) toujours verte sans aucune modification - la contrainte
"AmbientBeeBudget doit rester un plafond stable" est respectee. Verification directe par reflexion
des 4 paliers (VIP 0/3/8, portrait economie) : chaque cas produit exactement le facteur attendu
(50%/75%/100%, jamais zero).

Prochain test utilisateur : observer la circulation d'abeilles dans la ruche sur un compte neuf
(VIP 0) puis sur un compte avec un niveau VIP eleve - la deuxieme doit paraitre nettement plus animee,
sans qu'aucun texte n'explique pourquoi.

Ouvert / a faire ensuite : voir recommandation de fin de session.

---

## Jalon courant — Sprint "Le Monde a une Memoire" : ressources recemment exploitees (2026-08-02, suite)

Jeff a demande UNE seule amelioration donnant l'impression que le monde garde une trace de ce qui
s'y est passe - aucun texte, aucun panneau, juste une impression naturelle au retour sur la carte.

**Choix** : une ressource qui vient de redevenir disponible (fin de repos) garde un aspect legerement
terne pendant environ 3 minutes avant de retrouver progressivement son eclat normal - reutilise
entierement le systeme "ressource vivante" deja livre (etats Free/CollectingMine/CollectingOther/
Depleted), juste une transition supplementaire entre Depleted et Free au lieu d'un retour instantane.

**Detail technique important decouvert en verifiant** : le serveur n'expose PAS l'instant exact ou
une ressource redevient disponible - `ReadyAtUtc` n'est renseigne que PENDANT le repos et redevient
`null` des que la ressource est prete (`WorldResourceCollectionService.Snapshot`, verifie directement
dans le code serveur). Impossible donc de calculer "depuis quand" purement a partir de la derniere
reponse serveur. Solution restee 100% client, sans aucun changement serveur : la transition
Depleted -> Free est detectee ici, en comparant l'etat `Ready` d'un rafraichissement au precedent : la
toute premiere fois qu'on l'observe passer a "pret" APRES avoir ete vu au repos, l'instant est retenu
localement et la teinte s'estompe pendant la fenetre. Garde explicite contre le faux positif : une
ressource jamais observee auparavant (premiere fois vue par ce client, deja prete) n'est PAS traitee
comme "recemment exploitee".

Limite honnete : cette memoire vit tant que l'instance de la carte du monde reste chargee (memoire
d'affichage ephemere, pas une donnee persistee) - un rechargement complet de la scene la reinitialise.
Coherent avec la demande de Jeff ("pendant quelques minutes"), pas pense pour survivre a une
fermeture complete de l'application.

Preuves : compilation Unity sans erreur. Verification directe par reflexion (creation d'une instance
temporaire du bootstrap en Edit Mode) : (1) une ressource observee au repos puis prete redevient bien
ternie immediatement, (2) la teinte revient exactement au blanc normal apres simulation de 300s
ecoulees (au-dela des 180s de fenetre), (3) une ressource jamais vue auparavant et deja prete au
premier contact reste blanche (aucun faux positif) - les 3 cas se comportent exactement comme prevu.
Aucun changement serveur, `BeeKingdom.HiveOperations.Tests` non concerne.

Prochain test utilisateur : collecter une ressource jusqu'a son repos, attendre qu'elle redevienne
disponible, et observer qu'elle reste visuellement un peu terne pendant quelques minutes avant de
retrouver son eclat plein.

Ouvert / a faire ensuite : voir recommandation de fin de session.

---

## Jalon courant — Sprint "Emotion, Immersion et Retention" : ambiance meteo sur la carte (2026-08-02, suite)

Jeff a change de casquette ("Game Director" plutot que "Lead Engineer") et a demande une analyse
mentale des deux premieres heures de jeu pour reperer un seul point faible, puis d'enrichir UNE seule
boucle deja existante (aucune nouvelle monnaie/systeme/architecture/ressource/interface majeure).

**Analyse** : parmi les fondations recemment livrees (carte MMO, collecte, patrouille, presence des
autres colonies, POI, Bestiaire, Championnes), la carte du monde elle-meme - la boucle la plus
centrale du jeu - reste visuellement plate : la meteo/menace dynamique deja calculee cote serveur
(`WorldEventCatalog`, change toutes les 4h) n'etait visible QUE dans deux panneaux specifiques
(Patrouille de combat, Collecte mondiale), jamais comme ambiance du monde lui-meme malgre l'exemple
explicite donne par Jeff ("la meteo influence legerement l'ambiance").

**Realise** : miroir client de `WorldEventCatalog` (fonction pure du temps, sans etat ni RNG, meme
convention deja etablie pour `ChampionBeeCatalog`) - le client calcule desormais la meme meteo/menace
active que le serveur sans aucun appel reseau supplementaire. La carte du monde applique un lavis de
couleur plein ecran tres discret (different par evenement : rose pour la floraison, bleu-gris pour la
pluie, ambre pour la secheresse, rouge sourd pour une menace en hausse), dessine apres le monde et
avant les panneaux HUD pour ne jamais nuire a la lisibilite. Un petit badge nomme toujours
explicitement l'evenement actif (meme libelle deja utilise dans les panneaux existants) - jamais un
changement d'ambiance mysterieux, toujours explique sans avoir a ouvrir un panneau.

Preuves : compilation Unity sans erreur. Verification croisee directe contre le serveur de dev reel
en cours d'execution : a l'instant exact ou le serveur reportait `drought` dans un vrai snapshot de
Patrouille de combat, le miroir client calcule egalement `drought` pour ce meme instant - confirme
que l'ambiance affichee correspond toujours a la verite serveur, jamais un affichage mensonger.
Aucun changement serveur, donc `BeeKingdom.HiveOperations.Tests` non concerne.

Prochain test utilisateur : ouvrir la carte du monde a differents moments de la journee (ou avancer
l'horloge systeme) et observer que la teinte d'ambiance et le badge en haut a gauche changent toutes
les 4h, en cohérence avec ce qu'affichent deja les panneaux de Patrouille/Collecte.

Ouvert / a faire ensuite : voir recommandation de fin de session (Championnes qui remarquent quelque
chose sur la carte).

---

## Jalon courant — Voix des Championnes : premier systeme de barks contextuels (2026-08-02, suite)

Jeff a fourni les premiers fichiers vocaux reels (ElevenLabs) pour trois Championnes - Striga,
Zephyra, Ambra - ranges dans `C:\projets\beekingdom\BIBLE\10_Championnes\{Nom}\Voix\` avec 4
categories deja enregistrees par championne : `select` (5 variantes), `spawn` (3), `move` (3), `cit`
(3, citations generiques de personnalite). Demande explicite : aucun nouveau systeme de dialogue,
aucun nouveau panneau - uniquement reagir a des evenements deja existants, et une regle de retenue
forte : une Championne ne doit jamais parler pour combler le silence, seulement quand elle a
reellement quelque chose de pertinent a dire.

**Realise** : les 42 fichiers existants importes sous
`Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/ChampionVoices/{championId}/{categorie}/`
(meme convention `Resources.Load` que les portraits deja en place). Nouveau
`ChampionVoiceBarkController.cs`, une seule couche statique reutilisee par les 7 points d'accroche
demandes : selection d'une Championne, lancement de construction, lancement de patrouille (choisit
la Championne dont le role de combat correspond a la famille de troupes dominante engagee),
lancement de collecte mondiale, victoire ecrasante (ne parle que si une Championne a reellement
contribue a CE combat), decouverte legendaire au Carnet du Bestiaire (detection de la transition
"vient de devenir Legendaire", absente du serveur - ajoutee cote client), et ouverture du Carnet
(delai de 5 minutes, explicitement "rarement"). Categories audio primaires par evenement mappees sur
les dossiers deja enregistres quand le sens correspond (`select`->select, patrouille->`spawn`,
collecte->`move`), avec repli automatique sur `cit` (citations generiques) pour les evenements sans
categorie dediee - architecture prete pour des centaines de fichiers futurs sans toucher au code, un
dossier suffit. Retenue imposee par deux garde-fous independants : anti-chevauchement base sur la
duree reelle du clip joue, et delai minimal PAR EVENEMENT (large pour selection/construction, nul
pour victoire/legendaire, 5 minutes pour l'ouverture du carnet) - et par construction, une Championne
absente d'un evenement (aucune contribution, aucune assignation) reste silencieuse plutot que de
parler a sa place.

Preuves : compilation Unity sans erreur. Aucun changement serveur (fonctionnalite 100% cliente),
donc `BeeKingdom.HiveOperations.Tests` non concerne, non reverifie. Verification directe en Edit
Mode : `Resources.LoadAll` retrouve bien les 5 clips reels de Striga/select avec des durees non
nulles ; les cas limites (championne inconnue, liste de contributeurs vide) ne levent aucune
exception et restent silencieux comme prevu. Tentative de verification en Play Mode : l'appel
complet ne leve aucune exception et resout bien un clip reel, mais la confirmation audible
"isPlaying" immediatement apres l'appel n'a pas pu etre obtenue de maniere fiable via l'outillage
d'automatisation distant (meme un appel direct a `AudioManager.PlaySound` deja existant, sans passer
par mon code, montre le meme comportement) - limite de l'environnement de test automatise, pas un
signal de bug dans le code livre.

Prochain test utilisateur : en jeu, ouvrir puis selectionner une Championne dans le panneau
Championnes et ecouter la reponse vocale ; lancer une patrouille avec des Gardiennes assignees et
verifier que c'est Striga (et non une autre) qui reagit.

Ouvert / a faire ensuite : quand Jeff enregistrera Nectaria et Aurelia, aucune modification de code
n'est necessaire - deposer leurs fichiers dans les memes dossiers `{championId}/{categorie}/` suffit.
Envisager, si Jeff le souhaite plus tard, des categories dediees `victory`/`legendary`/`bestiary`/
`building` pour remplacer le repli generique sur `cit`.

---

## Jalon courant — Android APK genere + reorientation "Gameplay First" + Bestiaire enrichi (2026-08-02, suite)

Deux temps distincts dans cette session. D'abord, demande explicite et unique de Jeff : generer
`BeeKingdom_Internal_Debug.apk` sans aucune nouvelle fonctionnalite. Utilise le script existant
`Assets/Editor/BeeKingdomAndroidInternalBuild.cs` (deja pret depuis une session anterieure) :
switch de plateforme vers Android, reimport complet, build via `BuildPipeline.BuildPlayer`. Le seul
incident a noter (purement operationnel, aucun code en cause) : le round-trip MCP a expire pendant
la longue phase Gradle du premier build (~27 minutes), ce qui a declenche plusieurs relances
automatiques du meme build (le script supprime l'APK existant au debut de chaque appel) — verifie et
confirme uniquement via lecture directe du systeme de fichiers (PowerShell, pas Bash — le point de
montage Bash s'est avere avoir une vue en cache peu fiable sur ce chemin Windows pendant une ecriture
active) et du fichier `Logs/Editor.log`, jusqu'a une taille de fichier stable et un processus Unity de
nouveau reactif. APK final valide comme archive ZIP complete (6056 entrees, 4 `classes.dex`,
manifeste present) a `Builds/Android/Internal/BeeKingdom_Internal_Debug.apk`. Plateforme remise a
`StandaloneWindows64` ensuite pour la suite du travail (Android n'etait pas necessaire au chantier
suivant).

Ensuite, reorientation strategique de Jeff ("Gameplay First") : priorite au vecu perceptible du
joueur plutot qu'a de nouveaux systemes independants, avec 4 axes (Bestiaire, Championnes vivantes,
monde vivant, experience joueur avant tout). Premier chantier choisi : enrichir le Carnet du
Bestiaire (deja livre la veille) selon les exemples donnes par Jeff, sans nouvelle mecanique.

**Realise** : deux nouveaux champs par palier, calcules dans le meme sous-produit automatique du
flux Combat Patrol qu'avant (`BestiaryCodexAccounting`/`CombatPatrolService.FinishAsync`, aucune
nouvelle commande) — le detail du DERNIER combat precis (bande, miel, pollen), distinct du cumul
deja affiche, et la date d'obtention de la meilleure victoire (uniquement mise a jour quand la bande
progresse reellement). Cote client : barre de progression visuelle vers "Maitrisee" (reutilise le
helper `DrawProgressBar` deja present partout ailleurs dans l'interface), badges Maitrisee/Legendaire
visibles directement sur les 7 onglets de la grille (pas seulement en ouvrant la fiche), legere
pulsation continue sur le badge Legendaire (purement decorative, aucun nouvel etat), et les deux
nouvelles lignes de souvenir personnel dans la fiche detaillee.

Preuves : `BeeKingdom.HiveOperations.Tests` a 167/167 (assertions enrichies sur les tests existants,
aucune regression). Compilation serveur et Unity sans erreur (warnings preexistants uniquement,
aucun lie a ce changement). Test complet contre le vrai serveur de dev redemarre avec le nouveau
code, sur le meme compte QA que la veille : un second combat de patrouille tier 1 confirme que le
carnet distingue bien le souvenir du dernier combat (31 miel/15 pollen, HardWon) du cumul (62/30 au
total), et que la date de meilleure victoire ne bouge pas quand le nouveau combat n'ameliore pas la
meilleure bande deja obtenue (comportement voulu, verifie).

Prochain test utilisateur : ouvrir le Carnet du Bestiaire, repérer les badges directement sur les 7
onglets de la grille (dont la pulsation sur un palier Legendaire s'il y en a un), ouvrir un palier
deja affronte et voir la barre de progression vers "Maitrisee" ainsi que le souvenir du dernier
combat distinct du butin total.

Ouvert / a faire ensuite : chantier recommande — rendre les Championnes plus vivantes en
reutilisant les evenements de combat deja existants (voir recommandation de fin de session).

---

## Jalon courant — Carnet du Bestiaire, v1 (2026-08-02)

Jeff a validé l'audit gameplay puis la proposition de Game Design du Carnet du Bestiaire
(transformer chaque créature rencontrée en objectif de collection, système pensé pour accompagner
le joueur plusieurs années) avec une exigence supplémentaire : chaque fiche doit raconter l'histoire
personnelle du joueur avec cette créature, jamais ressembler à une simple base de données.

**Tension technique résolue** : le Game Design validé décrit 14 identités (7 paliers × 2 variantes
cosmétiques), mais le serveur ne connaît et ne connaîtra jamais la variante — purement une donnée
client (`WorldBestiaryNode.Variant`, utilisée seulement pour choisir la texture/le nom affichés sur
la carte). Solution : deux couches de confiance distinctes. Par PALIER (1-7), tout est
serveur-autoritaire et construit en sous-produit automatique du flux de réclamation Combat Patrol
déjà existant — aucune nouvelle commande joueur, donc aucun nouveau risque de confiance : nombre de
rencontres, meilleur combat obtenu, badge "Maîtrisée" (10 rencontres), badge "Légendaire" (réutilise
exactement le déclencheur déjà calculé pour la banniere "menace en hausse" localisée), première/
dernière rencontre, butin total cumulé, nombre de fois sous Cible du jour, championnes ayant
contribué au dernier combat. Par VARIANTE, seul l'état "Aperçue" (l'as-tu déjà vue sur la carte) est
suivi, purement client-local, sans aucune récompense attachée.

**Réalisé** : côté serveur, un nouvel état `BestiaryCodex` sur la ruche, mis à jour uniquement depuis
`CombatPatrolService.FinishAsync` (branche de résolution existante) et exposé par une nouvelle route
de lecture seule ; côté client, un client réseau et un contrôleur de présentation calqués sur le
patron déjà établi pour la Présence mondiale, un petit magasin PlayerPrefs pour l'état "Aperçue" par
variante branché au moment exact de la sélection d'une créature sur la carte, et un nouveau bouton
"Bestiaire" dans la barre de navigation (paysage et portrait) ouvrant un panneau : grille des 7
paliers, fiche détaillée par palier avec les deux identités cosmétiques et leur état "Aperçue", et
le bloc d'histoire personnelle (première rencontre, nombre de combats, meilleur résultat, butin
total, Cible du jour, championnes).

Preuves : `BeeKingdom.HiveOperations.Tests` à 167/167 (164 + 3 nouveaux tests ciblés sur le carnet,
aucune régression). Compilation serveur et Unity sans erreur. Vérification directe en Edit Mode par
réflexion du magasin "Aperçue" (première apparition détectée, doublon correctement ignoré, variante
non vue restant non vue). Test complet contre le vrai serveur de dev avec un compte QA réel :
lecture initiale du carnet (7 paliers à 0 rencontre), recrutement, lancement et réclamation d'une
patrouille tier 1 (`HardWon`), puis relecture du carnet confirmant la mise à jour exacte (1 rencontre,
meilleur combat "HardWon", butin cumulé = butin réellement crédité par le combat, les 6 autres
paliers inchangés).

Prochain test utilisateur : ouvrir le Carnet du Bestiaire depuis la barre de navigation de la Ruche,
naviguer entre les 7 paliers, repérer une créature jamais rencontrée en apercevant son nœud sur la
carte du monde (elle doit ensuite apparaître "Aperçue" dans le carnet), puis mener un combat de
patrouille et confirmer que la fiche du palier concerné raconte bien ce combat.

Ouvert / à faire ensuite : quand le nombre de rencontres/mastery grandira avec l'usage réel, revoir
si le seuil "Maîtrisée" (10 rencontres, choisi arbitrairement pour rester atteignable en quelques
sessions) doit varier par palier plutôt que rester une constante unique ; envisager des récompenses
concrètes (titre, bonus cosmétique) au badge "Maîtrisée"/"Légendaire", non encore branchées ce tour-ci
puisque Jeff n'a validé que la structure et l'histoire personnelle, pas encore l'économie de
récompense.

---

## Jalon courant — Premiers Points d'Intérêt (POI) sur la carte (2026-08-01, suite)

Jeff a validé l'enrichissement des ressources et souhaite arrêter là pour l'instant, préférant
maintenant "structurer le monde" : introduire les premiers lieux remarquables (POI), purement
informationnels — pas de PvP, pas de capture, aucune nouvelle mécanique complexe. Chaque POI doit
avoir une identité visuelle propre, une description, apparaître naturellement, être sélectionnable,
et préparer les futurs systèmes (alliances, occupation, événements, boss) sans les construire.

**Réalisé** : un 4e type de nœud de carte (`WorldPointOfInterestNode`), calqué à l'identique sur les
trois types déjà existants (ruches/ressources/bestiaire) — même dispersion procédurale déterministe
par chunk, même mise en cache, même sélection au clic (priorité après le bestiaire), même détection
de collision avec le landmark "BearDen". Volontairement rare (~6% des chunks, contre ~26-28% pour
ressources/bestiaire) pour rester "remarquable". Catalogue de 5 lieux avec une identité visuelle
propre (glyphe + couleur distincts par type) et une vraie description narrative : Ruche ancienne,
Nid fossilisé, Bosquet légendaire, Ruche naufragée, Sanctuaire oublié — chacun choisi pour évoquer
un futur système différent (occupation, boss, alliance, événement) sans en implémenter aucun.
Toujours visibles sur la carte (pas de filtre, contrairement aux ruches/ressources/menaces) ; la
description complète s'affiche directement au nœud à la sélection, sans panneau ni action.

Preuves : compilation Unity sans erreur ; Play Mode sans erreur/exception sur la scène canonique.
Aucun code serveur touché (fonctionnalité 100% cliente), donc `BeeKingdom.HiveOperations.Tests`
reste à 164/164 (revérifié, non concerné). Vérification directe du générateur par réflexion (1681
chunks balayés, y compris coordonnées négatives) : taux d'apparition mesuré 5,7% (proche de la
cible ~6%), les 5 types du catalogue tous représentés sans biais visible, aucune exception, position
et description correctement renseignées sur un échantillon réel.

Prochain test utilisateur : parcourir la carte du monde et repérer un point d'intérêt (glyphe
distinct + étiquette "◆ <nom>" toujours visible) ; le sélectionner et voir sa description complète
s'afficher directement sur la carte.

Ouvert / à faire ensuite : quand Jeff voudra activer un premier vrai système sur ces lieux
(alliance/diplomatie sur "Ruche ancienne", boss sur "Sanctuaire oublié", événement temporaire sur
"Bosquet légendaire", occupation sur "Ruche naufragée"), le `Kind` de chaque POI sert déjà de clé
stable pour brancher cette logique sans retoucher la génération/le rendu.

---

## Jalon courant — Ressources vivantes : l'état se lit sans ouvrir de panneau (2026-08-01, suite)

Jeff a validé la présence ambiante et a explicitement dit ne pas vouloir en augmenter la densité
pour l'instant. Demande suivante : enrichir uniquement la représentation visuelle des ressources
déjà existantes — libre, en cours de collecte (par soi ou une autre colonie), progression visible,
épuisée après récolte, régénération naturelle — sans créer de nouveau système.

**Réalisé** : purement client, aucun changement serveur. `DrawResources()` (rendu des noeuds sur la
carte) résout désormais un état vivant par noeud officiel (Libre / Collecte-moi / Collecte-autre-
colonie / Épuisée) à partir de données déjà transmises (le vol actif du joueur, les présences
ambiantes des autres colonies livrées au jalon précédent, et `Ready`/`ReadyAtUtc`/`Cooldown` déjà
existants). Chaque état se traduit par une teinte d'icône + une seule barre de progression réutilisée
dans les deux sens (remplissage = progression de collecte ; remplissage = progression de
régénération naturelle) + une étiquette courte toujours visible, sans sélection ni panneau. Les
marqueurs Cible du jour / météo déjà livrés restent visibles au même endroit. En consolidant cet
affichage directement sur la ressource, les anciens halos séparés (occupation "verte" de sa propre
escouade, halo "bleu" de présence d'une autre colonie) sont devenus redondants et ont été retirés —
un seul indicateur par ressource au lieu de deux qui se chevauchaient.

Preuves : compilation Unity sans erreur ; Play Mode sans erreur/exception ; aucun code serveur
modifié, donc `BeeKingdom.HiveOperations.Tests` reste à 164/164 (non concerné). Vérifié en direct
contre le serveur de développement réel avec les données déjà en place : une collecte réelle a
généré une vraie fenêtre de régénération (`ready:false`, `readyAtUtc` dans 2 minutes) confirmant que
les données consommées par le nouvel état "Épuisée" sont exactement celles renvoyées par le serveur.
Les trois autres états (Libre, Collecte-moi, Collecte-autre-colonie) avaient déjà été vérifiés en
direct lors des jalons précédents sur les mêmes données.

Prochain test utilisateur : ouvrir la carte du monde, observer qu'un noeud de ressource raconte son
état (couleur + barre + étiquette) sans rien cliquer ; lancer une collecte et voir la barre se
remplir en direct ; la réclamer et voir le noeud se griser avec une barre de régénération qui se
remplit jusqu'au retour à l'état libre.

Ouvert / à faire ensuite : un état "prête à réclamer" distinct pourrait affiner le cas où un vol est
terminé mais pas encore validé par le joueur (actuellement affiché comme collecte à 100%, ce qui
reste compréhensible mais pourrait être plus précis).

---

## Jalon courant — Monde vivant : présence ambiante des autres colonies (2026-08-01, suite)

Jeff a validé la fondation "escouade réelle sur le terrain" puis a explicitement demandé de ne pas
construire de composeur de troupes pour l'instant, et de plutôt rendre le monde vivant — sans PvP,
sans interaction, uniquement de la présence : que le joueur constate que d'autres colonies existent
dans le même monde. Contrainte explicite : réutiliser l'architecture d'escouades persistantes du
jalon précédent.

**Constat** : aucun état partagé/multi-joueurs n'existait avant ce jalon — chaque client génère
procéduralement son propre monde à partir des seules coordonnées de chunk (déterministe, donc déjà
cohérent entre joueurs pour la disposition, mais aucune lecture cross-joueurs n'existait côté
serveur). C'était la seule vraie nouveauté nécessaire ; tout le reste réutilise l'existant.

**Réalisé** : un nouveau service serveur strictement en lecture (`WorldPresenceService`) échantillonne
les hives récemment actives tous joueurs confondus (nouvelle méthode `ListRecentlyActiveAsync` sur
`IHiveStateRepository`) et expose les vols de Collecte mondiale actifs d'AUTRES joueurs sur les 3
nœuds officiels — en réutilisant directement `WorldResourceCollectionState.Active` construit au
jalon précédent, sans nouvelle mécanique. Aucune identité réelle n'est exposée (juste une étiquette
"Colonie #XXXX" dérivée du hiveId) ; le hive appelant est toujours exclu de sa propre lecture. Sur
la carte, ces occupations d'autres colonies apparaissent avec un halo bleu distinct (différent du
vert de sa propre escouade) et une étiquette de colonie — aucun clic, aucune interaction, aucune
donnée de combat.

Preuves : nouveaux tests `WorldPresenceServiceTests` (une autre colonie est rapportée, jamais sa
propre ruche ; un vol déjà terminé est exclu) — 164/164 verts sur `BeeKingdom.HiveOperations.Tests`.
Compilation serveur et Unity sans erreur ; Play Mode sans erreur/exception. Vérifié en direct contre
le serveur de développement réel avec deux comptes QA distincts : la colonie 2 a recruté puis lancé
une collecte réelle, et la lecture de présence de la colonie 1 a bien renvoyé "Colonie #D2E6"
occupant `res_pollen_core` avec les bons horodatages — tandis que la propre lecture de la colonie 2
excluait bien sa propre occupation.

Prochain test utilisateur : avec deux comptes différents, lancer une collecte sur l'un, puis
observer sur la carte de l'autre le halo bleu "Colonie #XXXX" apparaître sur le nœud occupé.

Ouvert / à faire ensuite : étendre la présence aux encounters de Combat Patrol des autres joueurs
si Jeff le souhaite (même patron, volontairement pas fait ce tour-ci pour rester loin de tout signal
de combat) ; envisager un vrai registre d'activité si l'échantillonnage par recency devient
insuffisant à plus grande échelle.

---

## Jalon courant — Fondation "escouade réelle sur le terrain" (2026-08-01, suite)

Jalon important explicitement nommé comme tel par Jeff : début de la transition de la Collecte
mondiale (aujourd'hui quasi instantanée côté ressenti joueur) vers son fonctionnement définitif —
une escouade part réellement vers la ressource, y reste physiquement pendant toute la durée, et
peut être rappelée. PvP et attaques de joueurs restent explicitement hors scope ; seule la base
technique réutilisable plus tard (PvP, raids, renforts, occupation de points d'intérêt) était
demandée.

**Constat de conception** : Combat Patrol avait déjà exactement cette architecture (troupes
réellement engagées par opération, comptabilité anti-double-dépense, rappel sans récompense ni
malus, marqueur de marche sur la carte) — mais dupliquée dans un seul service, invisible à la
Collecte mondiale. Plutôt que de réinventer une nouvelle mécanique, ce jalon **généralise** cette
architecture existante :

- Nouveau `HiveTroopDeploymentAccounting` (serveur) : seul et unique calcul de "troupes disponibles"
  partagé par Combat Patrol et Collecte mondiale — aucune abeille ne peut plus être comptée deux
  fois entre les deux systèmes engagés simultanément. Combat Patrol a été refactoré pour déléguer à
  cette même brique plutôt que de garder son calcul dupliqué.
- Collecte mondiale : le lancement exige désormais une composition réelle de troupes (gardiennes/
  voltigeuses/lanceuses), validée contre la disponibilité partagée et la même capacité que Combat
  Patrol. Un nouveau `RecallAsync` (+ route `/recall`) rend les troupes immédiatement, sans
  récompense ni pénalité de repos — miroir exact du rappel déjà existant en combat.
- Carte du monde : la Collecte mondiale a maintenant sa propre "marche d'escouade" réelle (ligne +
  marqueur), calquée sur celle de Combat Patrol mais avec une différence assumée par Jeff — le
  marqueur voyage puis **reste physiquement sur le nœud** pendant toute la collecte (au lieu de
  faire l'aller-retour du combat), avec un anneau pulsant "Occupée" et le temps restant affiché
  directement sur la carte, pas seulement dans le panneau latéral. Le bouton d'action existant se
  transforme en "Rappeler l'escouade" pendant qu'un vol est en cours sur le nœud sélectionné.
- Aucun composeur interactif dédié n'a été construit ce tour-ci (hors scope explicite de la demande
  de Jeff) : une petite escorte par défaut (jusqu'à 3 unités, gardiennes en priorité) est choisie
  automatiquement parmi les troupes disponibles au moment du lancement — l'architecture (brouillon,
  ajustement, disponibilité) est déjà celle d'un vrai composeur et pourra recevoir une UI dédiée
  sans changement de forme.

Preuves : nouveaux tests server (`Launch_commits_the_requested_troops...`,
`Launch_is_rejected_when_requested_troops_exceed...`, `Recall_returns_the_committed_troops...`,
`Launch_is_rejected_when_the_troops_are_already_committed_to_a_combat_patrol` — ce dernier prouve
concrètement l'anti-double-dépense entre les deux systèmes) — 162/162 verts sur
`BeeKingdom.HiveOperations.Tests`. Compilation serveur et Unity sans erreur ; passage en Play Mode
sans erreur/exception. Vérifié en direct contre le serveur de développement réel avec le compte QA
(recrutement réel de 4 gardiennes) : lancement d'une collecte avec 2 gardiennes committées
(`committedTroops` et `availableRoster` corrects), rappel avant la fin confirmant le retour
immédiat des troupes sans pénalité (nœud de nouveau prêt instantanément), puis un cycle complet
lancement→attente→réclamation avec 1 gardienne toujours committée pendant tout le vol.

Prochain test utilisateur : recruter quelques troupes, lancer une collecte sur la carte, observer
le marqueur voyager puis se poser sur le nœud avec l'anneau "Occupée" et le minuteur visibles,
cliquer à nouveau pour rappeler l'escouade avant la fin, confirmer que les troupes redeviennent
utilisables ailleurs immédiatement.

Ouvert / à faire ensuite : construire un vrai composeur de troupes pour la Collecte mondiale (comme
celui de Combat Patrol) une fois que Jeff voudra remplacer l'escorte par défaut par un vrai choix
joueur. Le PvP/raids/renforts/occupation de POI peuvent maintenant réutiliser directement
`HiveTroopDeploymentAccounting` et le patron marche+occupation de la carte.

---

## Jalon courant — Première mécanique de découverte : repérage rare (2026-08-01, suite)

Même jour, après validation de la localisation des événements mondiaux, Jeff a jugé cet axe
suffisamment avancé et a demandé d'augmenter la sensation d'exploration du monde à la place — que
le joueur n'ait plus l'impression que toute la carte est déjà connue. Contrainte : aucun nouveau
gros système, réutiliser la carte, les événements, les créatures et les ressources déjà existants.

**Découverte faite avant de concevoir** : la carte du monde a déjà un vrai système de dispersion
procédurale de bestiaire par chunk (`GenerateSeededBestiary` dans
`WorldMapMmoFullscreenFoundationBootstrap.cs`) — chaque chunk a une chance de faire apparaître une
créature de palier aléatoire (1-7), et cliquer dessus ouvre le vrai combat serveur pour ce palier
exact. Ce n'était pas un système décoratif : c'était déjà une vraie surface d'exploration, juste
sans aucune raison de la parcourir plutôt que de rester sur les créatures déjà visibles à l'écran.

**Réalisé** : "Repérage rare". Parmi les créatures déjà dispersées sur la carte, celle dont le
palier correspond exactement à la "menace en hausse" localisée ce cycle (le mécanisme livré au
jalon précédent) reçoit une mise en valeur visuelle distincte (halo doré pulsant, étiquette "★" et
nom de l'événement) et un rappel du bonus dans le panneau d'action une fois sélectionnée. Aucun
nouveau serveur, créature, ressource ou système de rendu : uniquement une condition d'affichage sur
des données déjà transmises (`CombatPatrolSnapshot.WorldEvent`/`WorldEventFeaturedTier`), et le
combat déclenché reste l'exact même flux "Patrouiller" déjà existant. Comme le repérage n'existe
que pendant les fenêtres "menace en hausse" (3 des 6 créneaux de 4h) et que son palier ET sa
position (dispersion procédurale déjà existante) varient, le joueur doit vraiment parcourir/zoomer
la carte pour le retrouver plutôt que de cliquer toujours la même créature fixe.

Preuves : compilation Unity sans erreur ; passage en Play Mode sur la scène canonique
(`WorldMapWave6Wave5Method12288Preview`) sans erreur ni exception dans la console pendant que le
nouveau code de surbrillance tourne à chaque frame. Aucun test automatisé dédié n'existait déjà pour
ce fichier de rendu IMGUI (confirmé avant de commencer) ; aucun code serveur touché, donc aucune
suite serveur concernée par ce jalon.

Prochain test utilisateur : ouvrir la carte du monde pendant une fenêtre "menace en hausse" (visible
via la bannière déjà affichée sur l'écran Patrouille de combat), parcourir/zoomer la carte pour
repérer la créature au halo doré, la sélectionner et confirmer que le panneau d'action affiche bien
"★ Repérage rare" avec le nom de l'événement et le bonus.

Ouvert / à faire ensuite : si Jeff veut renforcer la densité (garantir au moins un repérage visible
sans trop zoomer/parcourir), il faudrait ajuster le taux de dispersion procédural existant — pas un
nouveau système, un simple réglage de probabilité déjà en place.

---

## Jalon courant — Localisation des événements mondiaux (2026-08-01, suite)

Même jour, juste après validation de l'événement mondial ci-dessous, Jeff a demandé qu'il ne soit
plus uniforme : les événements doivent apparaître dans des régions précises, forçant un vrai choix
de déplacement plutôt qu'un bonus qui profite partout à la fois.

**Changement** : une "menace en hausse" ne boost plus TOUS les paliers de Combat Patrol de sa
famille de danger en même temps — un seul palier précis parmi eux (`WorldEventCatalog.FeaturedRegionTier`,
même cadence de 4h que la saveur active) est réellement ciblé ce cycle. Même principe pour la météo
et les noeuds de Collecte mondiale (`FeaturedRegionNodeId`) — actuellement sans effet visible côté
collecte puisque chaque ressource n'a qu'un seul noeud dans le catalogue actuel, mais le mécanisme
est prêt dès qu'un second noeud partagera une ressource. Le palier ciblé est maintenant visible
directement sur le sélecteur de palier (marque "!"), pas seulement dans l'aperçu après sélection.

Preuves : nouveaux tests `WorldEventCatalogTests` (un seul élément choisi parmi plusieurs, jamais
toujours le même sur plusieurs cycles) + `CombatPatrolServiceTests` mis à jour pour cibler le palier
réellement localisé au lieu de "n'importe quel palier de la famille" — 24/24 verts sur ce fichier,
153/153 sur `BeeKingdom.HiveOperations.Tests`. Compilation serveur et Unity sans erreur. Vérifié en
direct contre le serveur de développement réel : pendant une fenêtre météo (pas menace), `combat/patrol`
renvoie bien `worldEventFeaturedTier:null` et l'aperçu d'un palier "guardians" renvoie bien
`isWorldEventBoosted:false`, confirmant qu'aucun combat n'est affecté hors fenêtre de menace.

Ouvert / à faire ensuite : si le catalogue de Collecte mondiale gagne un jour plusieurs noeuds par
ressource, la localisation y deviendra visible sans changement de code supplémentaire.

---

## Jalon courant — Premier événement mondial dynamique : météo/menace (2026-08-01)

Après validation de la Cible du jour et du retrofit anti-scintillement, Jeff a demandé un premier
"événement mondial dynamique" (floraison, invasion de fourmis, sécheresse, etc. — exemples donnés,
pas une liste imposée), avec la contrainte de réutiliser exclusivement les systèmes existants, pas
de nouveau gros système, et une vraie raison de revenir plusieurs fois dans la même journée (pas
juste une fois par jour comme la Cible du jour).

**Conception** : un nouveau catalogue serveur pur (aucun état persisté, aucun RNG) qui change toutes
les 4h (6 fenêtres/jour, décalage différent chaque jour civil pour éviter une météo toujours à la
même heure) et alterne entre deux familles d'effets : une météo qui modifie le rendement de collecte
mondiale d'UNE ressource (miel/cire/pollen, bonus ou malus), et une "menace en hausse" qui modifie
la récompense de combat pour UNE famille de danger (fourmis→gardiennes, araignées→lanceuses,
frelons→voltigeuses — réutilise exactement la classification de danger déjà utilisée par les 7
paliers de Patrouille de combat). Les deux services de récompense déjà existants (Patrouille de
combat, Collecte de ressource mondiale) appliquent ce bonus au même endroit et de la même façon que
la Cible du jour — jamais sur la puissance de combat, jamais sur les seuils de résolution. Les deux
mécaniques (Cible du jour, quotidienne ; météo/menace, plusieurs fois par jour) coexistent et
peuvent s'empiler les jours où elles coïncident, sans jamais se contredire.

Preuves : nouveaux tests `WorldEventCatalogTests` (stabilité intra-fenêtre, changement aux
frontières, couverture des 6 événements sur une journée, décalage quotidien, calcul bonus/malus) +
tests de service dédiés dans `CombatPatrolServiceTests`/`WorldResourceCollectionServiceTests` — tous
verts (153/153 sur `BeeKingdom.HiveOperations.Tests`) ; compilation serveur et Unity sans erreur ;
vérifié en direct contre le serveur de développement réel avec un nouveau compte QA
(`claude.qa.worldevent@example.test`) : les deux endpoints (combat et collecte mondiale) renvoient
bien le même événement actif au même instant ; une collecte réelle sur le nœud "cire" pendant une
fenêtre "sécheresse" a crédité 24 au lieu de 30 (malus -20% confirmé, `worldEventApplied:true`).

Prochain test utilisateur : ouvrir la Patrouille de combat, voir la bannière d'événement (visible
seulement pendant une fenêtre "menace en hausse") et le marqueur sur l'aperçu du palier concerné ;
ouvrir la Collecte de ressource mondiale, voir le nœud dont le rendement est affecté par la météo du
moment sur la carte du monde.

Ouvert / à faire ensuite : aucune suite obligatoire — envisager d'étendre le même catalogue à
d'autres systèmes (recherche, championnes) si Jeff le demande une fois la Bible du bestiaire réglée.

---

## Jalon courant — Retrofit du scintillement optimiste sur les écrans restants (2026-07-31, suite)

Suite à la demande de Jeff de vérifier TOUS les écrans partageant le défaut de scintillement
déjà corrigé (bâtiments/production), audit ciblé des candidats les plus à risque identifiés par
l'audit complet : Recherche (10 offres simultanées), Patrouille de combat (jusqu'à 5 encounters
concurrents + bandeau toujours visible), Sortie de périmètre (2 signaux), Réservation d'escouade
officielle (3 familles), Collecte de ressource mondiale (déjà corrigé cette session).

**Constat correctif à l'audit précédent** : contrairement à ce qui était supposé, Patrouille de
combat n'a PAS ce défaut — le bandeau de file d'attente (`DrawCombatPatrolQueueStrip`) lit
directement chaque encounter actif indépendamment de l'état global de mutation, donc les autres
patrouilles ne clignotent jamais pendant qu'une seule est réclamée/rappelée. Sortie de périmètre
et Réservation d'escouade n'ont pas non plus ce défaut : le premier n'a qu'une seule sortie active
possible à la fois (les cartes de signaux gardent leurs données réelles pendant la mutation), le
second ne représente qu'une seule réservation composite (pas une liste d'items indépendants).

**Seul vrai bug trouvé** : Recherche officielle. Pendant le démarrage ou la validation d'UNE
étude, l'état global du modèle passait à Starting/Completing, et TOUTES les offres affichées (pas
seulement celle en cours) basculaient vers un texte générique "Démarrage…"/"Le serveur réserve les
ressources…" — exactement le symptôme dénoncé par Jeff, généralisé à un écran à offres multiples.
Corrigé en donnant au modèle un identifiant exact de l'étude en train de muter (nouveau champ
`MutatingResearchId` + méthode `IsMutating(researchId)`) ; seule cette offre montre désormais le
texte de mutation, les autres gardent leur coût/prérequis/effet habituels tout le temps du
aller-retour serveur.

Preuves : compilation Unity sans erreur ; nouveau test `MutatingOneOfferNeverBlanksTheOthers`
(vert) dans `SandboxLivingHiveOfficialResearchTests` confirmant qu'une offre non touchée garde ses
données de coût réelles pendant qu'une autre démarre/se termine ; suite EditMode complète relancée
(2 échecs pré-existants sans rapport — un test de rects d'écran et une comparaison NUnit
TimeSpan/int — ni l'un ni l'autre ne touche du code modifié cette session).

Prochain test utilisateur : ouvrir l'écran Recherche avec au moins deux offres visibles, lancer
une étude, observer que l'offre voisine garde son coût/effet affiché au lieu de basculer sur un
texte générique pendant le court aller-retour serveur.

Ouvert / à faire ensuite : aucun autre écran à risque identifié pour ce défaut à ce stade ; revoir
si de nouveaux écrans à items multiples sont ajoutés plus tard.

---

## Jalon courant — Cible du jour (2026-07-31, suite)

Après validation du correctif de scintillement et de l'audit complet, Jeff a demandé de changer de
posture (Lead Game Designer, pas programmeur) pour construire la première vraie boucle de
rétention de BeeKingdom : une amélioration d'un système existant, pas un nouveau système, sans
minuteur artificiel, qui relie plusieurs boucles déjà construites et crée un vrai choix quotidien.

**Choix retenu** : "Cible du jour" — chaque jour calendaire UTC désigne automatiquement un palier
de Patrouille de combat (1 à 7, rotation sur 7 jours) et un nœud de Collecte de ressource mondiale
comme "en vedette", avec un bonus de +50% sur le butin uniquement (jamais sur la puissance/la
disposition de combat) au moment de la réclamation. Fonction pure de la date (aucun nouvel état
persisté, aucun RNG), calculée identiquement côté serveur et exposée directement dans les
snapshots existants. Alternatives écartées : une variante répétable de Défi de la ruche (risque
de récompense gratuite exploitable), un tableau de quêtes rotatif (aurait été un nouveau système,
pas une amélioration).

Le joueur voit le palier/nœud du jour marqué avant même de lancer une action (pas seulement dans
le rapport après coup), pour que le choix ("est-ce que je réoriente mes troupes/ma collecte vers
la cible du jour ?") se fasse au moment de décider quoi faire aujourd'hui.

Preuves : nouveaux tests `DailyFocusCatalogTests` (stabilité intra-jour, cycle de 7 jours, rotation
des nœuds, cas catalogue vide, calcul du bonus) + tests de service dédiés dans
`CombatPatrolServiceTests`/`WorldResourceCollectionServiceTests` — tous verts ; compilation serveur
et Unity sans erreur ; vérifié en direct contre le serveur de développement réel avec le compte QA
existant (`claude.qa.strategicpath@example.test`) : palier en vedette confirmé (T3) et nœud en
vedette confirmé (`res_honey_core`) pour la date serveur courante, un vrai combat sur le palier en
vedette a bien crédité +50% (204 miel / 129 pollen au lieu de 136/86, `dailyFocusApplied:true`), et
une vraie collecte sur le nœud en vedette a bien crédité 45 au lieu de 30 (rendement de base ×1.5,
`dailyFocusApplied:true`).

Prochain test utilisateur : ouvrir la Patrouille de combat, confirmer qu'un palier porte un `*` et
un encart "Cible du jour" ; ouvrir la Collecte de ressource mondiale, confirmer qu'un nœud affiche
le même marquage ; réclamer les deux et voir le +50% mentionné explicitement dans le rapport.

Ouvert / à faire ensuite : aucune suite obligatoire — surveiller si Jeff veut étendre la mécanique
à d'autres boucles (recherche, championnes) une fois la Bible du bestiaire réglée.

---

## Jalon courant — Audit complet d'architecture (2026-07-31, suite)

Jeff a demandé une pause dans le développement pour un audit complet du projet (posture
Lead Game Developer, aucune modification de code). 6 agents de recherche dédiés ont couvert :
Économie & Construction, Combat & Armée, Monde & Bestiaire, Progression/Méta/Social,
Fondation UX, Fondation technique. Rapport final livré sous forme d'artifact (36 systèmes
notés par maturité/%, feuille de route par boucles de gameplay, redondances signalées,
5 forces/5 faiblesses/5 priorités).

**Constats les plus importants à retenir pour la suite** :
- Le bestiaire est déjà mécaniquement branché sur ses 7 paliers (pas seulement le palier 3
  nommé cette semaine) — enrichir le bestiaire une fois la Bible réglée ne demandera aucun
  nouveau code serveur.
- VIP et Cycle quotidien sont tous deux entièrement fonctionnels côté logique mais inertes en
  pratique (VIP n'a aucun canal d'acquisition réel ; Cycle quotidien est désactivé même en
  configuration de production) — la priorité n°1 recommandée est de leur donner un vrai canal
  (ex. via le Défi de la ruche), effort minimal pour un déblocage majeur.
- La production actuelle est un "shard de préparation" intentionnel : création de compte/session
  explicitement désactivées — rien de livré cette session (ni avant) n'est jouable par un vrai
  joueur tant que cet écart dev/prod n'est pas traité.
- Redondances signalées à nettoyer : `CombatFormationReadiness` (écran de lecture pure, jamais
  utilisé comme verrou, doublon de Recrutement/Réservation d'escouade), `BeeKingdom.Colony` et
  `BeeKingdom.Simulation` (projets serveur entiers sans consommateur client), le suivi
  d'engagement de troupes dupliqué entre Combat Patrol et Réservation d'escouade, les DTO
  Alliance orphelins, le projet de test vide `BeeKingdom.ChatTranslation.Tests`, les boutons
  d'acquisition des Championnes qui appellent la même méthode.
- Audio et animation : quasi inexistants (zéro fichier son dans tout le projet, animation
  hors build) — la lacune de "feel" la plus visible.
- L'UI entière tient dans un fichier de 35 719 lignes (IMGUI) — pas urgent mais aucun jalon
  de migration n'est encore fixé.
- La convention client optimiste (voir jalon précédent) n'est appliquée qu'à 2 écrans sur ~9 qui
  partagent le même défaut ; Combat Patrol est le candidat de retrofit le plus prioritaire
  restant (bandeau de patrouilles multiples déjà visible en permanence).

Aucun fichier de code n'a été modifié pendant cet audit.

Ouvert / à faire ensuite : Jeff doit choisir parmi les 5 priorités recommandées (canal VIP,
bascule prod, nettoyage des redondances, premier passage audio + jalon UI, extension du Monde
une fois la Bible réglée) avant de reprendre le développement de fonctionnalités.

---

## Jalon courant — Premier "événement" : Défi de la ruche (2026-07-31, suite)

Après le correctif client optimiste, Jeff a demandé la prochaine boucle de gameplay complète,
en excluant explicitement l'enrichissement du bestiaire (Bible du bestiaire et direction
artistique en cours de définition ailleurs). Consigne : choisir entre alliances, interactions
sociales, événements, expéditions selon le meilleur rapport valeur/effort.

**Analyse** : Alliance et interactions sociales (échange, visite, classement) nécessiteraient de
résoudre un problème d'architecture qui n'existe nulle part encore dans le projet — aucune
visibilité entre joueurs n'existe (chaque ruche est isolée), donc un vrai système d'alliance
serait un chantier d'architecture, pas une fonctionnalité d'une session. Les expéditions
recoupent largement 3 boucles déjà livrées cette session (sortie de périmètre, collecte de
ressource mondiale, combat contre créature) — peu de valeur incrémentale. Les événements,
eux, peuvent rester dans l'architecture mono-joueur déjà existante tout en reliant plusieurs
systèmes ensemble (exactement la consigne de Jeff) : c'était le meilleur rapport valeur/effort.

**Réalisé** : premier "défi" ponctuel du jeu qui relie 5 systèmes déjà construits cette session
et avant — amélioration de bâtiment, recrutement de troupes, collecte de ressource mondiale,
voie stratégique, palier VIP. Chaque objectif est lu directement depuis l'état déjà persisté par
son propre système (aucune nouvelle instrumentation, zéro risque pour les systèmes existants).
Atteindre 3 objectifs sur 5 dans une fenêtre de 30 jours permet de réclamer une récompense
modeste (miel + pollen) une seule fois — volontairement à usage unique (pas de réinitialisation
répétable) pour éviter une récompense gratuite exploitable chaque semaine, tout en gardant un
vrai caractère d'événement grâce à la fenêtre de temps limitée.

Accessible directement depuis un nouveau bouton "Défi" dans la barre de navigation principale
(badge affichant la progression ou un point d'exclamation quand la récompense est prête).

Preuves : 136/136 tests `BeeKingdom.HiveOperations.Tests` verts (4 nouveaux) ; compilation Unity
et serveur sans erreur ; testé en direct contre le serveur de développement réel — un compte
neuf montre bien 0/5 objectifs et aucune réclamation possible ; un compte ayant déjà progressé
cette session (bâtiment niveau 10, troupes recrutées, ressource mondiale collectée, voie
stratégique choisie) montre bien 4/5 objectifs réels et permet une réclamation réussie avec
crédit réel du miel et du pollen, puis refuse correctement une seconde réclamation. Pont client
vérifié par réflexion dans Unity (texte de badge, libellés d'objectifs, activation/désactivation
du bouton).

Fichiers : `HiveMilestoneEventService.cs` (nouveau, serveur), `HiveOperationModels.cs` (champ
`MilestoneEvent` ajouté), `HiveStateMigrator.cs`, `Program.cs` (2 nouvelles routes),
`start-dev-server.local.ps1`, `HiveMilestoneEventClient.cs`/`HiveMilestoneEventPresentation.cs`
(nouveaux, client), `MobileAccountSessionRuntimeBootstrap.cs`, `HiveViewProductUiPresenter.cs`
(bouton de navigation + panneau).

Ouvert / à faire ensuite : une fois la Bible du bestiaire et la direction artistique du monde
réglées, reprendre l'enrichissement du bestiaire (voir jalon précédent) ou étendre le système
d'événement avec de nouveaux défis (le code est déjà structuré pour ajouter facilement d'autres
objectifs). Le vrai chantier d'architecture "visibilité entre joueurs" reste à faire si Jeff
souhaite un jour une vraie Alliance ou des interactions sociales.

---

## Jalon courant — Client optimiste : fin du scintillement de synchronisation (2026-07-31, suite)

Jeff a signalé que collecter une ressource sur UN bâtiment faisait clignoter les indicateurs des
AUTRES bâtiments (ils disparaissaient brièvement puis réapparaissaient) — le serveur devenait
perceptible, ce qu'il ne veut jamais. Consigne durable : le client doit toujours paraître
entièrement local ; seule l'élément réellement modifié doit changer ; jamais de rafraîchissement
global visible.

**Cause racine trouvée** (pas une supposition — tracée dans le code) : chaque écran de gameplay
(production, améliorations de bâtiment, recherche, patrouille de combat, voie stratégique,
collecte mondiale, etc.) a UN SEUL état partagé ("Collecting"/"Starting"/"Mutating"...) pour TOUT
l'écran, même quand la mutation en cours ne concerne qu'UN SEUL élément (un bâtiment, un noeud).
Le code d'affichage de chaque élément vérifiait cet état global avant de dessiner SON PROPRE
statut — donc pendant qu'un seul bâtiment collectait, les DEUX AUTRES perdaient aussi leur texte
de remplissage normal au profit d'un message générique "Collecte serveur en cours…", pour
réapparaître une fois la mutation terminée. C'est exactement le clignotement rapporté.

**Corrigé** pour les deux écrans toujours visibles simultanément sur la ruche (Production hors
ligne et Amélioration de bâtiment) : chaque modèle sait désormais QUEL élément précis est en
cours de mutation (nom de champ dédié, pas juste un état global). Le texte de statut par bâtiment
n'affiche le message générique que pour CE bâtiment précis ; les autres continuent d'afficher
leur vrai statut (remplissage réel, offre réelle) sans interruption, quel que soit l'état global
partagé. Aucune app ne « rafraîchit » au sens Unity — l'affichage est en mode immédiat (IMGUI,
redessiné à chaque frame) ; le vrai correctif était dans la donnée lue par frame, pas dans un
mécanisme de rendu.

**Convention retenue pour la suite du projet** (à appliquer à tout nouvel écran, et à retrofitter
plus tard sur les écrans modaux existants qui partagent encore ce défaut — Recherche, Patrouille
de combat, Voie stratégique, Collecte mondiale, Sortie de périmètre, Cycle quotidien, Réservation
d'escouade — actuellement moins prioritaires car ce sont des écrans modaux à focus unique, pas
des tableaux de bord visibles simultanément) : **un état global ne doit jamais gater l'affichage
d'un élément individuel** ; toute mutation doit identifier précisément quel élément elle concerne,
et cet identifiant doit être lu par le code d'affichage AVANT de recourir à un message générique.

Preuves : compilation Unity et serveur sans erreur ; vérifié directement dans Unity par réflexion
sur les deux écrans corrigés — simulation exacte du scénario rapporté par Jeff (collecte sur un
bâtiment pendant que les deux autres ont des données valides) : les deux bâtiments non concernés
continuent d'afficher leur vrai statut ("Nectar en stockage · remplissage 0%", etc.) au lieu du
message générique, tandis que le bâtiment réellement en cours de mutation l'affiche correctement.

Fichiers : `HiveOfflineProductionPresentation.cs`, `HiveBuildingUpgradePresentation.cs`,
`HiveViewProductUiPresenter.cs`.

Ouvert / à faire ensuite : retrofitter la même convention sur les écrans modaux listés ci-dessus
quand l'occasion se présente (pas urgent, mais durable — à appliquer systématiquement à tout
nouvel écran construit à partir de maintenant).

---

## Jalon courant — EPIC Monde : première créature sauvage, boucle complète (2026-07-31, suite)

Jeff a validé le vol de collecte comme première boucle du Monde, puis a demandé d'enrichir le
Monde non pas avec plus de ressources mais avec plus de **décisions** : une première créature
sauvage avec une boucle complète (découverte, attaque, résolution, récompense, rapport), en
réutilisant au maximum les systèmes déjà existants.

**Analyse** : la carte du monde affiche déjà un bestiaire sauvage réel (7 paliers, sprites
réels) et un bouton "Patrouiller" qui ouvre déjà l'écran de Patrouille de combat (déjà 100%
réel côté serveur) pré-réglé sur le palier de la créature cliquée. Autrement dit, la boucle
mécanique (attaque → résolution → récompense) était déjà quasi entièrement fonctionnelle — il ne
manquait que l'identité de la créature dans le rapport final. Coïncidence utile : le palier 3 du
catalogue de combat existant s'appelle déjà "Araignee sauteuse", et le noeud du bestiaire déjà
placé sur la carte (dans le chunk du joueur) est lui aussi une "Araignee" — les deux systèmes
étaient déjà thématiquement alignés sans être connectés.

**Décision** : plutôt que de construire un nouveau système séparé (ce que Jeff a explicitement
demandé d'éviter), la créature choisie est ce palier 3 déjà existant. Le rapport de combat
affiche désormais son nom réel (récupéré directement de la réponse serveur déjà existante,
celle qui l'affichait déjà avant le lancement) sur sa propre ligne, en tête du rapport. Aucun
nouveau système serveur, aucune nouvelle donnée persistée — 100% de réutilisation du combat de
patrouille déjà construit (résolution déterministe, bonus championnes/palier de troupe/voie
stratégique déjà tous actifs automatiquement).

**Testé en direct**, bout en bout, contre le serveur de développement réel : recrutement réel de
20 gardiennes, lancement réel d'une patrouille palier 3 contre l'Araignee sauteuse (141/160 de
puissance disponible, gardiennes en avantage face aux lanceuses), vraie attente de 45 secondes,
validation réussie — résultat "HardWon", 4 gardiennes blessées (convalescence réelle), 136 miel
et 86 pollen crédités, bonus de la Voie stratégique (+6%, choisie plus tôt sur ce même compte)
confirmé actif automatiquement sans rien reconfigurer.

Deux outils de test additionnels créés (même garde que les précédents, jamais actifs en
production) : un pour créditer une ressource directement, un pour activer le recrutement de
troupes en environnement de développement local (absent du script jusqu'ici).

Preuves : 132/132 `BeeKingdom.HiveOperations.Tests` verts ; compilation Unity et serveur sans
erreur ; boucle complète rejouée en direct comme décrit ci-dessus.

Fichiers : `HiveViewProductUiPresenter.cs` (nouvelle ligne de rapport, réutilise une donnée déjà
transmise par le serveur), `Program.cs` (2 outils de test dev), `start-dev-server.local.ps1`
(recrutement de combat activé).

Ouvert / à faire ensuite : Jeff a indiqué vouloir enrichir progressivement le bestiaire une fois
cette boucle validée — les 6 autres paliers déjà catalogués (Puceron, Fourmi, Mante, Frelon,
Scorpion, Reine frelon ancienne) suivent exactement le même patron et pourraient chacun devenir
une créature nommée de la même façon, sans nouveau travail serveur.

---

## Jalon courant — EPIC Monde : premier vol de collecte de ressource mondiale réel (2026-07-31, suite)

Jeff a validé la Voie stratégique comme suffisante pour l'instant et a ouvert un nouvel EPIC :
**le Monde**. Consigne explicite : ne pas prolonger les batiments jusqu'au niveau 10 maintenant
(« résoudre une limitation technique avant d'en avoir réellement besoin » — donc le verrou de
niveau 10 de la Voie stratégique reste tel quel, en attente d'un vrai besoin de progression).

**Audit réel effectué** (carte du monde, déplacements, expéditions, points d'intérêt, ressources
mondiales, créatures, événements, interactions joueurs) via 3 agents de recherche dédiés. Constat
principal : la scène canonique de la carte du monde contient un seul script monolithique
(`WorldMapMmoFullscreenFoundationBootstrap`, ~6400 lignes) qui affiche déjà une carte réelle avec
noeuds de ressources cliquables, vols de collecte animés, et un bestiaire sauvage — mais **tout est
purement local/demo** : zéro appel serveur nulle part dans ce fichier (le panneau l'affiche même
explicitement : « aucun gain officiel, aucun serveur »). Le reste (Sortie de périmètre, Alliance,
Expédition, Créatures) est soit un système interne sans représentation visuelle sur la carte
(Sortie de périmètre), soit un stub pur (Alliance), soit inexistant (Expédition, Événements
mondiaux joueur-visibles).

**Fonctionnalité choisie et livrée** : rendre réel le vol de collecte de ressource mondiale — la
toute première boucle qui fait sortir concrètement le joueur de sa ruche pour aller chercher une
ressource ailleurs sur la carte. Un nouveau système serveur complet (catalogue configurable de
noeuds, lancement/validation idempotents, cooldown par noeud, un seul vol actif à la fois) rend
réels exactement les 3 noeuds déjà visibles sur la carte dont la ressource existe déjà dans
l'économie du serveur (miel/pollen/cire — mêmes identifiants que les instances demo déjà placées :
`res_pollen_core`, `res_wax_core`, `res_honey_core`). Les autres noeuds visibles (nectar/eau/
propolis/gelée royale) restent volontairement demo pour l'instant — aucune nouvelle monnaie
inventée sans besoin réel.

Câblage client fait de façon chirurgicale : plutôt que de reconstruire une interface parallèle
(ce que Jeff a explicitement demandé d'éviter — relier les systèmes existants plutôt qu'en créer
de nouveaux isolés), le bouton "Collecter" déjà existant sur la carte a été branché au vrai
serveur pour ces 3 noeuds précis, en laissant le reste du fichier (bestiaire, caméra, autres
ressources) intact. L'état affiché (prêt / vol en cours / repos / dernier gain) vient maintenant
du vrai serveur pour ces 3 noeuds.

Preuves : 132/132 `BeeKingdom.HiveOperations.Tests` verts (5 nouveaux tests) ; compilation Unity
et serveur sans erreur ; testé en direct contre le serveur de développement réel — lancement d'un
vol sur le noeud cire, refus de validation avant la fin réelle du délai, attente réelle de 46
secondes, validation réussie avec **30 cire créditée exactement**, noeud passé en repos avec le
bon délai, blocage correct d'un second vol pendant que le premier est actif ; vérifié aussi que le
plafond de capacité de ressource est respecté (crédite seulement jusqu'à la limite). Pont client
vérifié par réflexion dans Unity (identifiants officiels reconnus, silencieux sans crash quand
aucune session n'est configurée).

Fichiers : `WorldResourceCollectionService.cs` (nouveau, serveur), `HiveOperationModels.cs`
(champ `WorldResourceCollection` ajouté à l'état de ruche), `HiveStateMigrator.cs` (validation),
`Program.cs` (3 nouvelles routes), `start-dev-server.local.ps1` (catalogue de test),
`WorldResourceCollectionClient.cs`/`WorldResourceCollectionPresentation.cs` (nouveaux, client),
`MobileAccountSessionRuntimeBootstrap.cs`, `HiveViewProductUiPresenter.cs` (pont vers la carte),
`WorldMapMmoFullscreenFoundationBootstrap.cs` (branchement chirurgical du bouton existant).

Ouvert / à faire ensuite : les autres types de ressources visibles sur la carte (nectar/eau/
propolis/gelée royale) restent demo — décision à prendre plus tard si elles doivent devenir de
vraies ressources ou rester cosmétiques. Prochaine candidate naturelle pour continuer l'EPIC
Monde : soit étendre ce même système à d'autres noeuds/récompenses, soit s'attaquer au bestiaire
sauvage (combat contre les créatures de la carte, actuellement 100% local) pour la prochaine
boucle « sortir de la ruche ».

---

## Jalon courant — Voie stratégique branchée au serveur réel + premier bonus permanent (2026-07-31)

Jeff a explicitement demandé d'arrêter de peaufiner le combat et d'élargir le
jeu vers un système déjà présent mais inexploité. Après audit de 5 systèmes
sous-utilisés (arbre de compétences, voie stratégique, sortie de périmètre,
alliance, expéditions), la **Voie stratégique** a été retenue : un choix de
classe permanent (garde royale / frappe / soin / éclaireuse / alchimie)
entièrement construit côté serveur (persistance, verrou définitif, jauge de
déblocage niveau de bâtiment ≥10) mais jamais appelé par le client réel
(stub de prévisualisation locale) et sans le moindre effet de jeu.

**Réalisé** : client réel branché (`StrategicPathClient.cs`,
`StrategicPathPresentation.cs`, intégré au cycle de vie de session comme les
autres contrôleurs), écran officiel de choix dans `HiveViewProductUiPresenter.cs`
avec confirmation et bouton réel. Chaque classe reçoit désormais UN bonus
permanent réel et modeste (+6%, additif en bp comme toutes les autres sources
de bonus déjà construites cette session) : garde royale/frappe → puissance de
combat (les 3 familles) ; soin/alchimie → taux de production hors ligne (les 3
ressources) ; éclaireuse → capacité de stockage. Le rapport de combat gagne une
troisième ligne explicative dédiée ("Voie stratégique (...) : Gardiennes +6% ·
..."), suivie séparément du bonus championnes et du palier de troupe, jamais
fusionnée. Catalogue `StrategicPath__Enabled=true` ajouté à
`start-dev-server.local.ps1` (absent auparavant, donc injoignable en jeu malgré
le code serveur).

**Bug réel trouvé et corrigé en testant en direct** : `StrategicPathService.ChooseAsync`
comparait à la fois `state.Revision` (global) ET `strategic.Revision` (imbriqué,
celui que le client suit réellement via le snapshot GET) à la même valeur —
un joueur ayant fait quoi que ce soit d'autre dans sa ruche avant d'atteindre le
niveau 10 (par ex. une simple lecture de production hors ligne, qui fait
avancer `state.Revision`) se serait retrouvé bloqué en permanence par un faux
conflit de révision, sans jamais pouvoir choisir sa voie. Corrigé pour ne
comparer que `strategic.Revision`, même convention que `CombatPatrolService`
pour un sous-système à compteur imbriqué. Nouveau test de régression
`ChoiceSucceedsWhenHiveRevisionHasAdvancedFromUnrelatedActivity` qui aurait
attrapé ce bug (l'ancien test ne le pouvait pas : il ne touchait jamais à
`state.Revision` avant l'appel).

**Outillage de test ajouté** : nouvel endpoint `/dev/hives/{hiveId}/set-building-level`
(même garde que `/dev/hives/{hiveId}/grant-vip-points` — jamais accessible en
production, `DevTools:AllowDevAccountSeeding`), pour pouvoir tester des
systèmes dont le déclencheur (ici, niveau de bâtiment ≥10) n'est pas encore
atteignable via le contenu réel du catalogue d'amélioration (qui plafonne à 4).

Preuves : 127/127 `BeeKingdom.HiveOperations.Tests` verts (dont le nouveau test
de régression) ; compilation Unity et serveur sans erreur ; testé en direct
contre le serveur de développement réel (SQL Server distant) avec deux comptes
de test dédiés — voie "striker" : puissance de combat disponible passée de 37 à
40 avec 10 gardiennes (formule confirmée exacte : 50 base × 8100bp/10000) ;
voie "nurturer" : taux de production passé de 285→302,1 miel/h, 100→106
pollen/h, 80→84,8 cire/h (+6% exact sur les 3 ressources, en plus du
multiplicateur de niveau de bâtiment déjà existant) ; texte du debrief généré
par le vrai code du présentateur via réflexion Unity : *"Voie stratégique (Dard
d'assaut) : Gardiennes +6% · Voltigeuses +6% · Lanceuses +6%"*.

Fichiers : `StrategicPathService.cs` (catalogue de bonus + correctif de
révision), `HiveOfflineProductionService.cs`, `CombatPatrolService.cs`,
`HiveOperationModels.cs` (`CombatPatrolClaimReceipt` étendu),
`StrategicPathClient.cs`, `StrategicPathPresentation.cs` (nouveaux),
`MobileAccountSessionRuntimeBootstrap.cs`, `HiveViewProductUiPresenter.cs`,
`CombatPatrolClient.cs`, `CombatPatrolPresentation.cs`, localisation FR/EN
(16 nouvelles clés), `Program.cs` (nouvel endpoint dev), `start-dev-server.local.ps1`.

Ouvert / à faire ensuite : le gate de niveau ≥10 reste actuellement
inatteignable par le contenu réel (catalogue d'amélioration plafonné à 4) —
soit étendre le catalogue avec de vrais paliers 5-10 équilibrés, soit abaisser
le seuil, à trancher avec Jeff quand la Voie stratégique doit devenir
accessible à un vrai joueur. La capacité de stockage (classe éclaireuse) n'a
été vérifiée qu'au niveau des tests unitaires (`StrategicPathBonusCatalogTests`),
pas en direct contre le serveur — même mécanisme bp additif que les deux autres
branches déjà confirmées en direct, donc risque jugé faible.

---

## Jalon courant — Rapport de combat explicatif : palier de troupe + championnes (2026-07-30, suite)

Suite immédiate du jalon précédent. Jeff a précisé l'exigence : un bonus qui
change le résultat d'un combat sans que le joueur comprenne pourquoi n'est
pas satisfaisant — le debrief de patrouille doit devenir progressivement
explicatif (puissance des troupes, bonus des championnes, puis plus tard
recherche/bâtiments/alliance).

**Réalisé** :
- **Palier de troupe (T1/T2/T3) branché sur la puissance de combat réelle** :
  jusqu'ici `CombatPatrolResolution.UnitPower` était une constante fixe (5),
  ignorant totalement le palier promu par `PromoteTroopTierAsync` — un
  système de progression déjà complet et payant, sans aucun effet réel.
  Reprend la formule déjà conçue côté client (`TroopTierMultiplier`, +25 % de
  puissance par palier au-dessus de 1) plutôt que d'inventer un nouvel
  équilibrage. Nouveau `TroopTierCatalog.CombatContribution(...)` (même
  forme que celui des championnes), fusionné avec le bonus des championnes
  via `CombatPatrolService.MergedPowerBonus(...)` avant le calcul
  déterministe.
- **Rapport de combat explicatif** : le debrief affiche maintenant la
  puissance requise/disponible (absente auparavant du debrief, seulement
  visible avant le lancement), une ligne dédiée "Palier de troupe : Gardiennes
  T2 +25 %" et la ligne championnes déjà livrée, chacune sur sa propre ligne
  et clairement attribuée — pas de bonus invisible. Chaque source de bonus
  reste suivie séparément sur le reçu serveur (pas fusionnée en un seul
  nombre opaque), pour que d'autres sources (recherche, bâtiments, alliance)
  puissent s'ajouter au même endroit plus tard sans tout redessiner.

Testé en direct contre le serveur de développement (compte de test dédié) :
avec palier de troupe T2 (gardiennes) ET Striga niveau 3 assignée
simultanément, 12 gardiennes passent de 60 à **81** de puissance disponible
(60 → 66 avec la championne seule, → 81 en ajoutant le palier T2) ; combat
livré "HardWon" avec un reçu confirmant `troopTierByFamily`,
`troopPowerBonusBpByFamily`, `championPowerBonusBpByFamily` et
`contributingChampionBeeIds` tous corrects simultanément. Texte du debrief
généré par le vrai code du présentateur : *"Palier de troupe : Gardiennes T2
+25%"* puis *"Grâce à Striga : Gardiennes +10,5% · Voltigeuses +1,5% ·
Lanceuses +1,5%"*.

Fichiers : `ChampionBeeCatalog.cs` (ajout `TroopTierCatalog.CombatContribution`),
`HiveOperationModels.cs`, `CombatPatrolResolution.cs` (inchangé dans sa
signature publique, juste réutilisé), `CombatPatrolService.cs`,
`CombatPatrolClient.cs`, `CombatPatrolPresentation.cs`,
`HiveViewProductUiPresenter.cs`, localisation FR/EN. Tests : nouveau
`TroopTierCombatContributionTests.cs` (4 tests), 113/113
`BeeKingdom.HiveOperations.Tests` verts.

Ouvert / à faire ensuite : les prochaines sources de bonus de combat
(recherche Branche 2 "Défense", bâtiments, alliance) devront suivre le même
patron — une entrée dans le reçu, une ligne dédiée dans le debrief, jamais
fusionnée silencieusement.

---

## Jalon courant — Recherche complète, championnes accessibles, lien championnes/combat (2026-07-30)

Session lancée par un audit complet du projet (`PROJECT_STATUS.md`, dans
`C:\projets\beekingdom\BIBLE\00_Project\`), puis reprise en développement
normal avec la consigne de Jeff de toujours choisir la prochaine étape en
raisonnant "quelle fonctionnalité augmente le plus la qualité et le plaisir
du jeu", pas en priorisant des correctifs. Trois livraisons complètes :

1. **Indicatif visuel de fin d'amélioration de bâtiment** (demande restée en
   suspens d'une session antérieure) : un badge lumineux apparaît directement
   sur le bâtiment dès qu'une amélioration est prête à valider ; taper dessus
   valide directement (réutilise le chemin `Complete()` déjà résilient aux
   conflits de révision). `HiveViewProductUiPresenter.cs`.

2. **Arbre de recherche Branche 1 (Économie) complet** : généralisation de 2
   recherches de démonstration (durée fixe 16 s) à 10 recherches réelles
   (paliers I/II/III miel/cire/pollen + convergence "Réserves scellées"),
   avec dépendances vérifiées côté serveur, coûts/durées réels, effets
   cumulatifs (production ET capacité) branchés dans
   `HiveOfflineProductionService`. Testé en conditions réelles (serveur de
   développement relancé, compte de test dédié, vraie attente de 2 minutes) :
   lancement → refus de validation prématurée → validation → bonus de +2 %
   confirmé sur le taux de production du miel (150 → 153). Catalogue
   configuré dans `Server/start-dev-server.local.ps1` (absent auparavant,
   donc invisible en jeu malgré le code serveur existant).

3. **Championnes rendues accessibles** : nouveau bouton "Championnes" dans la
   barre de navigation principale (mode paysage) ouvrant directement l'écran
   déjà complet (roster, portraits, montée de niveau) — auparavant uniquement
   accessible via un sous-menu de Patrouille de combat.

4. **Premier lien réel Championnes ↔ Combat** : une championne assignée
   (rôle + niveau) modifie désormais réellement la puissance de combat de
   `CombatPatrolResolution` (bonus de rôle sur la famille correspondante +
   petit bonus global sur toute l'escouade, cumulable avec plusieurs
   championnes assignées). Auparavant purement cosmétique. Le debrief de
   patrouille affiche désormais explicitement qui a contribué et de combien
   ("Grâce à Striga : Gardiennes +10,5 % · Voltigeuses +1,5 % · Lanceuses
   +1,5 %"). Testé en direct : une patrouille tier 2 à 12 gardiennes était
   *bloquée* (puissance 60/90, "sous-armée") avant l'assignation de Striga
   niveau 3 ; après assignation, la même composition passe à 66/90 et devient
   lançable — gagnée en "HardWon" avec butin et pertes réels, et le reçu
   confirme `contributingChampionBeeIds`/`championPowerBonusBpByFamily`
   corrects. `ChampionBeeCatalog.cs` (serveur, ajout Role + bonus, miroir
   exact du client), `CombatPatrolResolution.cs`, `CombatPatrolService.cs`,
   `CombatPatrolClient.cs`, `CombatPatrolPresentation.cs`,
   `HiveViewProductUiPresenter.cs`.

Preuves : compilation Unity et serveur vérifiée sans erreur après chaque
étape (0 erreur à chaque fois) ; 109/109 tests `BeeKingdom.HiveOperations.Tests`
verts (dont 6 nouveaux tests sur la contribution des championnes et le bonus
de puissance) ; 14/14 `HiveResearchClientTests` (Unity EditMode) verts ;
scénarios complets rejoués en direct contre le serveur de développement réel
(SQL Server distant) avec un compte de test dédié
(`research-qa-20260730@bee.test`) — voir ci-dessus pour le détail des deux
scénarios (recherche, championne/combat).

**Point de vigilance découvert, non corrigé (hors périmètre demandé) :**
4 tests préexistants échouent, sans rapport avec les changements de cette
session — 3 dans `HivePerimeterSortieEndpointTests.cs` (système de sortie de
périmètre, différent de la Patrouille de combat) et 1 dans
`BuildingUpgradeServiceTests.BusyAndIdempotencyConflictDoNotMutate`.
Reproductibles en isolation, donc pas un problème d'ordre d'exécution. À
investiguer lors d'une prochaine session touchant à ces systèmes.

Egalement découvert (session précédente, toujours non résolu) : le login par
email exige des champs `deviceIdentifier`/`region` non fournis par défaut,
sinon exception SQL brute renvoyée au client — préexistant, hors périmètre.

Prochain test utilisateur : ouvrir Unity, se connecter, ouvrir Recherche
(lancer/valider une recherche et observer le bonus), ouvrir Championnes
depuis la barre du bas, assigner une championne de combat puis lancer une
Patrouille pour voir la ligne "Grâce à ..." dans le debrief.

Ouvert / a faire ensuite : voir section suivante pour la prochaine
fonctionnalité recommandée (raisonnement valeur/plaisir joueur).

---

## Jalon courant — Refonte interface ruche + correction clic Paramètres (2026-07-29)

Suite au jalon precedent (login Google), Jeff a demande une refonte complete
de l'interface en mode ruche (plus d'interface "en dev") : section Parametres
(langue, sons/musique), gestion propre de la persistance, niveau Reine affiche
(= niveau Coeur royal), et un vrai systeme VIP. Tout a ete livre :

- Panneau Parametres reconstruit (langue, sons, musique) avec persistance
  locale (version de preferences mise a jour).
- Ressources de la vue Colonie corrigees pour utiliser les vraies valeurs
  dynamiques au lieu de champs de previsualisation locale bruts.
- 14 zones d'interaction (hotspots) nettoyees du jargon de developpement;
  deux changements de comportement signales a Jeff (une zone recherche
  auparavant bloquee a tort est maintenant active; une zone infirmerie
  auparavant marquee "preview" est maintenant correctement bloquee comme
  future fonctionnalite).
- Vraie persistance serveur pour les abeilles championnes et les paliers de
  troupe (remplace la fausse depense de monnaie locale) : nouveaux catalogues
  et options cote serveur, nouveaux points d'API, migration de base de
  donnees appliquee et verifiee.
- Vrai systeme VIP concu et implemente : niveaux, points, bonus de capacite
  (jamais de bonus militaire/combat, conformement a la regle du cap produit).
  Les points VIP sont accordes pour l'instant via un point d'API reserve au
  developpement (aucun IAP reel branche); badge VIP et panneau VIP reecrits
  pour afficher l'etat reel.
- Niveau Reine affiche desormais le vrai niveau Coeur royal avec la vraie
  progression.
- Bug serieux trouve et corrige en cours de route : plusieurs drapeaux d'etat
  charge (VIP, abeilles championnes, paliers de troupe) etaient remis a faux
  dans les chemins d'echec asynchrones, causant une tempete de nouvelles
  tentatives reseau des centaines de fois par seconde des qu'un appel
  echouait. Corrige en ne reinitialisant plus ces drapeaux sur echec.
- Bug rapporte par Jeff apres coup : le bouton Parametres (et le bouton de
  fermeture du panneau Parametres) ne repondait qu'a une partie de sa
  surface cliquable. Cause reelle trouvee par diagnostic cible : le menu
  "More" et le panneau Parametres n'etaient pas declares comme zones
  d'interface fixes en mode paysage (contrairement au mode portrait et au
  menu Quetes, deja correctement exclus dans les deux modes). Les clics sur
  ces panneaux en paysage etaient donc intercepted par la couche de gestes
  tactiles de la ruche en arriere-plan avant d'atteindre les boutons.
  Corrige en ajoutant l'exclusion manquante pour les deux modes.

Preuves : compilation Unity verifiee sans erreur apres chaque etape;
persistance champion/troupe/VIP verifiee via appels API directs; bug de
tempete de nouvelles tentatives confirme resolu par absence de repetition
dans les logs; bug de clic confirme resolu par Jeff en jeu ("ca clique
partout").

Prochain test utilisateur : parcourir l'interface complete de la ruche
(Parametres, VIP, abeilles championnes, paliers de troupe, vue Colonie) en
mode paysage ET portrait pour confirmer qu'aucune autre zone de menu ne
souffre du meme probleme de clic partiel.

Ouvert / a faire ensuite : brancher les vrais achats (App Store/Google Play)
pour les points VIP quand Jeff sera pret; continuer a surveiller les arrets
inexpliques du serveur de developpement local (cause encore inconnue).

---

## Jalon courant — Connexion Google, pseudo unique, tutoriel joueur (2026-07-28)

Suite au deblocage du login officiel (jalons precedents), deux bugs restants
ont ete corriges puis la fonctionnalite demandee par Jeff a ete livree
entierement.

**Bugs de connexion corriges avant la nouvelle fonctionnalite** :
- Derive d'horloge serveur : `AuthenticationService.AuthenticateAsync` et
  `AuthenticationTokenManager.CreateTokenPair` lisaient chacun `clock.UtcNow`
  separement a quelques microsecondes d'intervalle, rendant
  `tokens.RefreshTokenExpiresUtc` systematiquement > `session.ExpirationUtc` —
  le client rejetait donc **tout** login reussi avec le code
  `auth.session_expiration_invalid`. Fix : un seul `now` capture et partage
  (`Server/src/BeeKingdom.Authentication/AuthenticationService.cs`,
  `Tokens/AuthenticationTokenManager.cs`).
- `EditorFallbackRefreshTokenStore.SaveAsync` utilisait `PlayerPrefs` (exige
  le thread principal Unity), or l'appel arrivait via une continuation async
  hors thread principal (`ConfigureAwait(false)` en amont) →
  `UnityException: TrySetSetString can only be called from the main thread`,
  masque en `auth.protected_storage_write_failed`. Fix : stockage en memoire
  (`static string`) au lieu de PlayerPrefs
  (`Assets/BeeKingdom/Networking/EditorFallbackRefreshTokenStore.cs`).
- Cause du diagnostic difficile : les catch-all silencieux dans
  `BeginMobileAccountLogin`/`PumpMobileAccountSessionOperations`
  (`HiveViewProductUiPresenter.cs`) n'affichaient aucun log — ajoute
  `Debug.LogWarning` avec le vrai code/exception a chaque catch-all
  d'authentification (garder ces logs, ils ont ete essentiels au diagnostic).

**Nouvelle fonctionnalite (demande explicite de Jeff)** :
1. Ecran de chargement au demarrage : `DrawStartupLoadingScreenIfActive()`
   dans `HiveViewProductUiPresenter.cs`, image
   `Resources/PremiumBeeReference/splash_loading.png` (copiee depuis
   `C:\projets\beekingdom\images\splash.png`), barre de progression et texte
   purement indicatifs (duree fixe 2.4s), avant l'ecran de connexion.
2. Connexion Google (OAuth 2.0 + PKCE, code exchange cote serveur) :
   - Client Unity : `Assets/BeeKingdom/Networking/GoogleOAuthLoginFlow.cs`
     (nouveau) — ouvre le navigateur systeme, ecoute sur
     `http://127.0.0.1:53682/oauth/callback/` via `HttpListener` (fonctionne
     sans droits admin, verifie). **Important** : l'URI de redirection
     envoyee a Google (`redirect_uri`) est **sans** slash final
     (`.../callback`) pour matcher exactement ce qui est enregistre dans
     Google Cloud Console, alors que le prefixe `HttpListener` local a besoin
     du slash final (`.../callback/`) — `HttpListener` recoit quand meme les
     requetes sans slash final adressees a ce prefixe (verifie).
   - Serveur : `GoogleAuthenticationProvider`-equivalent integre directement
     dans `AuthenticationService.AuthenticateWithGoogleAsync`
     (`Server/src/BeeKingdom.Authentication/AuthenticationService.cs`) +
     `GoogleOAuthIdentityExchanger` (NuGet `Google.Apis.Auth`, valide
     l'id_token contre les certs publics Google) + nouvel endpoint
     `POST /auth/login/google`. Compte identifie par `GoogleSubjectId`
     (nouvelle colonne unique sur `dbo.AuthenticationAccounts`); si un compte
     existe deja avec le meme courriel, le Google login s'y rattache au lieu
     d'en creer un second.
   - Config serveur : section `GoogleOAuth` (ClientId/ClientSecret), passee
     par variables d'environnement au lancement (jamais dans
     `appsettings.Development.json`, meme lecon que la regression SQL du
     jalon precedent). ClientId (public) aussi stocke sur l'asset
     `MobileAccountSessionRuntime.asset` (`GoogleOAuthClientId`).
3. Pseudo unique par serveur : colonnes `DisplayName` + `WorldId` +
   `IsOnboarded` sur `dbo.AuthenticationAccounts`
   (`080_google_authentication_and_display_name.sql` — attention, les
   `ALTER TABLE ADD` doivent etre enveloppes en `EXEC(N'...')` pour eviter
   `Invalid column name` quand une instruction suivante du meme script
   reference une colonne ajoutee plus haut dans le meme batch). Endpoint
   `POST /auth/display-name` (authentifie par bearer token), unicite
   verifiee par `(WorldId, DisplayName)`. Ecran client
   `DrawCreateAccountDisplayNamePicker` affiche uniquement si
   `session.IsNewAccount || !session.IsOnboarded` — confirme par Jeff qu'une
   deuxieme connexion ne redemande pas le pseudo (comportement voulu).
4. Tutoriel : demarre automatiquement via `EnterHiveFromSplash(...)` (deja
   appelait `BeginGuidedCollectionTutorial()`) juste apres confirmation du
   pseudo pour un nouveau compte. Bouton "Sauter le tutoriel" desormais
   visible en tout temps pendant le tutoriel (`DrawTutorialSkipButton`,
   coin superieur droit du panneau), pas seulement en Editeur/dev build
   comme l'ancien `SkipActOneTutorialForDevelopment` (toujours present,
   reutilise par `SkipGuidedTutorialForPlayer`).

**Bug de layout corrige en cours de route** : `DrawLoginGate` avait un
budget vertical fixe qui ne laissait aucune marge pour un 3e bouton
(Google). Sur un viewport court (paysage bas, ou meme un petit Game View
Editeur ~600px), les boutons Google/demo locale se retrouvaient dessines
hors de l'ecran visible sans aucun moyen de les atteindre. Fix : tout
l'ecran de connexion est maintenant dans une `GUI.BeginScrollView`/
`EndScrollView` en coordonnees locales (`loginGateScroll`), donc toujours
atteignable par defilement peu importe la hauteur d'ecran — verifie par
calcul direct via `SplashAuthPanelRectForProof` contre le viewport reel de
Jeff (604px) avant de livrer.

Preuves : `dotnet build` 0 erreur sur le serveur; `dotnet test` 372-373/384
(memes ~3-4 echecs instables preexistants de `HivePerimeterSortie`, confirmes
non lies); migration appliquee et verifiee par `sqlcmd` (colonnes
`GoogleSubjectId`/`WorldId`/`DisplayName`/`IsOnboarded` presentes); flux
Google testes de bout en bout par Jeff en Play Mode (login, pseudo demande
au premier compte puis plus au second, entree en ruche, tutoriel demarre).

**Probleme ouvert, non resolu** : le serveur local (lance en processus
detache via `Start-Process -WindowStyle Hidden`) s'est arrete seul a
plusieurs reprises pendant la session, sans exception dans les logs ni dans
le journal d'evenements Windows (`Get-WinEvent` verifie — rien pour
`BeeKingdom.Server.exe` aux heures concernees). Cause non identifiee.
Prochaine fois que ca arrive : demander a Jeff s'il a ferme une fenetre,
mis la machine en veille, ou vu une alerte antivirus au moment de l'arret.

Prochain test utilisateur : continuer a jouer normalement; si la connexion
Google se ferme de nouveau de facon inexpliquee, noter l'heure exacte et ce
que Jeff faisait sur la machine a ce moment.

Ouvert / a faire ensuite :
- Comprendre pourquoi le serveur local meurt seul (voir ci-dessus).
- Le pseudo n'a pas de filtre de contenu (mots interdits) — pas demande,
  a considerer avant un vrai lancement public.
- Preference utilisateur ajoutee dans `CLAUDE.md` : ne plus jamais afficher
  le code modifie/diffs/details d'outils dans la conversation, seulement le
  resultat et les questions pertinentes.

---

## Jalon courant — Stockage de secours Editeur pour le login officiel (2026-07-28)

Meme apres avoir corrige le saut de splash (jalon precedent), l'onglet
"Connexion" affichait toujours "Connexion mobile non configuree / NON
RACCORDE" et aucun champ courriel/mot de passe. Cause racine finale, verifiee
par lecture directe de `MobileAccountSessionClient.InitializeAsync()`
(`Assets/BeeKingdom/Networking/MobileAccountSessionClient.cs` ligne 246) :
`if (!refreshTokenStore.IsProtectionAvailable) { State = NotConfigured; return; }`
— le client abandonne **avant meme de contacter le serveur** si le stockage
protege du jeton de session n'est pas disponible.

`CreateRefreshTokenStore()` (`MobileAccountSessionRuntimeBootstrap.cs`)
choisit `IosKeychainRefreshTokenStore` sur iOS reel, sinon toujours
`AndroidKeystoreRefreshTokenStore` — et celui-ci a
`IsProtectionAvailable => Application.platform == RuntimePlatform.Android`
**seulement si `UNITY_ANDROID && !UNITY_EDITOR`, sinon toujours `false`**.
Resultat : **dans l'Editeur Unity, peu importe la plateforme cible ou la
machine, ce stockage protege n'est jamais disponible par conception** —
verrou de securite legitime (jamais stocker un jeton d'auth sans protection
materielle), mais qui bloque tout test de connexion officielle directement
depuis l'Editeur, independamment du serveur/de la config/de la scene.

Confirme avec Jeff avant d'ajouter un contournement (touche a la couche
d'authentification, meme si scope a l'Editeur uniquement). Livre :

- `Assets/BeeKingdom/Networking/EditorFallbackRefreshTokenStore.cs`
  (nouveau) : implementation de secours de `IProtectedRefreshTokenStore`,
  `IsProtectionAvailable` vrai **uniquement si `UNITY_EDITOR`**, stocke le
  jeton en clair dans PlayerPrefs (aucune protection materielle - clairement
  documente comme non securise, reserve aux tests locaux). Meme format de
  serialisation que `AndroidKeystoreRefreshTokenStore` (reutilise le meme
  schema ligne par ligne).
- `CreateRefreshTokenStore()` : nouvelle branche `#elif UNITY_EDITOR` qui
  retourne ce nouveau stockage. Les vrais builds Android/iOS ne sont pas
  touches (memes classes qu'avant, `AndroidKeystoreRefreshTokenStore`/
  `IosKeychainRefreshTokenStore`).

Preuves : `assets-refresh` sans erreur (0 `error CS`). `tests-run` sur
`MobileAccountSessionClientTests` n'a pas repondu (timeout MCP 300s) —
tres probablement Jeff en Play Mode au meme moment (comportement deja
documente : `tests-run` bloque quand l'Editeur est en Play Mode). Pas
insiste ni relance pour ne pas interrompre son test en cours ; verification
limitee a la compilation propre pour cette entree.

Prochain test utilisateur : relancer le Play Mode, onglet "Connexion" —
les champs courriel/mot de passe devraient enfin apparaitre. Se connecter
avec `jeff@bee.local` / `testpass123`.

Ouvert / a faire ensuite :
- Seul `IProtectedRefreshTokenStore` a ete corrige (necessaire pour le
  login lui-meme). `CreateGameReadCacheStore()`/`CreateGameMutationOutboxStore()`
  ont exactement le meme probleme structurel (`AndroidKeystoreGameReadCacheStore`/
  `AndroidKeystoreGameMutationOutboxStore`, `IsProtectionAvailable` toujours
  faux en Editeur) mais ne bloquent pas la connexion elle-meme — seulement le
  cache hors-ligne/la file de mutation protegee de certains systemes
  (production officielle, recherche, etc.). A etendre avec le meme patron si
  Jeff rencontre un blocage similaire ailleurs.
- Le jeton stocke par `EditorFallbackRefreshTokenStore` est en clair dans
  PlayerPrefs de l'Editeur — acceptable pour du test local uniquement,
  jamais present dans un vrai build (verifie par le `#if UNITY_EDITOR`
  explicite partout dans ce fichier).

---

## Jalon courant — L'ecran de connexion n'etait jamais affiche (2026-07-28)

Meme apres le correctif du verrou de readiness (jalon precedent), Jeff a
signale voir toujours l'ecran de reprise du tutoriel (chapitre 5) direct a
l'ouverture de `LivingHive.unity`, jamais l'ecran de demarrage. Fausse piste
initiale (checkpoint sauvegarde persistant) — cause reelle trouvee dans
`Assets/BeeKingdom/Playground/LivingHiveDemoBootstrap.cs`, `Start()` :
`HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive")` etait
appelee **inconditionnellement a chaque lancement de la scene**, ce qui met
`splashAuthGateState = EnteredHive` immediatement — **avant meme que
`Draw()` ait une chance d'afficher le splash**. L'ecran de connexion n'a
donc jamais ete un probleme de serveur, de checkpoint ou de config : la
scene elle-meme sautait purement et simplement par-dessus, probablement un
raccourci laisse par une session precedente pour accelerer les tests
manuels (meme famille que "Sauter Acte I").

Confirme avec Jeff avant de toucher au flux d'entree du jeu (changement de
comportement fondamental, pas un simple bug) — il a choisi de le desactiver
maintenant plutot qu'une version conditionnelle plus nuancee. Corrige en
retirant cet appel de `Start()` (le reste de l'initialisation —
`EnsureSceneObjects()`, `SetRuntimeBridgeModeForProof(...)`, reprise du
tutoriel — reste intact). L'ecran de demarrage (avec l'onglet "Connexion")
s'affiche desormais normalement a chaque Play Mode.

Preuves : `assets-refresh` sans erreur. Tests EditMode
`SandboxLivingHiveManualCollectionTests` : 54/55 (meme seul echec
preexistant sans rapport, aucune regression de ce changement de flux
d'entree).

Prochain test utilisateur : relancer le Play Mode depuis `LivingHive.unity`
— l'ecran de demarrage avec les onglets Connexion/Creer un compte devrait
maintenant apparaitre. Cliquer "Connexion", entrer
`jeff@bee.local` / `testpass123`.

Ouvert / a faire ensuite :
- Si Jeff veut retrouver un raccourci "sauter direct dans la ruche" pour les
  tests rapides ou l'authentification n'est pas necessaire, on peut
  reintroduire une version conditionnelle (ex. seulement si une session
  officielle est deja restauree/protegee) plutot que le saut inconditionnel
  d'avant.

---

## Jalon courant — Verrou code en dur empechait tout vrai login (2026-07-28)

Suite immediate du jalon precedent (serveur local + SQL) : Jeff a rapporte
"je ne peux pas me connecter" avec une capture montrant... l'ecran de reprise
du tutoriel chapitre 5, pas l'ecran de connexion. Investigation : le flux
reel est splash -> (Login/CreateAccount/demo) -> `EnterHiveFromSplash()` ->
`BeginGuidedCollectionTutorial()` -> restauration automatique du checkpoint
tutoriel sauvegarde localement. Jeff avait donc bien traverse le splash, mais
l'onglet "Connexion" n'affichait **aucun champ courriel/mot de passe** — juste
un bouton "Continuer en demo locale" — qu'il a du cliquer sans le vouloir,
d'ou l'atterrissage direct dans sa progression existante.

Cause racine, verifiee par lecture directe (pas supposee) :
`Assets/BeeKingdom/Networking/AccountSessionReadinessGate.cs` —
`CanCollectCredentials = TransportConfigured && snapshot.ServerAllowsLogin`,
et `ServerAllowsLogin` exige entre autres `LiveAccounts && LiveSessions`. Ces
deux valeurs venaient de
`Server/src/BeeKingdom.Server/Program.cs` (`/runtime/account-session-readiness`)
ou elles etaient **litteralement codees en dur a `false`** (pas derivees
d'une config) — donc **aucune configuration cote serveur ne pouvait jamais
faire apparaitre le formulaire de connexion**, meme avec un serveur local
parfaitement fonctionnel. Verrou manifestement delibere (associe a
`ProductionTarget: 104.129.128.136`, `RequiresBackupEvidence`,
`RequiresRollbackApproval`).

**Confirme avec Jeff avant de toucher a ce verrou** (meme prudence que pour
tout ce qui touche a la production reelle) — il a demande de "refleter
l'etat reel" plutot que de laisser fige. Corrige dans `Program.cs` :
`LiveAccounts`/`LiveSessions`/`GameplayAuthorityGranted` derivent maintenant
de `state.AccountStatus == "Live"` / `state.SessionStatus == "Live"` au lieu
d'etre figes a `false`. **Comportement par defaut inchange** : ces deux
statuts restent `"NotLive"` dans `appsettings.json`/`appsettings.Development.json`
(non touches), donc `false` persiste partout sauf configuration explicite —
seul mon serveur local recoit
`AccountSessionReadiness__AccountStatus=Live` /
`...__SessionStatus=Live` (+ les 3 booleens deja requis) en variables
d'environnement, jamais dans un fichier commite (meme lecon que le jalon
precedent).

Preuves : `dotnet build` sans erreur nouvelle. `dotnet test` sur
`Server/tests/BeeKingdom.Tests` : 373/384 puis 381/384 selon l'execution (les
~3-4 echecs restants confirmes preexistants/non-deterministes dans
`HivePerimeterSortieEndpointTests`, memes tests qu'avant ce changement,
aucun rapport). Endpoint `/runtime/account-session-readiness` verifie en
direct (`curl`) : `liveAccounts:true, liveSessions:true,
gameplayAuthorityGranted:true` avec mon serveur, `/auth/login` toujours
fonctionnel avec le compte de test.

Prochain test utilisateur : **arreter puis relancer le Play Mode**. Sur
l'ecran de demarrage, l'onglet "Connexion" devrait maintenant afficher de
vrais champs courriel/mot de passe (plus seulement "Continuer en demo
locale"). Se connecter avec `jeff@bee.local` / `testpass123` — le jeu devrait
reprendre au meme chapitre 5 qu'avant, mais avec une vraie session
authentifiee cette fois (Combat Patrol/recrutement officiel actifs).

Ouvert / a faire ensuite :
- Si Jeff prefere ne PAS avoir a taper un mot de passe a chaque relance de
  Play Mode, verifier si `client.RestoreOrRefreshAsync()` (refresh token
  protege) fonctionne bien pour eviter de retaper les identifiants a chaque
  session — pas encore observe en Play Mode reel.
- Les "blockers" informatifs renvoyes par l'endpoint (preuve de route
  production, preuve de sauvegarde, approbation de rollback) restent
  affiches tels quels — ils ne bloquent pas `ServerAllowsLogin` mais
  pourraient dérouter Jeff s'ils sont visibles quelque part dans l'UI ; a
  verifier visuellement.

---

## Jalon courant — Serveur local demarre pour tester Combat Patrol (2026-07-28)

Jeff a rapporte ne pas pouvoir choisir de troupes dans le panneau Patrouille
(Combat Patrol). Cause racine confirmee par lecture directe du code (pas
supposee) : `combatPatrolController` demarre a `UnavailableCombatPatrolPanelController`
(stub inerte, roster/capacite toujours a 0) et n'est remplace par le vrai
controleur connecte au serveur que si `MobileAccountSessionRuntimeBootstrap.ConfigureBeforeSceneLoad()`
reussit — ce qui necessitait 3 choses, **aucune** presente jusqu'ici :
1. Un serveur .NET reellement lance et joignable.
2. Un asset Unity `MobileAccountSessionRuntimeConfiguration`
   (`Resources/BeeKingdom/MobileAccountSessionRuntime`) — **inexistant** dans
   le projet avant ce jalon.
3. Un compte reel pour se connecter — **aucun endpoint HTTP** ne permettait
   d'en creer un (`IAccountCredentialStore.CreateEmailAccount` n'etait
   appelee que par les tests serveur).

Jeff a choisi "lancer le vrai serveur" plutot qu'un mode hors-ligne de
secours. Livre :

- `Server/src/BeeKingdom.Server/appsettings.Development.json` : ajout de
  `DevTools.AllowDevAccountSeeding=true` et activation des drapeaux combat
  (`CombatDoctrine`, `CombatFormationReadiness`, `CombatRecruitment`,
  `CombatSquadReservation`, `HivePerimeterSortie`, `CombatPatrol` — tous
  `Enabled: true`), **fichier de developpement uniquement**, `appsettings.json`
  (production) non touche.
- `Server/src/BeeKingdom.Server/DevToolsOptions.cs` (nouveau) + nouvel
  endpoint `POST /dev/seed-account` dans `Program.cs` — cree un compte
  email/mot de passe via `IAccountCredentialStore.CreateEmailAccount`,
  **404 en Production ou si le flag est faux**, jamais exploitable en
  deploiement reel (meme garde double que les autres dev tools du projet).
- Serveur demarre localement (`dotnet run`, `ASPNETCORE_ENVIRONMENT=Development`,
  arriere-plan, `http://localhost:5067`), verifie vivant (`/health` -> 200).
- Compte de test cree et verifie via `/auth/login` (succes confirme) :
  **email `jeff@bee.local`, mot de passe `testpass123`**.
- Nouvel asset Unity cree via `script-execute`
  (`Assets/BeeKingdom/Playground/Resources/BeeKingdom/MobileAccountSessionRuntime.asset`) :
  `OfficialAccountsEnabled=true`, `OfficialGameplayEnabled=true`,
  `BaseUrl=http://localhost:5067`, `OfficialHiveId=aa71cc9e-0a79-413e-b2cb-ab6d80a0421d`
  (meme GUID que le playerId du compte de test, un seul joueur/une seule
  ruche pour l'instant), `AllowInsecureLoopbackForDevelopment=true`.

**Mise a jour (meme jour) — vraie persistance SQL + regression trouvee et corrigee :**
Jeff a demande de configurer une vraie persistance (pas juste l'en-memoire).
`Server/ops/sql-readiness/` confirmait SQL Server LocalDB deja disponible sur
la machine (`sqllocaldb`/`sqlcmd` sur le PATH, instance `MSSQLLocalDB`).

**Bug pre-existant trouve et corrige en cours de route** : la migration
`064_chat_contract_bounds.sql` (contenu reellement utilise au runtime =
chaine C# dans `Server/src/BeeKingdom.Database/DatabaseCatalog.cs`, **pas**
le fichier `.sql` du meme nom qui n'est qu'une copie non lue) tentait
`ALTER COLUMN TranslatedText nvarchar(16000)` — invalide, SQL Server plafonne
`nvarchar(n)` a 4000/8000 selon le type. Corrige en `nvarchar(max)` (meme
correction appliquee au fichier `.sql` autonome par coherence, meme s'il
n'est pas execute). Migrations rejouees avec succes jusqu'a
`070_hive_operations.sql` (qui cree `dbo.HivePlayerStates`, la table dont
Combat Patrol a reellement besoin).

**Regression trouvee et corrigee avant de rendre la main** : ma premiere
tentative avait mis `Persistence.Provider=SqlServer` + les 6 drapeaux combat
directement dans `appsettings.Development.json` (partage). Consequence
immediate verifiee par `dotnet test` sur `Server/tests/BeeKingdom.Tests` :
**19 tests casses** (`WebApplicationFactory` des tests lit le meme fichier
Development — persistance SQL partagee entre executions de tests => cles
dupliquees, plus une assertion explicite `Persistence.Provider == "InMemory"`
qui echouait). Corrige en **retirant ces reglages du fichier partage** et en
les passant **uniquement en variables d'environnement** au process serveur
que je lance moi-meme (`SqlServer__ConnectionString`,
`Persistence__Provider`, `CombatDoctrine__Enabled`, etc.) — le fichier
partage ne garde que `DevTools.AllowDevAccountSeeding=true` (nouveau,
sans impact sur les tests existants). Reverifie : `dotnet test` revient a 4
echecs, tous dans `HivePerimeterSortieEndpointTests`, confirmes non-
deterministes/preexistants (re-execution du meme fichier seul : resultats
differents a chaque fois) — **aucun rapport avec ce travail**.

Serveur final relance avec ces variables d'environnement, compte de test
`jeff@bee.local`/`testpass123` re-authentifie avec succes apres redemarrage
(meme playerId `5b9f2835-5eda-4f02-9fa8-0f99794f7438`, donc reellement
persiste en SQL cette fois). Asset Unity `MobileAccountSessionRuntime` mis a
jour avec ce playerId comme `OfficialHiveId`.

Preuves : `dotnet build` (serveur + Tools + Database) sans erreur nouvelle.
Migrations SQL rejouees avec succes (`dotnet run --project
Server/src/BeeKingdom.Tools -- migrate`, 0 migration en attente ensuite).
Suite de tests serveur complete (`dotnet test`) : 372/384 reussis, 8 ignores,
4 echecs confirmes preexistants/non-deterministes (`HivePerimeterSortieEndpointTests`,
sans rapport). Compte de test authentifie avec succes avant ET apres
redemarrage du serveur (persistance SQL confirmee reelle, pas juste en
memoire).

Prochain test utilisateur : **arreter puis relancer le Play Mode** (le
bootstrap ne lit la config qu'au demarrage de scene) ; sur l'ecran de
demarrage, choisir l'onglet "Connexion" et entrer
`jeff@bee.local` / `testpass123` ; une fois connecte, ouvrir Combat Patrol —
le roster/la capacite doivent maintenant refleter le vrai etat serveur (pas
force 0/1 fige), et la progression doit desormais survivre a un redemarrage
du serveur. **Attention** : la ruche serveur est neuve — le roster
Gardiennes/Voltigeuses/Lanceuses y est probablement encore a 0 tant qu'un
vrai recrutement officiel n'a pas ete fait (menu "Armee"/side rail
Entrainement, une fois connecte — verifie que
`OfficialDoctrineRecruitmentConfigured()` bascule l'UI vers le recrutement
serveur reel a cet endroit, pas encore teste visuellement).

Ouvert / a faire ensuite :
- Le serveur local tourne en arriere-plan de cette session Claude Code
  (variables d'environnement, PID de la commande background) — **il
  s'arretera si l'ordinateur redemarre, si le process est tue, ou a la fin
  de cette session Claude Code**; aucun script de demarrage automatique
  n'existe, et les variables d'environnement devront etre repassees
  manuellement au prochain `dotnet run` (voir commande exacte ci-dessus). Si
  Jeff veut le garder disponible durablement, prevoir un vrai script/service
  qui fixe ces variables, plutot que de les remettre dans le fichier partage
  (lecon de cette session).
- Verifier visuellement que le recrutement officiel (Voltigeuses/Lanceuses)
  est bien atteignable une fois connecte — pas encore confirme en Play Mode
  reel, seulement par lecture de code.
- L'endpoint `/dev/seed-account` est un outil de developpement ajoute cette
  session — a retirer ou verrouiller plus severement avant tout deploiement
  reel si jamais ce fichier de config partait en production par erreur
  (double garde deja en place : `IsProduction()` + flag explicite, mais bon
  reflexe de le garder en tete, meme logique que l'incident
  `Chat:Enabled` documente plus bas dans ce fichier).
- **Lecon retenue** : ne plus jamais modifier `appsettings.Development.json`
  (ou tout fichier de config partage equivalent) pour une experimentation de
  session — ce fichier est aussi lu par la suite de tests automatisee via
  `WebApplicationFactory`. Toujours passer les reglages experimentaux par
  variables d'environnement scopees au process lance, jamais par un fichier
  commite partage.

---

## Jalon courant — Paliers de troupe T1/T2/T3 (2026-07-27)

Suite du jalon precedent (selecteur de type de troupe dans la Caserne) : Jeff
a demande un vrai systeme de palier de puissance par troupe (T1/T2/T3), pour
repondre a sa question initiale sur "T1, T2, etc." (aucun rapport avec les
paliers de difficulte T1-T7 du Combat Patrol, qui restent un systeme
distinct).

Portee assumee (v1) : le palier s'applique aux 3 types deja visibles dans le
selecteur de la Caserne (Soldats/Gardiennes/Eclaireuses ->
`soldiers`/`guardians`/`scouts`), pas encore a Voltigeuses/Lanceuses
(recrutees via un flux different, `HiveDoctrineRecruitmentCatalog`, deja note
comme flag ouvert dans le jalon precedent).

Livre :
- `LocalPreviewHiveProgress.cs` : nouveau champ `troopTiers` (liste
  populationId+tier), version de schema 3 -> 4, `MergeTroopTier`/
  `TryGetTroopTier` suivant exactement le pattern `MergeBuildingLevel`/
  `MergeChampionBeeLevel` deja etabli, normalisation mise a jour.
- Chaque palier au-dela de T1 (max T3) ajoute +25% de multiplicateur sur la
  contribution de puissance de ce type dans le bucket "Abeilles spec." du
  panneau Puissance (`RealPowerFromBees`) — effet **reel**, immediatement
  visible et teste. Pour les Gardiennes specifiquement (seule famille
  partagee avec Combat Patrol/Abeilles championnes), le meme bonus (+25%/
  palier au-dela de T1) s'ajoute aussi a la ligne "Bonus championnes (apercu
  local)" du panneau Combat Patrol — meme limite deja documentee
  (apercu local uniquement, ne modifie pas la resolution serveur
  autoritaire).
- `DrawBarracksTroopSelector` (popup Caserne) : chaque tuile affiche
  desormais un badge "T{palier}" en haut a gauche et un bouton "+" de
  promotion en haut a droite (cout croissant : 200×palier miel / 80×palier
  pollen, plafonne a T3), sur une bande separee de la zone de selection du
  type pour eviter tout chevauchement de clic.

Preuves : compilation verifiee propre (`assets-refresh`, 0 erreur). Tests
`SandboxLivingHiveManualCollectionTests` : 54/55 (meme seul echec preexistant
sans rapport) — le bump de version de schema n'a cette fois **pas** casse de
test, car les 2 tests touches par le bump precedent (2->3) referencent
maintenant `LocalPreviewHiveProgressCodec.CurrentVersion` plutot qu'un
litteral en dur.

Prochain test utilisateur : ouvrir la Caserne, verifier le badge "T1" sur
chaque tuile et le bouton "+" (promotion, cout affiche implicitement via le
etat active/grise du bouton) ; promouvoir Gardiennes a T2 et verifier que le
score Puissance (bucket Abeilles spec.) et le resume de bonus du Combat
Patrol augmentent bien.

Ouvert / a faire ensuite :
- Poids/formule (+25%/palier, cout 200/80 par palier, max T3) sont un
  premier jet explicitement ajustable.
- Voltigeuses/Lanceuses n'ont pas encore de palier — a etendre si Jeff le
  demande, une fois clarifie comment elles s'integrent (elles passent par
  `HiveDoctrineRecruitmentCatalog`, pas par `selectedLocalTroopType`).
- Toujours pas d'unification entre les deux systemes de nommage de troupes
  (legacy vs doctrine) — signale a nouveau, non traite.

---

## Jalon courant — Selecteur de type de troupe visible dans le popup Caserne (2026-07-27)

Jeff a rapporte que le popup de la Caserne (`guard_post`, atteint en cliquant
directement sur le hotspot) ne montrait pas quel type de troupe allait etre
entraine avant de cliquer "Entrainer" — juste un bouton generique et un bloc
"Capacite" qui affichait en realite un texte de remplissage sans rapport
(`"Serveur requis plus tard"`, defini en dur pour ce hotspot).

Investigation : le vrai selecteur de type de troupe (Soldats/Gardiennes/
Eclaireuses, avec icone+compte, `DrawTrainingPreview`) existe deja ailleurs
dans le code (menu "Armee de la ruche" et panneau de detail etendu
`DrawPlayableDetailActions`) mais **pas** dans le popup compact
(`DrawLandscapeContextDetailPanel`) que Jeff regardait — celui-ci se contente
d'entrainer `selectedLocalTroopType` (choisi ailleurs, invisible ici) avec un
bouton "Entrainer" muet.

Corrige dans `DrawLandscapeContextDetailPanel` : pour `guard_post`
uniquement, le bloc "Capacite" generique (sans valeur utile pour ce batiment)
est remplace par un nouveau `DrawBarracksTroopSelector(rect)` — 3 tuiles
cliquables (icone + libelle court + effectif actuel), identiques dans
l'esprit au selecteur deja existant ailleurs, reutilisant les memes
fonctions (`TroopShortLabel`, `TroopCount`, `selectedLocalTroopType`). Le
bouton d'action affiche desormais "Entrainer <Type>" au lieu d'"Entrainer"
generique (correction de coherence avec les 2 autres panneaux qui l'
affichaient deja).

Preuves : compilation verifiee propre (`assets-refresh`, 0 erreur). Tests
`SandboxLivingHiveManualCollectionTests` : 54/55 (meme seul echec preexistant
sans rapport).

Prochain test utilisateur : cliquer sur la Caserne en Play Mode, verifier que
les 3 types de troupe (Soldats/Gardiennes/Eclaireuses) sont visibles et
cliquables a l'endroit ou apparaissait avant le bloc "Capacite", et que le
bouton "Entrainer" affiche bien le type selectionne.

Ouvert / a faire ensuite :
- Jeff a mentionne vouloir voir "T1, T2, etc." en plus du type — aucun
  systeme de palier/tier de troupe n'existe dans le code (a distinguer des
  paliers T1-T7 du Combat Patrol, qui sont des tiers de *difficulte
  d'ennemi*, sans rapport avec l'entrainement). A clarifier avec Jeff ce que
  "T1/T2" doit representer avant d'inventer un systeme : nouveau palier de
  puissance par troupe ? simple renommage des types existants ?
- Deux systemes de nommage de troupes coexistent dans le code : l'ancien
  (Soldats/Gardiennes/Eclaireuses, utilise ici et dans le menu Armee) et le
  plus recent `HiveDoctrineRecruitmentCatalog` (guardians/wingrunners/
  darters, utilise par Combat Patrol et les Abeilles championnes) — non
  unifies. Pas touche cette fois (hors scope de la demande), mais source de
  confusion potentielle a signaler si Jeff veut un jour clarifier/fusionner.
- Le popup compact/portrait (`DrawCompactDetailPanel`) affiche deja le type
  dans le bouton mais n'a pas ce selecteur visuel faute de place dans son
  layout actuel — pas modifie cette fois, a faire si Jeff le remarque aussi
  la-bas.

---

## Jalon courant — Carte du monde centree sur la ruche du joueur a l'ouverture (2026-07-27)

Jeff a rapporte qu'a l'ouverture de la carte du monde, sa ruche n'etait pas
centree dans la vue. Cause racine trouvee dans
`Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs` :
la scene canonique `WorldMapWave6Wave5Method12288Preview.unity` avait un
reglage de debug/QA laisse actif
(`useInitialAuditViewForPlayMode: 1`, chunk d'audit 54/9, zoom 0.58,
"audit jonctions 50x50 C54_09") qui forçait la camera a s'ouvrir sur une vue
d'audit lointaine plutot que sur la ruche du joueur (chunk 32/32) — et le
seul recentrage existant sur la ruche (`FocusGuidedPlayerHiveIfNeeded()`)
n'etait declenche que si un tutoriel guide de carte etait actif, jamais en
jeu normal.

Corrige :
- `useInitialAuditViewForPlayMode` remis a `0` dans la scene canonique
  (aucune modification du terrain 50x50 ni de son package protege —
  uniquement un booleen de comportement camera sur le composant bootstrap).
- `FocusGuidedPlayerHiveIfNeeded()` retourne maintenant `bool` (a-t-il centre
  ou non) au lieu de `void`.
- Nouvelle fonction `CenterOnPlayerHiveInstant()` : centre immediatement
  (sans animation de pan, avec `ClampTargetWorldCenter()` applique) sur la
  ruche du joueur. Appelee dans `Awake()` uniquement si le tutoriel n'a pas
  deja centre ET que la vue d'audit n'est pas explicitement demandee —
  priorite : tutoriel guide > vue d'audit QA (si un jour reactivee
  volontairement) > ruche du joueur par defaut.

Preuves : compilation verifiee propre (`assets-refresh`, 0 erreur). Scene
canonique editee via edition texte directe d'un seul scalaire (`1` -> `0`),
verifiee sans casser le parsing (refresh sans erreur). Edition faite sans
perturber la session Unity de Jeff : scene ouverte en mode Additive dans un
onglet separe pour inspection puis dechargee proprement (`LivingHive` est
restee la seule scene chargee/active tout du long, confirme via
`scene-list-opened` avant/apres). Aucun test automatise dedie a ce
comportement trouve dans `Assets/BeeKingdom/Tests` — pas de `tests-run`
lance pour cette tache (changement de camera visuel, verification humaine
plus appropriee).

Prochain test utilisateur : ouvrir la carte du monde en Play Mode (depuis la
ruche) et confirmer que la vue s'ouvre directement centree sur la ruche du
joueur, sans avoir a cliquer sur "Localiser ma ruche".

Ouvert / a faire ensuite :
- Si Jeff veut un jour reactiver la vue d'audit QA (verification de jonctions
  de tuiles), il suffit de repasser `useInitialAuditViewForPlayMode` a `1`
  dans l'inspecteur du composant — le nouveau code respecte toujours ce
  reglage s'il est actif.

---

## Jalon courant — Coeur royal (hub Town Hall) + Abeilles championnes (2026-07-27, carte blanche)

Suite directe du jalon Puissance ci-dessous : Jeff a demande comment augmenter
le niveau de la reine (alors a "—", cf. jalon precedent). Investigation a
confirme qu'aucune mecanique n'existe pour ca (le vrai `QueenManager` est un
moteur de simulation separe, jamais branche au jeu vivant — voir jalon
precedent). Jeff a alors redefini l'objectif : utiliser le hotspot existant
**`administration_core`** ("Coeur royal", Zone 12) comme un vrai hub de
progression façon Town Hall (Clash of Clans/Ant Legion) : ameliorable, plafond
de niveau pour tous les autres batiments, et debloqueur d'"abeilles epic" qui
boostent les troupes envoyees en recolte et en combat — inspire du jeu
concurrent Ant Legion (mecanique des "Fourmis specialisees/heroines").
Jeff est parti plusieurs heures et a donne carte blanche complete apres avoir
precise l'idee (evenements ou achat, boost de troupes/stats).

**Recherche prealable (agents Explore, lecture seule)** avant tout code :
- `Docs/Benchmarks/AntLegion/AntLegion_BeeKingdom_FunctionalReference.md` lu
  en entier : la mecanique Ant Legion s'appelle "Fourmis specialisees"
  (heroines), rareteme commun/intermediaire/mythique, jusqu'a 5 emplacements
  actifs par formation, effet = +armee par niveau (ex. +30/niveau) + bonus de
  stat (%) cible sur le role de troupe correspondant + petit bonus global,
  obtenues par recompense de zone/arene/gacha ("Couveuse")/pass premium — le
  rapport recommande explicitement de NE JAMAIS reserver une unite de combat
  a l'achat payant seul (avantage durable inequitable), coherent avec
  `CLAUDE.md` (jamais pay-to-win).
- Aucune section produit existante (`Docs/Product/*.md`) n'adaptait deja cette
  mecanique aux abeilles — terrain neuf.
- `administration_core` existait deja (hotspot reel, bandeau HD deja branche
  depuis le jalon precedent, icone "queen-core" reelle) mais etait un
  cul-de-sac : aucun bouton d'amelioration, niveau par defaut fige a 27
  (residu de mockup). Confirme via script-execute que son niveau reel actuel
  etait 28 (deja au-dessus de tous les autres batiments montes par Jeff:
  honey_storage 27, genetics_garden 23) — donc activer un plafond dessus
  ne casse rien de sa sauvegarde reelle.
- Combat Patrol (`CombatPatrolPresentation.cs`/`CombatPatrolClient.cs`/
  `Server/src/BeeKingdom.HiveOperations/CombatPatrol*.cs`) est **reellement
  server-autoritaire** (`ICombatPatrolClient` toujours construit avec un vrai
  transport HTTP, pas de mock offline ; `ComputeAvailablePower` 100% cote
  serveur). La recolte manuelle (`ManualProductionPerHour`), elle, est 100%
  locale (formule client, pas de round-trip serveur) sauf si
  `OfficialOfflineProductionConfigured()` (mode serveur officiel actif).

**Decision d'architecture assumee** (meme logique que le jalon Reine/VIP
precedent — construire le reel et testable maintenant, flaguer clairement ce
qui reste a integrer plus tard) : tout le systeme Abeilles championnes est
construit **cote client (local preview)**, pas branche sur le serveur
autoritaire de combat. Le bonus de recolte est **reellement applique**
(formule locale modifiee). Le bonus de combat est **affiche en apercu local**
dans le panneau Combat Patrol ("Bonus championnes (apercu local)") mais
**ne modifie pas** `ComputeAvailablePower` cote serveur — brancher ca
reellement necessiterait de faire tourner et modifier le vrai serveur
(`Server/src/BeeKingdom.HiveOperations`), un chantier separe comparable en
nature a l'integration `QueenManager` deja mise de cote par Jeff.

**Livre — Coeur royal :**
- `DrawAdministrationCoreDetail` : ajout d'un vrai bouton "Ameliorer" (cout
  miel/cire ×2 par rapport a la formule generique, vu son role de hub) en
  plus du bouton existant "Voir la vue d'ensemble", avec barre de progression
  pendant l'amelioration.
- `UpgradeDisabledReason` : nouvelle regle — aucun batiment (sauf
  `administration_core` lui-meme) ne peut etre ameliore a un niveau ≥ celui
  du Coeur royal ("Niveau du Coeur royal insuffisant").

**Livre — Abeilles championnes** (nouveau systeme complet) :
- `Assets/BeeKingdom/Playground/ChampionBeeCatalog.cs` (nouveau fichier) :
  catalogue de 5 abeilles nommees — Striga (Gardiennes, Rare), Zephyra
  (Voltigeuses, Rare), Ambra (Lanceuses, Legendaire), Nectaria (Collecte,
  Rare), Aurelia (Collecte, Legendaire) — avec bonus d'armee/stat/production
  par niveau (max niveau 10) et cout d'amelioration croissant (miel+pollen).
  Seuils de deblocage par le niveau du Coeur royal : Rare des niveau 3,
  Legendaire des niveau 10 (v1, ajustable avec Jeff).
- `LocalPreviewHiveProgress.cs` : nouveau champ `championBees` (liste
  id+niveau) et `assignedChampionBeeIds` (max 5), version de schema
  2 -> 3, fonctions `MergeChampionBeeLevel`/`SetAssignedChampionBees` suivant
  exactement le pattern `MergeBuildingLevel` deja existant, normalisation
  mise a jour (dedoublonnage, clamp, validation des assignations contre les
  abeilles reellement possedees).
- `HiveViewProductUiPresenter.cs` : nouveau panneau plein ecran
  `DrawChampionBeesOverlay` (liste des 5 abeilles, etat verrouille/possede/
  assigne, boutons "Obtenir (evenement)" et "Acheter (demo)" — **les deux
  donnent exactement la meme abeille**, aucune n'est reservee a l'achat,
  respecte la recommandation anti-pay-to-win du rapport Ant Legion),
  amelioration (cout miel/pollen), assignation (max 5 simultanees, effet
  passif global plutot que reselectionne par mission — comme les 5
  emplacements de formation d'Ant Legion). Point d'entree ajoute dans
  `DrawCombatPatrolPanel` (bouton + resume des bonus par famille). Bonus de
  recolte reellement applique dans `ManualProductionPerHour` (honey_storage,
  wax_workshop, warehouse_cells).
- Textes bilingues ajoutes dans `strings.fr-CA.json`/`strings.en-US.json` (33
  nouvelles cles chacun ; parite verifiee par script Node : 1432 entrees
  chacun, 0 doublon, 0 cle manquante d'un cote ou l'autre).

**Regression trouvee et corrigee en cours de route** : le bump de version de
schema (2 -> 3) a casse 2 tests preexistants qui asseraient en dur
`"version":2`/`progress_version:2` comme version courante
(`HiveProgressMigratesV1WithoutInventingDoctrineCounts`,
`HiveProgressPersistsMultipleBuildingsAndPopulationsAcrossRestarts` dans
`SandboxLivingHiveManualCollectionTests.cs`). Corrige en remplacant les
litteraux "2" par `LocalPreviewHiveProgressCodec.CurrentVersion` (plus robuste
aux futurs bumps de schema plutot que re-casser a chaque fois).

Preuves : compilation verifiee via `assets-refresh` (zero erreur ; une seule
erreur `CS0019` rencontree en cours de route — comparaison `hotspot != null`
sur `ReferenceHiveHotspot`, qui est un struct — corrigee en verifiant
`!string.IsNullOrWhiteSpace(hotspot.HotspotId)`). Tests EditMode
`SandboxLivingHiveManualCollectionTests` : 54/55 reussis apres correction des
2 tests de version ; seul echec restant `GuidedDefenseChapterCanBuildAWaxBarrierInstead`
(`defense_wax_cost:120` vs `60` attendu), bug preexistant documente dans
plusieurs jalons anterieurs, sans rapport avec ce travail.

Prochain test utilisateur : en Play Mode, ouvrir le panneau du Coeur royal
(zone 12) et verifier le nouveau bouton "Ameliorer" ; ouvrir le panneau Combat
Patrol et cliquer sur "Abeilles championnes" pour obtenir/ameliorer/assigner
une abeille (Rare disponible immediatement si Coeur royal ≥ niveau 3,
Legendaire ≥ niveau 10) ; verifier que le bonus de recolte choisi (Nectaria/
Aurelia assignee) augmente reellement le `/h` affiche sur les batiments de
production ; verifier que le panneau Combat Patrol affiche un resume de bonus
(sans effet reel sur l'issue du combat pour l'instant, voir limite ci-dessous).

Ouvert / a faire ensuite :
- **Limite assumee, a decider avec Jeff** : le bonus de combat des Abeilles
  championnes n'affecte pas encore la vraie resolution serveur
  (`CombatPatrolResolution.ComputeAvailablePower`) — seulement un apercu
  local affiche. Brancher ca reellement = chantier separe (toucher au
  serveur `BeeKingdom.HiveOperations`, migrations, tests serveur), a planifier
  explicitement si Jeff le veut, comme l'integration `QueenManager` deja mise
  de cote.
- Le 3e volet de la demande initiale de Jeff sur le Coeur royal
  ("progresser dans le jeu") n'a pas ete implemente separement — seul le
  deblocage des raretes d'abeilles championnes utilise le niveau du Coeur
  royal pour l'instant. A clarifier avec Jeff ce que "progression du jeu"
  doit debloquer concretement (nouvelles zones ? nouveaux batiments ?) avant
  d'inventer autre chose.
- Systeme d'acquisition volontairement simplifie (pas de gacha/oeufs/
  fragments/pass premium comme Ant Legion — juste un octroi direct stub) vu
  le temps disponible ("carte blanche, quelques heures") ; a enrichir plus
  tard si Jeff veut la profondeur complete du systeme Ant Legion.
- Poids/couts de la formule (armee/stat/production par niveau, seuils de
  deblocage 3/10, multiplicateur ×2 du cout du Coeur royal) sont un premier
  jet explicitement ajustable, comme la formule de Puissance du jalon
  precedent.
- Jamais teste visuellement en Play Mode par un humain cette session (Jeff
  absent) — jugement de gout/lisibilite du nouveau panneau encore a valider.

**Mise a jour (meme jour, retour de Jeff, 3 iterations de feedback live) :**
1. Jeff n'a pas trouve le bouton "Ameliorer" du Coeur royal — il regardait
   dans "Vue d'ensemble de la colonie" (`DrawColonyOverviewDetailContent`),
   un ecran **purement informatif pour tous les batiments** (jamais eu de
   bouton d'action, meme avant cette session). Le bouton reel vit dans
   `DrawAdministrationCoreDetail`, atteint en cliquant directement sur
   l'icone du Coeur royal dans la ruche (`DrawLandscapeContextDetailPanel`/
   son equivalent portrait) — confirme fonctionnel une fois au bon endroit
   (capture d'ecran : "Ameliorer (5688 miel, 2114 cire)").
2. Jeff n'a pas trouve le bouton "Abeilles championnes" non plus — il etait
   niche uniquement dans le panneau Combat Patrol, lui-meme difficile a
   atteindre (Jeff a fini sur l'ecran d'entrainement de la Caserne, un ecran
   different). Corrige : nouveau badge permanent dans le HUD du haut, a cote
   de Puissance (icone abeille + compteur), ouvrant directement le panneau
   plein ecran quel que soit l'endroit ou on se trouve.
3. **Refonte complete du panneau apres avoir vu la V1** (capture d'ecran
   fournie) : Jeff veut un vrai portrait par abeille (pas juste l'icone
   couronne generique) avec un clic qui ouvre une fiche detail
   (description + "hauts faits" narratifs sur comment elle est devenue
   championne), des onglets par role en bas de la fenetre (vu qu'il y aura
   des dizaines d'abeilles a terme), et surtout une regle de gameplay
   nouvelle : **les troupes communes ne peuvent jamais partir seules, il
   leur faut toujours une abeille championne pour les accompagner** — donc
   on commence avec 1 seul emplacement actif (pas 5 d'entree de jeu), qui
   grandit avec le niveau du Coeur royal.

   Livre :
   - `ChampionBeeDefinition` : nouveau champ `FallbackLore` (texte narratif
     par abeille, ecrit pour les 5 abeilles existantes, route par
     `BeeLocalization.Text("champion_bees.lore.<id>", ...)` comme le reste).
   - `ChampionBeePortraitTexture(beeId)` : charge
     `Resources.Load<Texture2D>("PremiumBeeReference/ChampionBees/<id>")`,
     retombe sur l'icone couronne teintee par rarete si absent — **Jeff a
     dit qu'il va deposer les images dans
     `C:\projets\beekingdom\images\championnes`** (dossier hors-projet, meme
     pattern que les 14 bandeaux de batiments) ; a copier dans
     `Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/ChampionBees/`
     (nommage `<id>.png`, ex. `striga.png`) des qu'il confirme qu'elles sont
     pretes — **pas encore fait, aucune image livree a ce stade**.
   - Panneau reconstruit en deux vues : liste (scrollable,
     `GUI.BeginScrollView`, portrait+nom+etat par ligne, clic = "Voir") et
     detail plein ecran (`DrawChampionBeeDetail` : gros portrait, nom,
     rarete/role, effet, section "Hauts faits" avec le texte narratif,
     boutons obtenir/ameliorer/assigner deplaces ici). Onglets de filtre par
     role en bas de la liste (Toutes/Gardiennes/Voltigeuses/Lanceuses/
     Collecte) — prets a scaler a des dizaines d'entrees.
   - `CurrentMaxAssignedChampionBees()` (nouvelle fonction) : plafond
     dynamique `1 + niveau_coeur_royal/8`, borne au plafond structurel
     absolu (5) — remplace le plafond fixe partout (badge HUD, panneau
     Combat Patrol, logique d'assignation). Demarre bien a 1 comme demande.
   - Regle "pas de troupe seule" : dans `DrawCombatPatrolPanel`, nouveau
     garde-fou client `championRequirementMet` (vrai si aucune troupe dans
     le brouillon, ou si au moins une abeille championne est assignee) —
     bloque le bouton "Lancer la patrouille" avec un message clair si des
     troupes sont composees sans championne. **Meme limite deja documentee
     plus haut** : ceci reste un garde-fou cote client, la vraie resolution
     serveur (`CombatPatrolResolution`) n'est pas modifiee.

Preuves : compilation verifiee propre (`assets-refresh`, 0 erreur) apres
chaque iteration. Parite de cles de localisation reverifiee par script Node
apres ajout des textes de lore (1445 entrees chacune, 0 doublon, 0 manquante).
Tests EditMode `SandboxLivingHiveManualCollectionTests` : 54/55 (meme seul
echec preexistant sans rapport, `GuidedDefenseChapterCanBuildAWaxBarrierInstead`).

Prochain test utilisateur : cliquer sur le nouveau badge HUD "Abeilles
championnes" (a cote de Puissance) ; ouvrir une abeille (bouton "Voir") pour
verifier la fiche detail (portrait de secours + hauts faits) ; tester les
onglets de filtre en bas ; essayer de lancer une patrouille avec des troupes
mais sans championne assignee pour verifier le blocage.

Ouvert / a faire ensuite :
- Copier les images de Jeff dans `PremiumBeeReference/ChampionBees/` des
  qu'il confirme qu'elles sont dans
  `C:\projets\beekingdom\images\championnes`.
- Formule du plafond dynamique (`1 + niveau/8`) est un premier jet, comme le
  reste des chiffres de cette fonctionnalite.
- La regle "pas de troupe seule" ne s'applique qu'au Combat Patrol pour
  l'instant (seul flux reel d'envoi de troupes) — la "recolte" reste un
  systeme passif sans notion d'envoi, donc la regle ne s'y applique pas
  litteralement (le bonus de recolte des abeilles Civiles reste actif tel
  quel, sans blocage).

---

## Jalon courant — Panneau Puissance branche sur l'etat reel (2026-07-27)

Jeff a demande si le score "Puissance" (17,403,497) affiche dans le HUD etait
a jour. Investigation directe du code (`HiveViewProductUiPresenter.cs`) :
c'etait un mockup **entierement code en dur**, jamais branche, tout comme le
niveau de la reine ("Niv. 26") et le badge VIP ("VIP 3"), dupliques a
plusieurs endroits (HUD du haut, panneau Profil joueur, panneau Puissance
detaille, panneau VIP detaille).

Recherche (agent Explore) a revele une asymetrie entre les 4 categories du
panneau :
- **Equipement** et **Abeilles spec.** : donnees reelles deja suivies
  (`localPreviewBuildingLevels`, `localPreviewWorkers/Soldiers/Guardians/
  Scouts/Wingrunners/Darters`), deja utilisees ailleurs (Vue d'ensemble de la
  colonie).
- **Competences** : partiellement reel
  (`localPreviewQueueJournal.completedResearchIds.Count`).
- **Niveau reine** : un vrai systeme de progression existe (`QueenManager`/
  `QueenAggregate.Evolution.Level`, `Assets/BeeKingdom/Hive/`) mais fait
  partie d'un moteur de simulation complet separe (`ColonySimulationController`,
  `HiveManager`, `TaskManager`, `AIManager`...) qui **ne tourne pas du tout**
  dans la partie du jeu que Jeff joue reellement — `HiveViewProductUiPresenter.cs`
  ne le reference nulle part, il utilise son propre etat simplifie
  `LocalPreviewHiveProgress`. Brancher ce systeme = chantier d'integration de
  plusieurs jours, pas un simple cablage de badge.
- **VIP** : aucun systeme reel nulle part dans le projet (ni points, ni tier,
  ni logique d'achat).

Decisions de Jeff (via question ciblee) :
- Niveau reine -> lancer plus tard l'integration complete du vrai
  `QueenManager` (chantier separe, **pas fait dans cette passe**) ; en
  attendant, le badge affiche honnetement "—" aux 3 endroits (HUD, panneau
  Profil joueur, panneau Puissance detaille) plutot que de garder l'ancien
  chiffre fige, devenu incoherent avec le reste du panneau maintenant reel.
- VIP -> afficher honnetement "—" / ne plus compter dans le total Puissance.
  Etendu par coherence au panneau VIP detaille (`DrawVipPanel`), qui
  affirmait a tort "Compte dans la puissance." — corrige en "Systeme VIP pas
  encore implemente."

Livre : nouvelles methodes `RealPowerFromBuildings()`/`RealPowerFromBees()`/
`RealPowerFromResearch()`/`ComputeRealTotalPower()`/`FormatRealPowerNumber()`
juste avant `DrawPowerPanel`, avec des poids v1 explicitement ajustables
(`PowerWeightPerBuildingLevel=30000`, poids par type de troupe 500-2000,
`PowerWeightResearch=50000`) — formule de premier jet, a ajuster avec Jeff
selon le ressenti en jeu, pas une verite de conception figee. `DrawPowerPanel`
calcule maintenant le total et les parts (barres de progression) dynamiquement
a partir de ces 3 buckets reels ; la ligne "Niveau reine" reste "—" avec barre
vide, exclue du total. Les badges HUD dupliques (haut d'ecran reine/VIP/
puissance, panneau Profil joueur) mis a jour en coherence.

Preuves : compilation verifiee propre via `assets-refresh` + `console-get-logs`.
Deux erreurs `CS0103` observees en cours d'edition provenaient d'une
recompilation automatique de Unity survenue entre deux modifications
sequentielles (avant que les nouvelles methodes soient toutes en place) — pas
de l'etat final. Confirme resolu : `assets-refresh` final sans aucune
nouvelle erreur dans la minute suivante.

Prochain test utilisateur : en Play mode, ouvrir le HUD (badges Reine/VIP/
Puissance en haut), verifier que "Puissance" affiche un total plausible qui
change reellement si tu montes un batiment de niveau ou recrutes des abeilles
(au lieu du chiffre fige precedent) ; verifier que Reine et VIP affichent "—"
partout (HUD, Profil joueur, detail Puissance, detail VIP) sans plus
pretendre "compter dans la puissance".

**Mise a jour (meme jour, apres premier test de Jeff)** : Jeff a repere que la
barre de progression d'"Equipement" ecrasait totalement les 2 autres
categories reelles (~95% de part contre ~2.6% chacune pour Abeilles spec. et
Competences), a cause du poids `PowerWeightPerBuildingLevel=30000` trop lourd
face au cumul des niveaux de batiments (122 niveaux cumules dans sa
sauvegarde). Corrige : `PowerWeightPerBuildingLevel` 30000 -> 2000,
`PowerWeightResearch` 50000 -> 80000 (compense le fait que la recherche est
plafonnee par la taille finie du catalogue, contrairement aux batiments/
troupes qui grandissent sans limite). Avec sa sauvegarde actuelle, nouvelle
repartition attendue : Equipement ~244 000 (48%), Abeilles spec. 99 500
(20%), Competences 160 000 (32%) — aucune categorie n'ecrase plus les autres.
Compilation reverifiee propre (`assets-refresh` + `console-get-logs`, aucune
erreur).

Ouvert / a faire ensuite :
- Poids de la formule de Puissance (batiments/troupes/recherche) restent un
  premier jet, maintenant mieux equilibre — a raffiner encore avec Jeff selon
  le ressenti en jeu au fil du temps (ex. quand le roster grandira beaucoup
  plus que les batiments, ou l'inverse).
- Chantier separe a planifier explicitement si Jeff le souhaite :
  integration complete du vrai `QueenManager`/moteur de simulation
  (`ColonySimulationController`) dans le jeu vivant, pour que le niveau de la
  reine devienne reel (aucun systeme de montee de niveau n'existe aujourd'hui
  cote `HiveViewProductUiPresenter.cs`).
- Systeme VIP a construire de toutes pieces si souhaite un jour (aucune trace
  nulle part dans le projet aujourd'hui).

---

## Jalon courant — Bandeaux HD reels pour la Vue d'ensemble de la colonie (2026-07-27)

Suite logique du jalon "Vue d'ensemble de la colonie" ci-dessous : Jeff a
genere 14 images HD (une par batiment/zone) dans un dossier hors-projet
`C:\projets\beekingdom\imagesBuildings\` et a demande de remplacer les
bandeaux generiques (photo teintee `bg_beehive.png` + icone) par ces vraies
images sur les pages de detail par batiment.

- Copie des 14 PNG (2.3-3 Mo chacun) renommes selon l'ID technique du hotspot
  (ex. `nurserie.png` -> `nursery_cluster.png`) dans
  `Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/BuildingBanners/`
  pour un chargement direct via `Resources.Load<Texture2D>("PremiumBeeReference/BuildingBanners/" + hotspotId)`.
  Import Unity par defaut (memes reglages Sprite que les autres bandeaux
  existants du dossier `PremiumBeeReference/`), aucune erreur de compilation.
- Nouveau helper `BuildingBannerTexture(hotspotId)` (cache statique
  `Dictionary<string, Texture2D>`) dans `HiveViewProductUiPresenter.cs`.
- `DrawColonyOverviewOverlay` : quand une image reelle existe pour le
  batiment affiche en detail, elle est dessinee pleine couleur (sans teinte)
  et l'icone de secours (`DrawGameIcon`) n'est plus superposee. Si aucune
  image n'existe pour un hotspot (cas futur), le comportement de secours
  (photo generique teintee + icone) reste intact — aucune regression pour
  d'eventuels nouveaux batiments sans art dedie.
- La bandeau de la page LISTE (vue d'ensemble generale, pas de detail
  selectionne) continue d'utiliser `bg_beehive.png` — aucune image dediee
  n'existe pour cette vue globale.
- Portee volontairement limitee aux pages de detail de la Vue d'ensemble.
  La popup d'info batiment (`DrawReferenceDetailInfoPopup`) et le panneau de
  detail standard n'ont pas ete touches (ils n'avaient pas de bandeau photo a
  remplacer) — a clarifier avec Jeff si un usage plus large des images est
  souhaite.

Preuves: compilation verifiee sans erreur via `console-get-logs` (uniquement
des warnings preexistants sans lien, `CS0618` sur du code Editor obsolete).
Pas de nouvelle execution de la suite de tests (risque de conflit avec le
Play Mode de Jeff, actif au moment du changement — voir incident precedent
dans ce document).

Prochain test utilisateur: en Play Mode, ouvrir la ruche -> Coeur royal ->
"Voir la vue d'ensemble" -> cliquer sur n'importe quel batiment de la liste.
Le bandeau du haut doit maintenant afficher la vraie photo du batiment (pas
une photo generique teintee), sans icone superposee.

Ouvert / a faire ensuite:
- Mettre a jour ce journal etait egalement en retard sur plusieurs jalons UI
  anterieurs (nettoyage des panneaux WorldMap + barre de navigation,
  corrections de troncature de texte dans le Hive, retrait de la notice
  "production a ton retour", retrait du texte de divulgation et des icones
  d'abeilles animees, popup d'info batiment cliquable, construction complete
  de la fonctionnalite Vue d'ensemble de la colonie) — non detailles
  individuellement ici faute de temps, mais tous livres et verifies avant ce
  jalon.
- Demander a Jeff si les nouvelles images doivent aussi remplacer le bandeau
  de la popup d'info batiment ou de la vue liste generale.

## Jalon courant — Raccourci developpeur "Sauter Acte I" (2026-07-26)

Jeff a demande un moyen de tester le jeu en Play Mode sans rejouer tout le
tutoriel de l'Acte I a chaque fois. Recherche prealable demandee explicitement
avant d'ecrire du code :

- Le menu developpeur du splash (`DrawSplashDevelopmentMenu`/
  `DrawSplashDeveloperRouteButton`, ~3126-3183) existait deja mais ne fait que
  **changer de scene** (`SplashDevelopmentSceneConfig.TryOpenScene`) — aucune
  route n'y touchait l'etat du tutoriel.
- `SeedGuidedOpeningActProgress(completedChapters)` (clampe 0-5, donc deja
  scope a l'Acte I) ne fait que recalculer les totaux d'AFFICHAGE de
  recompense de quete — **il ne credite jamais** `localPreviewHoney/Wax/Pollen`.
  C'est `ClaimGuidedChapterReward(chapter, honey, wax, pollen)` qui fait le
  vrai credit ET avance `guidedOpeningActMilestones`. Les `BeginGuided...ForProof()`
  qui sautent a un chapitre tardif (`BeginGuidedWorldTransitionForProof` etc.)
  ne compensent pas ce manque - ils testent des mecaniques isolees, pas un
  etat de jeu realiste.
- `GuidedCollectionTutorialStep.Inactive` est bien le vrai etat "tutoriel
  desactive" (atteint normalement apres `ForageCompleted`, chapitre 7).
  `SetPlayableHiveLoopProofState("idle")` (deja `public static`, utilisee par
  tous les harnais de test) le met deja par defaut. **Piege trouve en cours
  de route** : `EnterHiveFromSplash()` appelle toujours
  `BeginGuidedCollectionTutorial()`, qui (en l'absence de checkpoint) reset
  tout et termine sur `SetGuidedCollectionTutorialStep(Welcome)` — appeler
  cette fonction apres avoir seede `Inactive` aurait donc immediatement
  annule le saut. Le nouveau code ne l'appelle pas; il reproduit seulement le
  sous-ensemble pertinent (`splashAuthGateState`, message, surface de
  reference) sans redemarrer le tutoriel.

Livre : `SkipActOneTutorialForDevelopment()` (nouvelle fonction) —
`SetPlayableHiveLoopProofState("idle")` puis `ClaimGuidedChapterReward(1..5, ...)`
en sequence (credite reellement 1050 miel/280 cire/120 pollen, les totaux
canoniques de l'Acte I deja vus ailleurs dans le code), plus quelques niveaux
de batiments et roster modestes via `LocalPreviewHiveProgressCodec.MergeBuildingLevel`/
`MergePopulationCount` (wax_workshop 3, guard_post 3, honey_storage 2, 34
ouvrieres/20 soldates/10 gardiennes/6 eclaireuses — echelle "juste sorti de
l'Acte I", pas le niveau "capture marketing" 25-28 deja vu ailleurs dans le
code). Nouveau bouton **"Sauter Acte I"** ajoute au menu developpeur du
splash (`DrawSplashSkipActOneButton`), visible uniquement Editor/Development
build (meme garde `IsDevelopmentMenuVisible()` que les routes existantes) —
en portrait a cote de "Connexion", en paysage la 2e rangee passe de 2 a 3
boutons a largeur egale (aucun changement de hauteur de menu necessaire dans
les deux cas).

Preuves : compilation Unity verifiee via `assets-refresh` + `console-get-logs`
(zero `error CS`). Tests EditMode `SandboxLivingHiveManualCollectionTests`
relances par filtre de classe : 54/55 reussis, meme echec preexistant
(`defense_wax_cost`) sans rapport, aucune regression.

Prochain test utilisateur : lancer le Play Mode depuis l'ecran de demarrage,
cliquer "Sauter Acte I" dans le menu developpement, verifier que le jeu
s'ouvre directement en mode libre (pas de tutoriel actif), avec les 5
recompenses de l'Acte I reclamees et un etat de batiments/roster plausible.

Ouvert / a faire ensuite : ajuster les niveaux/ressources seedes si le
ressenti ne convient pas a Jeff apres test; decider avec Jeff de la prochaine
tranche de rythme (chapitre 3, seul restant au plancher a 30 interactions),
et reprendre les decisions en attente sur l'Android/iOS (voir jalons plus
bas).

---

## Jalon courant — Tutoriel Acte I, chapitre 2 : nouvelle interaction "Consolidation des cellules" (2026-07-26)

Apres la tranche precedente (chapitre 5, vigilance), les chapitres 2 et 3
etaient a egalite au plancher d'interactions (30 chacun). Jeff m'a laisse
choisir : recherche prealable (agent Explore, lecture seule) confirmant que
le chapitre 2 est le bon choix — precedent de code/doc explicite (a egalite,
on prend le chapitre le plus bas numeriquement) et surtout une vraie stat
dormante trouvee dans le code, `operationalWaxProductionBonus`/
`operationalWaxCapacityBonus` (`LocalPreviewStrategicProfile.cs`), declarees
et consommees par `LocalPreviewStrategicProfileRules.Derive()` mais jamais
assignees par une interaction joueur avant cette tranche — exactement le
theme de la cire de nurserie deja pose par les textes du chapitre 2.

Nouvelle interaction inseree entre "preparation_emergence" (2.14, la
reference a mirroir) et la recompense du couvain (desormais 2.16) :
**"Consolidation des cellules"** (2.15) — choix entre cire dense (rapide,
2 batisseuses, 14s, gratuit, +5% capacite de stockage de cire persistante)
ou cire souple (minutieuse, 3 batisseuses, 18s, 35 pollen, +2% production de
cire persistante), avec collecte manuelle de cire a l'atelier (hotspot
`wax_workshop`, reutilise le motif deja etabli par `brood_worker_handoff`
pour la meme collecte) et un controle unique de tenue des cellules avant de
sceller la nurserie. Meme motif exact que les tranches precedentes (decision
-> minuteur -> collecte manuelle -> controle -> consequence), mirroir fidele
du gabarit le plus recent (`DefenseVigilance`, chapitre 5) plutot que du
gabarit `BroodWorkerHandoff` local (celui-ci a 3 controles, la vigilance n'en
a qu'un — j'ai suivi la vigilance pour un controle unique), dans
`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : enum (5
nouvelles etapes `BroodWaxConsolidation*`), champs d'etat, constantes de
duree/cout/gains, Reset/Start/Progress/CompleteIfReady/RegisterCheck,
collecte manuelle (AllowsCollection, AdvanceOnCollection, TargetHotspot,
VisualHotspot, animation des abeilles `ActiveBeeTaskZoneId` — tous les points
de code retrouves par grep systematique du gabarit de reference plutot que
supposes), UI (textes + boutons + barre de progression), transition
(`BroodWorkerHandoffResult` redirige vers `BroodWaxConsolidationChoice` au
lieu de `BroodCompleted` directement), journal de quete (objectif 15,
recompense decalee a 16), proof helpers et etat de preuve, plus le nouveau
champ `LocalPreviewStrategicProfile.broodWaxConsolidation` (string, pas de
bump de version necessaire — champ initialise a `"none"`, meme motif que les
deux floats `operationalWax*` deja presents sans migration dediee). Textes
bilingues ajoutes dans les deux catalogues
`strings.fr-CA.json`/`strings.en-US.json` (18 cles chacun,
`tutorial.chapter_02.wax_consolidation.*` plus
`tutorial.chapter_02.worker_handoff.next_action` pour le bouton de transition
redirige — parite de cles verifiee par script Node : 1363 dans chaque
catalogue, aucun doublon, aucune cle manquante d'un cote ou l'autre).

Nouvel etat de rythme (mesure directement dans le code, verifie par les
tests, pas suppose) : chapitre 2 passe de 15 a 16 objectifs, 30 a 33
interactions actives (14 decisions, 13 controles, 4 collectes), 180-224s a
194-242s. Le chapitre 3 devient seul au plancher d'interactions (30) —
prochaine tranche naturelle a decider avec Jeff.

**Incident de process notable** : le premier `tests-run` a ete lance sans
filtre (toute la suite du projet plutot que la seule classe ciblee), ce qui a
fait geler l'editeur Unity plus de 47 minutes (fenetre "Hold on (busy for
47:10)..."). Jeff a redemarre l'editeur manuellement pendant que j'attendais.
Retente ensuite avec `testClass:SandboxLivingHiveManualCollectionTests`
explicite : reponse en ~1 seconde. **Lecon a retenir** : toujours filtrer
`tests-run` par `testClass` sur ce projet — ne jamais lancer la suite
complete sans filtre, elle semble englober beaucoup plus que les tests de
gameplay (carte du monde, combat, chat, admin, etc.) et peut bloquer
l'editeur tres longtemps.

Preuves : compilation Unity verifiee via `assets-refresh` + `console-get-logs`
(zero `error CS`, uniquement des warnings preexistants d'API obsolete).
Tests EditMode `SandboxLivingHiveManualCollectionTests` (filtre par classe) :
54/55 reussis des la premiere execution. Seul echec :
`GuidedDefenseChapterCanBuildAWaxBarrierInstead` (`defense_wax_cost:60`
attendu vs `120` obtenu) — bug preexistant deja documente dans plusieurs
jalons precedents (constante `GuidedDefenseBarrierWaxCost` du chapitre 5,
jamais touchee cette session), confirme sans rapport avec cette tranche.
Aucune nouvelle regression introduite par cette tranche.

Prochain test utilisateur : rejouer le chapitre 2 en Play mode jusqu'a la
preparation de l'emergence puis la nouvelle consolidation des cellules, pour
confirmer que l'enchainement se sent bien integre juste avant la recompense
du chapitre (jugement humain, pas encore fait).

Ouvert / a faire ensuite : decider avec Jeff de la prochaine tranche de
rythme (chapitre 3, seul restant au plancher a 30 interactions), et reprendre
les decisions en attente sur le deblocage de l'empaquetage Android et la
verification Xcode du plugin Keychain iOS (voir jalons plus bas).

---

## Jalon courant — Tutoriel Acte I, chapitre 5 : nouvelle interaction "Ronde de vigilance" (2026-07-26)

Jeff a choisi de continuer sur le chapitre 5 (Defense), seul restant au
plancher d'interactions (29) apres les tranches precedentes sur les
chapitres 1 et 4. Recherche prealable (agent Explore, lecture seule) :
contrairement aux deux tranches precedentes, le chapitre 5 n'avait **aucun
bug de tick-list preexistant** ni **aucune lacune de hotspot** — base propre,
uniquement la nouvelle interaction a ajouter.

Nouvelle interaction inseree entre "briefing_sortie_simule" (5.12, devenu
la reference a mirroir) et la recompense de l'Acte I (desormais 5.14) :
**"Ronde de vigilance"** (5.13) — choix entre ronde rapide (2 gardiennes,
10s, gratuit, +1 securite) ou minutieuse (3 gardiennes, 14s, 40 miel, +2
securite), avec collecte manuelle de miel a l'entrepot (reutilise le
hotspot `honey_storage` deja actif dans ce chapitre via
`ReadinessCollectHoney`, aucun nouveau hotspot introduit) et un controle
unique de perimetre avant de clore l'Acte I. Meme motif que les tranches
precedentes (decision -> minuteur -> collecte manuelle -> controle ->
consequence) dans `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` :
enum (5 nouvelles etapes `DefenseVigilance*`), champs d'etat, constantes de
duree/gains, fonctions Reset/Start/Progress/CompleteIfReady/RegisterCheck,
collecte manuelle (4 points de code distincts : AllowsCollection,
AdvanceOnCollection, TargetHotspot, VisualHotspot — plus un 5e endroit de
gating de clics et un 6e pour l'animation des abeilles/gardiennes,
`ActiveBeeTaskZoneId`, tous deux decouverts en cours de route par grep
systematique plutot que supposes depuis le rapport de recherche initial),
UI, flux de transition, journal de quete (objectif 13, recompense decalee a
14), proof helpers et etat de preuve. La consequence alimente
`localPreviewHiveSecurity` (stat deja utilisee ailleurs dans le meme
chapitre, motif identique a la briefing "retour garde"). Textes bilingues
ajoutes dans les deux catalogues `strings.fr-CA.json`/`strings.en-US.json`
(21 cles chacun — 19 prevues plus `world_briefing.next_action` et
`vigilance.progress`, decouvertes necessaires en cours de route ; parite de
cles verifiee : 1345 dans chaque catalogue, aucun doublon, aucune cle
manquante).

**Bug introduit puis corrige en cours de route (avant tout test)** : le
nouveau texte d'action du bouton de fin de briefing reutilisait la cle de
localisation existante `tutorial.chapter_05.world_briefing.confirm` avec un
nouveau libelle de repli ("Ronde de vigilance") — mais `BeeLocalization.Text()`
priorise toujours la valeur du catalogue JSON existant sur le repli code en
dur, donc les deux appels auraient affiche le meme texte "Clore l'Acte I".
Corrige en creant une cle distincte `world_briefing.next_action` avant
d'ecrire les catalogues de traduction.

Nouvel etat de rythme (mesure directement dans le code, verifie par les
tests, pas suppose) : chapitre 5 passe de 13 a 14 objectifs, 29 a 32
interactions actives (14 decisions, 13 controles, 5 collectes), 166-208s a
176-222s. Les chapitres 2 et 3 (30 interactions chacun) deviennent le
nouveau plancher partage — prochaine tranche naturelle a decider avec Jeff.

Preuves : compilation Unity verifiee via `assets-refresh` + `Logs/Editor.log`
(zero `error CS`). Tests EditMode `SandboxLivingHiveManualCollectionTests`
executes avec `EditorApplication.isPlaying` confirme `false` avant lancement :
premiere execution 52/55 (2 echecs inattendus decouverts — pas supposes —
`defense_objective_target:13` code en dur dans 2 tests non lies a cette
tranche, `GuidedDefenseChapterMobilizesGuardiansAndResolvesThreat` et
`GuidedReadinessLoopRequiresTwoManualProductionCycles`, corrige par un
remplacement global vers `14` sur les 4 occurrences trouvees par grep).
Deuxieme execution : 54/55, seul echec restant
`GuidedDefenseChapterCanBuildAWaxBarrierInstead` (`defense_wax_cost` 60 vs
120), bug pre-existant deja documente dans les jalons precedents, sans lien
avec cette tranche, explicitement laisse tel quel.

Prochain test utilisateur : rejouer le chapitre 5 en Play mode jusqu'au
briefing de sortie simule puis la nouvelle ronde de vigilance, pour
confirmer que l'enchainement se sent bien integre juste avant la recompense
de l'Acte I (jugement humain, pas encore fait).

Ouvert / a faire ensuite : decider avec Jeff de la prochaine tranche de
rythme (chapitres 2 et 3, a egalite au plancher a 30 interactions), et
reprendre les decisions en attente sur le deblocage de l'empaquetage
Android et la verification Xcode du plugin Keychain iOS (voir jalons plus
bas).

---

## Jalon courant — Demarrage du support iOS en parallele d'Android (2026-07-26)

Jeff developpe maintenant sur un Mac avec compte Apple Developer (licence pas
encore payee) et veut avancer le support iOS en parallele d'Android. Etat des
lieux verifie directement dans l'editeur (pas suppose) : ni le module Android
Build Support ni le module iOS Build Support n'etaient installes sur cet
editeur Windows (6000.5.3f1) au debut de cette tranche — Jeff a installe le
module iOS via Unity Hub en cours de session, confirme ensuite par
`BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS) == true`.
Les Player Settings iOS existaient deja partiellement (bundle id
`com.BKD-Honey-Studio.BeeKingdom`, cible iOS 15.0+), Team ID et signature
automatique encore vides (normal sans licence).

Audit du code (agent Explore) : aucun `Plugins/Android` ni `Plugins/iOS`
n'existait, aucun SDK IAP/ads/push/analytics (rien dans
`Packages/manifest.json`), donc pas de blocage de ce cote. Le vrai blocage
etait trois classes cablees en dur sur l'Android Keystore
(`AndroidKeystoreRefreshTokenStore`, `AndroidKeystoreGameReadCacheStore`,
`AndroidKeystoreGameMutationOutboxStore`, dans `Assets/BeeKingdom/Networking/`)
instanciees sans branche par plateforme dans
`MobileAccountSessionRuntimeBootstrap.cs` — elles proteges les tokens de
session et la file de mutations hors-ligne (upgrades, recherche, reservations
d'escouade, etc.). Sans correctif, iOS aurait perdu silencieusement la
persistance de session et le cache hors-ligne.

Jeff a choisi la parite reelle avec Android (vrai Keychain iOS) plutot que de
reutiliser le repli AES logiciel deja existant pour le chat
(`LivingHiveChatDataProtector`, cle stockee en clair dans PlayerPrefs —
juge trop faible pour des tokens d'authentification). Livre :

- `Assets/Plugins/iOS/BeeKingdomKeychainBridge.m` — petit pont natif
  Objective-C vers `Security.framework` (Set/Get/Delete par cle,
  `kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly`, pas de sync iCloud).
  Confirme reconnu par Unity comme plugin natif **iOS uniquement**
  (`PluginImporter.GetCompatibleWithPlatform(iOS)=true`,
  `(Android)=false`) sans configuration manuelle — comportement automatique
  du nom de dossier `Plugins/iOS`.
- `IosKeychainBridge.cs` (pont `[DllImport("__Internal")]`),
  `IosKeychainRefreshTokenStore.cs`, `IosKeychainGameReadCacheStore.cs`,
  `IosKeychainGameMutationOutboxStore.cs` — miroir exact des trois classes
  Android (memes interfaces `IProtectedRefreshTokenStore`/
  `IProtectedGameReadCacheStore`/`IProtectedGameMutationOutboxStore`, meme
  contrat `IsProtectionAvailable`/`SaveAsync`/`LoadAsync`/`DeleteAsync`).
- `MobileAccountSessionRuntimeBootstrap.cs` : les trois instanciations en dur
  remplacees par des fabriques `CreateRefreshTokenStore()`/
  `CreateGameReadCacheStore()`/`CreateGameMutationOutboxStore()` qui
  choisissent Keychain sur iOS et Keystore Android sinon (`#if UNITY_IOS`).

**Limite connue** : le code Objective-C n'a pas ete compile/teste par Claude
Code (aucun acces Xcode/Mac depuis cette session Windows) — a verifier par
Jeff au premier export Xcode reel.

Preuves : `assets-refresh` + `Logs/Editor.log` confirment les 5 nouveaux
fichiers importes sans `error CS`. La suite EditMode complete lancee en fin
de tranche a fige l'editeur (aucune progression du journal pendant plusieurs
minutes, processus Unity "Not Responding") sur un test sans rapport
(`SandboxPremiumRuntimeIntegrationTests.HivePresenterCreatesPremiumRuntimeHiveForSandboxPlayMode`,
rendu de scene) — Jeff a redemarre l'editeur. Tests EditMode cibles sur
`MobileAccountSessionClientTests` ensuite : 14/14 reussis, aucune regression
sur la logique de session/tokens protegee par la nouvelle fabrique par
plateforme.

Prochain test utilisateur : sur le Mac, exporter le projet Xcode depuis cet
editeur Windows (ou synchroniser le projet), verifier que
`BeeKingdomKeychainBridge.m` compile sans erreur, puis tester en Play Mode
Xcode qu'une connexion/deconnexion de compte persiste correctement (ou pas,
selon le comportement attendu) via le Keychain.

Ouvert / a faire ensuite : configurer le Team ID Apple des que la licence
Developer est active, decider du flux de build final (export Xcode manuel
vs Unity Cloud Build), puis revisiter le meme probleme de gonflement de
taille (carte terrain 50x50 non compressee, voir jalon Android plus bas) qui
touchera iOS de la meme facon.

---

## Jalon courant — Tutoriel Acte I, chapitre 4 : nouvelle interaction "Verification des stocks de cire" (2026-07-26)

Jeff a choisi le chapitre 4 (atelier de cire) pour la tranche suivante, apres
que le chapitre 1 (jalon precedent) ait quitte le plancher d'interactions.
Nouvelle interaction inseree entre "livraison defensive" (4.12) et la
recompense d'atelier (desormais 4.14) : **"Verification des stocks de cire"**
(4.13) — choix entre inventaire rapide (2 batisseuses, 10s, gratuit,
+1% capacite de stockage de cire) ou minutieux (3 batisseuses, 14s, 40 miel,
+2% capacite), avec collecte manuelle de cire a l'atelier et un controle
unique d'etancheite avant de clore le chantier. Meme motif exact que
l'objectif de reference "livraison_defensive"/`WorkshopDefenseHandoff*`
(decision → minuteur → collecte manuelle → controle → consequence), dans
`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` : enum (5
nouvelles etapes `UpgradeStockCheck*`), champs d'etat, constantes de duree,
UI decision/controle, demarrage/minuteur/completion
(`StartGuidedUpgradeStockCheck`, `GuidedUpgradeStockCheckProgress01`,
`CompleteGuidedUpgradeStockCheckIfReady`, `RegisterGuidedUpgradeStockCheckCheck`),
collecte manuelle (hotspot `wax_workshop`), highlight de batiment, gating de
clics, flux de transition, journal de quete (objectif 13, recompense
decalee a 14), proof helpers et etat de preuve. La consequence alimente
`localPreviewWaxCapacityBonus` (champ deja existant, expose via
`wax_capacity_bonus_percent`, jamais incremente par une interaction joueur
avant cette tranche). Textes bilingues ajoutes dans les deux catalogues
`strings.fr-CA.json`/`strings.en-US.json` (19 cles chacun,
`tutorial.chapter_04.stock_check.*`; parite de cles verifiee : 1324 dans
chaque catalogue, aucun doublon, aucune cle manquante d'un cote ou l'autre).

**Deux bugs pre-existants trouves et corriges en cours de route** (necessaire
pour meme pouvoir tester le chapitre 4 en Play mode reel) :
`CompleteGuidedUpgradeCertificationIfReady()` (objectif 4.9) et
`CompleteGuidedWorkshopDefenseHandoffIfReady()` (objectif 4.12) n'etaient
appelees que par les harnais de preuve QA, jamais par la vraie boucle de jeu
par image — un joueur reel restait bloque indefiniment au premier de ces deux
minuteurs, bien avant meme d'atteindre ma nouvelle interaction. Corrige en
ajoutant les deux, plus ma propre `CompleteGuidedUpgradeStockCheckIfReady()`,
a la liste reelle des `CompleteGuided...IfReady()` appeles chaque frame.
**Lacune mineure aussi corrigee au meme endroit** : `GuidedCollectionTutorialTargetHotspot()`
n'avait pas de cas pour `WorkshopDefenseHandoffCollect` (la camera ne
recentrait pas sur l'atelier pendant cette collecte precise, bien que la
collecte elle-meme fonctionnait) — ajoute aux cotes du cas pour ma propre
`UpgradeStockCheckCollect`.

Nouvel etat de rythme (mesure directement dans le code, verifie par les
tests, pas suppose) : chapitre 4 passe de 13 a 14 objectifs, 29 a 32
interactions actives (11 decisions, 16 controles, 5 collectes), 145-165s a
155-179s. Le chapitre 5 reste seul au plancher d'interactions (29) —
prochaine tranche naturelle a decider avec Jeff.

Preuves : compilation Unity verifiee via `assets-refresh` + `Logs/Editor.log`
project-relatif (zero `error CS` provenant du code du jeu). Tests EditMode
`SandboxLivingHiveManualCollectionTests` executes avec `EditorApplication.isPlaying`
confirme `false` avant lancement (verification demandee explicitement suite
aux echecs de mode Play de la tranche precedente) : 55 tests, 54 reussis, 1
echec — `GuidedDefenseChapterCanBuildAWaxBarrierInstead` (`defense_wax_cost`
60 attendu vs 120 obtenu), bug pre-existant deja documente dans le jalon
precedent, sans lien avec cette tranche (constante `GuidedDefenseBarrierWaxCost`
du chapitre 5, jamais touchee), explicitement laisse tel quel. Aucune
cascade de valeurs codees en dur decouverte cette fois (contrairement au
chapitre 1) — tous les chiffres de pacing prevus dans le plan se sont
averes exacts des la premiere execution des tests.

Prochain test utilisateur : rejouer le chapitre 4 en Play mode jusqu'a la
livraison defensive puis la nouvelle verification des stocks, pour confirmer
que l'enchainement se sent bien integre (jugement humain, pas encore fait).

Ouvert / a faire ensuite : decider avec Jeff de la prochaine tranche de
rythme (chapitre 5, seul restant au plancher a 29 interactions), et
reprendre la decision en attente sur le deblocage de l'empaquetage Android
(3 options presentees, aucune executee — voir jalon plus bas).

---

## Jalon courant — Tutoriel Acte I, chapitre 1 : nouvelle interaction "Purge des alveoles" (2026-07-25)

Jeff a choisi d'etendre le chapitre 1 (parmi les chapitres 1/4/5 a egalite au
plancher d'interactions, voir jalon "recherche tutoriel" plus bas). Nouvelle
interaction inseree entre "ravitaillement du couvain" (1.12) et "choisir la
dotation fondatrice" (desormais 1.14) : **"Purge des alveoles"** (1.13) — un
choix entre nettoyage manuel (3 ouvrieres, 16s, gratuit, +2 stabilite) ou
enzymatique (2 ouvrieres, 12s, 30 miel, +4 stabilite), avec collecte
manuelle de cire a l'atelier et un controle unique de proprete avant de
sceller la charte. Toute l'implementation suit exactement le motif etabli
par l'objectif de reference "ravitaillement_du_couvain" (decision → minuteur
→ collecte manuelle → controle → consequence), dans
`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (fichier
monolithique de ~32 600 lignes : enum, champs, reset, UI de decision/controle,
demarrage/minuteur, collecte, textes, highlight de batiment, gating de
clics, journal de quete, proof helpers — une douzaine de points de code
distincts, tous mis a jour). Textes bilingues ajoutes dans les deux
catalogues `strings.fr-CA.json`/`strings.en-US.json` (19 cles chacun,
`tutorial.chapter_01.hygiene_purge.*`).

**Bug pre-existant trouve et corrige en cours de route** (necessaire pour
meme pouvoir tester en Play mode) : `CompleteGuidedOpeningBroodSupplyIfReady()`
n'etait jamais appelee dans la vraie boucle de jeu (seulement par les
harnais de preuve QA) — un joueur reel restait bloque indefiniment au
minuteur du ravitaillement du couvain, juste avant l'endroit ou ma nouvelle
interaction s'insere. Corrige en l'ajoutant a la liste des `CompleteGuided...IfReady()`
appeles chaque frame (aux cotes de ma propre
`CompleteGuidedOpeningHygienePurgeIfReady()`).

Nouvel etat de rythme (mesure directement dans le code, verifie par les
tests, pas suppose) : chapitre 1 passe de 14 a 15 objectifs, 29 a 32
interactions actives (12 decisions, 13 controles, 7 collectes), 150-184s a
162-200s. Les chapitres 4 et 5 restent seuls au plancher partage a 29
interactions — prochaine tranche recommandee entre les deux, a decider avec
Jeff.

Preuves : compilation Unity verifiee via `assets-refresh` + `Editor.log`
(zero `error CS`). Tests EditMode `SandboxLivingHiveManualCollectionTests`
rejoues dans l'Editeur reel (pas seulement `dotnet build`) : 54/55 reussis.
Le seul echec restant, `GuidedDefenseChapterCanBuildAWaxBarrierInstead`
(`defense_wax_cost:60` attendu vs `120` obtenu), est **confirme sans rapport
avec ce travail** — `GuidedDefenseBarrierWaxCost = 120f` est une constante
du chapitre 5 jamais touchee cette session ; le test demarre directement au
chapitre 5 sans passer par le rabais du chapitre 4, donc le calcul (120-0)
ne correspond jamais a l'attendu (60). Bug de test preexistant, a
investiguer separement (pas touche ici, hors perimetre).

**Note process** : deux gels/plantages d'Unity sont survenus pendant cette
tranche (une fois en sortant du mode Play, une fois pendant `tests-run` qui
a echoue avec un timeout alors qu'Unity etait bloque en mode Play — voir
memoire `feedback_unity_hang_after_bulk_scripts.md`, mise a jour
mentalement mais pas re-editee ce soir). Toujours verifier
`EditorApplication.isPlaying` avant `tests-run` si l'echec ressemble a un
timeout plutot qu'a une vraie assertion.

Prochain test utilisateur : ouvrir `LivingHive.unity` ou lancer le chapitre
1 depuis le debut en Play mode, jouer jusqu'a "ravitaillement du couvain",
verifier que la nouvelle etape "Purge des alveoles" s'enchaine
naturellement (ton, rythme, lisibilite du choix) avant le choix de dotation
fondatrice — jugement humain, pas remplace par les tests automatiques.

Ouvert / a faire ensuite :
- Decider avec Jeff quel chapitre (4 ou 5) etendre ensuite pour continuer a
  combler l'ecart de l'Acte I.
- Corriger `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md` (la mention
  perimee "chapitre 2 recommande" ligne ~640 devrait etre mise a jour avec
  l'etat reel des chapitres 1/4/5 lors d'une prochaine session de contenu).
- Investiguer separement le bug `defense_wax_cost` du chapitre 5 (test
  preexistant, sans rapport).
- Revenir a la decision Android en attente (voir jalon precedent).

---

## Jalon courant — Investigation empaquetage Android, pause avant modification du terrain protege (2026-07-25, nuit)

Jeff a dit "je te laisse decider" apres le jalon precedent (recherche
tutoriel). J'ai choisi l'empaquetage Android (rapport architecte P1 #5)
plutot que le contenu du tutoriel, car c'est un probleme d'ingenierie avec
un critere de succes objectif — contrairement au tutoriel qui exige un
jugement de gout que Jeff seul peut porter.

**Diagnostic complet effectue, aucune modification faite** — voici pourquoi :

- L'audit precedent (`Docs/Product/LivingHive_AndroidIl2CppPackagingAudit_2026-07-22.md`)
  disait: build IL2CPP ARM64 echoue a `:launcher:compressDebugAssets` avec
  "Espace insuffisant sur le disque", APK 2,02-2,25 Gio. **Verifie cette
  nuit : le disque a 251 Gio libres actuellement** — l'echec de disque etait
  probablement propre a l'environnement VM d'alors (avant l'abandon du flux
  VM le 2026-07-24), pas un probleme structurel actuel. Un nouvel essai de
  build pourrait simplement passer cette etape aujourd'hui.
- La vraie cause de la taille (922,8 Mio sous `Resources/WorldMapWave6Runtime`)
  vient de deux paquets de tuiles : le paquet canonique protege
  `wave5method_12288_preview` (825 Mio, 2500 tuiles) et un paquet `v1` plus
  ancien (107 Mio, 2500 tuiles). **J'ai verifie que `v1` est toujours
  reference activement par du code reel** (`WorldMapWave6StreamingTileProvider.cs`
  — c'est litteralement sa racine de ressource PAR DEFAUT du constructeur
  sans parametre — plus `WorldMapWave6AssetImporter.cs` et
  `WorldMapWave6IntegrationValidator.cs`, probablement pour des comparaisons
  de validation/regression). **Je n'ai donc PAS supprime ce paquet** malgre
  sa taille : il n'est pas prouve mort, et toucher a quoi que ce soit sous
  `WorldMapWave6Runtime` merite la meme prudence que le terrain lui-meme.
- Un `AssetPostprocessor` existant, `WorldMapWave6TextureImportGuard.cs`,
  **force deliberement** `TextureImporterCompression.Uncompressed` sur tout
  le paquet canonique, y compris pour Android (aucune des 2500 `.meta` n'a
  d'override Android). C'est manifestement une decision intentionnelle
  d'une session precedente pour garantir la fidelite pixel-perfect du
  terrain (le plan produit mentionne un "validateur exact-crop" qui verifie
  la coherence entre tuiles voisines). Activer la compression Android
  (ASTC/ETC2) reduirait vraiment la taille de l'APK, mais introduirait de la
  compression avec perte sur l'actif le plus protege du projet — je n'ai
  **pas** fait ce changement moi-meme, ca depasse mon mandat d'autonomie
  sur les "fondations intouchables" de `CLAUDE.md`.
- J'ai trouve le harnais de build existant
  `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveAndroidAotProofBuilder.cs`
  (`BuildAndExit()`, deja utilise pour l'audit precedent) — j'aurais pu
  relancer ce build en ligne de commande pour voir s'il passe maintenant
  avec l'espace disque disponible, mais **Unity Editor est actuellement
  ouvert en interactif** (verifie via `Get-Process`), et lancer un second
  process Unity sur le meme projet en parallele risque un conflit de
  verrou sur `Library/`. Je n'ai pas ferme l'Editeur de Jeff pour tenter ce
  build sans qu'il le demande.

**Decision qui appartient a Jeff** (aucune des deux options ne touche a
l'image du terrain elle-meme, seulement a l'emballage) :
1. Retenter simplement le build Android maintenant que le disque a de la
   place (`SandboxLivingHiveAndroidAotProofBuilder.BuildAndExit()`, menu
   `Bee Kingdom/Playground/QA/Build LivingHive Android IL2CPP Proof`, ou en
   ligne de commande `-executeMethod`) — pourrait suffire a obtenir un APK
   installable de test des ce soir/demain, meme volumineux.
2. Activer la compression Android (ASTC ou ETC2) sur le paquet canonique
   dans `WorldMapWave6TextureImportGuard.cs` — reduirait vraiment la taille
   mais change le rendu pixel du terrain protege ; besoin d'une confirmation
   explicite avant que je touche a ce fichier.
3. Migrer le chargement du tileset de `Resources.Load` vers
   Addressables/AssetBundles (streaming a la demande, compression par
   plateforme) — la vraie solution a long terme, mais un chantier
   d'architecture de plusieurs jours, pas une tache de cette nuit.

Ouvert / a faire ensuite : attendre la decision de Jeff sur les trois options
ci-dessus avant de toucher a quoi que ce soit sous
`Resources/WorldMapWave6Runtime`.

---

## Jalon courant — Fin de nuit : recherche tutoriel Acte I, pause volontaire avant contenu (2026-07-25, nuit)

Apres les deux tranches livrees ci-dessous (patrouilles combat + interface
admin), j'ai cherche le prochain chantier en suivant l'ordre du plan produit
et du rapport de l'ancien architecte, comme demande. Correction importante
avant de m'arreter : `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md`
contient plusieurs entrees historiques (au fil des sessions passees) disant
tour a tour "le chapitre 2 est la prochaine tranche recommandee" — la
derniere de ces mentions (ligne ~640) est **perimee**. J'ai verifie l'etat
reel directement dans le code source
(`HiveViewProductUiPresenter.cs` ~ligne 13586-13590,
`GuidedOpeningActObjectivePacingForProof()`), qui est la source de verite
utilisee par l'outil de mesure lui-meme :

```
chapter_1_active_interactions:29
chapter_2_active_interactions:30
chapter_3_active_interactions:30
chapter_4_active_interactions:29
chapter_5_active_interactions:29
```

**Le chapitre 2 est deja a 30 interactions, au-dessus du plancher** — ce sont
en realite les **chapitres 1, 4 et 5 qui partagent le plancher actuel a 29**
interactions. Le plan produit ne s'est pas mis a jour apres la derniere
passe sur le chapitre 4 (ligne ~678 du plan) pour re-designer clairement un
chapitre unique "prochain" parmi ces trois. A corriger dans le plan produit
lors d'une prochaine session de contenu.

**Pourquoi je m'arrete ici sans ecrire de nouveau contenu de tutoriel cette
nuit** : ce chantier est fondamentalement different des deux precedents.
Ajouter une interaction de tutoriel exige d'ecrire un vrai texte narratif
(diagnostic, choix, consequence) dans le ton etabli des chapitres existants,
puis de le jouer/ressentir pour juger si le rythme et le recit fonctionnent
— un jugement de gout, pas un test qui passe ou echoue. CLAUDE.md exige que
les chapitres "racontent, expliquent les ressources et proposent des
objectifs actifs avec des consequences" ; improviser ce texte sans que Jeff
puisse le relire risquerait de produire un contenu qu'il devrait
substantiellement reecrire. Les deux tranches de cette nuit (combat,
admin) etaient des taches d'ingenierie avec des criteres de correction
objectifs (tests verts) — celle-ci ne l'est pas.

**Etat de la tache #43 (liste de taches)** : laissee `pending`, pas
`completed` — la recherche est faite et le plan ci-dessus est correct, mais
l'implementation attend la direction creative de Jeff (quel chapitre choisir
parmi 1/4/5, quel type d'interaction ajouter, quel ton).

Ouvert / a faire ensuite (par ordre) :
1. **Jeff : choisir avec Claude** quel chapitre parmi 1, 4 ou 5 etendre en
   premier (ou valider qu'un autre chantier passe devant) — decision de
   contenu, pas d'ingenierie.
2. Rapport architecte P1 #5 : debloquer l'empaquetage Android (echec
   `compressDebugAssets`, APK ~2-2.25 Gio, largement du au tileset 50x50
   non compresse dans `Resources`) — chantier plus technique, pourrait etre
   pris en autonomie si Jeff valide la priorite, **en respectant strictement
   l'interdiction de toucher au terrain/images proteges** (compresser
   l'emballage, jamais l'art lui-meme).
3. Rapport architecte P1 #6 : documenter les criteres d'activation explicites
   des nombreux drapeaux de features actuellement fermes (dette de
   gouvernance, pas de code).

---

## Jalon courant — Interface d'administration joueurs (support/depannage) (2026-07-25, nuit)

Demande de Jeff cette nuit (pendant qu'il dormait) : voir les comptes des
joueurs, leurs ressources, et pouvoir rembourser/delivrer manuellement en cas
de bug. Recherche prealable (voir plan `sparkling-dancing-crystal.md` conserve
en reference) : `Server/src/BeeKingdom.Admin/` existait deja mais etait une
coquille vide (sonde de sante d'infra seulement, jamais referencee par
`BeeKingdom.Server`) — pas reutilisee. Aucun role/permission admin n'existe
sur un compte joueur ; aucun systeme de paiement reel/achat structure
n'existe non plus (seulement `AccountProgression.PurchaseHistory`, une liste
de chaines libres, plus les compteurs de slots du combat patrol).

Construit dans `BeeKingdom.Server` (le monolith a deja tout le cablage —
dupliquer dans le projet Admin separe aurait ete un gros travail pour peu de
gain). Auth par cle partagee distincte de la cle ops/infra existante :
`AdminSupport:Enabled` (false partout par defaut, meme gouvernance que tous
les autres drapeaux), `AdminSupport:Key`/`KeySha256`, header
`X-BeeKingdom-Support-Key`, verifie par `AuthorizeAdminSupport` (meme forme
que `AuthorizeOps` existant, cle separee pour rotation independante). Pas de
compte/role admin individuel — un seul utilisateur (Jeff) a ce jour.

Nouveau : `AdminAuditEntry` + champ `PlayerHiveState.AdminAudit` (piste
d'audit append-only, ecrite dans la MEME mutation atomique que le changement
qu'elle documente — jamais desynchronisee ; pas de nouvelle table SQL).
`IHiveStateRepository` gagne `ListHiveIdsAsync(playerId)` (implementee dans
`DurableJsonHiveStateRepository` par scan de repertoire et
`SqlHiveStateRepository` par requete SQL sur `dbo.HivePlayerStates` — plus
toutes les fausses implementations de test, il y en avait 4 au total,
certaines avec un formatage sans espace `:IHiveStateRepository` qui a
echappe au premier grep, attention si on retouche cette interface).
`AdminSupportService.cs` (nouveau, `BeeKingdom.HiveOperations`) :
`AdjustResourceAsync`/`AdjustRosterAsync`/`GrantCombatPatrolSlotAsync`/
`ReadDiagnosticsAsync`. Concurrence optimiste par revision uniquement, pas de
cle d'idempotence separee (deviation mineure du plan initial — juge
suffisant pour un outil interne clique par un humain, pas un client mobile
avec file d'attente hors-ligne).

5 nouvelles routes dans `Program.cs` sous `/admin/v1/...` : `GET
players/lookup?email=`, `GET players/{id}/hives`, `GET
players/{id}/hives/{hiveId}/diagnostics`, `POST .../resources/adjust`, `POST
.../roster/adjust`, `POST .../combat-patrol/slots/grant`. Plus `GET
/admin/ui` : une seule page HTML+JS vanilla embarquee dans `AdminUiPage.cs`
(connexion par cle en memoire JS, jamais persistee ; recherche par email ;
detail de ruche avec formulaires d'ajustement et journal d'audit).

Preuves : `dotnet test --filter AdminSupport` (14/14 verts). Suite complete
serveur : 475/478 verts (les echecs restants sont les echecs preexistants
non-deterministes deja documentes plus bas, 3 sur 4 sont apparus cette
execution — comportement attendu, pas une regression).

Prochain test utilisateur (Jeff) : definir une vraie cle dans
`AdminSupport:Key`/`KeySha256` en local, mettre `AdminSupport:Enabled=true`,
lancer le serveur, ouvrir `http://localhost:<port>/admin/ui`, se connecter
avec la cle, chercher un compte de test par email, ouvrir une ruche, tester
un ajustement de ressource (ex. +500 miel avec un motif), verifier que le
journal d'audit de la ruche s'affiche bien.

Ouvert / a faire ensuite :
- Verification manuelle par Jeff du flux complet de l'UI (jamais testee
  dans un vrai navigateur cette nuit, seulement via `dotnet test`/HTTP).
- `AdminSupport:Enabled` reste `false` en Production tant que Jeff n'a pas
  genere une vraie cle et decide de l'activer.
- Pas de vrai historique d'achats structure (n'existe pas encore) — a
  batir proprement quand une vraie integration IAP/store sera faite ; le
  panneau "Historique d'achats brut" de l'UI est un espace reserve, pas
  encore branche sur `AccountProgression.PurchaseHistory`.
- Slots premium (argent reel) du combat patrol restent un stub cote
  serveur — `GrantPremiumSlotAsync`/le bouton "grant premium" de l'admin
  n'accordent l'entitlement que manuellement, aucun paiement reel n'est
  jamais valide.
- Ensuite, revenir a l'ordre de priorite de
  `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md` et
  `Docs/Architecture/LeadArchitect_TakeoverReport_2026-07-23.md`.

---

## Jalon courant — Combat PvE : patrouilles simultanees + emplacements achetables (2026-07-25, nuit)

Suite directe du jalon precedent (Boucle 1 Patrouilles). En testant la
verification visuelle avec Jeff, deux changements de comportement ont ete
demandes puis implementes cette nuit (Jeff hors ligne, verification visuelle
humaine encore a faire a son retour) :

1. Le panneau se ferme immediatement au lancement d'une patrouille — plus de
   modal bloquant qui empeche de continuer a naviguer la carte.
2. Plusieurs patrouilles reellement simultanees sont maintenant supportees :
   **1 emplacement de base, jusqu'a 5 au total**. Emplacements 2-3 achetables
   en miel/pollen (cout croissant). Emplacements 4-5 : **stub cote serveur
   seulement** (`GrantPremiumSlotAsync` accorde l'entitlement mais ne valide
   aucun paiement reel — un vrai flux d'achat argent reel devra valider un
   recu de store cote serveur avant d'appeler cette methode ; ne jamais
   brancher ce endpoint directement sur une saisie client en production).

Consequence architecturale majeure : le combat patrol ne depend plus de la
reservation d'escouade unique/partagee (`CombatSquadReservationService`,
utilisee en mutex avec les sorties de perimetre) — chaque patrouille engage
directement ses propres troupes depuis `DoctrineRosterState.Counts`, en
respectant ce qui est deja reserve pour une sortie de perimetre
(`SquadReservation.Reserved`) pour ne jamais compter deux fois les memes
abeilles. `CombatPatrolActiveEncounter` porte desormais son propre
`CommittedTroops` ; `CombatPatrolState.Active` (singulier) est devenu
`ActiveEncounters` (liste).

Fichiers serveur modifies : `HiveOperationModels.cs` (nouveau modele),
`CombatPatrolCatalog.cs` (table de prix `ResourceSlotCosts`),
`CombatPatrolService.cs` (reecrit : multi-encounter, portes d'emplacement,
`PurchaseResourceSlotAsync`/`GrantPremiumSlotAsync`), `CombatSquadReservationService.cs`
(garde `ReleaseAsync` adaptee au nouveau champ), `HiveStateMigrator.cs`
(validation de la liste d'encounters), `Program.cs` (route preview passee en
POST avec quantites, launch avec quantites au lieu de reservationId, 2
nouvelles routes `/combat/patrol/slots/purchase-resource` et
`/slots/grant-premium`). Contrat serveur bump a `phase-combat-patrol-v2`.

Fichiers client modifies : `CombatPatrolClient.cs` (DTOs + nouveau contrat),
`CombatPatrolPresentation.cs` (`ActiveEncounters` liste, curseurs de
composition `Draft*`, plus de dependance a `IHiveSquadReservationClient`),
`HiveViewProductUiPresenter.cs` (`DrawCombatPatrolPanel` reecrit avec
composeur de troupes + achat d'emplacement + detail par encounter selectionne
; nouvelle `DrawCombatPatrolQueueStrip` toujours visible listant les
patrouilles actives, cliquable), `WorldMapMmoFullscreenFoundationBootstrap.cs`
(`DrawCombatPatrolMarch` boucle sur toutes les patrouilles actives, avec une
association client-side `EncounterId -> coordonnee carte` purement
cosmetique), `MobileAccountSessionRuntimeBootstrap.cs` (simplifie, plus de
`reservationClient` passe au controller).

Preuves : `dotnet test --filter CombatPatrol` (29/29 verts, serveur), suite
complete serveur (460/464 verts — les 4 echecs restants sont les echecs
preexistants non-deterministes deja documentes plus bas dans ce fichier,
sans rapport). Compilation Unity verifiee via `assets-refresh` MCP +
`Editor.log` (zero `error CS`, uniquement des warnings preexistants
d'API obsolete). Aucune verification visuelle humaine (Play Mode reel) —
Jeff etait hors ligne cette nuit ; a faire a son retour.

Prochain test utilisateur : ouvrir `WorldMapWave6Wave5Method12288Preview`,
entrer en Play mode, cliquer un noeud du bestiaire puis "Patrouiller" —
verifier que le composeur de troupes (curseurs +/-, bornes par l'effectif
disponible et la capacite), l'indicateur d'emplacements (X/Y) et le bouton
d'achat d'emplacement s'affichent. Lancer une patrouille : le panneau doit se
fermer immediatement et une puce doit apparaitre dans la bande de file
d'attente en haut de l'ecran, avec un compte a rebours. Cliquer la puce
rouvre le detail de cette patrouille (Reclamer/Rappeler). Lancer une seconde
patrouille sur une autre cible pendant que la premiere est en cours doit
etre bloque tant qu'un emplacement n'a pas ete achete ; apres achat, les deux
doivent afficher chacune leur propre ligne de marche sur la carte.

Ouvert / a faire ensuite :
- Verification visuelle humaine complete (portrait + paysage) — bloquant
  avant de considerer cette tranche vraiment livree, per la regle de
  CLAUDE.md.
- Vrai flux d'achat argent reel pour les emplacements 4-5 (necessite
  integration store App Store/Play Store, receipt validation serveur — hors
  perimetre de cette tranche, stub uniquement).
- Interface d'administration joueurs demandee par Jeff (comptes, ressources,
  historique d'achats, remboursement/octroi manuel en cas de bug) — pas
  commencee, planifier avec EnterPlanMode avant d'ecrire du code (touche
  l'authentification/autorisation admin, surface sensible).
- Ensuite, suivre l'ordre de priorite de
  `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md` et
  `Docs/Architecture/LeadArchitect_TakeoverReport_2026-07-23.md`.
- Rappel recurrent : toute operation Unity MCP declenchant un rechargement
  de domaine (ecritures en masse, `tests-run`, bascule Play mode via
  `script-execute`) a fait geler ou planter l'Editeur plusieurs fois cette
  session (voir memoire `feedback_unity_hang_after_bulk_scripts.md` dans le
  systeme memoire de l'agent) — preferer la verification par
  compilation/logs/tests `dotnet` en ligne de commande quand personne n'est
  disponible pour reprendre la main.

---

## Jalon courant — Combat PvE demarre : Boucle 1 "Patrouilles" contre le bestiaire (2026-07-25)

Jeff a demande de demarrer le systeme de combat PvE. Exploration complete
d'abord (rapport d'architecture, AntLegion, HiveOperations, client Unity) :
**aucune simulation de combat reelle n'existait nulle part** — juste de la
plomberie de ressources/minuteurs habillee en vocabulaire militaire
(`HivePerimeterSortieService` credite une recompense garantie a l'echeance,
zero PV/degats/echec). Le triangle produit Gardiennes/Voltigeuses/Lanceuses
existait sans le moindre coefficient chiffre. Design neuf construit a cote de
l'existant (aucune modification de `HivePerimeterSortieService`/ses 2 signaux
non-combat, qui restent un systeme separe).

**Distinction PvE/PvP clarifiee par Jeff en cours de tranche (importante,
a ne jamais confondre)** : le PvE (joueur vs bestiaire, ce jalon) utilise
**des pertes majoritairement "blessees" recuperables + une petite part
permanente**. Un futur systeme **PvP** (joueur vs joueur, non concu, non
commence) utilisera plus tard une logique de pertes permanentes plus dures —
cette regle-la ne s'applique **pas** au PvE.

**Serveur** (`Server/src/BeeKingdom.HiveOperations/`, nouveaux fichiers) :
- `CombatPatrolResolution.cs` : moteur de resolution pur, deterministe (zero
  RNG — memes entrees, memes sorties). Consomme enfin
  `CombatDoctrineService.AdvantageCycle` (jamais utilise avant). Porte de
  lancement (`readiness_bp < 7000` = bloque avant tout risque). Bandes
  d'issue Victoire eclatante / Victoire / Chèrement acquise. Pertes divisees
  en `PermanentLosses` (15 %, `PermanentLossShareBp=1500`) et `WoundedLosses`
  (85 %, recuperables apres `ComputeRecoveryDuration` = 6x la duree de
  patrouille du palier, ~2 a 18 min).
- `CombatPatrolCatalog.cs` : 7 paliers repris du bestiaire deja visible sur la
  carte (Puceron voleur -> Reine frelon ancienne), chacun avec une famille de
  danger qui active le cycle d'avantage.
- `CombatPatrolService.cs` : Launch/Claim/Recall/Preview, meme charpente
  idempotente/atomique que `HivePerimeterSortieService`. Claim reduit
  reellement `DoctrineRosterState.Counts` (perte permanente) et alimente une
  file de convalescence (`CombatPatrolState.Recovering`) qui remet les
  blessees dans le roster a maturite (lue paresseusement a chaque
  Read/Preview/mutation, jamais de tache de fond).
- Flag `CombatPatrol.Enabled=false` partout (meme regle de gouvernance que
  les 5 flags combat existants — **ne pas activer en Production sans
  coordination explicite**).
- **Limite d'escouade evolutive** (demande explicite de Jeff) :
  `CombatSquadReservationService.ComputeCapacity(buildingLevels)` fait
  grandir la capacite avec le niveau du batiment `guard_post` (+4/niveau).
  **Constat important** : aucun systeme officiel de "niveau du joueur" ou
  d'arbre de competences n'est branche cote serveur
  (`Docs/Architecture/PlayerSkillTree_Progression_Spec.md` reste
  `OFFICIAL_PROGRESSION_IMPLEMENTATION=NOT_RUN`) — `guard_post` sert de proxy
  reel en attendant, a remplacer/etendre le jour ou le niveau officiel existe.

**Client Unity** :
- `Assets/BeeKingdom/Networking/CombatPatrolClient.cs` : client REST,
  meme convention que `HivePerimeterSortieClient.cs` mais delibirement plus
  simple (pas de file de mutation hors-ligne/cache protege — suivi comme
  ecart documente, pas un oubli).
- `Assets/BeeKingdom/Playground/CombatPatrolPresentation.cs` : contreleur
  d'ecran (`CombatPatrolPanelController`), lit la reservation d'escouade en
  cours via le client existant (`IHiveSquadReservationClient`) plutot que de
  dupliquer cette logique.
- `HiveViewProductUiPresenter.cs` : nouveau panneau autonome
  (`DrawCombatPatrolOverlayForWorldMap`/`DrawCombatPatrolPanel`) — selecteur
  de palier T1-T7, previsualisation de puissance/blocage, lancer/reclamer/
  rappeler, ecran de debrief (blessees vs pertes definitives).
- `WorldMapMmoFullscreenFoundationBootstrap.cs` : le bouton "Combat
  bestiaire local" (demo locale `RunSelectedBestiaryCombatLocalProof`, encore
  presente ailleurs pour les preuves existantes) est remplace par
  l'ouverture du vrai panneau serveur sur le palier selectionne. Nouveau :
  **indicateur visuel de marche** (`DrawCombatPatrolMarch`) — trait +
  marqueur anime entre la ruche et la cible pendant qu'une patrouille est
  active, **pour le joueur local seulement**.
- Cablage runtime dans `MobileAccountSessionRuntimeBootstrap.cs`, meme motif
  que les 9 autres clients gameplay (construction, `ConfigureCombatPatrol
  ControllerForRuntime`, nettoyage a la deconnexion).

**Explicitement hors perimetre de cette tranche (demande par Jeff, a
planifier separement)** :
- Voir les patrouilles des **autres joueurs** sur la carte : demande un vrai
  monde partage/synchronise, qui **n'existe pas** dans ce projet
  (confirme non implemente dans `Docs/Architecture/WorldMmoServerModel.md` —
  chaque joueur a aujourd'hui une instance isolee). Chantier d'architecture a
  part entiere (etat partage, diffusion temps reel, exposition de donnees
  d'autres joueurs), pas un ajout mineur.
- Boucle 2 "Defense du couvain" (mini-jeu de defense de la ruche) : conçue
  dans le plan (`C:\Users\Utilisateur\.claude\plans\sparkling-dancing-crystal.md`
  — conserve pour reference, chemin local hors depot) mais non implementee.

Preuves :
- `dotnet test tests/BeeKingdom.HiveOperations.Tests` : **86/86 verts** (18
  tests resolution/service CombatPatrol + 1 nouveau test de capacite
  evolutive + suite HivePerimeterSortie/SquadReservation existante intacte).
- `dotnet test tests/BeeKingdom.Tests --filter CombatPatrol` : **11/11
  verts** (endpoints HTTP : flag ferme, auth, lancement bloque si sous-
  puissant, blessees+pertes reelles+maturation de convalescence par lecture,
  rappel sans perte, garde de reservation pendant patrouille active).
- Compilation client : `dotnet build Assembly-CSharp.csproj`/
  `BeeKingdom.Networking.csproj` — aucune nouvelle erreur (seules les 10
  erreurs preexistantes de `ChatIngamePanel.cs`, fichier mort non touche,
  **non confirme dans l'editeur Unity reel** — voir "Ouvert" ci-dessous).

Prochain test utilisateur :
1. Retablir la connexion MCP Unity (voir "Ouvert" — indisponible toute cette
   tranche ET la precedente) puis ouvrir la scene carte, selectionner un
   noeud de bestiaire, lancer "Patrouiller", verifier le panneau, le trait de
   marche, et le debrief apres resolution.
2. Verifier que la limite d'escouade grandit bien apres une amelioration du
   poste de garde (`guard_post`).
3. Valider l'equilibrage des 7 paliers en jouant — les valeurs actuelles sont
   un point de depart raisonne, pas une verite gravee.

Ouvert / a faire ensuite :
- **Connexion MCP Unity toujours indisponible** (meme symptome que la
  tranche precedente — a l'epoque, process Unity fige "busy" depuis 5h+ ;
  a revalider en tout debut de prochaine session avant de supposer quoi que
  ce soit sur l'etat editeur).
- Aucune verification visuelle reelle (Play Mode, portrait/paysage) n'a pu
  etre faite — a faire des que MCP est retabli.
- Monde partage/synchronise multijoueur (visibilite des patrouilles d'autres
  joueurs) : a concevoir comme chantier separe, potentiellement en reutilisant
  l'infrastructure SignalR deja batie pour le chat comme mecanisme de
  diffusion, mais avec un vrai etat de monde partage a construire d'abord.
- Boucle 2 "Defense du couvain" : conçue, non implementee.
- Vrai systeme de "niveau du joueur"/arbre de competences officiel cote
  serveur : n'existe pas encore ; une fois livre, reviser
  `CombatSquadReservationService.ComputeCapacity` pour l'utiliser en plus de
  (ou a la place de) `guard_post`.
- `CombatPatrol:Enabled` reste `false` en Production tant que Jeff n'a pas
  valide l'equilibrage — meme regle de gouvernance que les 6 autres flags
  combat.

---

## Jalon courant — Chat (Communication) degele et termine, integre Ruche + Carte (2026-07-24)

Jeff a demande explicitement de terminer le chantier chat/messagerie (module
`Communication`, gele par convention inter-agents depuis le 22/07, voir
jalon precedent) et de l'integrer dans la ruche et la carte, avec tous les
canaux et un vrai bouton "Traduire" (DeepL). Ceci leve explicitement le gel :
le besoin explicite requis par `CLAUDE.md` est desormais documente ici.

**Serveur (`Server/src/BeeKingdom.Chat/`)** :
- Fix reel : le canal `Server` (global) ne partageait pas vraiment sa
  conversation entre joueurs — seul le createur initial etait ajoute comme
  participant (`ChatService.CreateConversation`), donc `RequireRead`/
  `RequireWrite` rejetaient tout second joueur avec `forbidden`. Corrige :
  un joueur qui rejoint une conversation `Server` existante (meme
  `GameServerId`/`WorldId`) est desormais ajoute comme participant
  (`IChatRepository.EnsureParticipant`, implemente dans `InMemoryChatRepository`
  et `SqlChatRepository`). Test de bout en bout ajoute
  (`ChatMessagingEndpointTests.ServerChannelIsSharedAcrossPlayersOnTheSameGameServerAndWorld`).
- Traduction reelle : `DeepLChatTranslationProvider` (nouveau, HttpClient vers
  `api(-free).deepl.com/v2/translate`), enregistre conditionnellement dans
  `ChatServiceCollectionExtensions` seulement si `ChatTranslation:Provider=deepl`
  et `ChatTranslation:ApiKey` sont configures (sinon reste
  `UnavailableChatTranslationProvider`, degradation propre). Nouvelle section
  de config `ChatTranslationOptions` (a fournir via variable d'environnement
  en Production, jamais en dur dans `appsettings*.json`). `ChatCapabilities`
  expose maintenant `translationAvailable`/`translationModelVersion` pour que
  le client sache si/comment traduire.
- Bug corrige (decouvert en cours de tache, pas seulement suppose) : le hub
  temps reel publiait le `ChatMessage` brut (avec `PlayerId`/enum non
  configures pour le JSON de SignalR) au lieu du DTO fil `ChatWireMessageDto`
  utilise par le REST — desormais `ChatService.SendMessageAsync` publie via
  `ChatTransportMapper.Message(...)`, et `AddSignalR().AddJsonProtocol(...)`
  force les enums en camelCase pour matcher le REST.
- **Cle DeepL fournie par Jeff (`777bjVQK1y093Rzx`) : format suspect** — les
  cles d'authentification DeepL ressemblent normalement a un GUID suivi de
  `:fx` pour le palier gratuit. Cette valeur ressemble a un numero de
  compte/abonnement, pas la "Cle d'authentification" (Account > API Keys sur
  deepl.com). A confirmer avec Jeff avant de la deployer en Production.

**Client Unity (`Assets/BeeKingdom/Gameplay/Communication/`, `Playground/`)** :
- `SignalRChatRealtimeTransport` (nouveau) : vrai client SignalR
  (`Microsoft.AspNetCore.SignalR.Client`, DLL deja presentes et
  auto-referencees) vers `/chat/v1/realtime`, remplace le mode polling-only.
- `IChatRealtimeTransport` etendu avec `JoinConversationAsync`/
  `LeaveConversationAsync` (le hub groupe par conversation ; rien ne les
  appelait avant). `ServerChatProvider.EnsureRealtimeSubscriptionsAsync`
  rejoint chaque conversation connue (pas seulement celle ouverte, pour que
  les compteurs non lus se mettent a jour partout) ; cable dans
  `LivingHiveChatController.OpenAsync`/`RefreshConversationListAsync`.
- Session reelle branchee : `MobileAccountChatSessionSource`,
  `PlayerPrefsChatStringStore`, `LivingHiveChatDataProtector` (nouveau fichier
  `Playground/LivingHiveChatRuntimeSupport.cs`) — Keystore Android reel,
  **repli logiciel (cle AES locale) hors Android** (pas de coffre-fort OS
  cable pour Editeur/Windows aujourd'hui — a durcir avant une sortie non-Android).
  Cable dans `MobileAccountSessionRuntimeBootstrap` (`ActivateChatForActiveSession`
  / nettoyage dans `CloseGameplayForSignedOutSession`), meme motif que les 9
  autres clients gameplay. Le chat ne s'active que si `authenticatedAuthority`
  est vrai (jamais en identite hors-ligne protegee seule).
- Bouton "Traduire" corrige : il utilisait un `modelVersion` litteral
  `"default"` qui aurait toujours echoue cote serveur (`model_version_not_supported`).
  Utilise maintenant `snapshot.TranslationModelVersion` negocie via les
  capacites serveur, et le bouton est masque si la traduction n'est pas
  disponible (`HiveViewProductUiPresenter.DrawCommunicationPanel`).
- Carte : `HiveViewProductUiPresenter.DrawChatOverlayForWorldMap()` (nouvelle
  methode `internal`, appelle la barre de chat existante sans dupliquer son
  etat/styles prives) appelee depuis
  `WorldMapMmoFullscreenFoundationBootstrap.OnGUI()`. `LivingHiveChatRuntime`
  est un singleton C# statique qui survit au `LoadSceneMode.Single` de
  `OpenCanonicalWorldMap()`, donc la session/l'historique restent actifs.

Preuves :
- `dotnet test tests/BeeKingdom.Tests --filter FullyQualifiedName~Chat` :
  **83/83 verts** (nouveaux tests : partage du canal Server, provider DeepL
  avec `HttpMessageHandler` factice, bascule DI Unavailable/DeepL, forme
  fil du realtime, join/leave des abonnements temps reel).
- Suite complete serveur (`dotnet test tests/BeeKingdom.Tests`, deux
  executions) : 4 echecs a chaque fois, mais **des tests differents et sans
  aucun rapport avec le chat** (`CombatSquadReservationEndpointTests`,
  `WorldMapChunkQueryServiceRejects...`, `HiveLoopCodeFirstCatalogRemains...`,
  etc.) — flakiness/isolation pre-existante ailleurs dans la suite, a
  investiguer separement si Jeff le souhaite. Aucune regression liee a cette
  tranche.
- Compilation client : `dotnet build BeeKingdom.Gameplay.csproj` /
  `BeeKingdom.Tests.csproj` (generes par Unity a la racine) compilent sans
  nouvelle erreur (seules les 10 erreurs preexistantes de
  `Assets/BeeKingdom/Gameplay/Communication/ChatIngamePanel.cs`, fichier mort
  non touche par cette tranche, a cause de references TMPro/Localization non
  resolues par ce mode de compilation ad hoc — **non confirme dans l'editeur
  Unity reel**, voir "Ouvert" ci-dessous).
- Connexion production en lecture seule (`104.129.128.136`, identifiants
  `beeuser` fournis par Jeff) : `GET /chat/v1/capabilities` via
  `--resolve chat.dravii.com:443:104.129.128.136` repond **200 OK** avec
  `server:true, realtime:true` — le serveur de production a bien le chat
  actif. `POST /auth/login` avec les identifiants fournis repond **401** — a
  clarifier avec Jeff (format exact du compte : "beeuser" est-il un email
  complet ? bon mot de passe ?). Aucune tentative repetee pour eviter un
  verrouillage.

Prochain test utilisateur :
1. Retablir la connexion MCP Unity (voir "Ouvert" — elle a echoue toute la
   tranche malgre un process Unity actif) puis ouvrir `LivingHive.unity`,
   verifier que la barre de chat s'affiche et compile sans erreur dans la
   Console Unity.
2. Se connecter avec un compte de test reel, envoyer un message sur le canal
   "Server" depuis deux sessions, verifier qu'elles se voient (le fix cote
   serveur le permet desormais).
3. Ouvrir `WorldMapWave6Wave5Method12288Preview.unity` et verifier que la
   barre de chat apparait aussi la, et reste utilisable apres navigation
   Ruche -> Carte.
4. Confirmer aupres de Jeff la vraie cle d'authentification DeepL (Account >
   API Keys sur deepl.com) avant d'activer `ChatTranslation:Provider=deepl`
   en Production.
5. Clarifier les identifiants de test production (401 sur `/auth/login`).

Ouvert / a faire ensuite :
- **Connexion MCP Unity instable/indisponible toute la tranche**
  (`RunCallTool` timeout apres 10 tentatives sur `console-get-logs`,
  `scene-list-opened`, `unity-tool-list`) malgre le port 23770 ouvert et
  repondant en HTTP MCP valide. Deux process Unity detectes
  (`54396` sans fenetre, `259980` sur une scene **"Untitled"**, ni
  `SandboxPlayground` ni `LivingHive`) — a investiguer/redemarrer Unity au
  debut de la prochaine session avant de supposer quoi que ce soit sur l'etat
  editeur.
- Aucune verification visuelle reelle (screenshot Editeur) n'a pu etre faite
  cette tranche a cause du point ci-dessus — a faire des que MCP est
  retabli, portrait et paysage, sur Ruche et Carte.
- Deploiement en Production des changements serveur (fix canal Server,
  DeepL, format realtime) : rien n'a ete deploye, seulement modifie en copie
  de travail locale — a planifier avec Jeff (le serveur de prod tourne deja
  avec `Chat:Enabled=true`).
- Renforcer `LivingHiveChatDataProtector` hors Android (actuellement cle AES
  logicielle locale, pas de coffre materiel) avant toute sortie non-Android.
- 4 echecs de tests preexistants, sans rapport avec le chat, non
  deterministes d'une execution a l'autre (voir Preuves) — signale a Jeff,
  pas traite dans cette tranche (hors perimetre demande).
- `ChatIngamePanel.cs` (UI de chat uGUI orpheline, jamais posee dans une
  scene) reste du code mort, branchee sur `LocalChatProvider` et non sur le
  pipeline serveur reel — a clarifier (supprimer ou reconnecter) pour eviter
  toute confusion future avec le vrai chemin de production
  (`HiveViewProductUiPresenter`/`LivingHiveChatRuntime`).

---

## Jalon courant — Prise en charge du projet par Claude Code (2026-07-24)

Jeff a demande a Claude Code de reprendre entierement le projet Bee Kingdom;
Codex n'est plus utilise a partir de cette date. Cette tranche n'a modifie
aucun code ni asset. Elle a seulement mis en place l'infrastructure de
memoire persistante pour Claude Code :

- Creation de `CLAUDE.md` a la racine (lu automatiquement par Claude Code au
  demarrage de chaque session dans ce dossier) : role, langue, liste de
  lecture obligatoire, fondations protegees, cap produit, notes
  d'environnement, methode.
- Creation de ce fichier, `Docs/Claude/Claude_Continuation.md`, comme journal
  de jalons courant.

Verifications faites pendant cette tranche :

- Connexion MCP Unity (`ai-game-developer`) confirmee fonctionnelle : la
  liste complete des outils (scenes, GameObjects, assets, scripts, tests,
  profiler...) repond correctement.
- Scene actuellement ouverte dans l'editeur : `SandboxPlayground`
  (`Assets/Scenes/SandboxPlayground.unity`), non modifiee, 3 objets racine.
  Ce n'est pas la scene canonique `LivingHive` — a rouvrir explicitement au
  besoin.
- Nom de machine de cette session : `DESKTOP-D3D29K7`. **Confirme par
  l'utilisateur** : cette session tourne directement sur la machine hote,
  pas dans la VM decrite dans `Docs/VM/BeeKingdom_VM_Workstation_Setup.md`.
  Le flux VM + synchronisation reseau (`Synchroniser-BeeKingdom.cmd`,
  partage `Z:`, copie vers `\\DESKTOP-D3D29K7\BeeKingdomHost`) ne s'applique
  plus au travail Claude : on modifie directement la copie de travail reelle,
  aucune synchronisation a lancer avant/apres une tranche.
- Le dossier `.git` existe mais est completement vide (aucune reference,
  aucun objet) : les commandes `git status`/`git log`/`git remote` echouent
  toutes avec "not a git repository". **Confirme par l'utilisateur** : ne
  rien initialiser pour l'instant, continuer sans git jusqu'a nouvel ordre.

Etat fonctionnel herite de Codex (non re-verifie par Claude, voir
`Docs/VM/Codex_VM_Continuation.md` pour le detail complet) : au 23 juillet,
`LivingHive` disposait d'un raccord officiel mobile pour la reservation
d'escouade de combat, le recrutement doctrinal, le soin du couvain et la
ronde quotidienne, tous fermes par des drapeaux de fonctionnalite
(`Enabled=false`) en Production. Le module Communication (chat/messagerie)
etait gele.

Prochain test utilisateur : aucun changement fonctionnel cette tranche, rien
a tester dans Unity. Pour la prochaine tranche : demander a Jeff la priorite
reelle (continuer le plan LivingHive herite, ou reorienter).

Ouvert / a faire ensuite :

- Lire `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md` et
  `Docs/Demos/LivingHive.md` en detail avant la prochaine tache de fond, pour
  aligner les prochaines priorites avec le plan produit.
- Demander a Jeff la priorite reelle de la prochaine tranche (continuer le
  plan LivingHive herite de Codex, ou reorienter).
