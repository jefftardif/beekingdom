using System;
using UnityEngine;

namespace BeeKingdom.WorldMap
{
    // Mathematiques pures de la camera orthographique du monde. Zoom = demi-hauteur
    // visible en unites monde (orthographic size). Toute la physique de deplacement,
    // d'inertie, de pivot de zoom et de recentrage vit ici, testable sans Unity.
    public static class WorldCameraMath
    {
        public static float ClampZoom(float zoom, CameraSettings settings)
        {
            return Mathf.Clamp(zoom, settings.ZoomMin, settings.ZoomMax);
        }

        public static WorldVector2 ClampPosition(WorldVector2 position, CameraSettings settings)
        {
            if (!settings.HasBounds)
            {
                return position;
            }

            double x = Math.Max(settings.MinBound.X, Math.Min(settings.MaxBound.X, position.X));
            double y = Math.Max(settings.MinBound.Y, Math.Min(settings.MaxBound.Y, position.Y));
            return new WorldVector2(x, y);
        }

        // Position monde sous un point ecran, pour une camera orthographique centree.
        public static WorldVector2 ScreenToWorld(Vector2 screenPoint, Vector2 screenSize, WorldVector2 cameraPosition, float zoom)
        {
            double sx = (screenPoint.x - screenSize.x * 0.5) / (screenSize.y * 0.5) * zoom;
            double sy = (screenPoint.y - screenSize.y * 0.5) / (screenSize.y * 0.5) * zoom;
            return new WorldVector2(cameraPosition.X + sx, cameraPosition.Y + sy);
        }

        public static Vector2 WorldToScreen(WorldVector2 worldPoint, Vector2 screenSize, WorldVector2 cameraPosition, float zoom)
        {
            double dx = worldPoint.X - cameraPosition.X;
            double dy = worldPoint.Y - cameraPosition.Y;
            return new Vector2(
                (float)(dx / zoom * (screenSize.y * 0.5) + screenSize.x * 0.5),
                (float)(dy / zoom * (screenSize.y * 0.5) + screenSize.y * 0.5));
        }

        // Deplacement ecran -> deplacement monde, a zoom donne.
        public static WorldVector2 ScreenDeltaToWorld(Vector2 screenDelta, Vector2 screenSize, float zoom)
        {
            return new WorldVector2(
                screenDelta.x / (screenSize.y * 0.5) * zoom,
                screenDelta.y / (screenSize.y * 0.5) * zoom);
        }

        // Conserve le point monde sous le pivot pendant le changement de zoom.
        public static WorldVector2 ZoomAboutPivot(WorldVector2 cameraPosition, float oldZoom, float newZoom, WorldVector2 pivotWorld)
        {
            if (oldZoom <= 0f)
            {
                return cameraPosition;
            }

            double ratio = newZoom / oldZoom;
            double dx = (cameraPosition.X - pivotWorld.X) * ratio;
            double dy = (cameraPosition.Y - pivotWorld.Y) * ratio;
            return new WorldVector2(pivotWorld.X + dx, pivotWorld.Y + dy);
        }

        // Decroissance exponentielle de la vitesse (inertie), en unites monde/seconde.
        public static WorldVector2 DecayVelocity(WorldVector2 velocity, float decelerationTime, float deltaSeconds)
        {
            if (decelerationTime <= 0f || deltaSeconds <= 0f)
            {
                return velocity;
            }

            double factor = Math.Exp(-deltaSeconds / decelerationTime);
            return new WorldVector2(velocity.X * factor, velocity.Y * factor);
        }

        public static double Magnitude(WorldVector2 value)
        {
            return Math.Sqrt(value.X * value.X + value.Y * value.Y);
        }

        public static WorldVector2 ClampMagnitude(WorldVector2 value, double maxMagnitude)
        {
            double magnitude = Magnitude(value);
            if (magnitude <= maxMagnitude || magnitude <= 0d)
            {
                return value;
            }

            double scale = maxMagnitude / magnitude;
            return new WorldVector2(value.X * scale, value.Y * scale);
        }
    }

    // Etat complet de la camera, mute par le controleur.
    public struct WorldCameraState
    {
        public WorldVector2 Position;
        public float Zoom;
        public WorldVector2 Velocity;
    }
}
