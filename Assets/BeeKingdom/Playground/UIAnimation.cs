using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.UI
{
    public enum AnimationType
    {
        Fade,
        Scale,
        FadeAndScale
    }

    public enum Easing
    {
        Linear,
        EaseOutQuad,
        EaseOutCubic,
        EaseOutQuart,
        EaseOutExpo,
        EaseOutBack
    }

    public enum AnimationDirection
    {
        Forward,
        Reverse
    }

    public sealed class AnimationHandle
    {
        public string Key { get; }
        public AnimationType Type { get; }
        public float Duration { get; }
        public Easing Easing { get; }
        public float StartTime { get; private set; }
        public float EndTime { get; private set; }
        public AnimationDirection Direction { get; private set; }
        public bool IsPlaying => Direction != AnimationDirection.Forward || (UIAnimationLibrary.Now - StartTime) < Duration;
        public bool IsComplete => Direction == AnimationDirection.Forward && (UIAnimationLibrary.Now - StartTime) >= Duration;
        public float Progress01 => IsComplete ? 1f : Mathf.Clamp01((UIAnimationLibrary.Now - StartTime) / Duration);

        public AnimationHandle(string key, AnimationType type, float duration, Easing easing)
        {
            Key = key;
            Type = type;
            Duration = duration;
            Easing = easing;
        }

        public void Play(AnimationDirection direction = AnimationDirection.Forward)
        {
            Direction = direction;
            float now = UIAnimationLibrary.Now;
            if (direction == AnimationDirection.Forward)
            {
                StartTime = now;
                EndTime = now + Duration;
            }
            else
            {
                float elapsed = now - StartTime;
                StartTime = now - (Duration - elapsed);
                EndTime = now + elapsed;
            }
        }

        public void Stop()
        {
            Direction = AnimationDirection.Forward;
        }

        public float GetEasedProgress()
        {
            float t = Progress01;
            return EasingFunction(t, Easing);
        }

        private static float EasingFunction(float t, Easing easing)
        {
            switch (easing)
            {
                case Easing.Linear: return t;
                case Easing.EaseOutQuad: return 1f - (1f - t) * (1f - t);
                case Easing.EaseOutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Easing.EaseOutQuart: return 1f - Mathf.Pow(1f - t, 4f);
                case Easing.EaseOutExpo: return t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Easing.EaseOutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                default: return t;
            }
        }
    }

    public static class UIAnimationLibrary
    {
        private static readonly Dictionary<string, AnimationHandle> animations = new Dictionary<string, AnimationHandle>(128);
        private static float lastFrameTime;

        public static float Now
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return 1.6f;
#endif
                return Time.realtimeSinceStartup;
            }
        }

        public static float DeltaTime
        {
            get
            {
                float now = Now;
                float dt = now - lastFrameTime;
                lastFrameTime = now;
                return dt > 0f ? dt : 0f;
            }
        }

        public static AnimationHandle GetOrCreate(string key, AnimationType type, float duration, Easing easing = Easing.EaseOutCubic)
        {
            if (!animations.TryGetValue(key, out AnimationHandle handle))
            {
                handle = new AnimationHandle(key, type, duration, easing);
                animations[key] = handle;
            }
            return handle;
        }

        public static AnimationHandle GetOrCreate(string key, AnimationType type, float duration, Easing easing, out bool created)
        {
            created = !animations.TryGetValue(key, out AnimationHandle handle);
            if (created)
            {
                handle = new AnimationHandle(key, type, duration, easing);
                animations[key] = handle;
            }
            return handle;
        }

        public static bool TryGet(string key, out AnimationHandle handle)
        {
            return animations.TryGetValue(key, out handle);
        }

        public static void Remove(string key)
        {
            animations.Remove(key);
        }

        public static void Clear()
        {
            animations.Clear();
        }

        public static float GetFadeProgress(string key, float defaultValue = 1f)
        {
            if (TryGet(key, out AnimationHandle handle) && handle.Type != AnimationType.Scale)
            {
                return handle.GetEasedProgress();
            }
            return defaultValue;
        }

        public static float GetScaleProgress(string key, float defaultValue = 1f)
        {
            if (TryGet(key, out AnimationHandle handle) && handle.Type != AnimationType.Fade)
            {
                float p = handle.GetEasedProgress();
                if (handle.Type == AnimationType.FadeAndScale)
                {
                    return Mathf.Lerp(0.95f, 1f, p);
                }
                return Mathf.Lerp(0.96f, 1f, p);
            }
            return defaultValue;
        }

        public static float GetButtonPressProgress(string key)
        {
            if (TryGet(key, out AnimationHandle handle) && handle.Type == AnimationType.Scale)
            {
                float p = handle.GetEasedProgress();
                return Mathf.Lerp(0.96f, 1f, p);
            }
            return 1f;
        }

        public static void BeginWindowOpen(string key, float duration = 0.2f)
        {
            var handle = GetOrCreate(key, AnimationType.FadeAndScale, duration, Easing.EaseOutCubic);
            handle.Play(AnimationDirection.Forward);
        }

        public static void BeginWindowClose(string key, float duration = 0.18f)
        {
            var handle = GetOrCreate(key, AnimationType.FadeAndScale, duration, Easing.EaseOutCubic);
            handle.Play(AnimationDirection.Reverse);
        }

        public static void BeginButtonPress(string key, float duration = 0.08f)
        {
            var handle = GetOrCreate(key, AnimationType.Scale, duration, Easing.EaseOutQuad);
            handle.Play(AnimationDirection.Forward);
        }

        public static void BeginButtonRelease(string key, float duration = 0.12f)
        {
            var handle = GetOrCreate(key, AnimationType.Scale, duration, Easing.EaseOutBack);
            handle.Play(AnimationDirection.Reverse);
        }

        public static void BeginBadgeFadeIn(string key, float duration = 0.15f)
        {
            var handle = GetOrCreate(key, AnimationType.Fade, duration, Easing.EaseOutCubic);
            handle.Play(AnimationDirection.Forward);
        }

        public static void BeginBadgeFadeOut(string key, float duration = 0.12f)
        {
            var handle = GetOrCreate(key, AnimationType.Fade, duration, Easing.EaseOutCubic);
            handle.Play(AnimationDirection.Reverse);
        }

        public static void BeginChipPulse(string key, float duration = 0.15f)
        {
            var handle = GetOrCreate(key, AnimationType.Scale, duration, Easing.EaseOutQuad);
            handle.Play(AnimationDirection.Forward);
        }

        public static Rect ApplyWindowAnimation(Rect rect, string key)
        {
            if (!TryGet(key, out AnimationHandle handle)) return rect;

            float progress = handle.GetEasedProgress();
            float scale = Mathf.Lerp(0.95f, 1f, progress);
            float alpha = progress;

            Vector2 center = rect.center;
            float newWidth = rect.width * scale;
            float newHeight = rect.height * scale;
            return new Rect(center.x - newWidth * 0.5f, center.y - newHeight * 0.5f, newWidth, newHeight);
        }

        public static Color ApplyFade(Color color, string key)
        {
            if (!TryGet(key, out AnimationHandle handle) || handle.Type == AnimationType.Scale) return color;
            float alpha = handle.GetEasedProgress();
            return new Color(color.r, color.g, color.b, color.a * alpha);
        }

        public static Vector2 ApplyButtonScale(Vector2 size, string key)
        {
            if (!TryGet(key, out AnimationHandle handle) || handle.Type != AnimationType.Scale) return size;
            float scale = handle.GetEasedProgress();
            scale = Mathf.Lerp(0.96f, 1f, scale);
            return new Vector2(size.x * scale, size.y * scale);
        }

        public static float ApplyChipScale(string key, float baseScale = 1f)
        {
            if (!TryGet(key, out AnimationHandle handle) || handle.Type != AnimationType.Scale) return baseScale;
            float p = handle.GetEasedProgress();
            float scale = Mathf.Lerp(1f, 1.08f, p);
            return baseScale * scale;
        }

        public static bool IsWindowAnimating(string key)
        {
            return TryGet(key, out AnimationHandle handle) && handle.IsPlaying && (handle.Type == AnimationType.Fade || handle.Type == AnimationType.FadeAndScale);
        }

        public static bool IsWindowOpenComplete(string key)
        {
            return TryGet(key, out AnimationHandle handle) && handle.IsComplete && handle.Direction == AnimationDirection.Forward;
        }
    }
}