using System;
using UnityEngine;
using System.Collections.Generic;

namespace SkaterSeoul
{
    public enum SkateTrick { Ollie = 0, Kickflip = 3, TreFlip = 24 }
    public enum FailureType { None, UnderRotation, MissedCatch, LostBalance, WrongTrick }
    public enum SkateStance { Regular, Goofy }
    public enum TravelDirection { ForwardX, BackwardX }

    /// <summary>Own rules and input model; no third-party game code.</summary>
    public static class SkaterSeoulRules
    {
        public static int MasterySlot(SkateTrick trick)
        {
            switch (trick)
            {
                case SkateTrick.Ollie: return 0;
                case SkateTrick.Kickflip: return 1;
                case SkateTrick.TreFlip: return 2;
                default: throw new ArgumentOutOfRangeException("trick");
            }
        }

        public static SkateTrick TrickAtMasterySlot(int slot)
        {
            switch (slot)
            {
                case 0: return SkateTrick.Ollie;
                case 1: return SkateTrick.Kickflip;
                case 2: return SkateTrick.TreFlip;
                default: throw new ArgumentOutOfRangeException("slot");
            }
        }

        public static string Name(SkateTrick trick)
        {
            switch (trick)
            {
                case SkateTrick.Ollie: return "OLLIE";
                case SkateTrick.Kickflip: return "KICKFLIP";
                default: return "TRE FLIP";
            }
        }

        public static float Difficulty(SkateTrick trick)
        {
            switch (trick)
            {
                case SkateTrick.Ollie: return 15f;
                case SkateTrick.Kickflip: return 48f;
                default: return 65f;
            }
        }

        public static float InitialMastery(SkateTrick trick)
        {
            switch (trick)
            {
                case SkateTrick.Ollie: return 75f;
                case SkateTrick.Kickflip: return 45f;
                default: return 38f;
            }
        }

        public static float Probability(SkateTrick trick, float mastery, float gestureQuality)
        {
            if (gestureQuality < 0.27f) return 0f; // Never roll lucky success on a bad input.
            float z = -0.55f + 0.045f * (Mathf.Clamp(mastery, 0f, 100f) - Difficulty(trick))
                      + 2.5f * (Mathf.Clamp01(gestureQuality) - 0.7f);
            float p = 1f / (1f + Mathf.Exp(-z));
            return Mathf.Clamp(p, 0.01f, 0.985f);
        }

        public static bool ResolveProbabilisticLanding(float roll, float chance, out FailureType failure)
        {
            roll = Mathf.Clamp01(roll);
            chance = Mathf.Clamp01(chance);
            if (roll < chance || chance >= 1f)
            {
                failure = FailureType.None;
                return true;
            }

            float failureRoll = (roll - chance) / Mathf.Max(0.0001f, 1f - chance);
            failure = failureRoll < 0.55f ? FailureType.MissedCatch : FailureType.LostBalance;
            return false;
        }

        // V0 tutorial recognizer. NOT an assertion about Skate City's proprietary gesture mapping.
        // Normalize by screen HEIGHT so landscape/portrait widths have the same thresholds.
        public static bool RecognizeSwipe(Vector2 startPx, Vector2 endPx, float seconds,
            float screenHeight, out SkateTrick trick, out float quality)
        {
            return RecognizeSwipe(new Vector2[] { startPx, endPx }, seconds, screenHeight,
                out trick, out quality);
        }

        public static bool CanStartGesture(Vector2 position, Vector2 controlCenter, float controlRadius,
            bool beganOverHud)
        {
            return !beganOverHud && IsInsideControl(position, controlCenter, controlRadius);
        }

        public static bool IsInsideControl(Vector2 position, Vector2 controlCenter, float controlRadius)
        {
            return controlRadius > 0f && (position - controlCenter).sqrMagnitude <= controlRadius * controlRadius;
        }

        public static bool RecognizeTrickTap(Vector2 position, Vector2 controlCenter, float controlRadius,
            out SkateTrick trick)
        {
            trick = SkateTrick.Ollie;
            if (!IsInsideControl(position, controlCenter, controlRadius)) return false;
            Vector2 direction = (position - controlCenter) / controlRadius;
            if (direction.magnitude <= 0.38f || (direction.y >= 0.28f && Mathf.Abs(direction.x) < 0.2f))
                trick = SkateTrick.Ollie;
            else if (direction.y >= 0.28f && direction.x <= -0.2f)
                trick = SkateTrick.Kickflip;
            else if (direction.y >= 0.28f && direction.x >= 0.2f)
                trick = SkateTrick.TreFlip;
            else return false;
            return true;
        }

        public static bool RecognizeSwipe(IList<Vector2> samples, float seconds, float screenHeight,
            out SkateTrick trick, out float quality)
        {
            trick = SkateTrick.Ollie;
            quality = 0f;
            if (samples == null || samples.Count < 2) return false;
            float height = Mathf.Max(1f, screenHeight);
            Vector2 startPx = samples[0];
            Vector2 endPx = samples[samples.Count - 1];
            float dx = (endPx.x - startPx.x) / height;
            float dy = (endPx.y - startPx.y) / height;
            if (dy < 0.105f || seconds < 0.035f || seconds > 0.9f) return false;

            float pathLength = 0f;
            for (int i = 1; i < samples.Count; i++)
                pathLength += Vector2.Distance(samples[i - 1], samples[i]) / height;
            float displacement = new Vector2(dx, dy).magnitude;
            float straightness = displacement / Mathf.Max(0.001f, pathLength);
            if (straightness < 0.72f) return false;

            float diagonal = dx / Mathf.Max(dy, 0.001f);
            if (diagonal <= -0.40f) trick = SkateTrick.Kickflip;
            else if (diagonal >= 0.40f) trick = SkateTrick.TreFlip;
            float distanceScore = Mathf.InverseLerp(0.10f, 0.42f, displacement);
            float speedScore = 1f - Mathf.Clamp01(Mathf.Abs(seconds - 0.23f) / 0.63f);
            float targetSlope = trick == SkateTrick.Ollie ? 0f : (trick == SkateTrick.Kickflip ? -0.9f : 0.9f);
            float directionScore = 1f - Mathf.Clamp01(Mathf.Abs(diagonal - targetSlope) / 3.0f);
            quality = Mathf.Clamp01(0.10f + 0.40f * distanceScore + 0.25f * speedScore +
                0.15f * directionScore + 0.10f * straightness);
            return true;
        }

        public static string Letters(int mistakes)
        {
            const string skate = "SKATE";
            mistakes = Mathf.Clamp(mistakes, 0, 5);
            return skate.Substring(0, mistakes) + new string('_', 5 - mistakes);
        }
    }
}
