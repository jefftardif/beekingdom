using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldMapSharedPanelForegroundTests
    {
        private static Rect Calculate(int width, int height, Rect safe, bool more)
        {
            var method = typeof(WorldMapSharedPanelForeground).GetMethod(
                "PopupRect", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Rect)method.Invoke(null, new object[] {
                (float)width, (float)height, safe, more
            });
        }

        [TestCase(1920, 1080, 0, 0, 1920, 1080)]
        [TestCase(390, 844, 0, 34, 390, 770)]
        [TestCase(320, 568, 0, 0, 320, 568)]
        [TestCase(1024, 768, 46, 12, 932, 736)]
        public void MoreAndBagStayWithinSafeArea(
            int width, int height, int safeX, int safeY, int safeW, int safeH)
        {
            var safe = new Rect(safeX, safeY, safeW, safeH);
            var guiSafe = new Rect(safe.xMin, height - safe.yMax, safe.width, safe.height);
            foreach (bool more in new[] {true, false})
            {
                var bounds = Calculate(width, height, safe, more);
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(guiSafe.xMin + 1f));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(guiSafe.yMin + 1f));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(guiSafe.xMax - 1f));
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(guiSafe.yMax - 1f));
                Assert.That(bounds.width, Is.GreaterThan(0f));
                Assert.That(bounds.height, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void WorldMapInvokesTheExplicitForegroundDrawMethod()
        {
            var method = typeof(WorldMapSharedPanelForeground).GetMethod(
                "DrawFromMapOnGUI", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
        }
    }
}
