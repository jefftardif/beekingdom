using BeeKingdom.LivingHiveMenu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Tests.Editor.Interaction
{
    // Tests du Header supérieur du port uGUI (Reine / ressources / Boutique).
    // Ils verrouillent : les données preview locales (une seule source, pas de 2e vérité),
    // la géométrie pixel-écran miroir du monolithe (DrawPortraitTopHud / DrawStrategyTopHud),
    // la construction du Header dans le Canvas et la bascule fonctionnelle des deux panneaux.
    public class LivingHiveMenuHeaderTests
    {
        // --- Données preview locales (miroir des valeurs statiques du monolithe) ---

        [Test]
        public void FormatResourceMatchesMonolithBuckets()
        {
            Assert.That(LivingHiveMenuHeaderData.FormatResource(125800), Is.EqualTo("125.8K"));
            Assert.That(LivingHiveMenuHeaderData.FormatResource(72300), Is.EqualTo("72.3K"));
            Assert.That(LivingHiveMenuHeaderData.FormatResource(98450), Is.EqualTo("98.5K"));
            Assert.That(LivingHiveMenuHeaderData.FormatResource(52300), Is.EqualTo("52.3K"));
            Assert.That(LivingHiveMenuHeaderData.FormatResource(999), Is.EqualTo("999"));
            Assert.That(LivingHiveMenuHeaderData.FormatResource(1500000), Is.EqualTo("1.5M"));
        }

        [Test]
        public void ResourceAccentsMatchMonolithPalette()
        {
            Assert.That(LivingHiveMenuHeaderData.ResourceAccent("honey"), Is.EqualTo(new Color(1f, 0.68f, 0.12f)));
            Assert.That(LivingHiveMenuHeaderData.ResourceAccent("wax"), Is.EqualTo(new Color(1f, 0.82f, 0.28f)));
            Assert.That(LivingHiveMenuHeaderData.ResourceAccent("pollen"), Is.EqualTo(new Color(0.78f, 0.90f, 0.32f)));
            Assert.That(LivingHiveMenuHeaderData.ResourceAccent("bees"), Is.EqualTo(new Color(0.56f, 0.82f, 1f)));
            Assert.That(LivingHiveMenuHeaderData.ResourceAccent("capacity"), Is.EqualTo(new Color(0.84f, 0.76f, 0.60f)));
        }

        [Test]
        public void PreviewValuesAreCentralizedInHeaderData()
        {
            Assert.That(LivingHiveMenuHeaderData.PreviewValue("honey"), Is.EqualTo(125800));
            Assert.That(LivingHiveMenuHeaderData.PreviewValue("wax"), Is.EqualTo(72300));
            Assert.That(LivingHiveMenuHeaderData.PreviewValue("pollen"), Is.EqualTo(98450));
            Assert.That(LivingHiveMenuHeaderData.PreviewValue("bees"), Is.EqualTo(52300));
            Assert.That(LivingHiveMenuHeaderData.PreviewValue("capacity"), Is.EqualTo(400));
            Assert.That(LivingHiveMenuHeaderData.PreviewMax("capacity"), Is.EqualTo(600));
            Assert.That(LivingHiveMenuHeaderData.PreviewQueenLevel, Is.EqualTo(3));
        }

        // --- Géométrie portrait (DrawPortraitTopHud : 3 chips, chipW=(w-22-12)/3) ---

        [Test]
        public void PortraitHeaderGeometryMatchesMonolithRects()
        {
            const float w = 720f, h = 1280f;
            Rect header = LivingHiveMenuHeaderData.PortraitHeaderRect(w, h);
            Assert.That(header, Is.EqualTo(new Rect(8f, 8f, w - 16f, 94f)));

            Rect queen = LivingHiveMenuHeaderData.PortraitQueenRect(w, h);
            Assert.That(queen.xMin, Is.EqualTo(16f));
            Assert.That(queen.yMin, Is.EqualTo(14f));

            Rect[] chips = LivingHiveMenuHeaderData.PortraitResourceChipRects(w, h);
            Assert.That(chips.Length, Is.EqualTo(3));
            const float gap = 6f;
            float chipW = (header.width - 22f - gap * 2f) / 3f;
            Assert.That(chips[0].width, Is.EqualTo(chipW).Within(0.001f));
            Assert.That(chips[1].x, Is.EqualTo(chips[0].x + chipW + gap).Within(0.001f));
            Assert.That(chips[2].x, Is.EqualTo(chips[1].x + chipW + gap).Within(0.001f));
            Assert.That(chips[0].x, Is.EqualTo(header.x + 11f).Within(0.001f), "x départ = panel.x+11");
            Assert.That(chips[0].y, Is.EqualTo(60f).Within(0.001f), "chipY=panel.y(8)+52");
            Assert.That(chips[0].height, Is.EqualTo(36f).Within(0.001f), "chipH=36");
            // Les 3 chips restent dans la largeur du Header (bord droit à 11px du bord).
            Assert.That(chips[2].xMax, Is.LessThanOrEqualTo(header.xMax - 10f));
        }

        [Test]
        public void PortraitShopButtonSitsTopRightWithoutOverflowingHeader()
        {
            const float w = 720f, h = 1280f;
            Rect header = LivingHiveMenuHeaderData.PortraitHeaderRect(w, h);
            Rect shop = LivingHiveMenuHeaderData.PortraitShopRect(w, h);
            Assert.That(shop.xMax, Is.LessThanOrEqualTo(header.xMax - 8f));
            Assert.That(shop.yMax, Is.LessThanOrEqualTo(header.yMax));
            Assert.That(shop.width, Is.EqualTo(46f).Within(0.001f));
            Assert.That(shop.height, Is.EqualTo(40f).Within(0.001f));
        }

        // --- Géométrie paysage (DrawStrategyTopHud : 5 pils, resourceW formule) ---

        [Test]
        public void LandscapeHeaderHeightDependsOnTabletFlag()
        {
            Assert.That(LivingHiveMenuHeaderData.LandscapeHeaderHeight(1280f, 720f), Is.EqualTo(112f));
            Assert.That(LivingHiveMenuHeaderData.LandscapeHeaderHeight(1600f, 900f), Is.EqualTo(132f));
            Assert.That(LivingHiveMenuHeaderData.LandscapeHeaderHeight(1920f, 1080f), Is.EqualTo(132f));
        }

        [Test]
        public void LandscapeResourceRectsFillBetweenQueenAndShop()
        {
            const float w = 1280f, h = 720f;
            Rect[] pills = LivingHiveMenuHeaderData.LandscapeResourceRects(w, h);
            Assert.That(pills.Length, Is.EqualTo(5));

            float resourceX = 18f + 178f + 16f;
            float gap = 5f;
            float resourceW = Mathf.Max(82f, (w - resourceX - 42f - 18f - 5f * gap) / 5f);
            Assert.That(pills[0].x, Is.EqualTo(resourceX).Within(0.001f));
            Assert.That(pills[0].width, Is.EqualTo(resourceW).Within(0.001f));
            Assert.That(pills[4].xMax, Is.LessThanOrEqualTo(w - 8f - 44f), "les pils s'arrêtent avant la Boutique");

            Rect shop = LivingHiveMenuHeaderData.LandscapeShopRect(w, h);
            Assert.That(shop.x, Is.EqualTo(w - 8f - 44f).Within(0.001f));
        }

        // --- Construction du Header dans le Canvas ---

        [Test]
        public void BuildCreatesHeaderWithBackdropQueenChipsAndShop()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                Assert.That(canvas.IsHeaderBuilt, Is.True);

                RectTransform backdrop = FindChild(root.transform, "HeaderBackdrop");
                Assert.That(backdrop, Is.Not.Null, "HeaderBackdrop doit exister.");
                Assert.That(FindChild(root.transform, "HeaderQueen"), Is.Not.Null, "Bouton Reine.");
                Assert.That(FindChild(root.transform, "HeaderShop"), Is.Not.Null, "Bouton Boutique.");
                Assert.That(FindChild(root.transform, "HeaderChip_honey"), Is.Not.Null);
                Assert.That(FindChild(root.transform, "HeaderChip_wax"), Is.Not.Null);
                Assert.That(FindChild(root.transform, "HeaderChip_pollen"), Is.Not.Null);

                bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
                int expectedChips = portrait ? 3 : 5;
                Assert.That(canvas.HeaderResourceChipCount, Is.EqualTo(expectedChips));
                if (!portrait)
                {
                    Assert.That(FindChild(root.transform, "HeaderChip_bees"), Is.Not.Null);
                    Assert.That(FindChild(root.transform, "HeaderChip_capacity"), Is.Not.Null);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HeaderBackdropSitsTopLeftUnderHeaderRect()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                RectTransform b = FindChild(root.transform, "HeaderBackdrop");
                Assert.That(b, Is.Not.Null);
                bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
                Rect imgui = portrait
                    ? LivingHiveMenuHeaderData.PortraitHeaderRect(Screen.width, Screen.height)
                    : LivingHiveMenuHeaderData.LandscapeHeaderRect(Screen.width, Screen.height);
                Rect ui = new Rect(imgui.x, Screen.height - (imgui.y + imgui.height), imgui.width, imgui.height);

                Assert.That(b.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(b.anchorMax, Is.EqualTo(Vector2.zero));
                Assert.That(b.pivot, Is.EqualTo(Vector2.zero));
                Assert.That(b.sizeDelta.x, Is.EqualTo(ui.width).Within(0.001f));
                Assert.That(b.sizeDelta.y, Is.EqualTo(ui.height).Within(0.001f));
                Assert.That(b.anchoredPosition.x, Is.EqualTo(ui.x).Within(0.001f));
                Assert.That(b.anchoredPosition.y, Is.EqualTo(ui.y).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResourceChipValuesUseCentralizedPreviewAndFormat()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                Assert.That(canvas.HeaderResourceValue("honey"), Is.EqualTo("125.8K"));
                Assert.That(canvas.HeaderResourceValue("wax"), Is.EqualTo("72.3K"));
                Assert.That(canvas.HeaderResourceValue("pollen"), Is.EqualTo("98.5K"));
                if (!LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height))
                {
                    Assert.That(canvas.HeaderResourceValue("bees"), Is.EqualTo("52.3K"));
                    Assert.That(canvas.HeaderResourceValue("capacity"), Is.EqualTo("400/600"));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void QueenClickTogglesProfilePanel()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                Assert.That(canvas.QueenProfileShown, Is.False);
                Assert.That(canvas.PanelShown("QueenProfile"), Is.False);

                canvas.SimulateHeaderClick(LivingHiveMenuCanvas.HeaderQueenElementId);
                Assert.That(canvas.QueenProfileShown, Is.True);
                Assert.That(canvas.PanelShown("QueenProfile"), Is.True);

                canvas.SimulateHeaderClick(LivingHiveMenuCanvas.HeaderQueenElementId);
                Assert.That(canvas.QueenProfileShown, Is.False);
                Assert.That(canvas.PanelShown("QueenProfile"), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShopClickTogglesShopPanel()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                Assert.That(canvas.ShopShown, Is.False);
                canvas.SimulateHeaderClick(LivingHiveMenuCanvas.HeaderShopElementId);
                Assert.That(canvas.ShopShown, Is.True);
                Assert.That(canvas.PanelShown("Shop"), Is.True);

                canvas.SimulateHeaderClick(LivingHiveMenuCanvas.HeaderShopElementId);
                Assert.That(canvas.ShopShown, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // --- Positionnement Reine/Boutique (bug de conversion IMGUI -> uGUI corrigé) ---

        [Test]
        public void QueenProfilePanelUsesScreenToUiConversion()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                RectTransform panel = FindChild(root.transform, "Panel_QueenProfile");
                Assert.That(panel, Is.Not.Null, "Panel_QueenProfile doit exister.");

                bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
                Rect imgui = LivingHiveMenuHeaderData.QueenProfilePanelRect(portrait, Screen.width, Screen.height);
                Rect expectedUi = new Rect(imgui.x, Screen.height - (imgui.y + imgui.height), imgui.width, imgui.height);

                Assert.That(panel.anchoredPosition.x, Is.EqualTo(expectedUi.x).Within(0.001f));
                Assert.That(panel.anchoredPosition.y, Is.EqualTo(expectedUi.y).Within(0.001f));
                Assert.That(panel.sizeDelta.x, Is.EqualTo(expectedUi.width).Within(0.001f));
                Assert.That(panel.sizeDelta.y, Is.EqualTo(expectedUi.height).Within(0.001f));

                // Régression explicite du bug : la position ne doit PAS être la valeur IMGUI
                // brute (non convertie) telle qu'observée avant correction.
                Assert.That(panel.anchoredPosition.y, Is.Not.EqualTo(imgui.y).Within(0.001f),
                    "La position ne doit pas être la valeur IMGUI brute, non convertie.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShopPanelUsesScreenToUiConversion()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                RectTransform panel = FindChild(root.transform, "Panel_Shop");
                Assert.That(panel, Is.Not.Null, "Panel_Shop doit exister.");

                bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
                Rect imgui = LivingHiveMenuHeaderData.ShopPanelRect(portrait, Screen.width, Screen.height);
                Rect expectedUi = new Rect(imgui.x, Screen.height - (imgui.y + imgui.height), imgui.width, imgui.height);

                Assert.That(panel.anchoredPosition.x, Is.EqualTo(expectedUi.x).Within(0.001f));
                Assert.That(panel.anchoredPosition.y, Is.EqualTo(expectedUi.y).Within(0.001f));
                Assert.That(panel.sizeDelta.x, Is.EqualTo(expectedUi.width).Within(0.001f));
                Assert.That(panel.sizeDelta.y, Is.EqualTo(expectedUi.height).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void QueenProfilePanelStaysWithinScreenBounds()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                RectTransform panel = FindChild(root.transform, "Panel_QueenProfile");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.anchoredPosition.x, Is.GreaterThanOrEqualTo(0f));
                Assert.That(panel.anchoredPosition.y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(panel.anchoredPosition.x + panel.sizeDelta.x, Is.LessThanOrEqualTo((float)Screen.width));
                Assert.That(panel.anchoredPosition.y + panel.sizeDelta.y, Is.LessThanOrEqualTo((float)Screen.height));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShopPanelStaysWithinScreenBounds()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                RectTransform panel = FindChild(root.transform, "Panel_Shop");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.anchoredPosition.x, Is.GreaterThanOrEqualTo(0f),
                    "Régression du bug : Panel_Shop se retrouvait hors écran (X > largeur écran).");
                Assert.That(panel.anchoredPosition.y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(panel.anchoredPosition.x + panel.sizeDelta.x, Is.LessThanOrEqualTo((float)Screen.width));
                Assert.That(panel.anchoredPosition.y + panel.sizeDelta.y, Is.LessThanOrEqualTo((float)Screen.height));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HeaderOverlayPanelsRepositionCorrectlyWhenTriggered()
        {
            var root = new GameObject("MenuTest");
            try
            {
                var canvas = root.AddComponent<LivingHiveMenuCanvas>();
                canvas.Build();

                RectTransform queenPanel = FindChild(root.transform, "Panel_QueenProfile");
                RectTransform shopPanel = FindChild(root.transform, "Panel_Shop");
                Vector2 queenBefore = queenPanel.anchoredPosition;
                Vector2 shopBefore = shopPanel.anchoredPosition;

                // Screen.width/height ne peuvent pas être changés en EditMode : ce hook force
                // le chemin de repositionnement (celui déclenché par Update() lors d'un vrai
                // changement de résolution) pour verrouiller qu'il reproduit exactement la
                // même géométrie correcte, sans dépendre d'un rebuild complet du contenu.
                canvas.SimulateHeaderOverlayRepositionForProof();

                bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
                Rect queenImgui = LivingHiveMenuHeaderData.QueenProfilePanelRect(portrait, Screen.width, Screen.height);
                Rect shopImgui = LivingHiveMenuHeaderData.ShopPanelRect(portrait, Screen.width, Screen.height);
                Rect queenExpected = new Rect(queenImgui.x, Screen.height - (queenImgui.y + queenImgui.height), queenImgui.width, queenImgui.height);
                Rect shopExpected = new Rect(shopImgui.x, Screen.height - (shopImgui.y + shopImgui.height), shopImgui.width, shopImgui.height);

                Assert.That(queenPanel.anchoredPosition.x, Is.EqualTo(queenExpected.x).Within(0.001f));
                Assert.That(queenPanel.anchoredPosition.y, Is.EqualTo(queenExpected.y).Within(0.001f));
                Assert.That(shopPanel.anchoredPosition.x, Is.EqualTo(shopExpected.x).Within(0.001f));
                Assert.That(shopPanel.anchoredPosition.y, Is.EqualTo(shopExpected.y).Within(0.001f));

                // Idempotence : le repositionnement ne doit rien casser si Screen.width/height
                // n'ont en fait pas changé (identique au comportement de RebuildRail/RebuildHeader).
                Assert.That(queenPanel.anchoredPosition, Is.EqualTo(queenBefore));
                Assert.That(shopPanel.anchoredPosition, Is.EqualTo(shopBefore));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static RectTransform FindChild(Transform root, string name)
        {
            foreach (RectTransform child in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }
}