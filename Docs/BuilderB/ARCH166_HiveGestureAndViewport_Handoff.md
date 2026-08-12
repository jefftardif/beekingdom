# Builder-B Handoff - ARCH-166 Hive Gesture And Viewport

## Status

Preparation only. This document does not modify the main scene, runtime presenter, official gameplay, Android build, server connection, approved assets, or validated microcopy.

Prepared while QA-A validates BEE-761 to BEE-780. Builder-B prepares; Builder-A integrates later.

## Objective

ARCH-166 should correct the hive viewport behavior:

- tablet landscape: hive fills the maximum useful area;
- phone portrait: essential menus and panels remain visible;
- one finger means pan only;
- two fingers mean pinch zoom only;
- zoom is smooth and progressive;
- HUD, menus and panels stay fixed during zoom;
- hotspots, halos and zones remain aligned after pan/zoom.

## Probable Diagnostic

Likely causes to audit before implementation:

- Hive art, hotspot polygons, halos, bee presence and HUD may not be split into stable transform layers.
- Current gesture routing may allow a drag to become selection, or a pinch to also pan/select.
- Pan/zoom may apply directly to mixed UI roots, causing HUD or panels to move with the hive.
- Hotspot hit tests may use screen coordinates without consistently applying the inverse viewport transform.
- Selection halo may be drawn in a different coordinate space than the hotspot polygon.
- Portrait layout may compress the hive instead of giving the hive a pannable/croppable viewport under fixed HUD and bottom panel.
- Zoom may update immediately from raw pinch deltas, causing jitter when touch distance changes by tiny amounts.

## Recommended Unity Approach

Use a strict layer stack:

```text
HiveScreenRoot
  FixedHudLayer
    TopResourceHud
    StatusBadges
    NavigationRail
    DetailPanelOrBottomSheet
    SafeAreaPadding
  HiveViewportClip
    HiveCameraLayer
      HiveArt
      ZonePolygons
      SelectionHalos
      BeePresence
      DebugNormalizedPoints
```

Rules:

- Only `HiveCameraLayer` receives pan and zoom transforms.
- `FixedHudLayer` never inherits hive pan/zoom.
- Hotspots and halos live under the same transformed coordinate space as the hive art.
- Hit testing converts pointer screen position into hive normalized coordinates through the inverse of `HiveViewportClip` and `HiveCameraLayer`.
- Selection halos are generated from the same normalized polygon used by hit testing.
- Safe area is handled by fixed layout constraints, not by changing zone data.

## Viewport Layout Targets

Tablet landscape:

- Use maximum safe area after fixed HUD and mandatory panels.
- Prefer `aspect-fill inside clipped viewport` over shrinking the hive.
- Default frame should show the full useful hive width and keep the central queen/administration area readable.
- Side panels, if present, should be collapsible or overlay-light so the hive remains dominant.

Phone portrait:

- Top HUD and essential state remain fixed.
- Bottom rail/detail panel remains fixed or bottom-sheet style.
- Hive viewport sits between fixed top and bottom areas.
- The hive may be cropped and pannable rather than fully squeezed.
- Selected zone must remain reachable after detail panel opens.

Recommended initial viewport allocations:

```text
tabletLandscape:
  fixedTopHud: 8-12% height
  fixedBottomRail: 8-12% height
  hiveViewport: remaining safe area, target >= 76% height

phonePortrait:
  fixedTopHud: 9-13% height
  fixedBottomRail: 9-13% height
  detailPanelClosed: 0-12% height
  detailPanelOpen: max 34% height
  hiveViewport: remaining safe area, pannable/cropped
```

## Gesture Routing Pseudo-Algorithm

```text
onPointerDown(pointer):
  if pointer starts on FixedHudLayer:
    route to HUD
    block hive gesture for this pointer
    return

  add pointer to activeHivePointers

  if activeHivePointers.count == 1:
    gestureMode = CandidateTapOrPan
    startScreenPos = pointer.screenPos
    startTime = now
    accumulatedPan = 0

  if activeHivePointers.count == 2:
    gestureMode = PinchZoom
    cancel pending tap
    lock selection until all pointers released
    pinchStartDistance = distance(p0, p1)
    pinchStartCentroid = centroid(p0, p1)
    cameraStartZoom = camera.zoom
    cameraStartPan = camera.pan

onPointerMove(pointer):
  if gestureMode == CandidateTapOrPan:
    delta = pointer.screenPos - startScreenPos
    accumulatedPan = length(delta)

    if accumulatedPan >= panStartThresholdPx:
      gestureMode = Pan
      cancel pending tap

  if gestureMode == Pan:
    targetPan += pointer.delta / currentZoom
    suppressSelectionUntilRelease = true

  if gestureMode == PinchZoom:
    currentDistance = distance(p0, p1)
    currentCentroid = centroid(p0, p1)
    rawZoom = cameraStartZoom * (currentDistance / pinchStartDistance)
    targetZoom = clamp(rawZoom, minZoom, maxZoom)
    targetPan = keepMapPointUnderCentroid(cameraStartPan, cameraStartZoom, targetZoom, pinchStartCentroid, currentCentroid)
    suppressSelectionUntilRelease = true

onPointerUp(pointer):
  remove pointer from activeHivePointers

  if gestureMode == CandidateTapOrPan and elapsed <= tapMaxDuration and accumulatedPan < tapSlopPx:
    performHotspotHitTest(pointer.screenPos)

  if activeHivePointers.count == 0:
    gestureMode = None
    release selection suppression after selectionCooldownMs

  if activeHivePointers.count == 1 after pinch:
    gestureMode = Cooldown
    do not convert remaining finger into pan until it is lifted
```

Important routing rule:

```text
One finger: tap candidate, then pan after threshold.
Two fingers: pinch zoom only.
No selection during pan, pinch, or post-pinch cooldown.
```

## Damping And Smoothing Rules

Use target/current values:

```text
currentZoom = SmoothDamp(currentZoom, targetZoom, zoomVelocity, zoomSmoothTime)
currentPan = SmoothDamp(currentPan, targetPan, panVelocity, panSmoothTime)
```

Recommended starting values:

- `zoomSmoothTime`: `0.08` to `0.14` seconds.
- `panSmoothTime`: `0.05` to `0.10` seconds.
- `maxZoomVelocity`: cap to avoid a large jump after touch jitter.
- `pinchDeadZone`: ignore distance changes below `4 px`.
- `panDeadZone`: ignore movement below `2 px` until pan threshold is crossed.

Frame-rate independence:

- Apply smoothing in `Update` with `Time.unscaledDeltaTime` if UI must remain smooth during paused/slow states.
- Clamp `deltaTime` to avoid jumps after app resume, e.g. `min(deltaTime, 0.033)`.

Reduced motion:

- Keep interaction functional.
- Reduce animation overshoot and halo pulse.
- Do not disable pan/zoom.

## Zoom Limits

Compute limits from viewport and hive art bounds rather than hardcoding only one number.

Recommended initial policy:

```text
minZoomLandscape = max(viewportWidth / hiveUsefulWidth, viewportHeight / hiveUsefulHeight)
minZoomPortrait = max(viewportWidth / hiveUsefulWidth, viewportHeight / hiveUsefulHeight) * 1.08
defaultZoomLandscape = minZoomLandscape * 1.00 to 1.12
defaultZoomPortrait = minZoomPortrait * 1.10 to 1.25
maxZoom = minZoom * 2.20 to 2.80
```

Clamp pan so the hive never drifts away from the viewport:

```text
scaledHiveWidth = hiveUsefulWidth * currentZoom
scaledHiveHeight = hiveUsefulHeight * currentZoom
panXMin = viewportWidth - scaledHiveWidth - rightElasticMargin
panXMax = leftElasticMargin
panYMin = viewportHeight - scaledHiveHeight - bottomElasticMargin
panYMax = topElasticMargin
```

Recommended elastic margin:

- Production default: `0 px`.
- Debug/review only: up to `24 px` to reveal bounds.

## Anti-Selection Thresholds

Recommended starting thresholds:

- `tapSlopPx`: `8 px` phone, `10 px` tablet.
- `panStartThresholdPx`: `10 px` phone, `12 px` tablet.
- `tapMaxDuration`: `0.22 s`.
- `selectionCooldownAfterPanMs`: `120 ms`.
- `selectionCooldownAfterPinchMs`: `180 ms`.
- `minimumHotspotTouchDp`: `44 dp`.

Selection must be blocked when:

- pointer started over fixed HUD;
- pan threshold was crossed;
- two fingers were active at any time in the gesture;
- zoom target changed during the current pointer sequence;
- current smoothed pan/zoom has not settled enough and the pointer release was not a tap.

## Hotspot, Halo And Zone Alignment

Use one coordinate contract:

```text
zone data: hiveAssetNormalized 0..1
render: normalized -> hive local -> camera layer transform -> viewport -> screen
hit test: screen -> viewport -> inverse camera layer transform -> hive local -> normalized
halo: same polygon data as zone, rendered under HiveCameraLayer
```

Builder-A should avoid:

- duplicating halo coordinates in screen space;
- computing hit tests from post-layout screen rectangles only;
- applying zoom to art but not to overlays;
- applying zoom to overlays but not to art;
- scaling HUD with the hive camera layer.

Debug overlay recommendation:

- Show current zoom, pan, selected zone id, pointer mode, selection suppression reason.
- Draw normalized polygon vertices and halo from the same source list.
- Capture portrait and landscape screenshots at min/default/max zoom.

## Builder-A Checklist

- Confirm fixed layer split: `FixedHudLayer` and `HiveCameraLayer`.
- Confirm tablet landscape default framing maximizes useful hive area.
- Confirm phone portrait keeps top HUD, bottom rail and essential panel visible.
- Implement gesture state machine with one-finger pan and two-finger pinch separation.
- Add tap suppression after pan and pinch.
- Add damped target/current pan and zoom.
- Compute min/default/max zoom from viewport and hive art bounds.
- Clamp pan to prevent blank-space drift.
- Reproject hotspots through inverse transform before hit testing.
- Render halos from the same normalized polygon data used for hit tests.
- Validate selected zone halo alignment after pan and zoom.
- Validate portrait and landscape with safe-area variants.
- Validate reduced motion keeps pan/zoom usable.
- Keep all changes behind Builder-A integration review and Architect approval.

## Non-Claims

This handoff does not fix ARCH-166 in the game. It does not validate QA-A, BEE-761 to BEE-780, or a runtime build. It only prepares the technical integration plan for Builder-A.
