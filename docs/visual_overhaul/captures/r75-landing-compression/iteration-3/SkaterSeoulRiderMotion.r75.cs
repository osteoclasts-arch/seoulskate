using UnityEngine;

namespace SkaterSeoul
{
    public enum SkaterBodyPose { Ride, Push, Crouch, Air, Catch, Land, Bail }

    public sealed class SkaterSeoulRiderMotion : MonoBehaviour
    {
        internal const float PushCycleDuration = .86f;
        internal const float PushPlantStartSeconds = .215f;
        internal const float PushPlantEaseInSeconds = .020f;
        internal const float PushDeltaV = .72f;
        internal const float PushTravelDistance = .42f;

        internal static float PushContactDuration(float speed)
        {
            return Mathf.Clamp(PushTravelDistance / Mathf.Max(1.8f, Mathf.Abs(speed)), .05f, .22f);
        }

        public Animator animator;
        public Transform visualRoot;
        public Transform boardPivot;
        public SkateStance stance = SkateStance.Regular;

        private SkaterBodyPose pose;
        private SkateTrick trick;
        private FailureType failure;
        private float phase;
        private float speed;
        private float pushStart;
        private int pushCycleIndex = -1;
        private float pushContactDuration;
        private float landingStart;
        private float landingContactRootY;
        private float bailStart;
        private bool pushing;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 pushGroundAnchor;
        private Vector3 pushContactRequest;
        private string pushAnchorSurface;
        private int pushAnchorId;
        private bool pushContactActive;
        private bool pushAnchorValid;
        private Vector3 lastLeftFootTarget;
        private Vector3 lastRightFootTarget;
        private float lastLeftFootWeight;
        private float lastRightFootWeight;
        private int ikCallbackCount;
        private Vector3 trickNoseAxis;
        private Vector3 trickToeSideAxis;
        private bool trickAxesValid;
        private SkinnedMeshRenderer leftShoeRenderer;
        private SkinnedMeshRenderer rightShoeRenderer;
        private Mesh shoeDiagnosticMesh;
        private Vector3 lastFrontFreeTarget;
        private float lastFlick;
        private float lastTuck;
        private float lastFrontReleaseBlend;
        private float lastCatchBlend;
        private readonly RaycastHit[] pushGroundHits = new RaycastHit[8];

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visualRoot == null && animator != null) visualRoot = animator.transform;
            if (animator != null)
            {
                foreach (SkinnedMeshRenderer renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (renderer.gameObject.name == "Skate shoe dark upper L") leftShoeRenderer = renderer;
                    else if (renderer.gameObject.name == "Skate shoe dark upper R") rightShoeRenderer = renderer;
                }
                Debug.Assert(leftShoeRenderer != null && rightShoeRenderer != null,
                    "Kickflip diagnostics need both skinned shoe meshes.");
                shoeDiagnosticMesh = new Mesh { name = "Skater shoe diagnostic bake" };
                SkaterSeoulRiderIkRelay relay = animator.GetComponent<SkaterSeoulRiderIkRelay>();
                if (relay == null) relay = animator.gameObject.AddComponent<SkaterSeoulRiderIkRelay>();
                relay.motion = this;
            }
            if (visualRoot != null)
            {
                restPosition = visualRoot.localPosition;
                restRotation = visualRoot.localRotation;
            }
            if (animator != null) animator.applyRootMotion = false;
        }

        private void OnDestroy()
        {
            if (shoeDiagnosticMesh != null) Destroy(shoeDiagnosticMesh);
        }

        public void SetState(SkaterBodyPose next, SkateTrick activeTrick, float normalizedPhase,
            float speedMetersPerSecond, bool isPushing, FailureType activeFailure)
        {
            if (next != pose)
            {
                if (next == SkaterBodyPose.Air && boardPivot != null)
                {
                    trickNoseAxis = transform.right.normalized;
                    trickToeSideAxis = transform.forward * (stance == SkateStance.Regular ? -1f : 1f);
                    trickAxesValid = true;
                    Debug.Assert(Mathf.Abs(Vector3.Dot(trickNoseAxis, trickToeSideAxis)) < .01f,
                        "Initial skateboard nose and toe-side axes must be perpendicular.");
                }
                if (next == SkaterBodyPose.Push)
                {
                    if (pushCycleIndex < 0) BeginPushCycle(Time.time, speedMetersPerSecond);
                }
                if (pose == SkaterBodyPose.Push)
                {
                    pushContactActive = false;
                    pushAnchorValid = false;
                }
                if (next == SkaterBodyPose.Land && landingStart < 0f) landingStart = Time.time;
                if (next == SkaterBodyPose.Bail) bailStart = Time.time;
            }
            pose = next;
            trick = activeTrick;
            phase = Mathf.Clamp01(normalizedPhase);
            speed = speedMetersPerSecond;
            pushing = isPushing;
            failure = activeFailure;
            if (pose == SkaterBodyPose.Push)
            {
                int cycleIndex = Mathf.FloorToInt((Time.time - pushStart) / PushCycleDuration);
                if (cycleIndex != pushCycleIndex)
                {
                    pushCycleIndex = cycleIndex;
                    pushContactDuration = PushContactDuration(speed);
                }
            }
            if (animator != null)
            {
                // shortcut: a jogging cycle reads as walking on the deck; add authored skate clips to restore locomotion variation.
                animator.SetFloat("Speed", 0f, 0.12f, Time.deltaTime);
                animator.SetFloat("Push", pushing ? 1f : 0f);
            }
        }

        public void TriggerTrick()
        {
            if (animator != null)
            {
                animator.ResetTrigger("Trick");
                animator.SetTrigger("Trick");
            }
        }

        public void BeginPushCycle(float startedAt, float speedMetersPerSecond)
        {
            pose = SkaterBodyPose.Push;
            pushing = true;
            pushStart = startedAt;
            pushCycleIndex = 0;
            pushContactDuration = PushContactDuration(speedMetersPerSecond);
            pushContactActive = false;
            pushAnchorValid = false;
        }

        public void ResetPose()
        {
            pose = SkaterBodyPose.Ride;
            phase = 0f;
            landingStart = -1f;
            pushContactActive = false;
            pushAnchorValid = false;
            pushCycleIndex = -1;
            trickAxesValid = false;
            if (visualRoot != null)
            {
                visualRoot.localPosition = restPosition;
                visualRoot.localRotation = restRotation;
            }
        }

        public void BeginLanding(float contactTime)
        {
            landingStart = contactTime;
            landingContactRootY = visualRoot != null ? visualRoot.localPosition.y : restPosition.y;
            pose = SkaterBodyPose.Land;
        }

        private float LandingElapsed { get { return Mathf.Max(0f, Time.time - landingStart); } }
        private float LandingRecovery
        {
            get { return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(LandingElapsed / 0.28f)); }
        }
        private float LandingCompression
        {
            get
            {
                float elapsed = LandingElapsed;
                return elapsed <= 0.10f ? Mathf.SmoothStep(0f, 1f, elapsed / 0.10f) :
                    1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 0.10f) / 0.18f));
            }
        }

#if UNITY_EDITOR
        public float LandingElapsedForValidation { get { return pose == SkaterBodyPose.Land ? LandingElapsed : -1f; } }
        public float LandingPelvisDropForValidation
        {
            get { return visualRoot == null ? 0f : landingContactRootY - visualRoot.localPosition.y; }
        }
        public float LandingRecoveryErrorForValidation
        {
            get { return visualRoot == null ? float.PositiveInfinity :
                Mathf.Abs(visualRoot.localPosition.y - (restPosition.y - 0.068f)); }
        }
        public float LeftFootAnchorErrorForValidation { get { return FootAnchorError(HumanBodyBones.LeftFoot, lastLeftFootTarget); } }
        public float RightFootAnchorErrorForValidation { get { return FootAnchorError(HumanBodyBones.RightFoot, lastRightFootTarget); } }
        public float LeftKneeFlexForValidation { get { return KneeFlexDegrees(HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot); } }
        public float RightKneeFlexForValidation { get { return KneeFlexDegrees(HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot); } }
#endif

        private float FootAnchorError(HumanBodyBones bone, Vector3 target)
        {
            Transform foot = animator != null ? animator.GetBoneTransform(bone) : null;
            return foot == null ? float.PositiveInfinity : Vector3.Distance(target, foot.position);
        }

        public void ApplyBodyPose()
        {
            if (visualRoot == null) return;
            float crouch = pose == SkaterBodyPose.Crouch ? 1f : pose == SkaterBodyPose.Ride ? .4f : 0f;
            if (pose == SkaterBodyPose.Air && phase < 0.18f) crouch = 1f - phase / 0.18f;
            float catchWeight = pose == SkaterBodyPose.Catch ? 1f : 0f;
            float bail = pose == SkaterBodyPose.Bail ? Mathf.Clamp01((Time.time - bailStart) / 0.25f) : 0f;
            Vector3 offset = Vector3.zero;
            Quaternion lean = Quaternion.identity;
            if (pose == SkaterBodyPose.Push)
            {
                float cycle = Mathf.Repeat(Time.time - pushStart, PushCycleDuration);
                float plant = PushPlantWeight(cycle);
                crouch = 1.20f * plant;
                float gait = Mathf.Sin((Time.time - pushStart) * Mathf.PI * 4f);
                offset.z = -0.22f * plant;
                offset.y = -.06f - .05f * plant;
                lean = Quaternion.Euler(0f, 0f, 10f + Mathf.Abs(gait) * 3f);
            }
            if (pose == SkaterBodyPose.Air)
            {
                float air = Mathf.Sin(phase * Mathf.PI);
                float tuck = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.22f, .45f, phase)) *
                    (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.68f, .84f, phase)));
                crouch = .34f * air + .60f * tuck;
                offset.y = 0.035f * air;
                float bodyTwist = trick == SkateTrick.TreFlip ? 8f : trick == SkateTrick.Kickflip ? 4f : 0f;
                lean = Quaternion.Euler(0f, bodyTwist * air, 7f * air - 3f * tuck);
            }
            if (pose == SkaterBodyPose.Land)
            {
                float recovery = LandingRecovery;
                crouch = Mathf.Lerp(0.62f, 0.4f, recovery) + 0.70f * LandingCompression;
                catchWeight = 1f - recovery;
            }
            if (pose == SkaterBodyPose.Bail)
            {
                float sign = failure == FailureType.LostBalance ? -1f : 1f;
                lean = Quaternion.Euler(0f, 0f, sign * Mathf.Lerp(12f, 78f, bail));
                offset = new Vector3(failure == FailureType.MissedCatch ? 0.10f : -0.12f, -0.12f * bail, 0f);
            }
            if (pose == SkaterBodyPose.Catch) crouch = 0.62f;
            if (pose == SkaterBodyPose.Crouch) crouch = 1.0f;
            visualRoot.localPosition = restPosition + offset + Vector3.down * (0.17f * crouch);
            visualRoot.localRotation = restRotation * lean * Quaternion.Euler(0f, 0f, -10f * crouch + 8f * catchWeight);
        }

        public void ApplyFeetIk()
        {
            if (animator == null || boardPivot == null || !animator.isHuman) return;
            ikCallbackCount++;
            Vector3 front = stance == SkateStance.Regular ? new Vector3(0.24f, 0.068f, 0f) : new Vector3(-0.24f, 0.068f, 0f);
            Vector3 rear = new Vector3(-front.x, front.y, front.z);
            bool frontLeft = stance == SkateStance.Regular;
            Vector3 frontBoard = boardPivot.TransformPoint(front);
            Vector3 rearBoard = boardPivot.TransformPoint(rear);
            Vector3 leftPosition = frontLeft ? frontBoard : rearBoard;
            Vector3 rightPosition = frontLeft ? rearBoard : frontBoard;
            float leftFootWeight = 1f;
            float rightFootWeight = 1f;
            Quaternion footRotation = boardPivot.rotation;
            float frontFootYaw = 0f;
            float frontFootPitch = 0f;
            Vector3 frontFootPitchAxis = transform.right;

            if (pose == SkaterBodyPose.Push)
            {
                float cycle = Mathf.Repeat(Time.time - pushStart, PushCycleDuration);
                float plantEnd = PushPlantStartSeconds + pushContactDuration;
                float plant = PushPlantWeight(cycle);
                float recovery = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(plantEnd, plantEnd + .08f, cycle));
                float footSide = stance == SkateStance.Regular ? 1f : -1f;
                if (plant > .01f && !pushContactActive)
                {
                    Vector3 contact = boardPivot.TransformPoint(new Vector3(rear.x + .18f, rear.y, .06f * footSide));
                    pushContactRequest = contact;
                    int hitCount = Physics.RaycastNonAlloc(contact + Vector3.up * .8f, Vector3.down,
                        pushGroundHits, 1.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    float nearest = float.PositiveInfinity;
                    pushAnchorSurface = "fallback";
                    for (int i = 0; i < hitCount; i++)
                    {
                        RaycastHit hit = pushGroundHits[i];
                        if (hit.transform == null || hit.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                        nearest = hit.distance;
                        pushGroundAnchor = hit.point + Vector3.up * .06f;
                        pushAnchorSurface = hit.collider != null ? hit.collider.name : "unknown";
                    }
                    if (float.IsPositiveInfinity(nearest)) pushGroundAnchor = new Vector3(contact.x, .06f, contact.z);
                    pushAnchorId++;
                    pushContactActive = true;
                    pushAnchorValid = true;
                }
                if (plant <= .01f) pushContactActive = false;
                if (cycle > PushCycleDuration - .04f) pushAnchorValid = false;

                Vector3 recoveryStart = pushAnchorValid ? pushGroundAnchor : rearBoard;
                Vector3 rearSwing = Vector3.Lerp(recoveryStart, rearBoard, recovery);
                rearSwing.y += Mathf.Sin(recovery * Mathf.PI) * .13f;
                Vector3 rearPrep = boardPivot.TransformPoint(new Vector3(rear.x - .06f, rear.y + .07f, .10f * footSide));
                Vector3 rearFoot = Vector3.Lerp(rearBoard, rearPrep, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.02f, .18f, cycle)));
                if (plant > .01f) rearFoot = pushAnchorValid ? pushGroundAnchor : rearFoot;
                else if (cycle >= plantEnd && cycle < PushCycleDuration - .04f) rearFoot = rearSwing;
                if (frontLeft) rightPosition = rearFoot; else leftPosition = rearFoot;
                AvatarIKHint kneeHint = frontLeft ? AvatarIKHint.RightKnee : AvatarIKHint.LeftKnee;
                HumanBodyBones kneeBone = frontLeft ? HumanBodyBones.RightUpperLeg : HumanBodyBones.LeftUpperLeg;
                Transform hip = animator.GetBoneTransform(kneeBone);
                float kneeWeight = pushAnchorValid && cycle >= PushPlantStartSeconds && cycle < PushCycleDuration - .04f ? .85f : 0f;
                animator.SetIKHintPositionWeight(kneeHint, kneeWeight);
                if (hip != null)
                    animator.SetIKHintPosition(kneeHint, Vector3.Lerp(hip.position, rearFoot, .58f) +
                        Vector3.up * .10f + Vector3.right * .12f);
            }
            else
            {
                animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
                animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
            }

            if (pose == SkaterBodyPose.Air || pose == SkaterBodyPose.Catch)
            {
                float flick = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.20f, .34f, phase)) *
                    (1f - Mathf.SmoothStep(.58f, .70f, phase));
                float pop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.08f, .20f, phase)) *
                    (1f - Mathf.SmoothStep(.52f, .66f, phase));
                float trickFlick = trick == SkateTrick.Ollie ? 0f : flick;
                float ollieSlide = trick == SkateTrick.Ollie ? pop : 0f;
                float scoop = trick == SkateTrick.TreFlip ? flick : 0f;
                float catchStart = trick == SkateTrick.Kickflip ? .77f : .68f;
                float catchEnd = trick == SkateTrick.Kickflip ? .87f : .80f;
                float catchBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(catchStart, catchEnd, phase));
                float tuck = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.22f, .45f, phase)) *
                    (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.68f, .84f, phase)));
                lastFlick = flick;
                lastTuck = tuck;
                Vector3 noseAxis = trickAxesValid ? trickNoseAxis : transform.right;
                Vector3 toeSideAxis = trickAxesValid ? trickToeSideAxis :
                    transform.forward * (stance == SkateStance.Regular ? -1f : 1f);
                frontFootPitchAxis = noseAxis;
                float kickflipYawPulse = trick == SkateTrick.Kickflip
                    ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.20f, .34f, phase)) *
                        (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.58f, .70f, phase)))
                    : 0f;
                float kickflipFlick = trick == SkateTrick.Kickflip ? trickFlick : 0f;
                Vector3 frontFree = boardPivot.position + noseAxis *
                    (front.x + .22f * trickFlick + .14f * ollieSlide - .04f * tuck + .32f * kickflipFlick) +
                    Vector3.up * (front.y + .22f * trickFlick + .12f * ollieSlide + .07f * tuck) +
                    toeSideAxis * (.26f * trickFlick + .36f * kickflipFlick + .03f * kickflipYawPulse);
                Vector3 rearFree = transform.TransformPoint(new Vector3(rear.x - .04f * trickFlick - .15f * scoop + .04f * tuck,
                    boardPivot.localPosition.y + rear.y + .11f * (pop + scoop) + .11f * trickFlick + .07f * tuck,
                    rear.z + (stance == SkateStance.Regular ? .20f : -.20f) * scoop));
                float frontReleaseBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.10f, .26f, phase));
                lastFrontFreeTarget = frontFree;
                lastFrontReleaseBlend = frontReleaseBlend;
                lastCatchBlend = catchBlend;
                Vector3 frontPosition = Vector3.Lerp(frontBoard, frontFree, frontReleaseBlend);
                Vector3 rearPosition = Vector3.Lerp(rearBoard, rearFree, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.08f, .23f, phase)));
                frontPosition = Vector3.Lerp(frontPosition, frontBoard, catchBlend);
                rearPosition = Vector3.Lerp(rearPosition, rearBoard, catchBlend);
                leftPosition = frontLeft ? frontPosition : rearPosition;
                rightPosition = frontLeft ? rearPosition : frontPosition;
                float frontWeight = 1f;
                float rearWeight = 1f;
                leftFootWeight = frontLeft ? frontWeight : rearWeight;
                rightFootWeight = frontLeft ? rearWeight : frontWeight;
                footRotation = transform.rotation;
                if (trick == SkateTrick.Kickflip)
                {
                    float yawSign = Mathf.Sign(Vector3.Dot(Vector3.Cross(noseAxis, toeSideAxis), Vector3.up));
                    frontFootYaw = 24f * kickflipYawPulse * yawSign;
                    frontFootPitch = -18f * kickflipYawPulse * yawSign;
                    Debug.Assert(kickflipYawPulse >= 0f && kickflipYawPulse <= 1f && Mathf.Abs(yawSign) > .5f &&
                        Mathf.Abs(frontFootPitch) <= 18.001f &&
                        (Mathf.Abs(frontFootPitch) < .01f || Vector3.Dot(
                            Quaternion.AngleAxis(frontFootPitch, noseAxis) * toeSideAxis, Vector3.down) > 0f),
                        "Kickflip ankle pitch must stay bounded and point the front-foot toe downward.");
                }
                float kneeHintWeight = pose == SkaterBodyPose.Air ? .48f * tuck : .22f;
                SetKneeHint(AvatarIKHint.LeftKnee, HumanBodyBones.LeftUpperLeg, leftPosition, -1f, kneeHintWeight, tuck);
                SetKneeHint(AvatarIKHint.RightKnee, HumanBodyBones.RightUpperLeg, rightPosition, 1f, kneeHintWeight, tuck);
            }

            if (pose == SkaterBodyPose.Land)
            {
                float compression = LandingCompression;
                float hintWeight = (0.22f + 0.63f * compression) * (1f - LandingRecovery);
                SetKneeHint(AvatarIKHint.LeftKnee, HumanBodyBones.LeftUpperLeg, leftPosition, -1f, hintWeight,
                    1.5f * compression, 0.10f * compression);
                SetKneeHint(AvatarIKHint.RightKnee, HumanBodyBones.RightUpperLeg, rightPosition, 1f, hintWeight,
                    1.5f * compression, 0.10f * compression);
            }

            if (pose == SkaterBodyPose.Bail) leftFootWeight = rightFootWeight = 0.05f;
            Quaternion leftFootRotation = footRotation;
            Quaternion rightFootRotation = footRotation;
            if (Mathf.Abs(frontFootYaw) > .01f || Mathf.Abs(frontFootPitch) > .01f)
            {
                Quaternion flickFootRotation = Quaternion.AngleAxis(frontFootYaw, Vector3.up) *
                    Quaternion.AngleAxis(frontFootPitch, frontFootPitchAxis) * footRotation;
                if (frontLeft) leftFootRotation = flickFootRotation; else rightFootRotation = flickFootRotation;
            }
            SetFoot(AvatarIKGoal.LeftFoot, leftPosition, leftFootWeight, leftFootRotation);
            SetFoot(AvatarIKGoal.RightFoot, rightPosition, rightFootWeight, rightFootRotation);
            ApplyBalanceArms();
        }

        private void ApplyBalanceArms()
        {
            float weight = pose == SkaterBodyPose.Air ? .60f : pose == SkaterBodyPose.Push ? .46f :
                pose == SkaterBodyPose.Catch ? .42f : pose == SkaterBodyPose.Ride ? .25f :
                pose == SkaterBodyPose.Land ? Mathf.Lerp(.42f, .32f, LandingRecovery) : 0f;
            SetBalanceArm(AvatarIKGoal.LeftHand, HumanBodyBones.LeftUpperArm, AvatarIKHint.LeftElbow, -1f, weight);
            SetBalanceArm(AvatarIKGoal.RightHand, HumanBodyBones.RightUpperArm, AvatarIKHint.RightElbow, 1f, weight);
        }

        private void SetBalanceArm(AvatarIKGoal hand, HumanBodyBones upperArm, AvatarIKHint elbowHint,
            float side, float weight)
        {
            animator.SetIKPositionWeight(hand, weight);
            animator.SetIKRotationWeight(hand, 0f);
            animator.SetIKHintPositionWeight(elbowHint, weight * .35f);
            if (weight <= 0f) return;
            Transform shoulder = animator.GetBoneTransform(upperArm);
            if (shoulder == null) return;
            Vector3 outward = shoulder.position - animator.transform.position;
            outward.y = 0f;
            if (outward.sqrMagnitude < .001f) outward = transform.right * side;
            outward.Normalize();
            float lift = pose == SkaterBodyPose.Air ? .05f : pose == SkaterBodyPose.Push ? .01f :
                pose == SkaterBodyPose.Land ? Mathf.Lerp(.03f, -.10f, LandingRecovery) : pose == SkaterBodyPose.Ride ? -.07f : .03f;
            float forward = (side < 0f ? .12f : -.08f) * (pose == SkaterBodyPose.Air ? 1f : .65f);
            animator.SetIKPosition(hand, shoulder.position + outward * .25f + Vector3.up * lift +
                visualRoot.forward * forward);
            animator.SetIKHintPosition(elbowHint, shoulder.position + outward * .17f + Vector3.up * .02f +
                visualRoot.forward * (forward * .35f));
        }

        private void SetFoot(AvatarIKGoal goal, Vector3 position, float weight, Quaternion rotation)
        {
            if (goal == AvatarIKGoal.LeftFoot) { lastLeftFootTarget = position; lastLeftFootWeight = weight; }
            else { lastRightFootTarget = position; lastRightFootWeight = weight; }
            animator.SetIKPositionWeight(goal, weight);
            animator.SetIKRotationWeight(goal, weight);
            animator.SetIKPosition(goal, position);
            animator.SetIKRotation(goal, rotation);
        }

        private void SetKneeHint(AvatarIKHint hint, HumanBodyBones upperLeg, Vector3 footTarget,
            float side, float weight, float tuck, float forwardBias = 0f)
        {
            Transform hip = animator.GetBoneTransform(upperLeg);
            animator.SetIKHintPositionWeight(hint, weight);
            if (hip == null || weight <= 0f) return;
            animator.SetIKHintPosition(hint, Vector3.Lerp(hip.position, footTarget, .52f) +
                Vector3.up * (.06f + .12f * tuck) + transform.right * (side * .025f + forwardBias));
        }

        private float PushPlantWeight(float cycleSeconds)
        {
            float end = PushPlantStartSeconds + pushContactDuration;
            return Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(PushPlantStartSeconds, PushPlantStartSeconds + PushPlantEaseInSeconds, cycleSeconds)) *
                (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(end - .015f, end, cycleSeconds)));
        }

        public void LogFootDiagnostics(string label)
        {
            if (animator == null) return;
            Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Rigidbody body = GetComponent<Rigidbody>();
            Vector3 leftPosition = left != null ? left.position : Vector3.zero;
            Vector3 rightPosition = right != null ? right.position : Vector3.zero;
            Vector3 leftShoeCenter = leftPosition;
            Vector3 rightShoeCenter = rightPosition;
            if (shoeDiagnosticMesh != null && leftShoeRenderer != null)
            {
                leftShoeRenderer.BakeMesh(shoeDiagnosticMesh, true);
                Debug.Assert(shoeDiagnosticMesh.vertexCount > 0, "Baked left shoe must contain vertices.");
                leftShoeCenter = leftShoeRenderer.transform.TransformPoint(shoeDiagnosticMesh.bounds.center);
            }
            if (shoeDiagnosticMesh != null && rightShoeRenderer != null)
            {
                rightShoeRenderer.BakeMesh(shoeDiagnosticMesh, true);
                Debug.Assert(shoeDiagnosticMesh.vertexCount > 0, "Baked right shoe must contain vertices.");
                rightShoeCenter = rightShoeRenderer.transform.TransformPoint(shoeDiagnosticMesh.bounds.center);
            }
            bool frontLeft = stance == SkateStance.Regular;
            Vector3 frontFootLocal = boardPivot != null
                ? boardPivot.InverseTransformPoint(frontLeft ? leftPosition : rightPosition) : Vector3.zero;
            Vector3 frontShoeCenter = frontLeft ? leftShoeCenter : rightShoeCenter;
            Vector3 noseAxis = trickAxesValid ? trickNoseAxis : transform.right;
            Vector3 toeSideAxis = trickAxesValid ? trickToeSideAxis :
                transform.forward * (stance == SkateStance.Regular ? -1f : 1f);
            Transform frontFootBone = frontLeft ? left : right;
            float frontFootYaw = frontFootBone != null
                ? Vector3.SignedAngle(noseAxis, frontFootBone.right, Vector3.up) : 0f;
            Vector3 boardOrigin = boardPivot != null ? boardPivot.position : transform.position;
            float noseClearance = Vector3.Dot(frontShoeCenter - boardOrigin, noseAxis) - .295f;
            float toeClearance = Vector3.Dot(frontShoeCenter - boardOrigin, toeSideAxis) - .103f;
            Vector3 frontFootTarget = frontLeft ? lastLeftFootTarget : lastRightFootTarget;
            float targetNoseClearance = Vector3.Dot(frontFootTarget - boardOrigin, noseAxis) - .295f;
            float targetToeClearance = Vector3.Dot(frontFootTarget - boardOrigin, toeSideAxis) - .103f;
            string leftReach = ReachDiagnostic(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot, lastLeftFootTarget);
            string rightReach = ReachDiagnostic(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot, lastRightFootTarget);
            float cycle = pose == SkaterBodyPose.Push ? Mathf.Repeat(Time.time - pushStart, PushCycleDuration) : -1f;
            float gripNormalY = boardPivot != null ? boardPivot.up.y : 0f;
            float boardFlip = boardPivot != null ? Mathf.DeltaAngle(0f, boardPivot.localEulerAngles.x) : 0f;
            float boardYaw = boardPivot != null ? Mathf.DeltaAngle(0f, boardPivot.localEulerAngles.y) : 0f;
            Debug.Log("SKATER FOOT DIAG | " + label + " | callback=" + ikCallbackCount + " | pose=" + pose + " | trick=" + trick +
                " | frontFootYaw=" + frontFootYaw.ToString("F1") +
                " | phase=" + phase.ToString("F2") + " | left=" + lastLeftFootWeight.ToString("F2") +
                " target=" + lastLeftFootTarget + " bone=" + leftPosition +
                " error=" + Vector3.Distance(lastLeftFootTarget, leftPosition).ToString("F3") +
                " | right=" + lastRightFootWeight.ToString("F2") + " target=" + lastRightFootTarget +
                " bone=" + rightPosition + " error=" + Vector3.Distance(lastRightFootTarget, rightPosition).ToString("F3") +
                " | leftReach=" + leftReach + " rightReach=" + rightReach +
                " | currentBoardLocalBone=" + frontFootLocal + " shoeCenter=" + frontShoeCenter +
                " shoeRenderer=" + (frontLeft ? leftShoeRenderer != null : rightShoeRenderer != null) +
                " | shoeDToFrontBolts=" + noseClearance.ToString("F3") + " shoeDToToeEdge=" + toeClearance.ToString("F3") +
                " targetDToFrontBolts=" + targetNoseClearance.ToString("F3") + " targetDToToeEdge=" + targetToeClearance.ToString("F3") +
                " | bakedShoeCenter=" + frontShoeCenter + " flick=" + lastFlick.ToString("F2") + " tuck=" + lastTuck.ToString("F2") +
                " frontFree=" + lastFrontFreeTarget + " releaseBlend=" + lastFrontReleaseBlend.ToString("F2") +
                " catchBlend=" + lastCatchBlend.ToString("F2") + " rootScale=" + transform.lossyScale +
                " boardScale=" + (boardPivot != null ? boardPivot.lossyScale : Vector3.zero) +
                " | trickAxes=" + trickAxesValid + " noseAxis=" + noseAxis + " toeSideAxis=" + toeSideAxis +
                " | kneeFlex=" + KneeFlexDegrees(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot).ToString("F0") + "/" +
                KneeFlexDegrees(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot).ToString("F0") +
                " | landingElapsed=" + (pose == SkaterBodyPose.Land ? LandingElapsed.ToString("F3") : "-1") +
                " compression=" + (pose == SkaterBodyPose.Land ? LandingCompression.ToString("F2") : "0") +
                " pelvisDrop=" + (pose == SkaterBodyPose.Land ? (landingContactRootY - visualRoot.localPosition.y).ToString("F3") : "0") +
                " | pushCycle=" + cycle.ToString("F3") + " plant=" + (pose == SkaterBodyPose.Push ? PushPlantWeight(cycle).ToString("F2") : "0") +
                " anchorId=" + pushAnchorId + " anchorValid=" + pushAnchorValid + " anchor=" + pushGroundAnchor +
                " contactRequest=" + pushContactRequest + " anchorSurface=" + pushAnchorSurface +
                " | boardFlip=" + boardFlip.ToString("F1") + " boardYaw=" + boardYaw.ToString("F1") +
                " gripNormalY=" + gripNormalY.ToString("F2") +
                " | root=" + transform.position + " velocity=" + (body != null ? body.linearVelocity : Vector3.zero) +
                " board=" + (boardPivot != null ? boardPivot.position : Vector3.zero));
        }

        private float KneeFlexDegrees(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones foot)
        {
            Transform upperBone = animator.GetBoneTransform(upper);
            Transform lowerBone = animator.GetBoneTransform(lower);
            Transform footBone = animator.GetBoneTransform(foot);
            if (upperBone == null || lowerBone == null || footBone == null) return -1f;
            return 180f - Vector3.Angle(upperBone.position - lowerBone.position, footBone.position - lowerBone.position);
        }

        private string ReachDiagnostic(HumanBodyBones upperBone, HumanBodyBones lowerBone, HumanBodyBones footBone,
            Vector3 target)
        {
            Transform upper = animator.GetBoneTransform(upperBone);
            Transform lower = animator.GetBoneTransform(lowerBone);
            Transform foot = animator.GetBoneTransform(footBone);
            if (upper == null || lower == null || foot == null) return "unmapped";
            float reach = Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, foot.position);
            return Vector3.Distance(upper.position, target).ToString("F3") + "/" + reach.ToString("F3");
        }
    }
}
