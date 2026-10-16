using System.Reflection;
using BeeKingdom.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tutorial.Tests.Editor
{
    public sealed class TutorialPremiumDialogueTests
    {
        private static object Layout(int width, int height, Rect safe)
        {
            return typeof(TutorialDialoguePresenter).GetMethod("CalculateLayout",
                BindingFlags.NonPublic | BindingFlags.Static).Invoke(null,
                new object[] { (float)width, (float)height, safe });
        }
        private static Rect Box(object layout, string name)
        {
            return (Rect)layout.GetType().GetField(name).GetValue(layout);
        }

        [TestCase(1280, 720)]
        [TestCase(1600, 900)]
        [TestCase(1920, 1080)]
        [TestCase(2560, 1440)]
        [TestCase(3840, 2160)]
        public void LandscapeBar_LeavesPalaceAndBottomNavigationVisible(int width, int height)
        {
            var layout = Layout(width, height, new Rect(0, 0, width, height));
            Rect panel = Box(layout, "Panel");
            Assert.That(panel.height / height, Is.LessThanOrEqualTo(0.28f), "No return to the oversized black panel.");
            Assert.That(panel.width / width, Is.LessThanOrEqualTo(0.80f));
            Assert.That(panel.yMin / height, Is.GreaterThanOrEqualTo(0.61f));
            Assert.That((height - panel.yMax) / height, Is.GreaterThanOrEqualTo(0.095f));
            Assert.That(Box(layout, "Text").Overlaps(Box(layout, "Continue")), Is.False);
            Assert.That(Box(layout, "Text").Overlaps(Box(layout, "Skip")), Is.False);
            Assert.That(Box(layout, "Portrait").Overlaps(Box(layout, "Text")), Is.False);
        }

        [TestCase(844, 390, 47, 21, 750, 369)]
        [TestCase(1170, 2532, 0, 102, 1170, 2289)]
        [TestCase(320, 568, 0, 20, 320, 528)]
        public void MobileSafeArea_PreservesTouchTargets(int width, int height, int x, int y, int sw, int sh)
        {
            var layout = Layout(width, height, new Rect(x, y, sw, sh));
            Rect panel = Box(layout, "Panel");
            Rect safe = new Rect(x, height-y-sh, sw, sh);
            Assert.That(safe.Contains(panel.min) && safe.Contains(panel.max), Is.True);
            foreach (string name in new[] { "Text", "Name", "Portrait", "Continue", "Skip" })
            {
                Rect child = Box(layout, name);
                Assert.That(panel.Contains(child.min) && panel.Contains(child.max), Is.True, name);
            }
            Assert.That(Box(layout, "Continue").height, Is.GreaterThanOrEqualTo(48f));
            Assert.That(Box(layout, "Skip").height, Is.GreaterThanOrEqualTo(48f));
        }

        [Test]
        public void ButtonStyle_HasNoDefaultOrHighDpiBackgroundCoveringTheGoldArt()
        {
            var type = typeof(TutorialDialoguePresenter).Assembly.GetType("BeeKingdom.Tutorial.TutorialPremiumSkin");
            var method = type.GetMethod("ButtonStyle", BindingFlags.NonPublic | BindingFlags.Static);
            var style = (GUIStyle)method.Invoke(null, new object[] { Color.white, 24 });
            foreach (var state in new[] { style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused })
            {
                Assert.That(state.background, Is.Null);
                Assert.That(state.scaledBackgrounds == null || state.scaledBackgrounds.Length == 0, Is.True);
            }
        }
        [Test]
        public void ApprovedOrnamentsAndRoyalFont_AreAvailableToPlayerBuilds()
        {
            var atlas = Resources.Load<Texture2D>("TutorialPremium/royal-dialogue-atlas");
            Assert.That(atlas, Is.Not.Null);
            Assert.That(atlas.width, Is.EqualTo(352));
            Assert.That(atlas.height, Is.EqualTo(256));
            Assert.That(Resources.Load<Font>("TutorialPremium/Cinzel-Regular"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("WorldMapWave6Runtime/CombatMarch/ChampionMarchBody_zephyra"), Is.Not.Null);
        }
    }
}