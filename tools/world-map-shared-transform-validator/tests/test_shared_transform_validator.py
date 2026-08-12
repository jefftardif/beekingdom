from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from shared_transform_validator import (
    audit_source,
    audit_tile_directory,
    build_fixture,
    legacy_metrics,
    screen_to_world,
    validate_evidence,
    world_to_screen,
)


class SharedTransformValidatorTests(unittest.TestCase):
    @staticmethod
    def _write_tile_inventory(root: Path, duplicate_payloads: bool = False) -> None:
        png_header = b"\x89PNG\r\n\x1a\n" + b"\x00\x00\x00\rIHDR"
        dimensions = (516).to_bytes(4, "big") + (516).to_bytes(4, "big")
        for row in range(5):
            for column in range(5):
                index = row * 5 + column
                suffix = b"same" if duplicate_payloads else bytes([index])
                (root / f"R{row}C{column}_g2.png").write_bytes(
                    png_header + dimensions + suffix
                )
        (root / "manifest.runtime.unity.json").write_text(
            '{"tiles": [' + ",".join("{}" for _ in range(25)) + "]}",
            encoding="utf-8",
        )

    def test_oracle_inverse_round_trip(self) -> None:
        camera = {"center": [16640.0, 16640.0], "zoom": 1.35}
        screen = [1920.0, 1080.0]
        world = [17123.5, 16002.25]
        projected = world_to_screen(world, camera, screen)
        restored = screen_to_world(projected, camera, screen)
        self.assertAlmostEqual(restored[0], world[0], places=8)
        self.assertAlmostEqual(restored[1], world[1], places=8)

    def test_current_defect_fixture_is_rejected(self) -> None:
        report = validate_evidence(build_fixture("current-defect"))
        self.assertEqual(report["status"], "FAIL")
        self.assertIn("FULLSCREEN_UV_DECOUPLED", report["issue_codes"])
        self.assertIn("TERRAIN_PAN_QUASI_STATIC", report["issue_codes"])
        self.assertIn("ZOOM_FACTOR_NOT_SHARED", report["issue_codes"])
        self.assertIn("ZOOM_PIVOT_NOT_SHARED", report["issue_codes"])
        self.assertEqual(report["checks"]["hud_screen_space_invariant"], "PASS")

    def test_positive_shared_fixture_passes(self) -> None:
        report = validate_evidence(build_fixture("positive-shared"))
        self.assertEqual(report["status"], "PASS")
        self.assertEqual(report["issue_codes"], [])
        self.assertEqual(report["checks"]["shared_pan_delta"], "PASS")
        self.assertEqual(report["checks"]["shared_zoom_factor_and_pivot"], "PASS")

    def test_repeat_wrap_is_rejected(self) -> None:
        payload = build_fixture("positive-shared")
        payload["policies"]["wrap_mode"] = "Repeat"
        report = validate_evidence(payload)
        self.assertIn("WRAP_MODE_NOT_CLAMP", report["issue_codes"])

    def test_pilot_repeat_is_rejected(self) -> None:
        payload = build_fixture("positive-shared")
        payload["policies"]["pilot_repeat"] = True
        payload["policies"]["pilot_population_mode"] = "modulo_repeat"
        report = validate_evidence(payload)
        self.assertIn("PILOT_REPEATED_AS_LOGICAL_WORLD", report["issue_codes"])
        self.assertIn("PILOT_WORLD_POPULATION_POLICY_INVALID", report["issue_codes"])

    def test_hud_motion_is_rejected(self) -> None:
        payload = build_fixture("positive-shared")
        payload["transitions"][0]["hud_after"]["top"][0] += 1.0
        report = validate_evidence(payload)
        self.assertIn("HUD_TRANSFORM_CHANGED", report["issue_codes"])

    def test_25_unique_runtime_tiles_pass(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_tile_inventory(root)
            report = audit_tile_directory(root)
        self.assertEqual(report["status"], "PASS")
        self.assertEqual(report["actual_count"], 25)
        self.assertEqual(report["unique_sha256_count"], 25)

    def test_duplicate_runtime_tiles_fail(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_tile_inventory(root, duplicate_payloads=True)
            report = audit_tile_directory(root)
        self.assertEqual(report["status"], "FAIL")
        self.assertEqual(report["unique_sha256_count"], 1)

    def test_legacy_pan_is_quasi_static(self) -> None:
        metrics = legacy_metrics()
        self.assertAlmostEqual(metrics["entity_pan_delta_px"][0], -1689.6, places=5)
        self.assertAlmostEqual(metrics["terrain_pan_delta_px"][0], -37.2413793103, places=5)
        self.assertLess(metrics["terrain_to_entity_pan_ratio"], 0.023)

    def test_positive_source_shape_passes_static_gate(self) -> None:
        source = """
class Candidate {
  private void DrawActiveChunks() {
    if (wave3Provider == null || !wave3Provider.IsLoaded) {
      status = "Wave3 unavailable";
      return;
    }
    DrawWave3WorldTerrain();
  }
  private void DrawWave3WorldTerrain() {
    Rect rect = WorldRectToScreenRect(tile.WorldRect);
    GUI.DrawTextureWithTexCoords(rect, tile.Texture, tile.InnerUv, true);
  }
  private Rect WorldRectToScreenRect(Rect worldRect) {
    Vector2 min = WorldToScreen(worldRect.min);
    Vector2 max = WorldToScreen(worldRect.max);
    return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
  }
  Vector2 WorldToScreen(Vector2 world) { return SharedCamera.WorldToScreen(world); }
  Vector2 ScreenToWorld(Vector2 screen) { return SharedCamera.ScreenToWorld(screen); }
  private void ClampCamera() { Rect bounds = wave3Provider.WorldBounds; }
  // Legacy helpers may remain compiled, but the terrain dispatcher cannot call them.
  void DrawContinuousAtlasSurface() {
    Rect uv = ContinuousAtlasUvRect(Screen.width, Screen.height, currentZoom, currentWorldCenter);
    GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, Screen.width, Screen.height), atlas, uv, true);
  }
  Rect ContinuousAtlasUvRect(int width, int height, float zoom, Vector2 worldCenter) {
    float normalizedWorldX = worldCenter.x / (WorldChunkWidth * ChunkSize) - 0.5f;
    float zoomScale = Mathf.Lerp(1f, 0.74f, zoom);
    return new Rect(new Vector2(0.5f + normalizedWorldX * 0.36f, 0.5f), Vector2.one * zoomScale);
  }
  Rect TileTexCoords(Vector2Int chunk) {
    int tx = PositiveModulo(chunk.x, 4);
    return new Rect(tx * 0.25f, 0f, 0.25f, 0.25f);
  }
  void DrawHives() { Vector2 p = WorldToScreen(hive.WorldCoord); }
  void DrawResources() { Vector2 p = WorldToScreen(resource.WorldCoord); }
  void DrawFlightArc() {
    Vector2 a = WorldToScreen(origin);
    Vector2 b = WorldToScreen(destination);
  }
  void DrawFixedHud() { GUI.Label(new Rect(14f, 12f, 100f, 40f), label); }
  void DrawActionPanel() { GUI.Label(new Rect(10f, 100f, 100f, 40f), label); }
  void DrawFlightJournal() { GUI.Label(new Rect(10f, 150f, 100f, 40f), label); }
  private sealed class Wave3RuntimeGutterTileProvider {
    private const int Rows = 5;
    private const int Columns = 5;
    private readonly List<Wave3RuntimeTile> tiles = new List<Wave3RuntimeTile>(25);
    public bool IsLoaded { get; private set; }
    public void Load() {
      tiles.Clear();
      for (int row = 0; row < Rows; row++) {
        for (int column = 0; column < Columns; column++) {
          Texture2D texture = Resources.Load<Texture2D>("root/R" + row + "C" + column + "_g2");
          if (texture == null) { IsLoaded = false; tiles.Clear(); return; }
          tiles.Add(new Wave3RuntimeTile());
        }
      }
      IsLoaded = tiles.Count == 25;
    }
  }
}
"""
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "Candidate.cs"
            path.write_text(source, encoding="utf-8")
            report = audit_source(path)
        self.assertEqual(report["status"], "PASS")
        self.assertEqual(report["issue_codes"], [])
        self.assertFalse(report["source_checks"]["legacy_fullscreen_uv_fallback_reachable"])
        self.assertFalse(report["source_checks"]["pilot_modulo_repeat_path_reachable"])
        self.assertTrue(report["source_checks"]["wave3_load_failure_fail_closed"])

    def test_legacy_source_shape_reproduces_static_background_defect(self) -> None:
        source = """
class Legacy {
  private void DrawActiveChunks() {
    DrawContinuousAtlasSurface();
    DrawFallbackProxyTile();
  }
  private Vector2 WorldToScreen(Vector2 world) { return screenCenter + (world - currentWorldCenter) * currentZoom; }
  private void DrawContinuousAtlasSurface() {
    Rect uv = ContinuousAtlasUvRect(Screen.width, Screen.height, currentZoom, currentWorldCenter);
    GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, Screen.width, Screen.height), atlas, uv, true);
  }
  private Rect ContinuousAtlasUvRect(int width, int height, float zoom, Vector2 worldCenter) {
    float normalizedWorldX = worldCenter.x / (WorldChunkWidth * ChunkSize) - 0.5f;
    float zoomScale = Mathf.Lerp(1f, 0.74f, zoom);
    return new Rect(new Vector2(0.5f + normalizedWorldX * 0.36f, 0.5f), Vector2.one * zoomScale);
  }
  private Rect TileTexCoords(Vector2Int chunk) {
    int tx = PositiveModulo(chunk.x, 4);
    return new Rect(tx * 0.25f, 0f, 0.25f, 0.25f);
  }
  private void DrawFallbackProxyTile() { Rect tex = TileTexCoords(chunk); }
  private void DrawHives() { Vector2 p = WorldToScreen(hive.WorldCoord); }
  private void DrawResources() { Vector2 p = WorldToScreen(resource.WorldCoord); }
  private void DrawFlightArc() {
    Vector2 a = WorldToScreen(origin);
    Vector2 b = WorldToScreen(destination);
  }
  private void DrawFixedHud() { GUI.Label(new Rect(14f, 12f, 100f, 40f), label); }
  private void DrawActionPanel() { GUI.Label(new Rect(10f, 100f, 100f, 40f), label); }
  private void DrawFlightJournal() { GUI.Label(new Rect(10f, 150f, 100f, 40f), label); }
}
"""
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "Legacy.cs"
            path.write_text(source, encoding="utf-8")
            report = audit_source(path)
        self.assertEqual(report["status"], "FAIL")
        self.assertEqual(
            report["verdicts"]["CURRENT_STATIC_BACKGROUND_DEFECT_REPRODUCED"],
            "YES",
        )
        self.assertIn("FULLSCREEN_UV_DECOUPLED", report["issue_codes"])
        self.assertIn("PILOT_MODULO_REPEAT_PATH", report["issue_codes"])

    def test_shared_primary_with_reachable_legacy_fallback_is_still_rejected(self) -> None:
        source = """
class Hybrid {
  private void DrawActiveChunks() {
    if (wave3Provider.IsLoaded) { DrawWave3WorldTerrain(); return; }
    DrawContinuousAtlasSurface();
  }
  private void DrawWave3WorldTerrain() {
    Rect rect = WorldRectToScreenRect(tile.WorldRect);
    GUI.DrawTextureWithTexCoords(rect, tile.Texture, tile.InnerUv, true);
  }
  private Rect WorldRectToScreenRect(Rect worldRect) {
    Vector2 min = WorldToScreen(worldRect.min);
    Vector2 max = WorldToScreen(worldRect.max);
    return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
  }
  private Vector2 WorldToScreen(Vector2 world) { return SharedCamera.WorldToScreen(world); }
  private void DrawContinuousAtlasSurface() {
    Rect uv = ContinuousAtlasUvRect(Screen.width, Screen.height, currentZoom, currentWorldCenter);
    GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, Screen.width, Screen.height), atlas, uv, true);
  }
  private Rect ContinuousAtlasUvRect(int width, int height, float zoom, Vector2 worldCenter) {
    float normalizedWorldX = worldCenter.x / (WorldChunkWidth * ChunkSize) - 0.5f;
    float zoomScale = Mathf.Lerp(1f, 0.74f, zoom);
    return new Rect(new Vector2(0.5f + normalizedWorldX * 0.36f, 0.5f), Vector2.one * zoomScale);
  }
  private Rect TileTexCoords(Vector2Int chunk) {
    int tx = PositiveModulo(chunk.x, 4);
    return new Rect(tx * 0.25f, 0f, 0.25f, 0.25f);
  }
  private void ClampCamera() { Rect bounds = wave3Provider.WorldBounds; }
  private void DrawHives() { Vector2 p = WorldToScreen(hive.WorldCoord); }
  private void DrawResources() { Vector2 p = WorldToScreen(resource.WorldCoord); }
  private void DrawFlightArc() {
    Vector2 a = WorldToScreen(origin);
    Vector2 b = WorldToScreen(destination);
  }
  private void DrawFixedHud() { GUI.Label(new Rect(14f, 12f, 100f, 40f), label); }
  private void DrawActionPanel() { GUI.Label(new Rect(10f, 100f, 100f, 40f), label); }
  private void DrawFlightJournal() { GUI.Label(new Rect(10f, 150f, 100f, 40f), label); }
}
"""
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "Hybrid.cs"
            path.write_text(source, encoding="utf-8")
            report = audit_source(path)
        self.assertEqual(report["status"], "FAIL")
        self.assertTrue(report["source_checks"]["primary_wave3_shared_projection"])
        self.assertEqual(
            report["verdicts"]["PRIMARY_WAVE3_SHARED_PATH_STATIC_SUPPORT"],
            "PASS",
        )
        self.assertIn("DECOUPLED_TERRAIN_FALLBACK_REACHABLE", report["issue_codes"])


if __name__ == "__main__":
    unittest.main()
