#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SkaterSeoul
{
    public sealed class SkaterSeoulPlayModeProbe : MonoBehaviour
    {
        private const string PrefSnapshotKey = "SkaterSeoul.ValidationPlayerPrefsSnapshot";
        private const string PrefSnapshotReadyKey = "SkaterSeoul.ValidationPlayerPrefsSnapshotReady";

        [System.Serializable]
        private sealed class PlayerPrefValue
        {
            public string key;
            public int kind;
            public bool existed;
            public string stringValue;
            public float floatValue;
            public int intValue;
        }

        [System.Serializable]
        private sealed class PlayerPrefsSnapshot
        {
            public PlayerPrefValue[] values;
        }

        private readonly List<string> results = new List<string>();
        private StringBuilder telemetry;
        private string captureDirectory;
        private bool finished;

        public void StartCapture() { if (!finished) StartCoroutine(Run()); }

        public static void CapturePlayerPrefsSnapshot()
        {
            string[] keys = { "skater_progress_v3", "skater_progress_v2", "skater_progress_v1",
                "SS_Mastery_0", "SS_Mastery_1", "SS_Mastery_2", "SS_LegacyProgressImported_v1" };
            PlayerPrefsSnapshot snapshot = new PlayerPrefsSnapshot { values = new PlayerPrefValue[keys.Length] };
            for (int i = 0; i < keys.Length; i++)
            {
                PlayerPrefValue value = new PlayerPrefValue { key = keys[i], existed = PlayerPrefs.HasKey(keys[i]) };
                value.kind = i >= 3 && i <= 5 ? 1 : (i == 6 ? 2 : 0);
                if (value.existed)
                {
                    if (value.kind == 1) value.floatValue = PlayerPrefs.GetFloat(value.key);
                    else if (value.kind == 2) value.intValue = PlayerPrefs.GetInt(value.key);
                    else value.stringValue = PlayerPrefs.GetString(value.key);
                }
                snapshot.values[i] = value;
            }
            SessionState.SetString(PrefSnapshotKey, JsonUtility.ToJson(snapshot));
            SessionState.SetBool(PrefSnapshotReadyKey, true);
        }

        public static void RestorePlayerPrefsAfterRun()
        {
            if (!SessionState.GetBool(PrefSnapshotReadyKey, false)) return;
            PlayerPrefsSnapshot snapshot = JsonUtility.FromJson<PlayerPrefsSnapshot>(SessionState.GetString(PrefSnapshotKey, ""));
            if (snapshot != null && snapshot.values != null)
            {
                foreach (PlayerPrefValue value in snapshot.values)
                {
                    if (!value.existed) { PlayerPrefs.DeleteKey(value.key); continue; }
                    if (value.kind == 1) PlayerPrefs.SetFloat(value.key, value.floatValue);
                    else if (value.kind == 2) PlayerPrefs.SetInt(value.key, value.intValue);
                    else PlayerPrefs.SetString(value.key, value.stringValue ?? "");
                }
                PlayerPrefs.Save();
            }
            SessionState.SetBool(PrefSnapshotReadyKey, false);
            SessionState.EraseString(PrefSnapshotKey);
        }

        private IEnumerator Run()
        {
            SkaterSeoulPrototype game = FindFirstObjectByType<SkaterSeoulPrototype>();
            Rigidbody body = game != null ? game.GetComponent<Rigidbody>() : null;
            SkaterSeoulBoardRig rig = game != null ? game.GetComponent<SkaterSeoulBoardRig>() : null;
            if (game == null || body == null || rig == null)
            {
                Record(false, "visual slice controller, Rigidbody and board rig are present");
                Finish();
                yield break;
            }

            captureDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/visual_overhaul/captures"));
            Directory.CreateDirectory(captureDirectory);
            telemetry = new StringBuilder("time,x_m,speed_mps,wheel_angle_rad,signed_step_m,probes,max_gap_m,grounded\n");
            Random.InitState(20261023);
            float settleDeadline = Time.realtimeSinceStartup + 2f;
            while ((!rig.supported || rig.contactProbeCount != 4 || rig.maximumContactGap > .01f) &&
                Time.realtimeSinceStartup < settleDeadline)
                yield return new WaitForSecondsRealtime(.05f);
            Record(game.GroundedForValidation && rig.supported && rig.contactProbeCount == 4 && rig.maximumContactGap <= .01f,
                "four wheel probes see the plaza before a trick; gap=" + rig.maximumContactGap.ToString("F4") + "m");
            Record(game.ProgressSchemaForValidation, "runtime progress uses legacy v13 schema with all 39 stable trick slots");
            Capture("01_runtime_ride");
            CaptureWithoutHud("environment/hud_off");
            CaptureBoardView("board/04_board_3q", false);
            CaptureBoardView("board/05_board_underside", true);
            foreach (string name in new[] { "board/04_board_3q", "board/05_board_underside" })
            {
                string path = Path.Combine(captureDirectory, name + ".png");
                Record(File.Exists(path) && new FileInfo(path).Length > 0,
                    name + " material diagnostic was captured at 1280x720");
            }

            Collider ledge = GameObject.Find("Low ledge · skateable top")?.GetComponent<Collider>();
            bool ledgeWasEnabled = ledge != null && ledge.enabled;
            if (ledge != null) ledge.enabled = false;
            rig.ResetTelemetry();
            int startPhysicsStep = rig.physicsStepIndex;
            float startX = rig.lastIntegratedPosition.x;
            float startAngle = rig.rotationRadians;
            body.linearVelocity = new Vector3(5f, 0f, 0f);
            float previousTimeScale = Time.timeScale;
            Time.timeScale = .1f;
            float pushCaptureStart = Time.time;
            game.BeginPushForValidation();
            yield return CapturePushAt("motion/push_approach", pushCaptureStart, .215f, false);
            yield return CapturePushAt("motion/push_initial_contact", pushCaptureStart, .240f, false);
            yield return CapturePushAt("02_push_contact", pushCaptureStart, .260f, true);
            CaptureWithoutHud("motion/push_full_contact");
            yield return CapturePushAt("motion/push_drive", pushCaptureStart, .280f, false);
            yield return CapturePushAt("motion/push_toe_off", pushCaptureStart, .300f, false);
            yield return CapturePushAt("motion/push_recovery", pushCaptureStart, .420f, false);
            yield return CapturePushAt("motion/push_complete", pushCaptureStart, .600f, false);
            Time.timeScale = previousTimeScale;
            yield return SampleFor(1.35f, game, body, rig);
            game.EndPushForValidation();
            float releasedSpeed = game.SpeedForValidation;
            yield return SampleFor(8f, game, body, rig);
            yield return new WaitForFixedUpdate();
            float coastingSpeed = game.SpeedForValidation;
            float travelled = Mathf.Abs(body.position.x - startX);
            float sameStepTravel = Mathf.Abs(rig.lastIntegratedPosition.x - startX);
            float wheelRotation = rig.rotationRadians - startAngle;
            Record(travelled > 10f && coastingSpeed > .2f, "10 second push and coast; distance=" + travelled.ToString("F2") +
                "m, release=" + releasedSpeed.ToString("F2") + "m/s, coast end=" + coastingSpeed.ToString("F2") + "m/s");
            float wheelArcTravel = Mathf.Abs(wheelRotation * rig.wheelRadius);
            float wheelTravelMismatch = Mathf.Abs(wheelArcTravel - sameStepTravel);
            Record(wheelTravelMismatch < .05f,
                "wheel arc matches Rigidbody delta at physics steps " + startPhysicsStep + "→" + rig.physicsStepIndex +
                "; wheelArc=" + wheelArcTravel.ToString("F2") + "m, sameSampleBodyDelta=" + sameStepTravel.ToString("F2") +
                "m, liveBodyDelta=" + travelled.ToString("F2") + "m, mismatch=" + wheelTravelMismatch.ToString("F3") + "m");
            Record(rig.contactProbeCount == 4 && rig.maximumContactGap <= .01f,
                "ground support gap <= 0.01m; probes=" + rig.contactProbeCount + ", max gap=" + rig.maximumContactGap.ToString("F4") + "m");
            Capture("03_coast_after_10s");
            if (ledge != null) ledge.enabled = ledgeWasEnabled;

            game.ToggleMode();
            SkateTrick[] tricks = { SkateTrick.Ollie, SkateTrick.Kickflip, SkateTrick.TreFlip };
            FailureType[] failures = { FailureType.LostBalance, FailureType.MissedCatch, FailureType.UnderRotation };
            for (int i = 0; i < tricks.Length; i++)
            {
                SkateTrick trick = tricks[i];
                game.SetMasteryForValidation(trick, 100f);
                body.position = new Vector3(-5f + i * 3f, .174f, 0f);
                body.rotation = Quaternion.identity;
                body.linearVelocity = new Vector3(.6f, 0f, 0f);
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
                rig.ResetTelemetry();
                yield return new WaitForSecondsRealtime(.28f);
                Record(game.GroundedForValidation, SkaterSeoulRules.Name(trick) + " can begin from a settled support surface");

                float chance = game.SuccessChanceForValidation(trick, .99f);
                game.ForceNextRollForValidation(.01f);
                int attemptsBefore = game.AttemptCountForValidation;
                int drawsBefore = game.ProbabilityDrawCountForValidation;
                int progressAttemptsBefore = game.ProgressAttemptsForValidation(trick);
                float previousTrickTimeScale = Time.timeScale;
                Time.timeScale = .1f;
                float captureStart = Time.time;
                game.BeginTrickForValidation(trick, .99f);
                yield return CaptureAt("_success_crouch", captureStart, .09f);
                yield return CaptureAt("_success_pop", captureStart, .20f);
                yield return CaptureAt("_success_flick", captureStart, .16f + game.FlightDurationForValidation * .29f);
                if (trick != SkateTrick.Ollie) CaptureThreeQuarter("motion/" + SkaterSeoulRules.Name(trick).ToLowerInvariant() + "_success_flick_three_quarter");
                if (trick == SkateTrick.Kickflip)
                {
                    yield return CaptureAt("_success_flick_out", captureStart, .16f + game.FlightDurationForValidation * .35f);
                    CaptureThreeQuarter("motion/kickflip_success_flick_out_three_quarter");
                }
                if (trick == SkateTrick.TreFlip)
                {
                    yield return CaptureAt("_success_yaw_090", captureStart, .16f + game.FlightDurationForValidation * .30f);
                    CaptureThreeQuarter("motion/treflip_success_yaw_090_three_quarter");
                    yield return CaptureAt("_success_yaw_180", captureStart, .16f + game.FlightDurationForValidation * .40f);
                    CaptureThreeQuarter("motion/treflip_success_yaw_180_three_quarter");
                    yield return CaptureAt("_success_yaw_270", captureStart, .16f + game.FlightDurationForValidation * .50f);
                    CaptureThreeQuarter("motion/treflip_success_yaw_270_three_quarter");
                    yield return CaptureAt("_success_yaw_360", captureStart, .16f + game.FlightDurationForValidation * .68f);
                    CaptureThreeQuarter("motion/treflip_success_yaw_360_three_quarter");
                }
                else
                {
                    if (trick == SkateTrick.Kickflip)
                    {
                        yield return CaptureAt("_success_180", captureStart, .16f + game.FlightDurationForValidation * .40f);
                        yield return CaptureAt("_success_air_tuck", captureStart, .16f + game.FlightDurationForValidation * .55f);
                    }
                    else
                    {
                        yield return CaptureAt("_success_midflight", captureStart, .16f + game.FlightDurationForValidation * .50f);
                        yield return CaptureAt("_success_descent", captureStart, .16f + game.FlightDurationForValidation * .68f);
                    }
                }
                if (trick == SkateTrick.Kickflip)
                {
                    yield return CaptureAt("_success_precatch", captureStart, .16f + game.FlightDurationForValidation * .75f);
                    yield return CaptureAt("_success_catch_approach", captureStart, .16f + game.FlightDurationForValidation * .82f);
                    yield return CaptureAt("_success_catch_lock", captureStart, .16f + game.FlightDurationForValidation * .87f);
                    yield return CaptureAt("_success_prelanding", captureStart, .16f + game.FlightDurationForValidation * .90f);
                    CaptureThreeQuarter("motion/kickflip_success_catch_three_quarter");
                }
                float catchPhase = trick == SkateTrick.TreFlip ? .87f : .82f;
                yield return CaptureAt("_success_catch", captureStart, .16f + game.FlightDurationForValidation * catchPhase);
                if (trick == SkateTrick.TreFlip) CaptureThreeQuarter("motion/treflip_success_catch_three_quarter");
                Time.timeScale = previousTrickTimeScale;
                yield return WaitForAttempt(game, attemptsBefore);
                string trickName = SkaterSeoulRules.Name(trick).ToLowerInvariant();
                CaptureWithoutHud("motion/" + trickName + "_success_land_contact");
                yield return new WaitForSecondsRealtime(.12f);
                CaptureWithoutHud("motion/" + trickName + "_success_land_recovery");
                int attemptsAfter = game.AttemptCountForValidation;
                int drawsAfter = game.ProbabilityDrawCountForValidation;
                int progressAttemptsAfter = game.ProgressAttemptsForValidation(trick);
                Record(attemptsAfter == attemptsBefore + 1 && drawsAfter == drawsBefore + 1,
                    SkaterSeoulRules.Name(trick) + " resolves one attempt with one probability draw; attempts=" +
                    attemptsBefore + "→" + attemptsAfter + ", draws=" + drawsBefore + "→" + drawsAfter);
                Record(progressAttemptsAfter == progressAttemptsBefore + 1,
                    SkaterSeoulRules.Name(trick) + " records once in legacy growth save; attempts=" +
                    progressAttemptsBefore + "→" + progressAttemptsAfter);
                Record(game.LastFailureForValidation == FailureType.None,
                    SkaterSeoulRules.Name(trick) + " controlled valid landing");
                yield return new WaitForSecondsRealtime(.72f);

                body.position = new Vector3(-5f + i * 3f, .174f, 0f);
                body.rotation = Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
                yield return new WaitForSecondsRealtime(.25f);
                float rollChance = game.SuccessChanceForValidation(trick, .99f);
                if (failures[i] != FailureType.UnderRotation)
                    game.ForceNextRollForValidation(FailureRoll(rollChance, failures[i]));
                int mechanicalDraws = game.ProbabilityDrawCountForValidation;
                int failedAttemptBefore = game.AttemptCountForValidation;
                game.BeginTrickForValidation(trick, failures[i] == FailureType.UnderRotation ? .1f : .99f);
                yield return WaitForAttempt(game, failedAttemptBefore);
                CaptureWithoutHud("motion/" + SkaterSeoulRules.Name(trick).ToLowerInvariant() + "_bail_" + failures[i].ToString().ToLowerInvariant());
                Record(game.LastFailureForValidation == failures[i], SkaterSeoulRules.Name(trick) + " distinct bail: expected=" +
                    failures[i] + ", actual=" + game.LastFailureForValidation + ", roll=" + game.LastRollForValidation.ToString("F4") +
                    ", chance=" + rollChance.ToString("F4"));
                Record(game.ProbabilityDrawCountForValidation == mechanicalDraws + (failures[i] == FailureType.UnderRotation ? 0 : 1),
                    SkaterSeoulRules.Name(trick) + " mechanical failure draw policy");
                yield return new WaitForSecondsRealtime(.95f);
            }

            if (ledge != null) ledge.enabled = true;
            body.position = new Vector3(-1.3f, .174f, 0f);
            body.rotation = Quaternion.identity;
            body.linearVelocity = new Vector3(5.4f, 0f, 0f);
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            yield return new WaitForSecondsRealtime(.30f);
            game.SetCurrentTrickForValidation(SkateTrick.Ollie);
            Capture("07_obstacle_approach");
            int obstacleAttemptBefore = game.AttemptCountForValidation;
            game.SetMasteryForValidation(SkateTrick.Ollie, 100f);
            game.ForceNextRollForValidation(.01f);
            game.BeginTrickForValidation(SkateTrick.Ollie, .99f);
            yield return new WaitForSecondsRealtime(.22f);
            Capture("08_obstacle_ollie_airborne");
            yield return WaitForAttempt(game, obstacleAttemptBefore);
            Capture("09_obstacle_landing");
            Record(ledge != null && ledge.enabled && game.LastFailureForValidation == FailureType.None &&
                body.position.x > 4.35f,
                "OLLIE clears the collider-backed ledge and lands beyond it; endX=" + body.position.x.ToString("F2") +
                ", failure=" + game.LastFailureForValidation);
            yield return new WaitForSecondsRealtime(.35f);

            game.ToggleMode();
            game.ResetBattleForValidation();
            Record(game.MatchPhaseForValidation == "PlayerSet", "legacy S.K.A.T.E. match starts on the player setter phase");
            Capture("05_battle_player_set");
            game.SetMasteryForValidation(SkateTrick.Ollie, 100f);
            float battleChance = game.SuccessChanceForValidation(SkateTrick.Ollie, .99f);
            game.ForceNextRollForValidation(.01f);
            body.position = new Vector3(-2f, .174f, 0f);
            body.rotation = Quaternion.identity;
            body.linearVelocity = new Vector3(.6f, 0f, 0f);
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            yield return new WaitForSecondsRealtime(.30f);
            int battleAttemptBefore = game.AttemptCountForValidation;
            game.BeginTrickForValidation(SkateTrick.Ollie, .99f);
            yield return WaitForAttempt(game, battleAttemptBefore);
            yield return new WaitForSecondsRealtime(.05f);
            Record(game.MatchPhaseForValidation == "BotMatch", "successful player set advances to the legacy bot match phase");
            Capture("06_battle_bot_match");
            SkaterSeoulHud hud = game.GetComponent<SkaterSeoulHud>();
            game.enabled = false;
            hud.SetState(false, "××ATE", "×KATE", "YOUR TURN", "MATCH OLLIE", "LETTER +S",
                "OLLIE", 100, false, false, Vector2.zero, false);
            Capture("battle/07_penalty_ui_preview");
            game.enabled = true;
            yield return new WaitForSecondsRealtime(.1f);

            File.WriteAllText(Path.Combine(captureDirectory, "runtime_telemetry.csv"), telemetry.ToString());
            File.WriteAllLines(Path.Combine(captureDirectory, "playmode_results.txt"), results);
            Capture("04_runtime_after_tricks");
            string rideCapture = Path.Combine(captureDirectory, "01_runtime_ride.png");
            Record(File.Exists(rideCapture) && new FileInfo(rideCapture).Length > 0,
                "1280x720 CLI camera captures were written; ride frame=" + new FileInfo(rideCapture).Length + " bytes");
            File.WriteAllLines(Path.Combine(captureDirectory, "playmode_results.txt"), results);
            RestorePlayerPrefsAfterRun();
            Finish();
        }

        private IEnumerator SampleFor(float seconds, SkaterSeoulPrototype game, Rigidbody body, SkaterSeoulBoardRig rig)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                telemetry.Append(Time.unscaledTime.ToString("F3")).Append(',')
                    .Append(body.position.x.ToString("F4")).Append(',')
                    .Append(game.SpeedForValidation.ToString("F4")).Append(',')
                    .Append(rig.rotationRadians.ToString("F4")).Append(',')
                    .Append(rig.signedDistance.ToString("F5")).Append(',')
                    .Append(rig.contactProbeCount).Append(',')
                    .Append(rig.maximumContactGap.ToString("F5")).Append(',')
                    .Append(game.GroundedForValidation ? "1" : "0").Append('\n');
                yield return new WaitForSecondsRealtime(.1f);
            }
        }

        private IEnumerator WaitForAttempt(SkaterSeoulPrototype game, int attemptsBefore)
        {
            float deadline = Time.realtimeSinceStartup + 2.5f;
            while (game.AttemptCountForValidation == attemptsBefore && Time.realtimeSinceStartup < deadline)
                yield return new WaitForSecondsRealtime(.05f);
        }

        private IEnumerator CaptureAt(string suffix, float captureStart, float seconds)
        {
            float targetTime = captureStart + seconds;
            while (Time.time < targetTime) yield return null;
            SkaterSeoulPrototype game = FindFirstObjectByType<SkaterSeoulPrototype>();
            Debug.Log("SKATER TRICK CAPTURE TIME | " + suffix + " | target=" + targetTime.ToString("F3") +
                " time=" + Time.time.ToString("F3") + " realtime=" + Time.realtimeSinceStartup.ToString("F3"));
            if (game != null)
                Debug.Log("SKATER SEOUL trick phase " + suffix + ": flip=" + game.FlipDegreesForValidation.ToString("F0") +
                    " yaw=" + game.YawDegreesForValidation.ToString("F0"));
            CaptureFrozen("motion/" + SkaterSeoulRules.Name(GetCurrentTrick()).ToLowerInvariant() + suffix, false);
        }

        private IEnumerator CapturePushAt(string filename, float captureStart, float seconds, bool withHud)
        {
            float targetTime = captureStart + seconds;
            while (Time.time < targetTime) yield return null;
            Debug.Log("SKATER PUSH CAPTURE TIME | " + filename + " | targetOffset=" + seconds.ToString("F3") +
                " actualOffset=" + (Time.time - captureStart).ToString("F3") + " time=" + Time.time.ToString("F3"));
            CaptureFrozen(filename, withHud);
        }

        private void CaptureFrozen(string filename, bool withHud)
        {
            float oldTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                if (withHud) Capture(filename);
                else CaptureWithoutHud(filename);
            }
            finally { Time.timeScale = oldTimeScale; }
        }

        private SkateTrick GetCurrentTrick()
        {
            SkaterSeoulPrototype game = FindFirstObjectByType<SkaterSeoulPrototype>();
            return game == null ? SkateTrick.Ollie : game.CurrentTrickForValidation;
        }

        private void Capture(string filename)
        {
            FindFirstObjectByType<SkaterSeoulRiderMotion>()?.LogFootDiagnostics(filename);
            string path = Path.Combine(captureDirectory, filename + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            const int width = 1280;
            const int height = 720;
            Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null) throw new System.InvalidOperationException("The play-mode capture has no camera.");
            Canvas canvas = FindFirstObjectByType<Canvas>();
            RenderMode oldMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            Camera oldCanvasCamera = canvas != null ? canvas.worldCamera : null;
            float oldPlaneDistance = canvas != null ? canvas.planeDistance : 0f;
            CanvasScaler scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            Vector2 oldReferenceResolution = scaler != null ? scaler.referenceResolution : Vector2.zero;
            RectTransform safeArea = canvas != null ? canvas.transform.Find("Safe Area") as RectTransform : null;
            Vector2 oldSafeMin = safeArea != null ? safeArea.anchorMin : Vector2.zero;
            Vector2 oldSafeMax = safeArea != null ? safeArea.anchorMax : Vector2.one;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture oldTarget = camera.targetTexture;
            RenderTexture oldActive = RenderTexture.active;
            float oldAspect = camera.aspect;
            Texture2D image = null;
            target.Create();
            try
            {
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1f;
                    if (scaler != null) scaler.referenceResolution = new Vector2(width, height);
                    if (safeArea != null) { safeArea.anchorMin = Vector2.zero; safeArea.anchorMax = Vector2.one; }
                }
                camera.targetTexture = target;
                camera.aspect = width / (float)height;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,
                    new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                if (image.width != width || image.height != height)
                    throw new System.InvalidOperationException("The play-mode capture has the wrong dimensions.");
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                if (image != null) DestroyImmediate(image);
                camera.targetTexture = oldTarget;
                camera.aspect = oldAspect;
                RenderTexture.active = oldActive;
                if (canvas != null)
                {
                    canvas.renderMode = oldMode;
                    canvas.worldCamera = oldCanvasCamera;
                    canvas.planeDistance = oldPlaneDistance;
                }
                if (scaler != null) scaler.referenceResolution = oldReferenceResolution;
                if (safeArea != null) { safeArea.anchorMin = oldSafeMin; safeArea.anchorMax = oldSafeMax; }
                target.Release();
                DestroyImmediate(target);
            }
            Debug.Log("SKATER SEOUL CLI capture: " + path + " (" + width + "x" + height + ")");
        }

        private void CaptureWithoutHud(string filename)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) { Capture(filename); return; }
            bool wasEnabled = canvas.enabled;
            canvas.enabled = false;
            try { Capture(filename); }
            finally { canvas.enabled = wasEnabled; }
        }

        private void CaptureThreeQuarter(string filename)
        {
            Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            SkaterSeoulCameraRig follow = camera != null ? camera.GetComponent<SkaterSeoulCameraRig>() : null;
            if (camera == null) throw new System.InvalidOperationException("The play-mode capture has no camera.");
            Vector3 oldPosition = camera.transform.position;
            Quaternion oldRotation = camera.transform.rotation;
            bool followEnabled = follow != null && follow.enabled;
            Transform rider = GameObject.Find("Skater Physics Root")?.transform;
            if (follow != null) follow.enabled = false;
            Vector3 focus = (rider != null ? rider.position : Vector3.zero) + Vector3.up * 1.0f;
            camera.transform.position = focus + new Vector3(-7f, 2f, -18f);
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);
            try { CaptureFrozen(filename, false); }
            finally
            {
                camera.transform.position = oldPosition;
                camera.transform.rotation = oldRotation;
                if (follow != null) follow.enabled = followEnabled;
            }
        }

        private void CaptureBoardView(string filename, bool underside)
        {
            SkaterSeoulPrototype game = FindFirstObjectByType<SkaterSeoulPrototype>();
            Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            SkaterSeoulCameraRig follow = camera != null ? camera.GetComponent<SkaterSeoulCameraRig>() : null;
            Transform board = game != null ? game.boardPivot : null;
            if (camera == null || board == null) throw new System.InvalidOperationException("Board diagnostic capture needs a camera and board pivot.");
            List<Renderer> hiddenRenderers = new List<Renderer>();
            Renderer[] sceneRenderers = underside ? FindObjectsByType<Renderer>(FindObjectsSortMode.None) :
                (game.riderVisual != null ? game.riderVisual.GetComponentsInChildren<Renderer>(true) : new Renderer[0]);
            foreach (Renderer renderer in sceneRenderers)
                if (!underside || (renderer.transform != board && !renderer.transform.IsChildOf(board))) hiddenRenderers.Add(renderer);
            bool[] renderersWereEnabled = new bool[hiddenRenderers.Count];
            for (int i = 0; i < hiddenRenderers.Count; i++)
            {
                renderersWereEnabled[i] = hiddenRenderers[i].enabled;
                hiddenRenderers[i].enabled = false;
            }
            Vector3 oldPosition = camera.transform.position;
            Quaternion oldRotation = camera.transform.rotation;
            bool oldOrthographic = camera.orthographic;
            float oldOrthographicSize = camera.orthographicSize;
            bool followEnabled = follow != null && follow.enabled;
            if (follow != null) follow.enabled = false;
            Vector3 focus = board.position;
            camera.orthographic = true;
            camera.orthographicSize = .42f;
            camera.transform.position = underside ? focus - board.up * 1.2f + board.forward * .08f :
                focus + new Vector3(-1.1f, .58f, -1.25f);
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, underside ? board.right : Vector3.up);
            try { CaptureFrozen(filename, false); }
            finally
            {
                camera.transform.position = oldPosition;
                camera.transform.rotation = oldRotation;
                camera.orthographic = oldOrthographic;
                camera.orthographicSize = oldOrthographicSize;
                if (follow != null) follow.enabled = followEnabled;
                for (int i = 0; i < hiddenRenderers.Count; i++) hiddenRenderers[i].enabled = renderersWereEnabled[i];
            }
        }

        private static float FailureRoll(float chance, FailureType failure)
        {
            float conditionalFailure = failure == FailureType.MissedCatch ? .25f : .75f;
            return Mathf.Min(.999f, chance + (1f - chance) * conditionalFailure);
        }

        private void Record(bool passed, string description)
        {
            string line = (passed ? "PASS  " : "FAIL  ") + description;
            results.Add(line);
            if (passed) Debug.Log("SKATER SEOUL QA " + line);
            else Debug.LogError("SKATER SEOUL QA " + line);
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            Debug.Log("SKATER SEOUL PlayMode probe complete. Results: " + captureDirectory);
            if (Application.isBatchMode) EditorApplication.Exit(0);
            else EditorApplication.isPlaying = false;
        }
    }
}
#endif
