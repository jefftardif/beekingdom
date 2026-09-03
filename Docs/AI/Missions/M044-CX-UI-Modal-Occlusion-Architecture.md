# M044-CX - Permanent UI Occlusion / Render-Layer Architecture Fix

Date: 2026-09-03
Agent: Architecte / Codex

## Scope

Correction ciblée HiveMap pour empêcher les icônes de collecte manuelle et les abeilles de présentation de passer visuellement par-dessus les surfaces UI opaques, notamment Communication CHAT / MAIL, le mini-chat flottant, le rail bas, le header haut et le panneau FTUE.

## Root Cause

Le problème ne venait pas d'une position de bâtiment ni d'un zoom précis. Plusieurs systèmes dessinaient dans des couches différentes :

- les bâtiments et abeilles ambiantes sont rendus par la caméra monde;
- les marqueurs de collecte et petites abeilles de production sont dessinés en IMGUI depuis `HiveMapProductionBootstrap`;
- le menu principal est en uGUI `ScreenSpaceOverlay`;
- Communication CHAT / MAIL est en IMGUI plein écran, tandis que le mini-chat est une fenêtre IMGUI flottante non modale.

Les fenêtres plein écran déjà stables ne prouvaient pas une vraie architecture d'occlusion commune : certains marqueurs étaient simplement ignorés pendant ces écrans. Communication n'était pas dans cette garde côté production, donc ses surfaces noires pouvaient être recouvertes par les icônes et abeilles dessinées après.

Un patch précédent avait aussi introduit une coupure explicite des abeilles ambiantes via `SetActive(false)`. C'était fonctionnellement une disparition, pas une occlusion.

## Old Render Order

1. Caméra monde : ruche, bâtiments, abeilles ambiantes 3D.
2. uGUI `LivingHiveMenuCanvas` : header, rail, panneaux.
3. IMGUI Communication : fond noir CHAT / MAIL.
4. IMGUI production : icône de collecte, abeilles de production, feedback.

Résultat: les éléments de production pouvaient repasser au-dessus des pixels noirs de Communication et du HUD.

## New Render Rule

`HiveMapUiOcclusion` centralise les rectangles UI opaques en coordonnées IMGUI et calcule les régions visibles restantes pour les éléments monde dessinés en IMGUI.

Les marqueurs ne sont plus dessinés directement plein écran. `HiveMapProductionBootstrap` dessine dans des régions clippées :

- CHAT / MAIL ouvert => rectangle écran complet occultant => zéro région visible;
- mini-chat ouvert => rectangle flottant animé occultant => régions visibles conservées autour;
- header haut et rail bas => les marqueurs restent visibles seulement hors de ces bandes;
- dialogue FTUE => le rectangle réel d'occlusion du panneau et du portrait est soustrait.

La production continue de tick dans `Update`; seule la peinture des pixels sous UI opaque est découpée.

## Royal Palace Comparison

Royal Palace paraissait correct car les bootstraps voisins ne dessinaient souvent rien quand `HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost` était vrai. Cette mission remplace cette logique locale par une règle réutilisable d'occlusion pour les présentations monde IMGUI. Royal Palace reste dans la liste des surfaces plein écran occultantes de `HiveMapUiOcclusion`.

## Files Changed

- `Assets/BeeKingdom/Playground/HiveMapUiOcclusion.cs`
- `Assets/BeeKingdom/Playground/HiveMapProductionBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapAmbientBeesBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveMapOverlayInputGateBootstrap.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/BeeKingdom/Playground/Editor/SandboxLivingHiveUiStabilizationTests.cs`
- `Assembly-CSharp.csproj`

## Verification

Compilation:

- `dotnet restore Assembly-CSharp.csproj` passed.
- `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly` passed: 0 errors, 238 warnings.
- `dotnet restore Assembly-CSharp-Editor.csproj` passed.
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal /clp:ErrorsOnly` passed: 0 errors, 379 warnings.

Static checks:

- No remaining `HiveMapAmbientBeesBootstrap.SetVisible(...)`.
- No remaining `ambientBees.SetVisible(!blocked)` call in `HiveMapOverlayInputGateBootstrap`.
- Communication still blocks world input through `HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()`.
- Mini-chat remains non-modal but contributes its animated floating rect as a partial occluder.

EditMode coverage:

- Added proof test for rectangle subtraction used by the occlusion map.
- Added proof test that mini-chat occlusion is partial, not fullscreen.

Architecture documentation:

- Added `Docs/Architecture/HiveMap_UI_Window_Occlusion_Guide.md` as the durable implementation guide for future HiveMap windows.

## Play Mode Evidence

Not executed in this pass. No visual PASS is claimed without CEO/Unity Game View verification.

Recommended manual proof clicks:

1. Open canonical HiveMap scene in Play Mode.
2. Stay in landscape 16:9 or 1366x768-style Game View.
3. Pan/zoom until a production icon overlaps the future Chat area.
4. Open Communication > CHAT.
5. Confirm icon and production bees do not appear over the black Chat surface and do not pop/disappear while the map continues behind it.
6. Switch to MAIL and repeat.
7. Close the fullscreen screen back to the mini-chat floating panel.
8. Confirm the icon and production bees pass behind the mini-chat panel, while remaining visible around it.
9. Close Communication.
10. Confirm the same icon/bee resumes visible motion outside opaque HUD/rail zones.
11. Repeat in portrait/mobile aspect.

## Acceptance Checklist

A. Root cause documented: YES.
B. Previous hide/disappear behavior identified: YES.
C. Ambient bee `SetActive(false)` path removed: YES.
D. Production markers still tick independently of painting: YES.
E. Shared occlusion helper added: YES.
F. CHAT fullscreen registered as opaque: YES.
G. MAIL fullscreen registered as opaque: YES.
H. Royal Palace registered as opaque: YES.
I. Research and other modal surfaces registered as opaque: YES.
J. Header occlusion registered: YES.
K. Bottom rail occlusion registered: YES.
L. FTUE panel occlusion uses full current occlusion rect: YES.
M. Runtime C# build passes: YES.
N. Editor C# build passes: YES.
O. Static no-hide check passes for ambient bees: YES.
P. Mini-chat partial occlusion registered and covered by compile-time proof hook: YES.
Q. Real Play Mode visual certification across Chat/Mail/mini-chat/aspect ratios: NO, pending manual or automated Unity Game View capture.

Final verdict: not fully certified until Q is completed.
