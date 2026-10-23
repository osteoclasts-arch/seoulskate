using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Skater.Core;
using Skater.Game;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace SkaterSeoul
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SkaterSeoulPrototype : MonoBehaviour
    {
        public Transform boardPivot;
        public Transform riderVisual;
        public Camera followCamera;
        public SkateStance stance = SkateStance.Regular;
        public TravelDirection travelDirection = TravelDirection.ForwardX;
        [Header("Tuning")]
        public float cruiseSpeed = 5.4f;
        public float jumpVelocity = 4.8f;
        public float respawnAtX = 175f;
        public float displayQuality = 0.86f;

        private enum MotionState { Rolling, Crouching, Airborne, Bailing }
        private enum HudAction { None, Pause, Resume, Reset, ToggleMode }
        private MotionState state;
        private Rigidbody body;
        private SkaterSeoulBoardRig boardRig;
        private SkaterSeoulRiderMotion riderMotion;
        private SkaterSeoulHud hud;
        private float lastGroundContact;
        private float trickStarted;
        private float bailStarted;
        private float landingStarted;
        private float flightDuration;
        private float boardFlipDegrees;
        private float boardYawDegrees;
        private Vector3 bailBoardPosition;
        private Progress progress;
        private SkateMatch match;
        private readonly Tuning matchTuning = new Tuning();
        private double nextBotTurnAt;
        private bool matchResultRecorded;
        private int nextAttemptId;
#if UNITY_EDITOR
        private int validationAttemptCount;
        private int validationProbabilityDrawCount;
        private float validationLastRoll;
        private float validationForcedRoll = -1f;
        private FailureType validationLastFailure;
#endif
        private readonly List<Vector2> gestureSamples = new List<Vector2>(64);
        private SkateTrick currentTrick;
        private FailureType failure;
        private float attemptChance;
        private bool attemptResolved = true;
        private bool battleMode = true;
        private string message = "SWIPE TO TRICK";
        private bool tracking;
        private int trackingId = -1;
        private int pushTouchId = -1;
        private int hudTouchId = -1;
        private HudAction hudTouchAction;
        private bool pushHeld;
        private bool mouseTracking;
        private bool mousePushing;
        private HudAction mouseHudAction;
        private float touchStartTime;
        private float pushStartedAt;
        private int pushPhysicsCycle = -1;
        private float pushPhysicsPlantDuration;
        private Vector2 gestureStartPosition;
        private Quaternion bailBoardPose;
        private bool paused;
        private Vector3 boardRestPosition;

        private bool GameOver { get { return battleMode && match != null && match.Finished; } }
        private bool PlayerMatching { get { return battleMode && match != null && match.Phase == MatchPhase.PlayerMatch; } }
        private SkateTrick RequiredTrick { get { return match == null ? SkateTrick.Ollie : (SkateTrick)(int)match.RequiredTrick; } }

        private void OnEnable() { EnhancedTouchSupport.Enable(); }
        private void OnDisable() { EnhancedTouchSupport.Disable(); }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            boardRig = GetComponent<SkaterSeoulBoardRig>();
            riderMotion = GetComponent<SkaterSeoulRiderMotion>();
            hud = GetComponent<SkaterSeoulHud>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            state = MotionState.Rolling;
            if (boardPivot != null) boardRestPosition = boardPivot.localPosition;
            lastGroundContact = float.NegativeInfinity;
            if (travelDirection == TravelDirection.BackwardX) body.rotation = Quaternion.Euler(0f, 180f, 0f);
            LoadProgress();
            StartNewMatch();
        }

        private void LoadProgress()
        {
            bool hasLegacyProgress = PlayerPrefs.HasKey("skater_progress_v3") ||
                PlayerPrefs.HasKey("skater_progress_v2") || PlayerPrefs.HasKey("skater_progress_v1");
            progress = Progress.Load();
            const string importKey = "SS_LegacyProgressImported_v1";
            if (PlayerPrefs.HasKey(importKey)) return;

            if (!hasLegacyProgress)
            {
                for (int slot = 0; slot < 3; slot++)
                {
                    SkateTrick trick = SkaterSeoulRules.TrickAtMasterySlot(slot);
                    float saved = PlayerPrefs.GetFloat("SS_Mastery_" + slot, SkaterSeoulRules.InitialMastery(trick));
                    if (float.IsNaN(saved) || float.IsInfinity(saved)) saved = SkaterSeoulRules.InitialMastery(trick);
                    int index = TrickCatalog.IndexOf((Trick)(int)trick);
                    progress.Data.mastery[index] = Mathf.RoundToInt(Mathf.Clamp(saved, 0f, 100f));
                }
            }

            SkateTrick[] available = { SkateTrick.Ollie, SkateTrick.Kickflip, SkateTrick.TreFlip };
            foreach (SkateTrick trick in available)
            {
                Trick id = (Trick)(int)trick;
                if (!progress.IsObserved(id)) progress.Observe(id);
                if (!progress.IsLearned(id)) progress.LearnTrick(id);
            }
            progress.Save();
            PlayerPrefs.SetInt(importKey, 1);
            PlayerPrefs.Save();
        }

        private void StartNewMatch()
        {
            match = new SkateMatch(BotPersona.Amateur, 5, true,
                UnityEngine.Random.Range(1, int.MaxValue), new[] { Trick.Ollie, Trick.Kickflip, Trick.TreFlip });
            match.StartTurn(Time.timeAsDouble);
            matchResultRecorded = false;
            nextBotTurnAt = Time.timeAsDouble;
        }

        private void TickMatch()
        {
            if (!battleMode || match == null || match.Finished) return;
            double now = Time.timeAsDouble;
            if (match.IsPlayerTurn && now - match.TurnStart >= match.TurnLimit)
            {
                match.Timeout(now);
                message = match.LastEvent;
                nextBotTurnAt = now + 0.55;
            }
            if (match.IsBotTurn && now >= nextBotTurnAt)
            {
                match.OnBotTurn(now);
                message = match.LastEvent;
                nextBotTurnAt = now + (match.IsBotTurn ? 0.55 : 0.0);
            }
            RecordMatchResult();
        }

        private void RecordMatchResult()
        {
            if (match == null || !match.Finished || matchResultRecorded) return;
            bool won = match.Winner == MatchSide.Player;
            progress.AddMatch(won, match.RatingDelta(won), match.BotSetTricks);
            matchResultRecorded = true;
            message = won ? "YOU WIN S.K.A.T.E." : "BOT WINS S.K.A.T.E.";
        }

        private void Update()
        {
            HandleInput();
            if (paused) { UpdateHud(); return; }
            TickMatch();
            if (state == MotionState.Crouching && Time.time - trickStarted >= 0.16f) PopTrick();
            if (state == MotionState.Airborne) AnimateFlight();
            else if (state == MotionState.Bailing && Time.time - bailStarted > 0.85f)
            {
                state = MotionState.Rolling;
                if (boardPivot != null) boardPivot.localPosition = boardRestPosition;
                if (riderMotion != null) riderMotion.ResetPose();
                message += " | READY";
            }
            if (state == MotionState.Crouching)
            {
                float crouch = Mathf.Clamp01((Time.time - trickStarted) / 0.16f);
                if (boardPivot != null)
                    boardPivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -4f, crouch));
            }
            if (state == MotionState.Bailing)
            {
                float recovery = Mathf.Clamp01((Time.time - bailStarted) / 0.85f);
                if (boardPivot != null)
                {
                    boardPivot.localRotation = Quaternion.Slerp(bailBoardPose, Quaternion.identity, recovery);
                    boardPivot.localPosition = Vector3.Lerp(bailBoardPosition, boardRestPosition, recovery);
                }
            }
            else if (state != MotionState.Airborne && state != MotionState.Crouching)
            {
                if (boardPivot != null) boardPivot.localRotation = Quaternion.Slerp(boardPivot.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                float compress = Mathf.Clamp01(1f - (Time.time - landingStarted) / 0.28f);
                if (boardPivot != null) boardPivot.localPosition = Vector3.Lerp(boardRestPosition, boardRestPosition + Vector3.down * 0.035f, compress);
                if (riderMotion != null)
                    riderMotion.SetState(compress > 0f ? SkaterBodyPose.Land : (pushHeld ? SkaterBodyPose.Push : SkaterBodyPose.Ride),
                        currentTrick, 0f, body.linearVelocity.x, pushHeld, failure);
            }
            if (riderMotion != null && state == MotionState.Airborne)
            {
                float phase = Mathf.Clamp01((Time.time - trickStarted) / flightDuration);
                bool boardReady = currentTrick == SkateTrick.Ollie ||
                    (Mathf.Abs(boardFlipDegrees) >= 350f &&
                    (currentTrick != SkateTrick.TreFlip || Mathf.Abs(boardYawDegrees) >= 350f));
                float catchStart = currentTrick == SkateTrick.Kickflip ? .77f : .70f;
                riderMotion.SetState(phase >= catchStart && boardReady ? SkaterBodyPose.Catch : SkaterBodyPose.Air,
                    currentTrick, phase, body.linearVelocity.x, false, failure);
            }
            if (riderMotion != null && state == MotionState.Crouching)
                riderMotion.SetState(SkaterBodyPose.Crouch, currentTrick, 0f, body.linearVelocity.x, false, failure);
            if (riderMotion != null && state == MotionState.Bailing)
                riderMotion.SetState(SkaterBodyPose.Bail, currentTrick, 0f, body.linearVelocity.x, false, failure);
            UpdateHud();
        }

        private void FixedUpdate()
        {
            if (paused) return;
            if (!GameOver && state != MotionState.Bailing)
            {
                Vector3 v = body.linearVelocity;
                float direction = TravelSign;
                float speed = v.x * direction;
                if (pushHeld && state == MotionState.Rolling && speed < 8.5f)
                {
                    float elapsed = Mathf.Max(0f, Time.time - pushStartedAt);
                    int cycle = Mathf.FloorToInt(elapsed / SkaterSeoulRiderMotion.PushCycleDuration);
                    if (cycle != pushPhysicsCycle)
                    {
                        pushPhysicsCycle = cycle;
                        pushPhysicsPlantDuration = SkaterSeoulRiderMotion.PushContactDuration(speed);
                    }
                    float frameEnd = elapsed + Time.fixedDeltaTime;
                    float plantStart = cycle * SkaterSeoulRiderMotion.PushCycleDuration +
                        SkaterSeoulRiderMotion.PushPlantStartSeconds;
                    float plantEnd = plantStart + pushPhysicsPlantDuration;
                    float contactSeconds = Mathf.Max(0f, Mathf.Min(frameEnd, plantEnd) - Mathf.Max(elapsed, plantStart));
                    if (contactSeconds > 0f)
                        body.AddForce(Vector3.right * direction * SkaterSeoulRiderMotion.PushDeltaV *
                            contactSeconds / (pushPhysicsPlantDuration * Time.fixedDeltaTime), ForceMode.Acceleration);
                }
                else if (Grounded && Mathf.Abs(speed) > 0.01f)
                    body.AddForce(Vector3.right * -Mathf.Sign(v.x) * 0.20f, ForceMode.Acceleration);
                else if (state == MotionState.Airborne)
                    body.AddForce(Vector3.right * -v.x * 0.025f, ForceMode.Acceleration);
                if (Mathf.Abs(v.x) > 8.5f)
                {
                    v.x = Mathf.Sign(v.x) * 8.5f;
                    body.linearVelocity = v;
                }
                if (Mathf.Abs(v.z) > 0.02f)
                {
                    v.z = 0f;
                    body.linearVelocity = v;
                }
            }
            if ((TravelSign > 0f && body.position.x > respawnAtX) ||
                (TravelSign < 0f && body.position.x < -respawnAtX))
            {
                body.position = new Vector3(-8f * TravelSign, 0.31f, 0f);
                body.rotation = travelDirection == TravelDirection.BackwardX ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                state = MotionState.Rolling;
                if (boardRig != null) boardRig.ResetTelemetry();
                attemptResolved = true;
                tracking = false;
                trackingId = -1;
                pushHeld = false;
                pushPhysicsCycle = -1;
                pushTouchId = -1;
                gestureSamples.Clear();
                if (boardPivot != null) { boardPivot.localRotation = Quaternion.identity; boardPivot.localPosition = boardRestPosition; }
                if (riderMotion != null) riderMotion.ResetPose();
            }
        }

        private void LateUpdate() { }

        private void HandleInput()
        {
            if (paused)
            {
                Keyboard pausedKeys = Keyboard.current;
                if (pausedKeys != null && pausedKeys.escapeKey.wasPressedThisFrame) TogglePause();
                HandlePausedPointerInput();
                return;
            }
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.digit1Key.wasPressedThisFrame) BeginTrick(SkateTrick.Ollie, 0.9f);
                if (keys.digit2Key.wasPressedThisFrame) BeginTrick(SkateTrick.Kickflip, 0.9f);
                if (keys.digit3Key.wasPressedThisFrame) BeginTrick(SkateTrick.TreFlip, 0.9f);
                if (keys.rKey.wasPressedThisFrame) ResetBattle();
                if (keys.escapeKey.wasPressedThisFrame) { TogglePause(); return; }
                if (keys.spaceKey.wasPressedThisFrame) BeginPushOff();
                if (keys.spaceKey.wasReleasedThisFrame) EndPushOff();
            }

            if (Touchscreen.current != null)
            {
                foreach (Touch t in Touch.activeTouches)
                {
                    if (t.phase == InputTouchPhase.Began) BeginPointer(t.screenPosition, t.touchId, false);
                    else if (t.phase == InputTouchPhase.Moved || t.phase == InputTouchPhase.Stationary)
                        MovePointer(t.screenPosition, t.touchId, false);
                    else if (t.phase == InputTouchPhase.Ended)
                        EndPointer(t.screenPosition, t.touchId, false, false);
                    else if (t.phase == InputTouchPhase.Canceled)
                        EndPointer(t.screenPosition, t.touchId, false, true);
                }
            }
            if (Mouse.current != null)
            {
                Mouse mouse = Mouse.current;
                Vector2 position = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame) BeginPointer(position, -2, true);
                if (mouse.leftButton.isPressed) MovePointer(position, -2, true);
                if (mouse.leftButton.wasReleasedThisFrame) EndPointer(position, -2, true, false);
            }
        }

        private void BeginPointer(Vector2 position, int pointerId, bool isMouse)
        {
            HudAction action;
            if (GetHudActionAt(position, out action))
            {
                if (isMouse) mouseHudAction = action;
                else if (hudTouchId == -1) { hudTouchId = pointerId; hudTouchAction = action; }
                return;
            }
            if (IsInPushControl(position))
            {
                if (isMouse) mousePushing = true;
                else if (pushTouchId == -1) pushTouchId = pointerId;
                BeginPushOff();
                return;
            }
            if (!tracking && SkaterSeoulRules.CanStartGesture(position, TrickControlCenter(), ControlRadius(), false))
            {
                tracking = true;
                trackingId = pointerId;
                mouseTracking = isMouse;
                touchStartTime = Time.unscaledTime;
                gestureStartPosition = position;
                gestureSamples.Clear();
                RecordGestureSample(position, true);
            }
        }

        private void MovePointer(Vector2 position, int pointerId, bool isMouse)
        {
            if (tracking && trackingId == pointerId && mouseTracking == isMouse)
                RecordGestureSample(position, false);
        }

        private void EndPointer(Vector2 position, int pointerId, bool isMouse, bool canceled)
        {
            if (isMouse)
            {
                if (mousePushing) { mousePushing = false; EndPushOff(); }
                if (mouseHudAction != HudAction.None)
                {
                    HudAction action = mouseHudAction;
                    mouseHudAction = HudAction.None;
                    if (!canceled && GetHudActionAt(position, out HudAction releasedAction) && releasedAction == action)
                        PerformHudAction(action);
                }
            }
            else
            {
                if (pushTouchId == pointerId) { pushTouchId = -1; EndPushOff(); }
                if (hudTouchId == pointerId)
                {
                    HudAction action = hudTouchAction;
                    hudTouchId = -1;
                    hudTouchAction = HudAction.None;
                    if (!canceled && GetHudActionAt(position, out HudAction releasedAction) && releasedAction == action)
                        PerformHudAction(action);
                }
            }

            if (!tracking || trackingId != pointerId || mouseTracking != isMouse) return;
            if (canceled || !IsInsideScreen(position)) CancelGesture("TRICK INPUT CANCELED - TRY AGAIN");
            else
            {
                RecordGestureSample(position, true);
                FinishGesture(gestureSamples, gestureStartPosition);
                StopTracking();
            }
        }

        private void BeginPushOff()
        {
            if (paused || GameOver || pushHeld || state != MotionState.Rolling) return;
            pushHeld = true;
            float speed = Mathf.Max(0f, body.linearVelocity.x * TravelSign);
            pushStartedAt = Time.time;
            pushPhysicsCycle = 0;
            pushPhysicsPlantDuration = SkaterSeoulRiderMotion.PushContactDuration(speed);
            if (riderMotion != null) riderMotion.BeginPushCycle(pushStartedAt, speed);
            message = "PUSH";
        }

        private void EndPushOff() { pushHeld = false; pushPhysicsCycle = -1; }

        private void PerformHudAction(HudAction action)
        {
            if (action == HudAction.Pause) TogglePause();
            else if (action == HudAction.Resume) TogglePause();
            else if (action == HudAction.Reset) { ResetBattle(); TogglePause(); }
            else if (action == HudAction.ToggleMode) { ToggleMode(); TogglePause(); }
        }

        private Vector2 TrickControlCenter()
        {
            Rect safe = Screen.safeArea;
            return new Vector2(safe.xMin + safe.width * 0.17f, safe.yMin + safe.height * 0.15f);
        }

        private Vector2 PushControlCenter()
        {
            Rect safe = Screen.safeArea;
            return new Vector2(safe.xMin + safe.width * 0.895f, safe.yMin + safe.height * 0.13f);
        }

        private float ControlRadius() { return Screen.safeArea.height * 0.20f; }
        private float PushControlRadius() { return Mathf.Max(76f, Screen.safeArea.height * 0.12f); }

        private bool IsInPushControl(Vector2 position)
        {
            return SkaterSeoulRules.IsInsideControl(position, PushControlCenter(), PushControlRadius());
        }

        private bool GetHudActionAt(Vector2 screenPosition, out HudAction action)
        {
            Rect safe = Screen.safeArea;
            if (!paused)
            {
                Rect pauseButton = new Rect(safe.xMin + safe.width * .94f,
                    safe.yMin + safe.height * .82f, safe.width * .05f, safe.height * .06f);
                if (pauseButton.Contains(screenPosition)) { action = HudAction.Pause; return true; }
                action = HudAction.None;
                return false;
            }
            float centerX = safe.center.x;
            float centerY = safe.center.y;
            Rect resume = new Rect(centerX - safe.width * 0.22f, centerY + safe.height * 0.08f,
                safe.width * 0.44f, safe.height * 0.10f);
            Rect mode = new Rect(centerX - safe.width * 0.22f, centerY - safe.height * 0.06f,
                safe.width * 0.44f, safe.height * 0.10f);
            Rect reset = new Rect(centerX - safe.width * 0.22f, centerY - safe.height * 0.20f,
                safe.width * 0.44f, safe.height * 0.10f);
            if (resume.Contains(screenPosition)) { action = HudAction.Resume; return true; }
            if (mode.Contains(screenPosition)) { action = HudAction.ToggleMode; return true; }
            if (reset.Contains(screenPosition)) { action = HudAction.Reset; return true; }
            if (safe.Contains(screenPosition)) { action = HudAction.Resume; return true; }
            action = HudAction.None;
            return true;
        }

        private void HandlePausedPointerInput()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 point = Mouse.current.position.ReadValue();
                if (GetHudActionAt(point, out HudAction action)) PerformHudAction(action);
            }
            if (Touchscreen.current == null) return;
            foreach (Touch touch in Touch.activeTouches)
                if (touch.phase == InputTouchPhase.Began && GetHudActionAt(touch.screenPosition, out HudAction action))
                    PerformHudAction(action);
        }

        private void TogglePause()
        {
            paused = !paused;
            pushHeld = false;
            pushTouchId = -1;
            mousePushing = false;
            StopTracking();
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (!paused) message = "SWIPE TO TRICK";
        }

        private void UpdateHud()
        {
            if (hud == null) return;
            string playerLetters = "";
            string rivalLetters = "";
            string turn = "";
            string challenge = SkaterSeoulRules.Name(currentTrick);
            if (battleMode && match != null)
            {
                playerLetters = match.PlayerWord;
                rivalLetters = match.BotWord;
                switch (match.Phase)
                {
                    case MatchPhase.PlayerSet:
                        turn = "YOUR SET";
                        challenge = "CHOOSE A TRICK";
                        break;
                    case MatchPhase.PlayerMatch:
                        turn = "MATCH THE TRICK";
                        challenge = SkaterSeoulRules.Name(RequiredTrick);
                        break;
                    case MatchPhase.BotSet:
                        turn = "RIVAL'S TURN";
                        challenge = "WATCH THE SET";
                        break;
                    case MatchPhase.BotMatch:
                        turn = "YOUR TURN";
                        challenge = "MATCH " + SkaterSeoulRules.Name(RequiredTrick);
                        break;
                    default:
                        turn = "MATCH OVER";
                        challenge = match.Winner == MatchSide.Player ? "YOU WIN" : "RIVAL WINS";
                        break;
                }
            }
            Vector2 gestureDelta = tracking && gestureSamples.Count > 0
                ? gestureSamples[gestureSamples.Count - 1] - gestureStartPosition : Vector2.zero;
            hud.SetState(!battleMode, playerLetters, rivalLetters, turn, challenge, message,
                SkaterSeoulRules.Name(currentTrick), MasteryValue(currentTrick), pushHeld, tracking, gestureDelta, paused);
        }

        private void FinishGesture(IList<Vector2> samples, Vector2 startPosition)
        {
            SkateTrick recognized;
            float quality;
            if (SkaterSeoulRules.RecognizeSwipe(samples, Time.unscaledTime - touchStartTime,
                Screen.height, out recognized, out quality)) BeginTrick(recognized, quality);
            else if (SkaterSeoulRules.RecognizeTrickTap(startPosition, TrickControlCenter(), ControlRadius(), out recognized))
                BeginTrick(recognized, 0.86f);
            else message = "LEFT TRICK PAD: UP OLLIE / UP-LEFT KICK / UP-RIGHT TRE";
        }

        private void RecordGestureSample(Vector2 position, bool force)
        {
            if (gestureSamples.Count == 0 || force || Vector2.Distance(gestureSamples[gestureSamples.Count - 1], position) >= 2f)
            {
                if (gestureSamples.Count < 64) gestureSamples.Add(position);
                else gestureSamples[gestureSamples.Count - 1] = position;
            }
        }

        private bool IsInsideScreen(Vector2 point)
        {
            return point.x >= 0f && point.x <= Screen.width && point.y >= 0f && point.y <= Screen.height;
        }

        private void CancelGesture(string reason)
        {
            StopTracking();
            message = reason;
        }

        private void StopTracking()
        {
            tracking = false;
            trackingId = -1;
            mouseTracking = false;
            gestureSamples.Clear();
        }

        private bool Grounded { get { return (boardRig != null && boardRig.supported) || Time.time - lastGroundContact < 0.08f; } }
        private float TravelSign { get { return travelDirection == TravelDirection.ForwardX ? 1f : -1f; } }

        private void BeginTrick(SkateTrick trick, float quality)
        {
            if (GameOver) { message = "MATCH OVER - RESET TO PLAY AGAIN"; return; }
            if (state != MotionState.Rolling) { message = "WAIT FOR LANDING OR RECOVERY"; return; }
            if (!Grounded) { message = "WAIT UNTIL THE BOARD IS GROUNDED"; return; }
            if (battleMode && match != null && !match.IsPlayerTurn) { message = "WAIT FOR YOUR TURN"; return; }
            currentTrick = trick;
            displayQuality = Mathf.Clamp01(quality);
            attemptResolved = false;
            landingStarted = -1f;
            trickStarted = Time.time;
            flightDuration = Mathf.Max(0.5f, 2f * jumpVelocity / Mathf.Abs(Physics.gravity.y));
            boardFlipDegrees = 0f;
            boardYawDegrees = 0f;
            attemptChance = quality < 0.27f ? 0f :
                SuccessProbability(trick, quality);
            failure = quality < 0.27f ? FailureType.UnderRotation : FailureType.None;
            message = SkaterSeoulRules.Name(trick);
            state = MotionState.Crouching;
        }

        private void PopTrick()
        {
            state = MotionState.Airborne;
            trickStarted = Time.time;
            Vector3 v = body.linearVelocity;
            v.y = 0f;
            body.linearVelocity = v;
            body.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
            if (boardRig != null) boardRig.SetSupport(false, Vector3.up);
            if (riderMotion != null) riderMotion.TriggerTrick();
            message = "POP  ·  " + SkaterSeoulRules.Name(currentTrick);
        }

        private void AnimateFlight()
        {
            float t = Mathf.Clamp01((Time.time - trickStarted) / flightDuration);
            float spin = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.68f, t));
            float targetFlip = failure == FailureType.UnderRotation ? 245f : 360f;
            float stanceSign = stance == SkateStance.Regular ? 1f : -1f;
            boardFlipDegrees = currentTrick == SkateTrick.Ollie ? 0f : targetFlip * spin * stanceSign;
            boardYawDegrees = currentTrick == SkateTrick.TreFlip ? 360f * spin * stanceSign * TravelSign : 0f;
            float pop = currentTrick == SkateTrick.Ollie ? 17f : 12f;
            float pitch = t < 0.34f ? Mathf.Lerp(pop, 0f, Mathf.SmoothStep(0f, 1f, t / 0.34f)) : 0f;
            if (boardPivot != null)
            {
                boardPivot.localRotation = Quaternion.AngleAxis(boardYawDegrees, Vector3.up) *
                    Quaternion.AngleAxis(boardFlipDegrees, Vector3.right) * Quaternion.AngleAxis(pitch, Vector3.forward);
            }
        }

        private void OnCollisionEnter(Collision collision) { ReceiveGroundContact(collision); }
        private void OnCollisionStay(Collision collision) { ReceiveGroundContact(collision); }

        private void OnCollisionExit(Collision collision)
        {
            if (boardRig != null) boardRig.SetSupport(false, Vector3.up);
        }

        private void ReceiveGroundContact(Collision collision)
        {
            if (collision.contactCount == 0) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < 0.65f) continue;
                lastGroundContact = Time.time;
                if (boardRig != null) boardRig.SetSupport(true, collision.GetContact(i).normal);
                if (state == MotionState.Airborne && Time.time - trickStarted > 0.25f && body.linearVelocity.y <= 0.4f)
                    ResolveLanding();
                break;
            }
        }

        private void ResolveLanding()
        {
            if (attemptResolved || state != MotionState.Airborne) return;
            attemptResolved = true;
#if UNITY_EDITOR
            validationAttemptCount++;
#endif
            if (currentTrick != SkateTrick.Ollie && Mathf.Abs(boardFlipDegrees) < 300f && failure == FailureType.None)
                failure = FailureType.UnderRotation;
            else if (currentTrick == SkateTrick.TreFlip && Mathf.Abs(boardYawDegrees) < 300f && failure == FailureType.None)
                failure = FailureType.MissedCatch;
            Verdict verdict = EvaluateLanding(out float roll);
            bool landed = verdict.Success;
#if UNITY_EDITOR
            validationLastFailure = failure;
#endif
            PracticeResult growth = progress.AddPractice(verdict);
            MirrorMvpMastery();
            if (landed)
            {
                state = MotionState.Rolling;
                landingStarted = Time.time;
                if (riderMotion != null) riderMotion.BeginLanding(landingStarted);
                message = SkaterSeoulRules.Name(currentTrick) + "  ·  LANDED  +" +
                    (growth.MasteryAfter - growth.MasteryBefore) + "  ·  +" + growth.XpGain + " XP";
                if (boardPivot != null) boardPivot.localPosition = boardRestPosition + Vector3.down * 0.035f;
            }
            else
            {
                state = MotionState.Bailing;
                bailStarted = Time.time;
                bailBoardPose = boardPivot != null ? boardPivot.localRotation : Quaternion.identity;
                bailBoardPosition = boardRestPosition + (failure == FailureType.MissedCatch ? new Vector3(0f, -0.06f, 0.26f) :
                    (failure == FailureType.LostBalance ? new Vector3(0f, -0.04f, -0.18f) : new Vector3(0f, -0.05f, 0.06f)));
                if (failure == FailureType.LostBalance && boardPivot != null)
                    bailBoardPose = bailBoardPose * Quaternion.Euler(0f, 24f * TravelSign, 0f);
                message = failure == FailureType.UnderRotation ? "BAIL  ·  UNDER-ROTATED" :
                    (failure == FailureType.MissedCatch ? "BAIL  ·  NO CATCH" : "BAIL  ·  LOST BALANCE");
            }
            AdvanceBattle(verdict);
        }

        private float SuccessProbability(SkateTrick trick, float quality)
        {
            Trick id = (Trick)(int)trick;
            float q = Mathf.Clamp01(quality);
            PopGrade grade = q >= 0.90f ? PopGrade.Perfect : (q >= 0.60f ? PopGrade.Good :
                (q >= 0.35f ? PopGrade.Ok : PopGrade.Miss));
            double accuracy = TrickSuccessModel.EffectiveAccuracy(q * 100.0, grade);
            return (float)TrickSuccessModel.Calculate(accuracy, MasteryValue(trick),
                TrickSuccessModel.RelevantStat(id, progress.Stats), TrickSuccessModel.Difficulty(id),
                0.0, 0.0, matchTuning);
        }

        private int MasteryValue(SkateTrick trick)
        {
            return progress == null ? Mathf.RoundToInt(SkaterSeoulRules.InitialMastery(trick)) :
                progress.MasteryOf((Trick)(int)trick);
        }

        private Verdict EvaluateLanding(out float roll)
        {
            Trick id = (Trick)(int)currentTrick;
            if (!TrickCatalog.TryGetInput(id, out TrickInput input))
                throw new InvalidOperationException("The active MVP trick is missing from the legacy catalog: " + id);
            PopGrade grade = displayQuality >= 0.90f ? PopGrade.Perfect :
                (displayQuality >= 0.60f ? PopGrade.Good : (displayQuality >= 0.35f ? PopGrade.Ok : PopGrade.Miss));
            LandingSample sample = new LandingSample
            {
                PopOffset = 0.0,
                AttemptedOffset = 0.0,
                Grade = grade,
                Flip = input.Flip,
                Roll = boardFlipDegrees,
                BoardSpin = boardYawDegrees,
                Yaw = 0.0,
                YawTarget = input.YawTarget,
                Direction = TravelSign < 0f ? -1 : 1,
                Pitch = 0.0,
                ImpactSpeed = body.linearVelocity.magnitude,
                AirTime = flightDuration,
                Stance = Stance.Regular,
                NaturalStance = stance == SkateStance.Goofy ? NaturalStance.Goofy : NaturalStance.Regular,
                InputAccuracy = displayQuality * 100.0
            };
            Verdict verdict = Judge.Evaluate(sample, matchTuning, progress.Stats, progress.Mastery, Stance.Regular);
            int attemptId = ++nextAttemptId;
            roll = -1f;

            if (failure != FailureType.None)
            {
                verdict.Success = false;
                verdict.Reason = LegacyFailure(failure);
                verdict.Score = 0;
                verdict.SuccessProbability = 0.0;
                verdict.SuccessRoll = -1.0;
                verdict.AttemptId = attemptId;
                return verdict;
            }
            if (!verdict.Success)
            {
                failure = FailureFor(verdict.Reason);
                verdict.AttemptId = attemptId;
                return verdict;
            }

            roll = UnityEngine.Random.value;
#if UNITY_EDITOR
            if (validationForcedRoll >= 0f)
            {
                roll = validationForcedRoll;
                validationForcedRoll = -1f;
            }
            validationProbabilityDrawCount++;
            validationLastRoll = roll;
#endif
            verdict = TrickSuccessModel.Apply(verdict, sample, progress.Stats, MasteryValue(currentTrick),
                matchTuning, roll, attemptId);
            if (!verdict.Success)
            {
                float conditionalFailure = (roll - (float)verdict.SuccessProbability) /
                    Mathf.Max(0.0001f, 1f - (float)verdict.SuccessProbability);
                failure = conditionalFailure < 0.55f ? FailureType.MissedCatch : FailureType.LostBalance;
            }
            return verdict;
        }

        private static FailReason LegacyFailure(FailureType value)
        {
            switch (value)
            {
                case FailureType.UnderRotation: return FailReason.UnderRotated;
                case FailureType.MissedCatch: return FailReason.UnstableLanding;
                case FailureType.LostBalance: return FailReason.BadLanding;
                case FailureType.WrongTrick: return FailReason.OffAxis;
                default: return FailReason.None;
            }
        }

        private static FailureType FailureFor(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.IncompleteFlip:
                case FailReason.UnderRotated:
                case FailReason.WeakPop:
                case FailReason.MissedPop:
                case FailReason.LatePop:
                case FailReason.EarlyPop:
                    return FailureType.UnderRotation;
                case FailReason.UnstableLanding:
                    return FailureType.MissedCatch;
                default:
                    return FailureType.LostBalance;
            }
        }

        private void MirrorMvpMastery()
        {
            for (int slot = 0; slot < 3; slot++)
            {
                SkateTrick trick = SkaterSeoulRules.TrickAtMasterySlot(slot);
                PlayerPrefs.SetFloat("SS_Mastery_" + slot, MasteryValue(trick));
            }
            PlayerPrefs.Save();
        }

        private void AdvanceBattle(Verdict verdict)
        {
            if (!battleMode || match == null || !match.IsPlayerTurn) return;
            match.OnPlayerAttempt(verdict, Time.timeAsDouble);
            message = match.LastEvent;
            nextBotTurnAt = Time.timeAsDouble + 0.55;
            RecordMatchResult();
        }

        public void ResetBattle()
        {
            if (state == MotionState.Airborne) { message = "FINISH THE CURRENT TRICK BEFORE RESET"; return; }
            if (!battleMode) { message = "PRACTICE MODE"; return; }
            StartNewMatch();
            message = "NEW BATTLE - SET A TRICK";
        }

        public void ToggleMode()
        {
            if (state != MotionState.Rolling) { message = "MODE CAN CHANGE AFTER RECOVERY"; return; }
            battleMode = !battleMode;
            if (battleMode) StartNewMatch();
            else match = null;
            message = battleMode ? "BATTLE MODE - SET A TRICK" : "PRACTICE MODE - MASTERY IS SAVED";
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

#if UNITY_EDITOR
        public void BeginTrickForValidation(SkateTrick trick, float quality = 0.9f) { BeginTrick(trick, quality); }
        public void BeginPushForValidation() { BeginPushOff(); }
        public void EndPushForValidation() { EndPushOff(); }
        public void ForceNextRollForValidation(float roll) { validationForcedRoll = Mathf.Clamp01(roll); }
        public void SetMasteryForValidation(SkateTrick trick, float value)
        {
            progress.Data.mastery[TrickCatalog.IndexOf((Trick)(int)trick)] = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 100f));
        }
        public float SpeedForValidation { get { return body == null ? 0f : body.linearVelocity.magnitude; } }
        public float FlipDegreesForValidation { get { return boardFlipDegrees; } }
        public float YawDegreesForValidation { get { return boardYawDegrees; } }
        public bool GroundedForValidation { get { return Grounded; } }
        public int AttemptCountForValidation { get { return validationAttemptCount; } }
        public int ProbabilityDrawCountForValidation { get { return validationProbabilityDrawCount; } }
        public float LastRollForValidation { get { return validationLastRoll; } }
        public FailureType LastFailureForValidation { get { return validationLastFailure; } }
        public SkateTrick CurrentTrickForValidation { get { return currentTrick; } }
        public float FlightDurationForValidation { get { return flightDuration; } }
        public float LandingElapsedForValidation { get { return landingStarted < 0f ? -1f : Time.time - landingStarted; } }
        public float MasteryForValidation(SkateTrick trick) { return MasteryValue(trick); }
        public float SuccessChanceForValidation(SkateTrick trick, float quality) { return SuccessProbability(trick, quality); }
        public int ProgressAttemptsForValidation(SkateTrick trick)
        {
            return progress.Data.trickAttempts[TrickCatalog.IndexOf((Trick)(int)trick)];
        }
        public bool ProgressSchemaForValidation
        {
            get { return progress.Data.version == 13 && progress.Data.mastery.Length == TrickCatalog.Count; }
        }
        public string MatchPhaseForValidation { get { return match == null ? "Practice" : match.Phase.ToString(); } }
        public void SetCurrentTrickForValidation(SkateTrick trick) { currentTrick = trick; UpdateHud(); }
        public void ResetBattleForValidation() { battleMode = true; StartNewMatch(); }
#endif
    }
}
