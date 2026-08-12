using System;
using UnityEngine;

namespace BeeKingdom.WorldMap
{
    public enum WorldPointerGestureKind
    {
        Tap = 0,
        DoubleTap = 1,
        DragStart = 2,
        Drag = 3,
        DragEnd = 4,
        Zoom = 5
    }

    public struct WorldPointerGesture
    {
        public WorldPointerGestureKind Kind { get; }
        public Vector2 ScreenPoint { get; }
        public Vector2 ScreenDelta { get; }
        public float ZoomFactor { get; }

        public WorldPointerGesture(WorldPointerGestureKind kind, Vector2 screenPoint, Vector2 screenDelta = default, float zoomFactor = 1f)
        {
            Kind = kind;
            ScreenPoint = screenPoint;
            ScreenDelta = screenDelta;
            ZoomFactor = zoomFactor;
        }
    }

    // Source brute d'entree (souris/clavier d'un cote, touches mobiles de l'autre).
    public interface IWorldInputSource
    {
        bool PrimaryDown { get; }
        Vector2 PrimaryPosition { get; }
        Vector2 ScreenSize { get; }
        float ScrollDelta { get; }
        bool PinchActive { get; }
        float PinchRatio { get; }
        Vector2 PinchPivot { get; }
        bool MoveLeft { get; }
        bool MoveRight { get; }
        bool MoveUp { get; }
        bool MoveDown { get; }
    }

    // Horloge injectable (tests : horloge virtuelle).
    public interface IWorldInputClock
    {
        double NowSeconds { get; }
    }

    // Traduit la source brute en gestes : tap, double tap (compatible futur),
    // drag (avec seuil) et zoom (molette ou pincement). Ne connait pas la carte.
    public sealed class WorldInputProcessor
    {
        private readonly IWorldInputSource source;
        private readonly IWorldInputClock clock;
        private readonly CameraSettings settings;
        private bool dragging;
        private Vector2 dragStartScreen;
        private double lastTapTime = double.NegativeInfinity;
        private Vector2 lastTapScreen;

        public event Action<WorldPointerGesture> Gesture;

        public WorldInputProcessor(IWorldInputSource source, IWorldInputClock clock, CameraSettings settings)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Process()
        {
            if (source.PrimaryDown)
            {
                if (!dragging)
                {
                    dragging = true;
                    dragStartScreen = source.PrimaryPosition;
                    Raise(WorldPointerGestureKind.DragStart, source.PrimaryPosition);
                }
                else
                {
                    Raise(WorldPointerGestureKind.Drag, source.PrimaryPosition, source.PrimaryPosition - dragStartScreen);
                }
            }
            else if (dragging)
            {
                Vector2 endScreen = source.PrimaryPosition;
                float dragDistance = Vector2.Distance(endScreen, dragStartScreen);
                dragging = false;
                if (dragDistance <= settings.DragThresholdPixels)
                {
                    HandleTap(endScreen);
                }
                else
                {
                    Raise(WorldPointerGestureKind.DragEnd, endScreen);
                }
            }

            if (source.PinchActive && source.PinchRatio > 0f)
            {
                Raise(WorldPointerGestureKind.Zoom, source.PinchPivot, zoomFactor: source.PinchRatio);
            }
            else if (Mathf.Abs(source.ScrollDelta) > 0.001f)
            {
                Raise(WorldPointerGestureKind.Zoom, source.PrimaryPosition, zoomFactor: Mathf.Exp(source.ScrollDelta * 0.1f));
            }
        }

        private void HandleTap(Vector2 screen)
        {
            double now = clock.NowSeconds;
            bool isDoubleTap = settings.DoubleClickEnabled &&
                now - lastTapTime <= settings.DoubleClickWindowSeconds &&
                Vector2.Distance(screen, lastTapScreen) <= settings.DoubleClickRadiusPixels;

            if (isDoubleTap)
            {
                lastTapTime = double.NegativeInfinity;
                Raise(WorldPointerGestureKind.DoubleTap, screen);
            }
            else
            {
                lastTapTime = now;
                lastTapScreen = screen;
                Raise(WorldPointerGestureKind.Tap, screen);
            }
        }

        private void Raise(WorldPointerGestureKind kind, Vector2 screen, Vector2 delta = default, float zoomFactor = 1f)
        {
            Gesture?.Invoke(new WorldPointerGesture(kind, screen, delta, zoomFactor));
        }
    }
}
