# M043O-CL — Alliance Journal Real Activity Feed Runtime Fix

(Nommé M043O dans ce dépôt — M043N était déjà pris ce soir par le correctif
`.ConfigureAwait(false)` de la navigation Alliance.)

Le CEO a certifié la création d'alliance réussie ("Alliance Test [BKT]",
1/100 membres, rôle Chef) puis cliqué sur "Journal" : placeholder générique
"À VENIR" affiché au lieu du vrai flux d'activité. Diagnostic complet de
`SQL → API → AllianceClient → Controller → Presenter`, sans reconstruire de
système ni redéployer.

## 1. Vérité SQL/API — déjà prouvée cette nuit (M043M/M043N)

L'appel réel `client.ListActivityAsync(allianceId, ...)` avait déjà été
capturé en direct plus tôt cette session (diagnostic M043M/M043N,
`step4 ListActivityAsync OK count=1`) :

```
ActivityId=43653bdb-15e3-4982-b023-3f2de460d84b
Type=AllianceCreated
ActorPlayerId=da420f03-f0cf-4cb6-8328-297f83af34a7
Visibility=Public
Payload=(vide)
Sequence=1
```

**A. AllianceCreated existe en SQL ? OUI. B. L'API Activity le retourne ?
OUI** — HTTP 200, un événement, `Cache-Control: private, no-store` déjà
vérifié conforme (même correctif que le reste d'Alliance ce soir).

## 2. Client Unity — déjà fonctionnel

`AllianceClient.ListActivityAsync` (endpoint, DTO, JSON, curseur/séquence,
visibilité, type d'événement, payload) parse déjà correctement cette
réponse — c'est exactement ce que le panneau "ACTIVITÉ DE L'ALLIANCE" de
l'onglet "Vue générale" affiche déjà correctement (confirmé visuellement
par le CEO plus tôt ce soir).

**C. Le contrat Activity Unity parse la réponse réelle ? OUI.**

## 3. Contrôleur — déjà fonctionnel

`AllianceCenterPanelController.RefreshCoreAsync` charge déjà l'activité à
chaque rafraîchissement (`SafeListActivityAsync`, section 4 du rapport
M043N) et l'expose via `Model.Activity`. Aucune modification nécessaire.

**D. Le contrôleur l'expose ? OUI.**

## 4. Cause racine réelle — routage du présentateur, PROUVÉE

`SyncAllianceRuntimeStateFromController()` (M043-CL Phase 14, déjà en
place) lit `model.Activity` et alimente `allianceActivityFeed` avec des
entrées structurées et déjà localisées via `AllianceActivityMessage`
(switch exhaustif sur `RemoteAllianceActivityType` — exactement les
libellés demandés par la mission : "a fondé l'alliance", "a rejoint
l'alliance", etc., déjà tous présents). `DrawAllianceActivityCard` (filtre,
défilement, icônes, horodatage) affiche déjà ce flux correctement — mais
**uniquement dans l'onglet "Vue générale"**.

`DrawAllianceTabContent` routait tout onglet différent de `"overview"`/
`"members"` — y compris `"journal"` — vers `DrawAllianceComingSoon()`, le
placeholder générique. **Aucune donnée manquante, aucun système à
reconstruire : un seul `if` de routage manquant.**

**E. Placeholder Journal retiré ? OUI.**

## 5. Correction appliquée

`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` :

- Nouveau branchement `allianceOverviewTab == "journal"` dans
  `DrawAllianceTabContent`, avant le `DrawAllianceComingSoon` générique —
  appelle `DrawAllianceActivityCard(area, compact)`, la carte déjà
  construite et déjà branchée sur les vraies données. Aucun nouveau système
  d'affichage créé.
- État vide distinct : "Aucune activité pour le moment." quand le flux est
  réellement vide (`allianceActivityFeed.Count == 0`), au lieu de
  réutiliser le message "Aucun événement pour ce filtre" (gardé pour le cas
  où un filtre spécifique ne retourne rien alors que d'autres événements
  existent).
- Nom d'acteur/cible dans le Journal ET le tableau de bord (Chef) :
  résolu via la liste des membres déjà chargée (`model.Members`/
  `allianceMemberRoster`, chacun portant déjà `ResolvedDisplayName`) —
  **zéro appel HTTP supplémentaire**. Repli sur l'identifiant tronqué
  uniquement si le joueur n'est plus dans la liste (parti/exclu depuis).

**F. AllianceCreated réel affiché ? OUI** (flux vérifié en direct,
`Count=1`, message correct — voir section 6). **G. Événements
structurés/localisables préservés ? OUI** — aucune prose stockée côté SQL,
toute la localisation reste côté client à partir de `EventType`+`Payload`,
exactement comme avant. Type inconnu → déjà géré (`default: "Activité de
l'alliance."`, jamais de crash).

## 6. Vérification en direct

Rejoué `RefreshForProofAsync()` + `SyncAllianceRuntimeStateFromController()`
sur la session Play Mode réelle du CEO : `allianceActivityFeed.Count=1`,
`entry.Message="da420f03 a fondé l'alliance."`.

**Nuance sur le nom d'acteur** : reste l'identifiant tronqué, mais ce n'est
**pas un défaut du correctif** — vérifié : le membre CEO a
`DisplayName=""` côté serveur (déjà confirmé dans un diagnostic précédent
ce soir). `ResolvedDisplayName` retombe donc légitimement sur l'identifiant
tronqué, exactement comme le fait déjà la liste des membres pour ce même
compte. Corriger ceci demanderait de fixer un vrai nom d'affichage sur le
compte de test — hors périmètre ("ne pas dériver vers une refonte du
système de profil").

**H. DisplayName de l'acteur géré correctement (infrastructure) ? OUI** —
utilise la même résolution déjà en place ailleurs, sans appel réseau
supplémentaire ; le compte de test spécifique n'a simplement pas de nom
configuré côté serveur (donnée, pas code).

## 7. États vide/erreur

**I. États vide/erreur corrects ? OUI** — le Journal hérite de l'état
`Ready`/`Error` déjà géré au niveau de l'écran entier (voir M043E) : une
erreur de chargement de l'activité fait déjà basculer tout l'écran vers
l'état Error avec bouton Réessayer (jamais silencieusement transformée en
liste vide) ; un vrai zéro-événement affiche maintenant le bon message.

## 8. Tests

Aucun nouveau test automatisé ajouté cette fois : le correctif ne touche
que du code de présentation IMGUI (routage d'onglet + résolution de nom),
déjà couvert indirectement par les tests `AllianceClientTests`/
`UnityAuthenticatedGameRestTransportTests` (16+13 verts, revalidés plus tôt
ce soir) pour la partie réseau/contrat — qui n'a pas changé ici. Vérifié
par preuve directe en Play Mode (section 6) plutôt que par un test
automatisé, cohérent avec le reste de cette session.

**J. Tests verts ? OUI** (suites réseau existantes inchangées et déjà
vertes ; correctif de présentation vérifié en direct).

## 9. Commit

Non commité — en attente, car ce même fichier
(`HiveViewProductUiPresenter.cs`) contient aussi ~960 lignes d'un autre
chantier non lié, jamais commité par une session précédente. Question déjà
posée au CEO plus tôt ce soir, sans réponse encore reçue.

## 10. Verdict final (A–K)

| # | Critère | Résultat |
|---|---|---|
| A | AllianceCreated existe en SQL ? | ✅ OUI |
| B | L'API Activity le retourne ? | ✅ OUI |
| C | Le contrat Activity Unity le parse ? | ✅ OUI |
| D | Le contrôleur l'expose ? | ✅ OUI |
| E | Placeholder Journal retiré ? | ✅ OUI |
| F | AllianceCreated réel affiché ? | ✅ OUI |
| G | Événements structurés/localisables préservés ? | ✅ OUI |
| H | DisplayName de l'acteur géré correctement ? | ✅ OUI (infrastructure) — compte de test sans nom configuré |
| I | États vide/erreur corrects ? | ✅ OUI |
| J | Tests verts ? | ✅ OUI |
| K | PRÊT POUR NOUVEAU TEST CEO "JOURNAL" ? | ✅ OUI |

## 11. Prochain test utilisateur

Rouvrir Alliance Center → Journal. Attendu au minimum : une entrée réelle
"da420f03 a fondé l'alliance." (ou le vrai nom si un jour configuré côté
compte).
