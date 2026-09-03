using System.Collections.Generic;
using BeeKingdom.LivingHiveMenu;
using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Shared IMGUI-space occlusion map for world-presentation overlays drawn from HiveMap.
    /// It clips decorative/resource indicators against opaque UI surfaces without disabling
    /// their gameplay state or their animation owners.
    /// </summary>
    public static class HiveMapUiOcclusion
    {
        public const int ModalWindowGuiDepth = -32000;

        private static readonly List<Rect> Occluders = new List<Rect>(12);
        private static readonly List<Rect> Scratch = new List<Rect>(16);

        public static void GetWorldPresentationVisibleRegions(List<Rect> results)
        {
            results.Clear();
            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            if (screen.width <= 0f || screen.height <= 0f) return;

            results.Add(screen);
            FillOpaqueUiOccluders(Occluders);

            for (int i = 0; i < Occluders.Count; i++)
            {
                Rect occluder = ClampToScreen(Occluders[i], screen);
                if (occluder.width <= 0f || occluder.height <= 0f) continue;

                Scratch.Clear();
                for (int r = 0; r < results.Count; r++)
                {
                    Subtract(results[r], occluder, Scratch);
                }

                results.Clear();
                results.AddRange(Scratch);
                if (results.Count == 0) break;
            }
        }

        public static bool HasVisibleAreaOutside(Rect candidate, List<Rect> visibleRegions)
        {
            for (int i = 0; i < visibleRegions.Count; i++)
            {
                if (Intersects(candidate, visibleRegions[i])) return true;
            }

            return false;
        }

        public static void SubtractForProof(Rect source, Rect occluder, List<Rect> results)
        {
            results.Clear();
            Subtract(source, occluder, results);
        }

        private static void FillOpaqueUiOccluders(List<Rect> results)
        {
            results.Clear();

            if (IsFullscreenOverlayOpen())
            {
                results.Add(new Rect(0f, 0f, Screen.width, Screen.height));
                return;
            }

            if (HiveViewProductUiPresenter.MiniChatOnlyOpenForExternalHost)
            {
                results.Add(HiveViewProductUiPresenter.MiniChatOcclusionRectForExternalHost);
            }

            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            results.Add(LivingHiveMenuSpec.RailRectForProof(portrait, Screen.width, Screen.height));
            results.Add(portrait
                ? LivingHiveMenuHeaderData.PortraitHeaderRect(Screen.width, Screen.height)
                : LivingHiveMenuHeaderData.LandscapeHeaderRect(Screen.width, Screen.height));

            if (BeeKingdom.Tutorial.TutorialDialoguePresenter.IsAnyDialogueVisible)
            {
                results.Add(BeeKingdom.Tutorial.TutorialDialoguePresenter.GetCurrentOcclusionRect());
            }
        }

        private static bool IsFullscreenOverlayOpen()
        {
            return HiveViewProductUiPresenter.CommunicationOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.ResearchOverlayOpenForExternalHost
                || LivingHiveResearchRuntime.IsModalOpen
                || HiveMapActivitiesBootstrap.ModalOpenForExternalHost
                || HiveMapRoyalPalaceBootstrap.ModalOpenForExternalHost
                || HiveMapArmyBootstrap.ModalOpenForExternalHost
                || HiveViewProductUiPresenter.AllianceOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.ConstructionOverlayOpenForExternalHost
                || HiveViewProductUiPresenter.SettingsOverlayOpenForExternalHost
                || HiveMapNurseryBootstrap.OverlayOpenForExternalHost
                || HiveMapProductionInfoBootstrap.OverlayOpenForExternalHost
                || HiveMapChampionHallBootstrap.OverlayOpenForExternalHost
                || HiveMapUnsupportedBuildingBootstrap.OverlayOpenForExternalHost;
        }

        private static Rect ClampToScreen(Rect rect, Rect screen)
        {
            float xMin = Mathf.Max(rect.xMin, screen.xMin);
            float yMin = Mathf.Max(rect.yMin, screen.yMin);
            float xMax = Mathf.Min(rect.xMax, screen.xMax);
            float yMax = Mathf.Min(rect.yMax, screen.yMax);
            return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
        }

        private static void Subtract(Rect source, Rect occluder, List<Rect> results)
        {
            if (!Intersects(source, occluder))
            {
                AddIfPositive(results, source);
                return;
            }

            Rect overlap = Rect.MinMaxRect(
                Mathf.Max(source.xMin, occluder.xMin),
                Mathf.Max(source.yMin, occluder.yMin),
                Mathf.Min(source.xMax, occluder.xMax),
                Mathf.Min(source.yMax, occluder.yMax));

            AddIfPositive(results, Rect.MinMaxRect(source.xMin, source.yMin, source.xMax, overlap.yMin));
            AddIfPositive(results, Rect.MinMaxRect(source.xMin, overlap.yMax, source.xMax, source.yMax));
            AddIfPositive(results, Rect.MinMaxRect(source.xMin, overlap.yMin, overlap.xMin, overlap.yMax));
            AddIfPositive(results, Rect.MinMaxRect(overlap.xMax, overlap.yMin, source.xMax, overlap.yMax));
        }

        private static bool Intersects(Rect a, Rect b)
        {
            return a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
        }

        private static void AddIfPositive(List<Rect> results, Rect rect)
        {
            if (rect.width > 0f && rect.height > 0f) results.Add(rect);
        }
    }
}
