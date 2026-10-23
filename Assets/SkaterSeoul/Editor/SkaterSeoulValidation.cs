#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkaterSeoul.Editor
{
    public static class SkaterSeoulValidation
    {
        [MenuItem("Tools/SKATER SEOUL/2 - Validate Game Rules")]
        public static void Run()
        {
            Validate();
            EditorUtility.DisplayDialog("Rule checks pass", "Static recognition, probability and letter checks passed.\nThis does not verify Unity Play Mode or mobile input.", "OK");
        }

        public static void RunBatch()
        {
            Validate();
        }

        private static void Validate()
        {
            Assert((int)SkateTrick.Ollie == 0 && (int)SkateTrick.Kickflip == 3 &&
                (int)SkateTrick.TreFlip == 24, "legacy stable trick IDs");
            for (int t = 0; t < 3; t++)
            {
                SkateTrick trick = SkaterSeoulRules.TrickAtMasterySlot(t);
                Assert(SkaterSeoulRules.MasterySlot(trick) == t, "mastery slot mapping " + trick);
                float previous = -1f;
                for (int mastery = 0; mastery <= 100; mastery += 5)
                {
                    float chance = SkaterSeoulRules.Probability(trick, mastery, 0.8f);
                    Assert(chance >= previous, "mastery monotonicity " + trick + " at " + mastery);
                    previous = chance;
                }
                Assert(SkaterSeoulRules.Probability(trick, 70f, 0.1f) == 0f, "bad gesture gate");
                Assert(SkaterSeoulRules.Probability(trick, 70f, 0.269f) == 0f, "quality just below minimum");
                Assert(SkaterSeoulRules.Probability(trick, 70f, 0.27f) > 0f, "quality minimum is accepted");
            }
            SkateTrick recognized;
            float quality;
            Assert(SkaterSeoulRules.RecognizeSwipe(Vector2.zero, new Vector2(0f, 250f), .22f, 700f,
                out recognized, out quality) && recognized == SkateTrick.Ollie, "Ollie recognition");
            Assert(SkaterSeoulRules.RecognizeSwipe(Vector2.zero, new Vector2(-220f, 250f), .22f, 700f,
                out recognized, out quality) && recognized == SkateTrick.Kickflip, "Kickflip recognition");
            Assert(SkaterSeoulRules.RecognizeSwipe(Vector2.zero, new Vector2(220f, 250f), .22f, 700f,
                out recognized, out quality) && recognized == SkateTrick.TreFlip, "TreFlip recognition");
            Vector2 trickPadCenter = new Vector2(160f, 140f);
            Vector2 pushButtonCenter = new Vector2(840f, 140f);
            Assert(SkaterSeoulRules.CanStartGesture(trickPadCenter, trickPadCenter, 100f, false), "left trick pad accepts touch");
            Assert(SkaterSeoulRules.IsInsideControl(pushButtonCenter, pushButtonCenter, 100f), "right push button accepts touch");
            Assert(SkaterSeoulRules.RecognizeTrickTap(trickPadCenter, trickPadCenter, 100f, out recognized) &&
                recognized == SkateTrick.Ollie, "center pad tap selects Ollie");
            Assert(SkaterSeoulRules.RecognizeTrickTap(trickPadCenter + new Vector2(-60f, 60f), trickPadCenter, 100f, out recognized) &&
                recognized == SkateTrick.Kickflip, "upper-left pad tap selects Kickflip");
            Assert(SkaterSeoulRules.RecognizeTrickTap(trickPadCenter + new Vector2(60f, 60f), trickPadCenter, 100f, out recognized) &&
                recognized == SkateTrick.TreFlip, "upper-right pad tap selects Tre Flip");
            Assert(!SkaterSeoulRules.RecognizeTrickTap(trickPadCenter + Vector2.down * 60f, trickPadCenter, 100f, out recognized),
                "lower pad tap is ignored");
            Assert(!SkaterSeoulRules.CanStartGesture(trickPadCenter, trickPadCenter, 100f, true), "HUD touch cannot start a trick");
            Assert(!SkaterSeoulRules.CanStartGesture(new Vector2(600f, 300f), trickPadCenter, 100f, false), "screen center is outside trick pad");
            Assert(!SkaterSeoulRules.IsInsideControl(new Vector2(1001f, 140f), pushButtonCenter, 100f), "touch outside push button rejected");
            Assert(!SkaterSeoulRules.CanStartGesture(trickPadCenter, trickPadCenter, 0f, false), "zero-size trick pad rejected");
            for (int aspect = 0; aspect < 3; aspect++)
            {
                float height = 800f;
                float width = height * (aspect == 0 ? 16f / 9f : (aspect == 1 ? 19.5f / 9f : 20f / 9f));
                for (int t = 0; t < 3; t++)
                {
                    SkateTrick expected = SkaterSeoulRules.TrickAtMasterySlot(t);
                    float dx = expected == SkateTrick.Ollie ? 0f : (expected == SkateTrick.Kickflip ? -0.30f : 0.30f);
                    List<Vector2> path = new List<Vector2>(20);
                    for (int sample = 0; sample < 20; sample++)
                    {
                        float progress = sample / 19f;
                        path.Add(new Vector2(width * .60f + dx * height * progress,
                            height * .20f + height * .36f * progress + Mathf.Sin(sample * 2.1f) * 0.8f));
                    }
                    Assert(SkaterSeoulRules.RecognizeSwipe(path, .22f, height, out recognized, out quality) &&
                        recognized == expected && quality >= 0.27f,
                        "20-sample gesture replay " + expected + " at " + width / height + ":1");
                }
            }
            List<Vector2> curved = new List<Vector2> { Vector2.zero, new Vector2(240f, 230f),
                new Vector2(-180f, 250f), new Vector2(0f, 250f) };
            Assert(!SkaterSeoulRules.RecognizeSwipe(curved, .22f, 700f, out recognized, out quality),
                "curved ambiguous gesture rejected");
            Assert(!SkaterSeoulRules.RecognizeSwipe(Vector2.zero, new Vector2(0f, 250f), .02f, 700f,
                out recognized, out quality), "gesture too short rejected");
            Assert(SkaterSeoulRules.Letters(0) == "_____", "zero letters");
            Assert(SkaterSeoulRules.Letters(5) == "SKATE", "end letters");
            Assert(SkaterSeoulRules.Letters(-1) == "_____", "negative letters clamp");
            Assert(SkaterSeoulRules.Letters(9) == "SKATE", "excess letters clamp");
            Assert(SkaterSeoulRules.ResolveProbabilisticLanding(0.2f, 0.3f, out FailureType result) &&
                result == FailureType.None, "probabilistic landing success");
            Assert(!SkaterSeoulRules.ResolveProbabilisticLanding(0.65f, 0.3f, out result) &&
                result == FailureType.MissedCatch, "single roll maps failed landing to missed catch");
            Assert(!SkaterSeoulRules.ResolveProbabilisticLanding(0.9f, 0.3f, out result) &&
                result == FailureType.LostBalance, "single roll maps failed landing to balance loss");
            Assert(SkaterSeoulRules.ResolveProbabilisticLanding(0.99f, 1f, out result) &&
                result == FailureType.None, "certain landing boundary");
            Debug.Log("SKATER SEOUL: static rules checks PASS. Play Mode and device testing are separate checks.");
        }

        private static void Assert(bool condition, string label)
        {
            if (!condition) throw new Exception("SKATER SEOUL rule test FAILED: " + label);
        }
    }
}
#endif
