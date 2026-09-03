# HiveMap UI Window Occlusion Guide

Date: 2026-09-03  
Audience: agents Unity / Bee Kingdom travaillant sur les fenêtres HiveMap  
Statut: règle d'architecture active

## Objectif

Toute nouvelle fenêtre HiveMap doit respecter une règle simple côté joueur :

> Si une surface UI est visuellement opaque, aucun élément monde ne doit apparaître par-dessus ses pixels.

Cela inclut :

- les icônes de collecte manuelle;
- les petites abeilles de production;
- les abeilles ambiantes visibles dans la ruche;
- les badges, feedbacks, highlights ou effets décoratifs liés aux bâtiments;
- les éléments futurs dessinés comme "présentation monde".

La solution correcte n'est pas de cacher ou désactiver ces éléments. Ils doivent continuer à vivre, se déplacer, produire, tick et conserver leur état. Seuls les pixels situés derrière une fenêtre opaque doivent être occultés.

## Le piège qui a causé M044-CX

HiveMap mélange plusieurs systèmes de rendu :

1. La caméra monde rend la ruche, les bâtiments et certaines abeilles 3D.
2. `LivingHiveMenuCanvas` rend le HUD/rail en uGUI `ScreenSpaceOverlay`.
3. Plusieurs fenêtres legacy sont rendues en IMGUI via `OnGUI`.
4. Les marqueurs de production HiveMap sont aussi rendus en IMGUI via `HiveMapProductionBootstrap`.

Dans Unity, l'ordre visuel entre ces familles n'est pas automatiquement celui qu'un humain attend. Une fenêtre noire en IMGUI peut sembler "au-dessus", puis un autre `OnGUI` peut redessiner une icône de collecte après elle. De même, une uGUI `ScreenSpaceOverlay` peut passer au-dessus d'un IMGUI selon le flux actif.

Le symptôme M044-CX était donc :

- Chat/Mail plein écran était opaque;
- les marqueurs de production étaient dessinés après;
- les icônes et abeilles de collecte passaient par-dessus la fenêtre noire;
- un patch précédent a essayé de corriger en désactivant des abeilles, ce qui créait une disparition visible au lieu d'une occlusion.

## Principe officiel

Chaque fenêtre doit déclarer ce qu'elle occulte.

Les éléments monde qui sont rendus par un pipeline overlay doivent lire la carte d'occlusion centrale avant de se dessiner.

Dans le code actuel, cette carte est :

`Assets/BeeKingdom/Playground/HiveMapUiOcclusion.cs`

Elle retourne des régions visibles en coordonnées IMGUI écran. Les marqueurs monde IMGUI se dessinent seulement dans ces régions.

## Vocabulaire

Fenêtre plein écran:

Une fenêtre qui possède toute la surface du Game View. Exemples : Communication CHAT, MAIL, Royal Palace, Research, Army.

Fenêtre flottante:

Une fenêtre qui ne couvre qu'un rectangle partiel. Exemple : mini-chat flottant.

Surface opaque:

Tout rectangle dont les pixels doivent cacher la ruche et les marqueurs derrière. Même si le design laisse légèrement transparaître l'image, si le joueur perçoit cette surface comme une fenêtre/panneau propriétaire, elle doit être enregistrée comme occluder.

Présentation monde:

Tout élément qui appartient conceptuellement à la ruche/carte mais qui est dessiné en overlay pour des raisons pratiques : badge, icône, feedback de collecte, abeilles de production, halo, timer ancré à un bâtiment.

## Règle de décision

Quand tu crées une nouvelle fenêtre, réponds à ces questions dans cet ordre :

1. Est-ce une vraie fenêtre plein écran?
2. Est-ce une fenêtre flottante opaque?
3. Est-ce un panneau HUD/rail/header persistant?
4. Des éléments monde peuvent-ils passer visuellement derrière elle pendant un zoom, un pan ou une animation?
5. La fenêtre peut-elle recevoir des clics au-dessus du monde?

Si la réponse à 4 est oui, ajoute son rectangle dans `HiveMapUiOcclusion`.

Si la réponse à 5 est oui, ajoute aussi sa garde dans `HiveMapOverlayInputGateBootstrap` ou dans une garde pointer-scoped si la fenêtre est non modale.

Occlusion visuelle et blocage input sont deux responsabilités différentes. Une fenêtre peut être :

- occluder visuel seulement;
- input blocker seulement;
- les deux.

Ne mélange pas les deux par accident.

## Fenêtres plein écran

Une fenêtre plein écran doit exposer ou utiliser un flag stable, par exemple :

```csharp
public static bool MyWindowOpenForExternalHost => myWindowOpen;
```

Puis elle doit être ajoutée dans `HiveMapUiOcclusion.IsFullscreenOverlayOpen()` :

```csharp
private static bool IsFullscreenOverlayOpen()
{
    return HiveViewProductUiPresenter.CommunicationOverlayOpenForExternalHost
        || MyWindowBootstrap.ModalOpenForExternalHost;
}
```

Quand ce flag est vrai, `HiveMapUiOcclusion` ajoute :

```csharp
new Rect(0f, 0f, Screen.width, Screen.height)
```

Cela signifie : aucune région visible n'existe pour les marqueurs monde IMGUI. Leur `Update` continue, mais leur rendu sous la fenêtre est occulté.

## Fenêtres flottantes

Une fenêtre flottante ne doit pas être ajoutée comme plein écran. Elle doit exposer son rectangle réel :

```csharp
public static bool MyFloatingWindowOpenForExternalHost => myWindowOpen;
public static Rect MyFloatingWindowRectForExternalHost => CalculateMyWindowRect();
```

Puis `HiveMapUiOcclusion.FillOpaqueUiOccluders(...)` doit ajouter ce rectangle :

```csharp
if (MyWindowBootstrap.MyFloatingWindowOpenForExternalHost)
{
    results.Add(MyWindowBootstrap.MyFloatingWindowRectForExternalHost);
}
```

Si la fenêtre utilise une animation de scale, slide ou fade qui change son rectangle dessiné, expose le rectangle après animation, pas seulement le rectangle cible.

Exemple déjà corrigé :

```csharp
public static Rect MiniChatOcclusionRectForExternalHost => MiniChatFloatingOcclusionRect(false);

private static Rect MiniChatFloatingOcclusionRect(bool worldMap)
{
    return UIAnimationLibrary.ApplyWindowAnimation(MiniChatFloatingRect(worldMap), "communication");
}
```

## Panneaux HUD, rail et header

Les surfaces persistantes comme le header haut et le rail bas doivent rester dans la carte d'occlusion, même quand aucune fenêtre modale n'est ouverte.

Leurs rectangles viennent des specs de géométrie, pas d'une approximation visuelle :

```csharp
LivingHiveMenuSpec.RailRectForProof(portrait, Screen.width, Screen.height);
LivingHiveMenuHeaderData.PortraitHeaderRect(Screen.width, Screen.height);
LivingHiveMenuHeaderData.LandscapeHeaderRect(Screen.width, Screen.height);
```

Si un nouveau panneau uGUI persistant est ajouté, il doit avoir une méthode de géométrie équivalente, idéalement dans le même style `*RectForProof`.

## Comment dessiner un élément monde IMGUI

Ne dessine pas directement sur tout l'écran.

Utilise les régions visibles retournées par `HiveMapUiOcclusion.GetWorldPresentationVisibleRegions(...)`.

Pattern recommandé :

```csharp
private readonly List<Rect> visibleRegions = new List<Rect>(16);

private void OnGUI()
{
    HiveMapUiOcclusion.GetWorldPresentationVisibleRegions(visibleRegions);

    for (int i = 0; i < visibleRegions.Count; i++)
    {
        Rect visible = visibleRegions[i];
        GUI.BeginGroup(visible);
        DrawWorldMarkersWithOffset(visible.position);
        GUI.EndGroup();
    }
}

private void DrawWorldMarkersWithOffset(Vector2 clipOffset)
{
    Rect marker = CalculateMarkerRect();
    Rect clippedMarker = new Rect(
        marker.x - clipOffset.x,
        marker.y - clipOffset.y,
        marker.width,
        marker.height);

    DrawMarker(clippedMarker);
}
```

Important : `GUI.BeginGroup(rect)` change l'origine locale du dessin. Il faut donc soustraire `visible.position` aux rectangles dessinés dans le groupe. Sinon les icônes seront décalées ou sembleront disparaître.

## Ce qu'il ne faut jamais faire

Ne jamais corriger une fuite visuelle en coupant l'objet :

```csharp
beeRoot.SetActive(false);
renderer.enabled = false;
canvas.enabled = false; // sauf décision explicite pour une UI qui doit réellement sortir du mode courant
```

Ne jamais faire une garde globale dans un renderer monde du type :

```csharp
if (AnyOverlayOpen) return;
```

Cette approche est acceptable uniquement pour des UI de contexte qui n'existent que dans le mode de base, comme une sidebar propre au mode ruche. Elle ne doit pas être utilisée pour des entités monde qui doivent continuer à exister derrière la fenêtre.

Ne jamais supposer que `GUI.depth`, `DefaultExecutionOrder` ou l'ordre des scripts réglera seul le problème. Ils peuvent aider à stabiliser un cas, mais l'occlusion doit rester géométrique et déclarative.

Ne jamais utiliser une approximation si le code possède déjà une fonction de layout. Les rectangles d'occlusion doivent venir de la même source que la fenêtre dessinée.

## Input gating

Une fenêtre opaque n'empêche pas automatiquement les clics de traverser.

Pour une fenêtre plein écran modale, ajoute son flag dans :

`Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs`

La règle actuelle :

```csharp
public static bool IsAnyOverlayBlocking()
```

doit retourner `true` si la fenêtre possède l'écran et doit bloquer les clics monde.

Pour une fenêtre flottante non modale, ne bloque pas tout le monde. Utilise une garde limitée à la position du pointeur :

```csharp
Vector2 guiPoint = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
bool blockedByWindowPointer = MyFloatingRect.Contains(guiPoint);
controller.IsEnabled = !blocked && !blockedByWindowPointer;
```

Le mini-chat suit cette règle : il est non modal, mais ses pixels bloquent les clics derrière son propre rectangle.

## uGUI ou IMGUI?

Préférence long terme : les nouvelles fenêtres complexes doivent être portées en uGUI ou UI Toolkit, avec des Canvas/Sorting explicites, des composants testables et une géométrie partagée.

IMGUI reste toléré dans HiveMap pour :

- les adaptateurs legacy `*ForExternalHost`;
- les fenêtres provisoires;
- les panneaux simples en attente de port;
- les outils internes ou preuves.

Si une nouvelle fenêtre IMGUI est créée, elle doit obligatoirement :

- exposer son état ouvert;
- exposer son rectangle d'occlusion;
- être enregistrée dans `HiveMapUiOcclusion`;
- être enregistrée dans l'input gate si elle reçoit des clics;
- avoir au moins un test de géométrie ou une preuve Play Mode.

## Checklist pour créer une nouvelle fenêtre

Avant de coder :

- Identifier si la fenêtre est plein écran, flottante ou persistante.
- Identifier si elle est modale ou non modale.
- Identifier son propriétaire d'état.
- Identifier la source unique de son rectangle.
- Identifier les éléments monde susceptibles de passer derrière.

Pendant le codage :

- Ajouter un flag `*OpenForExternalHost` ou équivalent.
- Ajouter un `Rect *RectForExternalHost` pour toute fenêtre partielle.
- Si animation de fenêtre, exposer le rect animé.
- Ajouter la fenêtre à `HiveMapUiOcclusion`.
- Ajouter la fenêtre à `HiveMapOverlayInputGateBootstrap` si elle bloque les clics.
- Ne pas désactiver abeilles, badges, renderers ou GameObjects pour masquer une fuite.
- Garder `Update` et l'état gameplay indépendants du rendu.

Après le codage :

- Compiler `Assembly-CSharp`.
- Compiler `Assembly-CSharp-Editor` si un test ou hook Editor a changé.
- Ajouter ou mettre à jour un test de rectangle.
- Faire une preuve Play Mode en pan et zoom.
- Tester paysage et portrait/mobile.
- Tester ouverture, animation, fermeture.
- Tester qu'un clic sur la fenêtre ne clique pas le bâtiment derrière.
- Tester qu'après fermeture, les éléments monde sont encore vivants et visibles.

## Tests recommandés

Test de soustraction de rectangles :

```csharp
HiveMapUiOcclusion.SubtractForProof(
    new Rect(0f, 0f, 100f, 100f),
    new Rect(20f, 30f, 40f, 50f),
    result);
```

Le test doit prouver que la région opaque découpe l'espace visible en rectangles restants.

Test de fenêtre flottante :

```csharp
HiveViewProductUiPresenter.OpenCommunicationPanelForProof();
Rect miniChat = HiveViewProductUiPresenter.MiniChatOcclusionRectForExternalHost;
Assert.That(miniChat.width, Is.LessThan(Screen.width));
Assert.That(miniChat.height, Is.LessThan(Screen.height));
```

Le test doit prouver qu'une fenêtre flottante n'est pas accidentellement traitée comme plein écran.

Test Play Mode visuel :

1. Ouvrir la scène canonique HiveMap.
2. Placer une icône de collecte sous le futur rectangle de fenêtre.
3. Ouvrir la fenêtre.
4. Pan/zoom pendant que l'élément monde continue d'évoluer.
5. Vérifier que l'élément passe sous les pixels opaques, sans pop ni disparition globale.
6. Fermer la fenêtre.
7. Vérifier que l'élément est encore actif et cohérent.

## Règle d'acceptation

Une nouvelle fenêtre HiveMap n'est pas prête si l'une des phrases suivantes est vraie :

- "On ne voit plus l'élément parce qu'on l'a désactivé."
- "Ça marche seulement si la fenêtre est ouverte avant le marqueur."
- "Ça marche seulement à ce zoom."
- "Ça marche en plein écran mais pas sur une fenêtre flottante."
- "On n'a pas testé avec un pan/zoom."
- "Le bouton de la fenêtre clique parfois le bâtiment derrière."

La fenêtre est prête seulement si :

- les surfaces opaques sont déclarées dans `HiveMapUiOcclusion`;
- l'input est bloqué selon son mode modal/non modal;
- les éléments monde continuent leur logique;
- les pixels sont correctement occultés pendant mouvement et zoom;
- paysage et portrait/mobile ont été vérifiés.

## Référence M044-CX

La correction de référence est documentée ici :

`Docs/AI/Missions/M044-CX-UI-Modal-Occlusion-Architecture.md`

Les fichiers runtime de référence sont :

- `Assets/BeeKingdom/Playground/HiveMapUiOcclusion.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
