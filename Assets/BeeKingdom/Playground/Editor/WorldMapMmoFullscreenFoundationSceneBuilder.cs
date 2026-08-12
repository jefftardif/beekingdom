using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapMmoFullscreenFoundationSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild World Map MMO Fullscreen Foundation Scene")]
        public static void RebuildWorldMapMmoFullscreenFoundationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WorldMapMmoFullscreenFoundation";

            GameObject bootstrap = new GameObject("World Map MMO Fullscreen Foundation");
            bootstrap.AddComponent<WorldMapMmoFullscreenFoundationBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.014f, 0.012f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("World Map MMO Fullscreen Foundation scene rebuilt at " + ScenePath);
        }

        public static void ValidateWorldMapMmoFullscreenFoundation()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("World map fullscreen foundation scene could not be opened.");
            if (Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>() == null) throw new System.InvalidOperationException("World map fullscreen foundation bootstrap is missing.");

            string[] rows = WorldMapMmoFullscreenFoundationBootstrap.WorldMapMmoFullscreenFoundationForProof();
            Require(rows, "dedicated_fullscreen_interface:true");
            Require(rows, "pan_zoom_enabled:true");
            Require(rows, "tile_chunk_model_prepared:true");
            Require(rows, "large_world_logical_space:true");
            Require(rows, "world_map_wave4_integration_step4a:true");
            Require(rows, "world_art_provider:manifest_driven");
            Require(rows, "world_art_wave1_grid:3x3");
            Require(rows, "world_art_wave2_5x5_without_scene_rewrite:true");
            Require(rows, "world_id:BK-DEMO-WORLD-WAVE6-LOCAL");
            Require(rows, "game_server_id:GS-DEMO-WAVE6-READINESS");
            Require(rows, "coordinate_model:WorldId,SectorId,ChunkId,TileCoord,WorldCoord");
            Require(rows, "chunk_size_world_units:512");
            Require(rows, "world_chunks:64x64");
            Require(rows, "active_chunk_neighborhood:5x5");
            Require(rows, "active_chunk_minimum_3x3:true");
            Require(rows, "chunk_activation_on_boundary_cross:true");
            Require(rows, "chunk_deactivation_outside_neighborhood:true");
            Require(rows, "single_large_sprite_logical_dependency:false");
            Require(rows, "deterministic_local_seed:738921");
            Require(rows, "placement_rules:min_hive_distance,no_hive_resource_overlap,limited_density_per_chunk,reproducible_seed");
            Require(rows, "test_hive_present:true");
            Require(rows, "visible_hives:deterministic_by_active_chunks");
            Require(rows, "visible_hive_roles:player,ally,neutral");
            Require(rows, "hive_models:beginning,mid,advanced,capital");
            Require(rows, "collectable_resources:pollen,nectar,wax,propolis,royal_jelly_demo");
            Require(rows, "visible_collectable_resources:deterministic_by_active_chunks");
            Require(rows, "local_snapshot_deterministic:true");
            Require(rows, "hive_selection_supported:true");
            Require(rows, "resource_selection_supported:true");
            Require(rows, "local_collect_action_supported:true");
            Require(rows, "collection_states:En vol,Collecte,Retour,Termine");
            Require(rows, "multiple_local_demo_flights_supported:true");
            Require(rows, "active_recent_flight_journal:true");
            Require(rows, "flight_anchors:world_coordinates");
            Require(rows, "flight_continues_across_chunk_boundary:true");
            Require(rows, "flight_movement_language:aerial_only");
            Require(rows, "overlays_separated_from_background:true");
            Require(rows, "hud_world_coordinates:true");
            Require(rows, "debug_chunk_bounds_toggle:true");
            Require(rows, "local_demo_reward_supported:true");
            Require(rows, "troop_movement_type:aerial_arc_swarm_trail");
            Require(rows, "painted_roads_ignored:true");
            Require(rows, "ground_routes_used:false");
            Require(rows, "official_collection:false");
            Require(rows, "official_combat:false");
            Require(rows, "persistent_economy:false");
            Require(rows, "inner_hive_touched:false");
            Require(rows, "world_map_local_lab:true");

            string[] localLab = WorldMapMmoFullscreenFoundationBootstrap.WorldMapLocalLabForProof();
            Require(localLab, "world_map_local_lab:true");
            Require(localLab, "local_lab_hives:PLAYER_TEST_HIVE,ENEMY_TEST_HIVE");
            Require(localLab, "local_lab_editable_position:true");
            Require(localLab, "local_lab_editable_level_1_50:true");
            Require(localLab, "local_lab_classes:Neutral,RoyalGuard,Striker,Nurturer,Scout,Alchemist");
            Require(localLab, "local_lab_stock_and_capacity:true");
            Require(localLab, "local_lab_hud_compact_collapsible:true");
            Require(localLab, "local_lab_buttons:Apply,Reset,Test collecte,Test combat");
            Require(localLab, "local_lab_collection_deterministic:true");
            Require(localLab, "local_lab_combat_deterministic:true");
            Require(localLab, "local_lab_official_gain:false");
            Require(localLab, "local_lab_server:false");
            Require(localLab, "local_lab_remote:false");
            Require(localLab, "local_lab_real_data:false");
            Require(localLab, "local_lab_serialized_local_only:true");
            string[] step3 = WorldMapMmoFullscreenFoundationBootstrap.WorldMapLargeWorldStep3SelfCheckForProof();
            Require(step3, "step3_self_check:true");
            Require(step3, "pan_crosses_multiple_chunks:C32_32_to_C35_32");
            Require(step3, "active_chunks_after_boundary_cross:25");
            Require(step3, "minimum_active_chunks_required:9");
            Require(step3, "flight_origin_worldcoord_stable_after_pan:true");
            Require(step3, "flight_destination_worldcoord_stable_after_pan:true");
            Require(step3, "flight_path_recomputed_from_worldcoord:true");
            Require(step3, "ground_route_graph_present:false");
            Require(step3, "painted_road_sampling_for_pathfinding:false");

            string[] step4A = WorldMapMmoFullscreenFoundationBootstrap.WorldMapWave4Step4AForProof();
            Require(step4A, "step4a_worldmap_unity_integration:true");
            Require(step4A, "scene:Assets/Scenes/WorldMapMmoFullscreenFoundation.unity");
            Require(step4A, "manifest_driven_content_provider:true");
            Require(step4A, "logical_world_chunks:64x64");
            Require(step4A, "active_window_chunks:5x5");
            Require(step4A, "runtime_python_required:false");
            Require(step4A, "entities_overlay_separate_from_tiles:true");
            Require(step4A, "aerial_flights_only:true");
            Require(step4A, "ground_routes_used:false");
            Require(step4A, "hud_fixed_during_pan_zoom:true");
            Require(step4A, "server_live:false");
            Require(step4A, "wave4_manifest_provider:true");
            Require(step4A, "wave4_manifest_lot:UIB_SectorWave1");
            Require(step4A, "wave4_manifest_grid:3x3");
            Require(step4A, "wave4_loaded_sectors:9");
            Require(step4A, "wave4_no_road_directive:true");
            Require(step4A, "wave4_future_5x5_without_scene_rewrite:true");

            string[] splash = HiveViewProductUiPresenter.SplashAuthDemoForProof();
            Require(splash, "dev_splash_scene_selector:true");
            Require(splash, "dev_splash_visible_only_editor_or_development:true");
            Require(splash, "dev_splash_worldmap_scene_path:Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity");
            Require(splash, "dev_splash_worldmap_wave6_exact_crop:true");
            Require(splash, "dev_splash_scene_config_centralized:true");
            Require(splash, "dev_splash_scene_build_settings_guard:true");
            if (!SplashDevelopmentSceneConfig.IsSceneEnabledInBuildSettings(SplashDevelopmentSceneConfig.WorldMapScenePath))
            {
                throw new System.InvalidOperationException("WorldMap scene is not enabled in Build Settings: " + SplashDevelopmentSceneConfig.WorldMapScenePath);
            }

            string[] step4B = WorldMapMmoFullscreenFoundationBootstrap.WorldMapRuntimeTileSeamStep4BForProof();
            Require(step4B, "step4b_runtime_tile_seam_correction:superseded_by_step5a_wave3");
            Require(step4B, "step4c_runtime_continuity_correction:superseded_by_step5a_wave3");
            Require(step4B, "tile_rect_strategy:wave3_runtime_tiles_shared_world_rects");
            Require(step4B, "continuous_atlas_single_draw:false");
            Require(step4B, "chunk_tile_draws_for_art:false_when_wave3_unavailable");
            Require(step4B, "pixel_snapping_for_primary_art:false");
            Require(step4B, "per_chunk_dark_overlay_removed:true");
            Require(step4B, "tile_atmosphere_pass:world_tile_post_terrain_pre_overlay");
            Require(step4B, "runtime_grid_pattern_visible:false");
            Require(step4B, "continuous_world_illusion_runtime_target:true");
            Require(step4B, "atlas_wrap_mode:Clamp");
            Require(step4B, "atlas_repeat_visible:false");
            Require(step4B, "atlas_uv_policy:wave3_inner_uv_clamp_no_repeat");
            Require(step4B, "visible_uv_never_samples_outside_0_1:true");
            Require(step4B, "single_surface_no_internal_tile_edges:superseded_by_wave3_gutter_tiles");
            Require(step4B, "no_5x5_master_integrated:false");
            Require(step4B, "wave3_runtime_tile_count:25");
            Require(step4B, "wave3_load_failure_fails_closed:true");
            Require(step4B, "canonical_static_uv_fallback_reachable:false");
            Require(step4B, "canonical_modulo_tile_fallback_reachable:false");
            Require(step4B, "source_png_modified:false");
            Require(step4B, "logical_world_chunks:64x64");
            Require(step4B, "active_window_chunks:5x5");
            Require(step4B, "overlays_separated_from_background:true");
            Require(step4B, "aerial_flights_only:true");
            Require(step4B, "ground_routes_used:false");
            Require(step4B, "server_live:false");
            Require(step4B, "dynamic_pan_crosses_three_chunks:true");
            Require(step4B, "dynamic_pan_active_window_preserved:25");
            Require(step4B, "dynamic_pan_flight_world_anchors_preserved:true");
            Require(step4B, "dynamic_pan_hud_fixed:true");
            Require(step4B, "dynamic_pan_no_ground_route_claim:true");
            Require(step4B, "dynamic_pan_surface_uv_bounded:superseded_by_shared_world_transform");
            Require(step4B, "dynamic_pan_visual_surface_no_repeat:true");
            Require(step4B, "dynamic_pan_no_hole_overlap_flash_static_contract:true");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string[] step4D = WorldMapMmoFullscreenFoundationBootstrap.WorldMapStep4DProofControlsForProof();
            Require(step4D, "step4d_deterministic_dev_proof_controls:true");
            Require(step4D, "step4d_compilation_guard:UNITY_EDITOR_OR_DEVELOPMENT_BUILD");
            Require(step4D, "step4d_release_menu_surface:false");
            Require(step4D, "step4d_canonical_scene:Assets/Scenes/WorldMapMmoFullscreenFoundation.unity");
            Require(step4D, "step4d_atomic_state_api:true");
            Require(step4D, "step4d_state_landscape_1920x1080_z0.85_C32_32:true");
            Require(step4D, "step4d_state_landscape_1920x1080_z1.10_C32_32:true");
            Require(step4D, "step4d_state_landscape_1920x1080_z1.35_C32_32:true");
            Require(step4D, "step4d_state_portrait_720x1280_z1.10_C32_32:true");
            Require(step4D, "step4d_pan_sequence_C32_32_C35_32_C36_32_z1.10:true");
            Require(step4D, "step4d_capture_output_under_workspace:true");
            Require(step4D, "step4d_refuses_non_play_mode:true");
            Require(step4D, "step4d_refuses_non_canonical_scene:true");
            Require(step4D, "step4d_refuses_missing_bootstrap:true");
            Require(step4D, "step4d_no_shader_blur_band_overlay:true");
            Require(step4D, "step4d_preserves_clamp_no_wrap:true");
            Require(step4D, "non_development_runtime_surface_added:false");

            string[] step4DController = WorldMapRuntimeContinuityStep4DProofController.WorldMapStep4DProofControllerForProof();
            Require(step4DController, "step4d_editor_controller:true");
            Require(step4DController, "step4d_output_root:Temp/WorldMapStep4DProof");
            Require(step4DController, "step4d_menu_capture_required_set:true");
            Require(step4DController, "step4d_manifest_includes_atlas_hash:true");
            Require(step4DController, "step4d_manifest_includes_product_hashes:true");
            Require(step4DController, "step4d_manifest_5x5_absent:true");
            Require(step4DController, "step4d_no_png_asset_modification:true");
            WorldMapRuntimeContinuityStep4DProofController.ValidateWorldMapRuntimeContinuityStep4DProofControls();
#endif

            string[] step5A = WorldMapMmoFullscreenFoundationBootstrap.WorldMapWave3SharedTransformStep5AForProof();
            Require(step5A, "step5a_wave3_shared_world_transform:true");
            Require(step5A, "user_reported_static_background_bug:fixed");
            Require(step5A, "terrain_primary_renderer:wave3_world_tiles");
            Require(step5A, "fullscreen_static_uv_surface_primary:false");
            Require(step5A, "terrain_entities_same_world_to_screen:true");
            Require(step5A, "hud_screen_space_fixed:true");
            Require(step5A, "wave3_grid:5x5");
            Require(step5A, "wave3_runtime_tile_count:25");
            Require(step5A, "wave3_runtime_tile_size:516x516");
            Require(step5A, "wave3_canonical_inner_size:512x512");
            Require(step5A, "wave3_gutter_pixels_each_side:2");
            Require(step5A, "wave3_uv_inner:2/516..514/516");
            Require(step5A, "wave3_macro_origin_chunk:C30_30");
            Require(step5A, "wave3_macro_center_chunk:C32_32");
            Require(step5A, "wave3_no_modulo_repeat:true");
            Require(step5A, "wave3_load_failure_fails_closed:true");
            Require(step5A, "canonical_static_uv_fallback_reachable:false");
            Require(step5A, "canonical_modulo_tile_fallback_reachable:false");
            Require(step5A, "wave3_texture_wrap:Clamp");
            Require(step5A, "wave3_filter:Bilinear");
            Require(step5A, "wave3_mapping_identity_no_flip_no_rotate:true");
            Require(step5A, "visual_camera_bounded_to_wave3_art:true");
            Require(step5A, "step5a_pan_changes_terrain:true");
            Require(step5A, "step5a_pan_delta_shared_terrain_entity:true");
            Require(step5A, "step5a_zoom_changes_terrain_scale:true");
            Require(step5A, "step5a_zoom_factor_shared_terrain_entity:true");
            Require(step5A, "step5a_hud_rect_unchanged_after_pan_zoom:true");
            Require(step5A, "no_shader_blur_band_overlay:true");
            Require(step5A, "png_source_modified:false");

            string[] wave6 = WorldMapMmoFullscreenFoundationBootstrap.WorldMapWave6IntegrationForProof();
            Require(wave6, "wave6_50x50_unity_integration:true");
            Require(wave6, "runtime_tile_count:2500");
            Require(wave6, "grid:50x50");
            Require(wave6, "runtime_tile_size:516x516");
            Require(wave6, "true_gutter_pixels_each_side:2");
            Require(wave6, "visual_camera_bounded_to_wave6_art:true");
            Require(wave6, "terrain_entities_landmarks_same_world_to_screen:true");
            Require(wave6, "hud_screen_space_fixed:true");
            Require(wave6, "old_wave5_25x25_canonical_active:false");
            Require(wave6, "old_wave3_5x5_canonical_active:false");
            Require(wave6, "bear_den_visible_by_default:true");
            Require(wave6, "bear_den_toggle_session_local:true");
            Require(wave6, "bear_visible:false");
            Require(wave6, "server_live:false");

            Debug.Log("World map MMO fullscreen foundation validation completed.");
        }

        private static void Require(string[] rows, string expected)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == expected) return;
            }

            throw new System.InvalidOperationException("Missing proof row: " + expected);
        }
    }
}
