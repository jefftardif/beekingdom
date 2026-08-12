using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBeeAntLegionHiveHudTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBeeAntLegionHiveHudTests();
                tests.AntLegionHudContainsRequestedLayout();
                tests.AntLegionHudContainsRequestedInteractions();
                tests.AntLegionHudLocksPremiumTextureDirectionAndScope();
                tests.ContextualSurfaceButtonResetsAfterHiveReturn();
                tests.TopBuildingRemainsClickableBelowResourceShelf();
                tests.AcademyContourStaysOnTheLeft();
                tests.HiveBackgroundClickClosesBuildingMenu();
                tests.UpgradeQueueUsesTheActiveBuildingLabel();
                tests.PlayerProfileRowsDoNotOverlap();
                Debug.Log("Ant Legion inspired hive HUD checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("Ant Legion inspired hive HUD checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void AntLegionHudContainsRequestedLayout()
        {
            string[] rows = HiveViewProductUiPresenter.AntLegionInspiredHiveHudForProof();

            AssertRow(rows, "ant_legion_inspired_hive_hud:true");
            AssertRow(rows, "left_time_indicators:construction,training,upgrade,research");
            AssertRow(rows, "left_time_indicators_visible:true");
            AssertRow(rows, "left_time_cards_premium:icon_well,time_badge,progress_bar");
            AssertRow(rows, "left_time_column_premium:backplate,vertical_axis,state_accents");
            AssertRow(rows, "player_button_top_left:true");
            AssertRow(rows, "top_command_cluster_premium:shelf,vip_progress,power_active_state");
            AssertRow(rows, "top_dropdown_close_buttons:player,vip,power");
            AssertRow(rows, "top_dropdown_layer:above_queue_timers_and_hud");
            AssertRow(rows, "vip_indicator_next_to_player:true");
            AssertRow(rows, "power_indicator_next_to_vip:true");
            AssertRow(rows, "top_resources_visible:true");
            AssertRow(rows, "top_resources:honey,wax,pollen,bees,capacity");
            AssertRow(rows, "top_resource_pills_premium:accented_icon_wells,production_tick,capacity_bar");
            AssertRow(rows, "top_resource_shelf_premium:true");
            AssertRow(rows, "top_building_click_area:visible_surface_below_resource_shelf");
            AssertRow(rows, "top_hud_hitboxes:precise_no_invisible_full_width_blocker");
            AssertRow(rows, "academy_location:left_carved_hive");
            AssertRow(rows, "right_upper_scenery_click:non_interactive");
            AssertRow(rows, "hive_background_click:closes_building_menu_and_deselects");
            AssertRow(rows, "hive_drag_or_ui_click:keeps_building_selection");
            AssertRow(rows, "upgrade_queue_building_label:dynamic_from_active_hotspot");
            AssertRow(rows, "player_profile_rows:level,statistics,skills_non_overlapping");
            AssertRow(rows, "player_profile_values:separate_readable_lines");
            AssertRow(rows, "portrait_resources_premium:accented_icon_wells,production_tick,capacity_bar");
            AssertRow(rows, "top_system_shortcuts:mail,alert");
            AssertRow(rows, "bottom_main_menu_visible:true");
            AssertRow(rows, "bottom_left_map_button:true");
            AssertRow(rows, "contextual_surface_button:single,Carte_in_hive,Ruche_on_map");
            AssertRow(rows, "contextual_surface_button_selected_after_return:false");
            AssertRow(rows, "bottom_left_map_button_premium:gold_edge,travel_glow,distinct_from_other_items");
            AssertRow(rows, "bottom_main_menu_active_state:subtle_header_band,gold_baseline");
            AssertRow(rows, "bottom_main_menu_badges:quests,mail,alliance,more");
            AssertRow(rows, "bottom_main_menu_premium_finish:hex_icon_sockets,etched_dividers,rail_ornament");
            AssertRow(rows, "chat_above_bottom_menu:true");
            AssertRow(rows, "communication_panel_click_opens:true");
        }

        [Test]
        public void AntLegionHudContainsRequestedInteractions()
        {
            string[] rows = HiveViewProductUiPresenter.AntLegionInspiredHiveHudForProof();

            AssertRow(rows, "player_button_contains:statistics,level,skills");
            AssertRow(rows, "player_menu_premium:header_band,level_card,stat_cards");
            AssertRow(rows, "vip_click_opens_menu:true");
            AssertRow(rows, "vip_menu_premium:vip_badge,perk_card,progress_bar");
            AssertRow(rows, "power_breakdown:equipment,queen_level,specialized_bees,skills,vip");
            AssertRow(rows, "power_menu_premium:row_cards,icons,share_bars");
            AssertRow(rows, "bottom_left_map_button_action:OpenCanonicalWorldMap");
            AssertRow(rows, "communication_system_preview:true");
            AssertRow(rows, "communication_panel_premium:channel_tabs,message_rows,quick_message_preview");
            AssertRow(rows, "communication_bar_premium:channel_accent,textured_panel,quick_toggle");
        }

        [Test]
        public void AntLegionHudLocksPremiumTextureDirectionAndScope()
        {
            string[] rows = HiveViewProductUiPresenter.AntLegionInspiredHiveHudForProof();

            AssertRow(rows, "premium_full_hd_direction:textured_dark_panels,fine_gold_accents,corner_caps,procedural_hd_icons");
            AssertRow(rows, "premium_menu_chrome:textured_wax_grain,etched_lines,subtle_amber_veil");
            AssertRow(rows, "orange_square_menu_style:removed_from_primary_hud");
            AssertRow(rows, "large_gold_bars_replaced_by_subtle_header_band:true");
            AssertRow(rows, "map_terrain_images_modified:false");
            AssertRow(rows, "resource_map_icons_scope:false");
            AssertRow(rows, "official_server_live:false");
        }

        [Test]
        public void ContextualSurfaceButtonResetsAfterHiveReturn()
        {
            FieldInfo activeMenu = typeof(HiveViewProductUiPresenter).GetField("activeMainMenuId", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(activeMenu, Is.Not.Null);
            activeMenu.SetValue(null, "SurfaceSwitch");

            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");

            Assert.That(activeMenu.GetValue(null), Is.EqualTo(string.Empty));
        }

        [Test]
        public void TopBuildingRemainsClickableBelowResourceShelf()
        {
            Assert.That(HiveViewProductUiPresenter.IsLandscapeHiveScreenPointInteractiveForProof(1920f, 1080f, 960f, 82f), Is.True);
            Assert.That(HiveViewProductUiPresenter.IsLandscapeHiveScreenPointInteractiveForProof(1920f, 1080f, 960f, 24f), Is.False);
            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(784f, 178f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Reserve miel"));
        }

        [Test]
        public void AcademyContourStaysOnTheLeft()
        {
            Assert.That(HiveViewProductUiPresenter.ReferenceHotspotAtArtPointForProof(471f, 262f), Is.EqualTo("academy_canopy"));
            Assert.That(HiveViewProductUiPresenter.ReferenceHotspotAtArtPointForProof(1180f, 230f), Is.EqualTo(string.Empty));
            Assert.That(HiveViewProductUiPresenter.PixelPerfectContourPriorityForProof(260f, 250f), Does.StartWith("academy_canopy|"));
            Assert.That(HiveViewProductUiPresenter.PixelPerfectContourPriorityForProof(1180f, 230f), Does.Not.StartWith("academy_canopy|"));
        }

        [Test]
        public void HiveBackgroundClickClosesBuildingMenu()
        {
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            Assert.That(HiveViewProductUiPresenter.IsReferenceBuildingMenuOpenForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.DeselectReferenceHotspotAtBackgroundArtPointForProof(1180f, 230f), Is.True);
            Assert.That(HiveViewProductUiPresenter.IsReferenceBuildingMenuOpenForProof(), Is.False);

            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            Assert.That(HiveViewProductUiPresenter.DeselectReferenceHotspotAtBackgroundArtPointForProof(784f, 178f), Is.False);
            Assert.That(HiveViewProductUiPresenter.IsReferenceBuildingMenuOpenForProof(), Is.True);
        }

        [Test]
        public void UpgradeQueueUsesTheActiveBuildingLabel()
        {
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("nursery_cluster");
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_action_confirm_upgrade");
            Assert.That(HiveViewProductUiPresenter.UpgradeQueueBuildingLabelForProof(), Is.EqualTo("Nurserie"));
        }

        [Test]
        public void PlayerProfileRowsDoNotOverlap()
        {
            Assert.That(HiveViewProductUiPresenter.PlayerProfileRowsDoNotOverlapForProof(), Is.True);
        }

        private static void AssertRow(string[] rows, string expected)
        {
            if (!rows.Any(row => string.Equals(row, expected, StringComparison.Ordinal)))
            {
                Assert.Fail("Expected proof row not found: " + expected + Environment.NewLine + string.Join(Environment.NewLine, rows));
            }
        }
    }
}
