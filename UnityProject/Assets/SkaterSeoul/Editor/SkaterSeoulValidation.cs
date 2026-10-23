#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Skater.Core;
using Skater.Game;

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
            Assert(TrickCatalog.Count == 39 && (int)Trick.Ollie == 0 && (int)Trick.Kickflip == 3 &&
                (int)Trick.TreFlip == 24, "imported catalog preserves all 39 stable IDs and MVP mappings");
            ProgressData freshProgress = new ProgressData();
            Assert(freshProgress.version == 13 && freshProgress.mastery.Length == 39 && freshProgress.stats.Length == 6 &&
                freshProgress.career.Length == 12 && freshProgress.missions.Length == 3,
                "legacy progression schema preserves v13 mastery, stats, career and missions");
            SkateMatch match = new SkateMatch(BotPersona.Amateur, 5, true, 123,
                new[] { Trick.Ollie, Trick.Kickflip, Trick.TreFlip });
            Assert(match.TurnLimit == 15.0 && match.PlayerWord.Length == 5 && match.BotWord.Length == 5,
                "imported S.K.A.T.E. keeps five letters and 15-second turns");
            match.StartTurn(0.0);
            match.OnPlayerAttempt(new Verdict { Success = true, Trick = Trick.Kickflip }, 1.0);
            Assert(match.Phase == MatchPhase.BotMatch && match.RequiredTrick == Trick.Kickflip,
                "successful setter attempt advances to the matching player");
            RiderStats defaultStats = RiderStats.Default;
            double noviceChance = TrickSuccessModel.Calculate(95.0, 15, 10.0,
                TrickSuccessModel.Difficulty(Trick.TreFlip), 0.0, 0.0);
            double skilledChance = TrickSuccessModel.Calculate(95.0, 75, 10.0,
                TrickSuccessModel.Difficulty(Trick.TreFlip), 0.0, 0.0);
            Assert(skilledChance > noviceChance && skilledChance <= .995 && defaultStats.Pop == 10,
                "imported success model uses mastery, six stats and bounded probability");
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
            ValidateWheelRotation();
            Debug.Log("SKATER SEOUL: static rules checks PASS. Play Mode and device testing are separate checks.");
        }

        private static void ValidateWheelRotation()
        {
            const float radius = 0.04f;
            Assert(Mathf.Abs(SkaterSeoulBoardRig.RotationFromDistance(1f, radius) - 25f) < 0.0001f,
                "1m forward wheel rotation at 40mm radius");
            Assert(Mathf.Abs(SkaterSeoulBoardRig.RotationFromDistance(-1f, radius) + 25f) < 0.0001f,
                "reverse wheel rotation sign");
            Assert(SkaterSeoulBoardRig.RotationFromDistance(0f, radius) == 0f, "resting wheel does not spin");
            foreach (int fps in new[] { 30, 60, 120 })
            {
                float total = 0f;
                for (int frame = 0; frame < fps; frame++)
                    total += SkaterSeoulBoardRig.RotationFromDistance(1f / fps, radius);
                Assert(Mathf.Abs(total - 25f) < 0.001f, "wheel rotation is render-rate independent at " + fps + "fps");
            }
        }

        [MenuItem("Tools/SKATER SEOUL/5 - Validate Visual Slice")]
        public static void ValidateVisualSlice()
        {
            const string scenePath = "Assets/SkaterSeoul/Scenes/SkaterSeoul_VisualSlice.unity";
            Assert(File.Exists(scenePath), "visual slice scene exists");
            Assert(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SkaterSeoul/Art/GeneratedSeoul/seoul_han_riverside_panorama.png") != null,
                "original Seoul panorama imported");
            Assert(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SkaterSeoul/Art/GeneratedSeoul/warm_concrete_albedo.png") != null,
                "concrete material imported");
            Texture2D deckArtwork = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SkaterSeoul/Art/GeneratedSeoul/seoul_deck_artwork.png");
            Assert(deckArtwork != null, "original Hangang deck artwork imported");
            GameObject boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SkaterSeoul/Prefabs/BoardVisual.prefab");
            Assert(boardPrefab != null, "board prefab exists");
            Assert(boardPrefab.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("Wheel_", StringComparison.Ordinal)) == 4,
                "board prefab has exactly four separate wheels");
            Renderer[] bushings = boardPrefab.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.IndexOf("bushing", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            Assert(bushings.Length == 4 && bushings.All(r => r.sharedMaterial != null && r.sharedMaterial.name.Contains("Urethane Bushings")),
                "all four skateboard bushings use the explicit urethane material mapping");
            MeshRenderer deckGraphic = boardPrefab.GetComponentsInChildren<MeshRenderer>(true)
                .FirstOrDefault(r => r.name == "Concave maple deck with printed underside" && r.sharedMaterials.Any(m =>
                    m != null && m.mainTexture == deckArtwork && m.shader.name == "Universal Render Pipeline/Lit"));
            Assert(deckGraphic != null,
                "board underside uses a scene-lit print material with the original Hangang graphic");
            GameObject riderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SkaterSeoul/Prefabs/SkaterCharacterVisual.prefab");
            Assert(riderPrefab != null && riderPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null,
                "character prefab contains a skinned humanoid mesh");
            Animator animator = riderPrefab.GetComponentInChildren<Animator>(true);
            Assert(animator != null && animator.avatar != null && animator.avatar.isHuman,
                "character prefab uses a Humanoid Avatar");
            Assert(riderPrefab.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Rigged Seoul Skater · Blender Clothing Pass"),
                "rider prefab uses the licensed Quaternius hoodie character");
            MethodInfo formatLetters = typeof(SkaterSeoulHud).GetMethod("FormatLetters", BindingFlags.NonPublic | BindingFlags.Static);
            Assert(formatLetters != null && (string)formatLetters.Invoke(null, new object[] { "××ATE" }) ==
                "<color=#F47E72>×</color> <color=#F47E72>×</color> <color=#F2D26B>A</color> <color=#F2D26B>T</color> <color=#F2D26B>E</color>",
                "S.K.A.T.E. penalty marks lost slots while keeping remaining letters visible");

            Scene loaded = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                SkaterSeoulPrototype prototype = null;
                Camera camera = null;
                foreach (GameObject root in loaded.GetRootGameObjects())
                {
                    if (prototype == null) prototype = root.GetComponentInChildren<SkaterSeoulPrototype>(true);
                    if (camera == null) camera = root.GetComponentInChildren<Camera>(true);
                }
                Assert(prototype != null, "visual scene has gameplay controller");
                SkaterSeoulBoardRig rig = prototype.GetComponent<SkaterSeoulBoardRig>();
                Assert(rig != null && rig.wheels != null && rig.wheels.Length == 4,
                    "gameplay controller owns four wheel visuals");
                Assert(Mathf.Abs(rig.wheelRadius - 0.039f) < 0.001f, "wheel contact radius retains the calibrated 39 mm gameplay value");
                Assert(rig.contactShadow != null, "rider has a ground contact shadow");
                PhysicsMaterial rolling = prototype.GetComponent<BoxCollider>().sharedMaterial;
                Assert(rolling != null && rolling.staticFriction <= .01f && rolling.dynamicFriction <= .01f,
                    "skate envelope has rolling contact friction low enough to push and coast");
                Assert(prototype.GetComponent<SkaterSeoulRiderMotion>() != null, "rider animation bridge exists");
                Assert(prototype.GetComponent<SkaterSeoulHud>() != null, "runtime Canvas HUD is configured");
                Assert(camera != null && camera.GetComponent<SkaterSeoulCameraRig>() != null,
                    "side follow camera controller is configured");
                Assert(prototype.GetComponentInChildren<SkinnedMeshRenderer>(true) != null,
                    "runtime rider is not a cube placeholder");
                Transform hair = prototype.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name == "Short textured crop hairstyle");
                Renderer hairRenderer = hair != null ? hair.GetComponent<Renderer>() : null;
                Assert(hairRenderer != null && hairRenderer.bounds.size.x < .5f && hairRenderer.bounds.size.y < .5f,
                    "hairstyle remains human-scaled on the Humanoid head rig");
                Transform head = prototype.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Casual_Head");
                Renderer headRenderer = head != null ? head.GetComponent<Renderer>() : null;
                Assert(headRenderer != null, "rider prefab has the source humanoid head mesh");
                Bounds headBounds = headRenderer.bounds;
                Bounds hairBounds = hairRenderer.bounds;
                Assert(hairBounds.max.y >= headBounds.max.y - .05f && hairBounds.center.y <= headBounds.max.y + .10f,
                    "hairstyle remains visible at the top of the skater's head");
            }
            finally { EditorSceneManager.CloseScene(loaded, true); }
            Debug.Log("SKATER SEOUL: visual slice asset checks PASS.");
        }

        private static void Assert(bool condition, string label)
        {
            if (!condition) throw new Exception("SKATER SEOUL rule test FAILED: " + label);
        }
    }
}
#endif
