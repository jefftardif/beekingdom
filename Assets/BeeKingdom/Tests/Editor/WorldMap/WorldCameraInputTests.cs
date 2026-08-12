using System.Collections.Generic;
using BeeKingdom.WorldMap;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldCameraMathTests
    {
        private static CameraSettings Settings()
        {
            return new CameraSettings(zoomMin: 8f, zoomMax: 512f);
        }

        [Test]
        public void ScreenWorldRoundTrip()
        {
            var screenSize = new Vector2(1080f, 1920f);
            var cameraPosition = new WorldVector2(1234.5d, -987.25d);
            float zoom = 64f;
            var screenPoint = new Vector2(270f, 480f);

            WorldVector2 world = WorldCameraMath.ScreenToWorld(screenPoint, screenSize, cameraPosition, zoom);
            Vector2 back = WorldCameraMath.WorldToScreen(world, screenSize, cameraPosition, zoom);

            Assert.That(back.x, Is.EqualTo(screenPoint.x).Within(1e-3f));
            Assert.That(back.y, Is.EqualTo(screenPoint.y).Within(1e-3f));
        }

        [Test]
        public void ZoomAboutPivotKeepsPivotWorldPointFixed()
        {
            var screenSize = new Vector2(1080f, 1920f);
            var cameraPosition = new WorldVector2(100d, 200d);
            float oldZoom = 64f;
            float newZoom = 128f;
            var pivot = new WorldVector2(150d, 220d);
            Vector2 screenPivot = WorldCameraMath.WorldToScreen(pivot, screenSize, cameraPosition, oldZoom);

            WorldVector2 newPosition = WorldCameraMath.ZoomAboutPivot(cameraPosition, oldZoom, newZoom, pivot);
            WorldVector2 worldAtPivot = WorldCameraMath.ScreenToWorld(screenPivot, screenSize, newPosition, newZoom);

            Assert.That(worldAtPivot.X, Is.EqualTo(pivot.X).Within(1e-6d));
            Assert.That(worldAtPivot.Y, Is.EqualTo(pivot.Y).Within(1e-6d));
        }

        [Test]
        public void ZoomOutFromCenterMovesPositionAway()
        {
            var pivot = new WorldVector2(0d, 0d);

            WorldVector2 newPosition = WorldCameraMath.ZoomAboutPivot(new WorldVector2(10d, 0d), 64f, 128f, pivot);

            Assert.That(newPosition.X, Is.EqualTo(20d).Within(1e-6d));
            Assert.That(newPosition.Y, Is.EqualTo(0d).Within(1e-6d));
        }

        [Test]
        public void ClampZoomRespectsRange()
        {
            CameraSettings settings = Settings();

            Assert.That(WorldCameraMath.ClampZoom(1f, settings), Is.EqualTo(8f));
            Assert.That(WorldCameraMath.ClampZoom(10000f, settings), Is.EqualTo(512f));
            Assert.That(WorldCameraMath.ClampZoom(64f, settings), Is.EqualTo(64f));
        }

        [Test]
        public void ClampPositionAppliesOnlyWhenBounded()
        {
            CameraSettings unbounded = Settings();
            CameraSettings bounded = new CameraSettings(
                hasBounds: true,
                minBound: new WorldPosition(-1000L, -1000L),
                maxBound: new WorldPosition(1000L, 1000L));

            WorldVector2 outside = new WorldVector2(5000d, -5000d);

            Assert.That(WorldCameraMath.ClampPosition(outside, unbounded), Is.EqualTo(outside));
            WorldVector2 clamped = WorldCameraMath.ClampPosition(outside, bounded);
            Assert.That(clamped.X, Is.EqualTo(1000d));
            Assert.That(clamped.Y, Is.EqualTo(-1000d));
        }

        [Test]
        public void DecayVelocityDecaysExponentially()
        {
            var velocity = new WorldVector2(100d, -50d);

            WorldVector2 decayed = WorldCameraMath.DecayVelocity(velocity, 0.5f, 0.5f);

            Assert.That(decayed.X, Is.LessThan(100d));
            Assert.That(decayed.X, Is.GreaterThan(10d));
            Assert.That(decayed.X / 100d, Is.EqualTo(decayed.Y / -50d).Within(1e-9d));
        }

        [Test]
        public void ClampMagnitudeCapsAndPreservesDirection()
        {
            var velocity = new WorldVector2(300d, 400d);

            WorldVector2 clamped = WorldCameraMath.ClampMagnitude(velocity, 100d);

            Assert.That(WorldCameraMath.Magnitude(clamped), Is.EqualTo(100d).Within(1e-6d));
            Assert.That(clamped.X, Is.EqualTo(60d).Within(1e-6d));
            Assert.That(clamped.Y, Is.EqualTo(80d).Within(1e-6d));
        }

        [Test]
        public void ScreenDeltaScalesWithZoom()
        {
            var screenSize = new Vector2(1080f, 1920f);
            var delta = new Vector2(100f, 50f);

            WorldVector2 wide = WorldCameraMath.ScreenDeltaToWorld(delta, screenSize, 100f);
            WorldVector2 narrow = WorldCameraMath.ScreenDeltaToWorld(delta, screenSize, 50f);

            Assert.That(wide.X, Is.EqualTo(narrow.X * 2d).Within(1e-6d));
        }
    }

    public sealed class WorldInputProcessorTests
    {
        private static (WorldInputProcessor processor, FakeInputSource source, FakeInputClock clock, List<WorldPointerGesture> gestures) Create(double now = 1000d)
        {
            var source = new FakeInputSource();
            var clock = new FakeInputClock(now);
            var settings = new CameraSettings(dragThresholdPixels: 8f, doubleClickWindowSeconds: 0.35f, doubleClickRadiusPixels: 36f);
            var processor = new WorldInputProcessor(source, clock, settings);
            var gestures = new List<WorldPointerGesture>();
            processor.Gesture += gestures.Add;
            return (processor, source, clock, gestures);
        }

        [Test]
        public void TapProducesDragStartAndTap()
        {
            (WorldInputProcessor processor, FakeInputSource source, _, List<WorldPointerGesture> gestures) = Create();

            source.PrimaryDown = true;
            source.PrimaryPosition = new Vector2(100f, 100f);
            processor.Process();
            source.PrimaryDown = false;
            processor.Process();

            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.DragStart), Is.True);
            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.Tap), Is.True);
            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.DragEnd), Is.False);
        }

        [Test]
        public void LongDragProducesDragButNoTap()
        {
            (WorldInputProcessor processor, FakeInputSource source, _, List<WorldPointerGesture> gestures) = Create();

            source.PrimaryDown = true;
            source.PrimaryPosition = new Vector2(100f, 100f);
            processor.Process();
            source.PrimaryPosition = new Vector2(200f, 200f);
            processor.Process();
            source.PrimaryDown = false;
            source.PrimaryPosition = new Vector2(200f, 200f);
            processor.Process();

            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.Drag), Is.True);
            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.DragEnd), Is.True);
            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.Tap), Is.False);
        }

        [Test]
        public void DoubleTapWithinWindowProducesDoubleTap()
        {
            (WorldInputProcessor processor, FakeInputSource source, FakeInputClock clock, List<WorldPointerGesture> gestures) = Create();

            source.PrimaryDown = true;
            processor.Process();
            source.PrimaryDown = false;
            processor.Process();
            clock.NowSeconds += 0.2d;
            source.PrimaryDown = true;
            processor.Process();
            source.PrimaryDown = false;
            processor.Process();

            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.DoubleTap), Is.True);
            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.Tap), Is.True);
        }

        [Test]
        public void SecondTapOutsideWindowIsASeparateTap()
        {
            (WorldInputProcessor processor, FakeInputSource source, FakeInputClock clock, List<WorldPointerGesture> gestures) = Create();

            source.PrimaryDown = true;
            processor.Process();
            source.PrimaryDown = false;
            processor.Process();
            clock.NowSeconds += 1d;
            source.PrimaryDown = true;
            processor.Process();
            source.PrimaryDown = false;
            processor.Process();

            int taps = gestures.FindAll(g => g.Kind == WorldPointerGestureKind.Tap).Count;
            Assert.That(taps, Is.EqualTo(2));
            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.DoubleTap), Is.False);
        }

        [Test]
        public void ScrollProducesZoomGesture()
        {
            (WorldInputProcessor processor, FakeInputSource source, _, List<WorldPointerGesture> gestures) = Create();

            source.ScrollDelta = 0.5f;
            processor.Process();

            Assert.That(gestures.Exists(g => g.Kind == WorldPointerGestureKind.Zoom && g.ZoomFactor > 1f), Is.True);
        }

        [Test]
        public void PinchOverridesScrollAndUsesPinchRatio()
        {
            (WorldInputProcessor processor, FakeInputSource source, _, List<WorldPointerGesture> gestures) = Create();

            source.PinchActive = true;
            source.PinchRatio = 0.8f;
            source.ScrollDelta = 0.5f;
            processor.Process();

            WorldPointerGesture zoom = gestures.Find(g => g.Kind == WorldPointerGestureKind.Zoom);
            Assert.That(zoom.ZoomFactor, Is.EqualTo(0.8f));
        }
    }
}
