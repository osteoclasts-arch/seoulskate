using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
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

        private enum MotionState { Rolling, Airborne, Bailing }
        private enum HudAction { None, Reset, ToggleMode }
        private MotionState state;
        private Rigidbody body;
        private float lastGroundContact;
        private float trickStarted;
        private float bailStarted;
        private float flightDuration;
        private float[] mastery = new float[3];
        private readonly List<Vector2> gestureSamples = new List<Vector2>(64);
        private SkateTrick currentTrick;
        private FailureType failure;
        private bool intendedSuccess;
        private bool attemptResolved = true;
        private bool defending;
        private bool botSetting;
        private bool battleMode = true;
        private SkateTrick requiredTrick;
        private int playerLetters;
        private int rivalLetters;
        private bool gameOver;
        private string message = "LEFT PAD: TAP O/K/T OR FLICK | RIGHT: PUSH OFF";
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
        private Vector2 gestureStartPosition;
        private Quaternion bailBoardPose;
        private Quaternion bailRiderPose;
        private Vector3 bailRiderPosition;
        private GUIStyle headlineStyle;
        private GUIStyle bodyStyle;
        private GUIStyle accentStyle;
        private GUIStyle controlStyle;
        private GUIStyle controlHintStyle;
        private Texture2D controlDisc;

        private void OnEnable() { EnhancedTouchSupport.Enable(); }
        private void OnDisable() { EnhancedTouchSupport.Disable(); }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            state = MotionState.Rolling;
            lastGroundContact = float.NegativeInfinity;
            if (travelDirection == TravelDirection.BackwardX) body.rotation = Quaternion.Euler(0f, 180f, 0f);
            bool repairedMastery = false;
            for (int i = 0; i < 3; i++)
            {
                SkateTrick t = (SkateTrick)i;
                string key = "SS_Mastery_" + i;
                float saved = PlayerPrefs.GetFloat(key, SkaterSeoulRules.InitialMastery(t));
                mastery[i] = float.IsNaN(saved) || float.IsInfinity(saved) ?
                    SkaterSeoulRules.InitialMastery(t) : Mathf.Clamp(saved, 0f, 100f);
                if (!Mathf.Approximately(saved, mastery[i]))
                {
                    PlayerPrefs.SetFloat(key, mastery[i]);
                    repairedMastery = true;
                }
            }
            if (repairedMastery) PlayerPrefs.Save();
        }

        private void Update()
        {
            HandleInput();
            if (state == MotionState.Airborne) AnimateFlight();
            else if (state == MotionState.Bailing && Time.time - bailStarted > 0.85f)
            {
                state = MotionState.Rolling;
                if (boardPivot != null) boardPivot.localRotation = Quaternion.identity;
                if (riderVisual != null)
                {
                    riderVisual.localPosition = Vector3.zero;
                    riderVisual.localRotation = Quaternion.Euler(0f, 0f, -5f);
                }
                message += " | READY";
            }
            if (state == MotionState.Bailing)
            {
                float recovery = Mathf.Clamp01((Time.time - bailStarted) / 0.85f);
                if (boardPivot != null) boardPivot.localRotation = Quaternion.Slerp(bailBoardPose, Quaternion.identity, recovery);
                if (riderVisual != null)
                {
                    riderVisual.localRotation = Quaternion.Slerp(bailRiderPose, Quaternion.Euler(0f, 0f, -5f), recovery);
                    riderVisual.localPosition = Vector3.Lerp(bailRiderPosition, Vector3.zero, recovery);
                }
            }
            else if (state != MotionState.Airborne)
            {
                if (boardPivot != null) boardPivot.localRotation = Quaternion.Slerp(boardPivot.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                if (riderVisual != null)
                {
                    riderVisual.localRotation = Quaternion.Slerp(riderVisual.localRotation, Quaternion.Euler(0f, 0f, -5f), Time.deltaTime * 9f);
                    riderVisual.localPosition = Vector3.Lerp(riderVisual.localPosition, Vector3.zero, Time.deltaTime * 9f);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!gameOver && state != MotionState.Bailing)
            {
                Vector3 v = body.linearVelocity;
                float forwardSpeed = v.x * TravelSign;
                forwardSpeed = pushHeld ?
                    Mathf.MoveTowards(forwardSpeed, cruiseSpeed, 8f * Time.fixedDeltaTime) :
                    Mathf.MoveTowards(forwardSpeed, 0f, 0.12f * Time.fixedDeltaTime);
                v.x = forwardSpeed * TravelSign;
                v.z = 0f;
                body.linearVelocity = v;
            }
            if ((TravelSign > 0f && body.position.x > respawnAtX) ||
                (TravelSign < 0f && body.position.x < -respawnAtX))
            {
                body.position = new Vector3(-8f * TravelSign, 0.31f, 0f);
                body.rotation = travelDirection == TravelDirection.BackwardX ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                state = MotionState.Rolling;
                attemptResolved = true;
                tracking = false;
                trackingId = -1;
                pushHeld = false;
                pushTouchId = -1;
                gestureSamples.Clear();
                if (boardPivot != null) boardPivot.localRotation = Quaternion.identity;
                if (riderVisual != null)
                {
                    riderVisual.localPosition = Vector3.zero;
                    riderVisual.localRotation = Quaternion.Euler(0f, 0f, -5f);
                }
                if (followCamera != null)
                    followCamera.transform.position = new Vector3(body.position.x + 1.5f * TravelSign, 3.25f, -19f);
            }
        }

        private void LateUpdate()
        {
            if (followCamera == null) return;
            Vector3 target = new Vector3(transform.position.x + 1.5f * TravelSign, 3.25f, -19f);
            followCamera.transform.position = Vector3.Lerp(followCamera.transform.position, target, Mathf.Clamp01(Time.deltaTime * 4.5f));
        }

        private void HandleInput()
        {
            Keyboard keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.digit1Key.wasPressedThisFrame) BeginTrick(SkateTrick.Ollie, 0.9f);
                if (keys.digit2Key.wasPressedThisFrame) BeginTrick(SkateTrick.Kickflip, 0.9f);
                if (keys.digit3Key.wasPressedThisFrame) BeginTrick(SkateTrick.TreFlip, 0.9f);
                if (keys.rKey.wasPressedThisFrame) ResetBattle();
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
            if (gameOver || pushHeld || state == MotionState.Bailing) return;
            pushHeld = true;
            float speed = Mathf.Max(0f, body.linearVelocity.x * TravelSign);
            Vector3 velocity = body.linearVelocity;
            velocity.x = Mathf.Min(cruiseSpeed, speed + 1.25f) * TravelSign;
            body.linearVelocity = velocity;
            message = "PUSH OFF - HOLD TO BUILD SPEED";
        }

        private void EndPushOff() { pushHeld = false; }

        private void PerformHudAction(HudAction action)
        {
            if (action == HudAction.Reset) ResetBattle();
            else if (action == HudAction.ToggleMode) ToggleMode();
        }

        private Vector2 TrickControlCenter()
        {
            Rect safe = Screen.safeArea;
            return new Vector2(safe.xMin + safe.width * 0.15f, safe.yMin + safe.height * 0.20f);
        }

        private Vector2 PushControlCenter()
        {
            Rect safe = Screen.safeArea;
            return new Vector2(safe.xMax - safe.width * 0.15f, safe.yMin + safe.height * 0.20f);
        }

        private float ControlRadius() { return Screen.safeArea.height * 0.145f; }

        private bool IsInPushControl(Vector2 position)
        {
            return SkaterSeoulRules.IsInsideControl(position, PushControlCenter(), ControlRadius());
        }

        private float GuiScale { get { return Screen.height / 720f; } }

        private Vector2 ScreenToGui(Vector2 position)
        {
            float scale = GuiScale;
            return new Vector2(position.x / scale, (Screen.height - position.y) / scale);
        }

        private void GetHudButtonRects(out Rect reset, out Rect mode)
        {
            float scale = GuiScale;
            Rect safe = Screen.safeArea;
            float right = safe.xMax / scale;
            float top = (Screen.height - safe.yMax) / scale;
            reset = new Rect(right - 166f, top + 12f, 150f, 42f);
            mode = new Rect(right - 166f, top + 62f, 150f, 42f);
        }

        private bool GetHudActionAt(Vector2 screenPosition, out HudAction action)
        {
            Vector2 point = ScreenToGui(screenPosition);
            GetHudButtonRects(out Rect reset, out Rect mode);
            if (reset.Contains(point)) { action = HudAction.Reset; return true; }
            if (mode.Contains(point)) { action = HudAction.ToggleMode; return true; }
            action = HudAction.None;
            return false;
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

        private bool Grounded { get { return Time.time - lastGroundContact < 0.18f; } }
        private float TravelSign { get { return travelDirection == TravelDirection.ForwardX ? 1f : -1f; } }

        private void BeginTrick(SkateTrick trick, float quality)
        {
            if (gameOver) { message = "MATCH OVER - RESET TO PLAY AGAIN"; return; }
            if (state != MotionState.Rolling) { message = "WAIT FOR LANDING OR RECOVERY"; return; }
            if (!Grounded) { message = "WAIT UNTIL THE BOARD IS GROUNDED"; return; }
            currentTrick = trick;
            attemptResolved = false;
            trickStarted = Time.time;
            flightDuration = Mathf.Max(0.5f, 2f * jumpVelocity / Mathf.Abs(Physics.gravity.y));
            float chance = defending && trick != requiredTrick ? 0f :
                SkaterSeoulRules.Probability(trick, mastery[(int)trick], quality);
            intendedSuccess = chance > 0f && UnityEngine.Random.value < chance;
            failure = intendedSuccess ? FailureType.None :
                (defending && trick != requiredTrick ? FailureType.WrongTrick :
                (quality < 0.27f ? FailureType.UnderRotation :
                (UnityEngine.Random.value < 0.38f ? FailureType.UnderRotation :
                (UnityEngine.Random.value < 0.52f ? FailureType.MissedCatch : FailureType.LostBalance))));
            message = SkaterSeoulRules.Name(trick) + " | INPUT " + Mathf.RoundToInt(quality * 100) +
                " | SUCCESS CHANCE " + Mathf.RoundToInt(chance * 100) + "%" +
                (failure == FailureType.WrongTrick ? " | WRONG TRICK: DEFENSE FAILS" : "");
            state = MotionState.Airborne;
            Vector3 v = body.linearVelocity;
            v.y = 0f;
            body.linearVelocity = v;
            body.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
        }

        private void AnimateFlight()
        {
            float t = Mathf.Clamp01((Time.time - trickStarted) / flightDuration);
            float spin = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.76f));
            if (!intendedSuccess && failure == FailureType.UnderRotation) spin *= 0.69f;
            float stanceSign = stance == SkateStance.Regular ? 1f : -1f;
            float flip = currentTrick == SkateTrick.Ollie ? 0f : 360f * spin * stanceSign;
            float shove = currentTrick == SkateTrick.TreFlip ? 360f * spin * stanceSign * TravelSign : 0f;
            if (boardPivot != null)
            {
                boardPivot.localRotation = Quaternion.AngleAxis(shove, Vector3.up) *
                    Quaternion.AngleAxis(flip, Vector3.right) *
                    Quaternion.AngleAxis(12f * Mathf.Sin(t * Mathf.PI), Vector3.forward);
            }
            if (riderVisual != null)
            {
                riderVisual.localPosition = new Vector3(0f, 0.11f * Mathf.Sin(t * Mathf.PI), 0f);
                riderVisual.localRotation = Quaternion.Euler(0f, 0f, -5f + 13f * Mathf.Sin(t * Mathf.PI));
            }
        }

        private void OnCollisionEnter(Collision collision) { ReceiveGroundContact(collision); }
        private void OnCollisionStay(Collision collision) { ReceiveGroundContact(collision); }

        private void ReceiveGroundContact(Collision collision)
        {
            if (collision.contactCount == 0) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < 0.65f) continue;
                lastGroundContact = Time.time;
                if (state == MotionState.Airborne && Time.time - trickStarted > 0.25f && body.linearVelocity.y <= 0.4f)
                    ResolveLanding();
                break;
            }
        }

        private void ResolveLanding()
        {
            if (attemptResolved || state != MotionState.Airborne) return;
            attemptResolved = true;
            bool landed = intendedSuccess;
            float gain = 0.65f + (landed ? 1.35f : 0f);
            int i = (int)currentTrick;
            mastery[i] = Mathf.Min(100f, mastery[i] + gain);
            PlayerPrefs.SetFloat("SS_Mastery_" + i, mastery[i]);
            PlayerPrefs.Save();
            if (landed) { state = MotionState.Rolling; message = SkaterSeoulRules.Name(currentTrick) + " LANDED!  +" + gain.ToString("F1") + " MASTERY"; }
            else
            {
                state = MotionState.Bailing;
                bailStarted = Time.time;
                bailBoardPose = boardPivot != null ? boardPivot.localRotation : Quaternion.identity;
                bailRiderPose = Quaternion.Euler(0f, 0f, failure == FailureType.LostBalance ? 74f :
                    (failure == FailureType.MissedCatch ? 28f : 46f));
                bailRiderPosition = failure == FailureType.MissedCatch ? new Vector3(0.12f, -0.28f, 0f) :
                    (failure == FailureType.LostBalance ? new Vector3(-0.24f, 0.02f, 0f) : new Vector3(0f, -0.06f, 0f));
                message = "BAIL: " + failure.ToString() + " | " + SkaterSeoulRules.Name(currentTrick);
            }
            AdvanceBattle(landed);
        }

        private float BotMastery(SkateTrick trick)
        {
            switch (trick)
            {
                case SkateTrick.Ollie: return 90f;
                case SkateTrick.Kickflip: return 64f;
                default: return 51f;
            }
        }

        private bool BotAttempt(SkateTrick trick)
        {
            float p = SkaterSeoulRules.Probability(trick, BotMastery(trick), 0.82f);
            return UnityEngine.Random.value < p;
        }

        private SkateTrick BotPick()
        {
            float r = UnityEngine.Random.value;
            if (r < 0.17f) return SkateTrick.Ollie;
            if (r < 0.63f) return SkateTrick.Kickflip;
            return SkateTrick.TreFlip;
        }

        private void AdvanceBattle(bool landed)
        {
            if (!battleMode) return;
            string actor = defending ? "PLAYER defender" : (botSetting ? "BOT setter" : "PLAYER setter");
            if (defending)
            {
                bool matched = landed && currentTrick == requiredTrick;
                if (!matched) playerLetters++;
                defending = false;
                message += matched ? " | DEFENDED!" : " | YOU GOT A LETTER";
                if (playerLetters < 5) ResolveBotSet();
            }
            else if (!botSetting && landed)
            {
                bool botLanded = BotAttempt(currentTrick);
                if (!botLanded) rivalLetters++;
                message += botLanded ? " | BOT MATCHED" : " | BOT FAILED: LETTER!";
            }
            else if (!botSetting)
            {
                message += " | YOUR SET FAILED";
                ResolveBotSet();
            }
            if (playerLetters >= 5 || rivalLetters >= 5)
            {
                gameOver = true;
                message = playerLetters >= 5 ? "BOT WINS S.K.A.T.E. - PRESS R / RESTART" :
                    "YOU WIN S.K.A.T.E. - PRESS R / RESTART";
            }
            Debug.Log("SKATER SEOUL BATTLE: " + actor + "; " +
                SkaterSeoulRules.Name(currentTrick) + " " + (landed ? "landed" : failure.ToString()) +
                "; YOU=" + playerLetters + " BOT=" + rivalLetters + (gameOver ? "; GAME OVER" : ""));
        }

        private void ResolveBotSet()
        {
            botSetting = true;
            SkateTrick botTrick = BotPick();
            if (BotAttempt(botTrick))
            {
                requiredTrick = botTrick;
                defending = true;
                message += " | BOT SET " + SkaterSeoulRules.Name(botTrick) + ": MATCH IT!";
            }
            else
            {
                botSetting = false;
                defending = false;
                message += " | BOT FAILED TO SET. YOUR TURN";
            }
        }

        public void ResetBattle()
        {
            if (state == MotionState.Airborne) { message = "FINISH THE CURRENT TRICK BEFORE RESET"; return; }
            playerLetters = 0;
            rivalLetters = 0;
            defending = false;
            botSetting = false;
            gameOver = false;
            requiredTrick = SkateTrick.Ollie;
            message = "NEW BATTLE - SET A TRICK";
        }

        public void ToggleMode()
        {
            if (state != MotionState.Rolling) { message = "MODE CAN CHANGE AFTER RECOVERY"; return; }
            battleMode = !battleMode;
            playerLetters = 0;
            rivalLetters = 0;
            defending = false;
            botSetting = false;
            gameOver = false;
            requiredTrick = SkateTrick.Ollie;
            message = battleMode ? "BATTLE MODE - SET A TRICK" : "PRACTICE MODE - MASTERY IS SAVED";
        }

        private void OnDestroy()
        {
            if (controlDisc != null) Destroy(controlDisc);
        }

        private void InitGui()
        {
            if (headlineStyle != null) return;
            headlineStyle = new GUIStyle(GUI.skin.label) { fontSize = 29, fontStyle = FontStyle.Bold };
            headlineStyle.normal.textColor = new Color(0.96f, 0.97f, 0.94f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            bodyStyle.normal.textColor = new Color(0.97f, 0.97f, 0.92f);
            accentStyle = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
            accentStyle.normal.textColor = new Color(0.56f, 0.98f, 0.83f);
            controlStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            controlStyle.normal.textColor = Color.white;
            controlHintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 13, fontStyle = FontStyle.Bold };
            controlHintStyle.normal.textColor = new Color(0.90f, 0.96f, 0.96f);
            controlDisc = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "SkaterSeoulControlDisc", hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                    controlDisc.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((32f - distance) * 0.5f)));
                }
            controlDisc.Apply();
        }

        private void DrawCircle(Vector2 center, float radius, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), controlDisc);
            GUI.color = Color.white;
        }

        private void DrawControlNode(Vector2 center, float radius, string label)
        {
            DrawCircle(center, radius, new Color(0.95f, 0.98f, 0.98f, 0.88f));
            DrawCircle(center, radius * 0.72f, new Color(0.08f, 0.15f, 0.18f, 0.76f));
            if (!string.IsNullOrEmpty(label))
                GUI.Label(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), label, controlHintStyle);
        }

        private void DrawControlButtons()
        {
            float scale = GuiScale;
            float radius = ControlRadius() / scale;
            Vector2 trickCenter = ScreenToGui(TrickControlCenter());
            Vector2 pushCenter = ScreenToGui(PushControlCenter());

            DrawCircle(trickCenter, radius + 2f, new Color(1f, 1f, 1f, 0.48f));
            DrawCircle(trickCenter, radius - 2f, new Color(0.025f, 0.08f, 0.10f, 0.70f));
            float nodeRadius = radius * 0.115f;
            float nodeOffset = radius * 0.60f;
            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.PI * 0.25f * i;
                Vector2 node = trickCenter + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * nodeOffset;
                string label = i == 0 ? "O" : (i == 7 ? "K" : (i == 1 ? "T" : ""));
                DrawControlNode(node, nodeRadius, label);
            }
            DrawCircle(trickCenter, radius * 0.34f, new Color(1f, 1f, 1f, 0.94f));
            DrawCircle(trickCenter, radius * 0.29f, new Color(0.08f, 0.15f, 0.18f, 0.92f));
            GUI.Label(new Rect(trickCenter.x - radius * 0.30f, trickCenter.y - radius * 0.14f,
                radius * 0.60f, radius * 0.28f), "TRICK", controlHintStyle);
            GUI.Label(new Rect(trickCenter.x - radius * 1.25f, trickCenter.y - radius - 30f,
                radius * 2.5f, 24f), "TAP O / K / T OR FLICK", controlHintStyle);

            DrawCircle(pushCenter, radius + 2f, new Color(1f, 1f, 1f, 0.48f));
            DrawCircle(pushCenter, radius - 2f, pushHeld ? new Color(0.10f, 0.46f, 0.54f, 0.92f) : new Color(0.025f, 0.08f, 0.10f, 0.74f));
            GUI.Label(new Rect(pushCenter.x - radius * 0.65f, pushCenter.y - radius * 0.30f,
                radius * 1.30f, radius * 0.34f), "PUSH", accentStyle);
            GUI.Label(new Rect(pushCenter.x - radius * 0.65f, pushCenter.y + radius * 0.02f,
                radius * 1.30f, radius * 0.25f), pushHeld ? "PUSHING" : "HOLD", controlHintStyle);

            GetHudButtonRects(out Rect reset, out Rect mode);
            GUI.color = new Color(0.03f, 0.12f, 0.15f, 0.82f);
            GUI.DrawTexture(reset, Texture2D.whiteTexture);
            GUI.DrawTexture(mode, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(reset, "RESET", controlHintStyle);
            GUI.Label(mode, battleMode ? "PRACTICE" : "BATTLE", controlHintStyle);
        }

        private void OnGUI()
        {
            InitGui();
            Matrix4x4 old = GUI.matrix;
            float scale = GuiScale;
            float guiWidth = Screen.width / scale;
            Rect safeArea = Screen.safeArea;
            float safeLeft = safeArea.xMin / scale;
            float safeTop = (Screen.height - safeArea.yMax) / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUI.color = new Color(0.02f, 0.12f, 0.15f, 0.85f);
            GUI.DrawTexture(new Rect(safeLeft + 16f, safeTop + 12f, Mathf.Min(680f, guiWidth - safeLeft - 210f), 184f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(safeLeft + 30f, safeTop + 19f, 625, 45), "SKATER SEOUL", headlineStyle);
            GUI.Label(new Rect(safeLeft + 30f, safeTop + 64f, 620, 32), battleMode ?
                "YOU   " + SkaterSeoulRules.Letters(playerLetters) + "     BOT   " + SkaterSeoulRules.Letters(rivalLetters) :
                "PRACTICE MODE  /  MASTERY SAVED", accentStyle);
            GUI.Label(new Rect(safeLeft + 30f, safeTop + 101f, 650, 27), !battleMode ? "PRACTICE: CHOOSE ANY TRICK" : (defending ?
                "DEFEND: " + SkaterSeoulRules.Name(requiredTrick) : "YOUR SET: CHOOSE A TRICK"), bodyStyle);
            GUI.Label(new Rect(safeLeft + 30f, safeTop + 130f, 656, 28), message, bodyStyle);
            string masteryText = "MASTERY";
            for (int i = 0; i < 3; i++)
            {
                SkateTrick t = (SkateTrick)i;
                int chance = Mathf.RoundToInt(100f * SkaterSeoulRules.Probability(t, mastery[i], displayQuality));
                masteryText += "   " + (i == 2 ? "TRE" : SkaterSeoulRules.Name(t)) + " " + chance + "%";
            }
            GUI.Label(new Rect(safeLeft + 30f, safeTop + 157f, 650, 24), masteryText, controlHintStyle);
            DrawControlButtons();
            GUI.matrix = old;
            GUI.color = Color.white;
        }
    }
}
