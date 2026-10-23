using UnityEngine;

namespace SkaterSeoul
{
    public sealed class SkaterSeoulBoardRig : MonoBehaviour
    {
        public Rigidbody body;
        public Transform[] wheels = new Transform[4];
        public Transform contactShadow;
        public float wheelRadius = 0.027f;
        public Vector3 supportNormal = Vector3.up;
        public bool supported;
        public float signedDistance;
        public float rotationRadians;
        public Vector3 lastIntegratedPosition;
        public int physicsStepIndex;
        public float maximumContactGap;
        public int contactProbeCount;

        private readonly Quaternion[] restRotations = new Quaternion[4];
        private readonly RaycastHit[] contactHits = new RaycastHit[8];
        private Vector3 previousPosition;
        private float angularSpeed;
        private bool initialized;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            CacheWheelRestRotations();
        }

        private void CacheWheelRestRotations()
        {
            if (wheels == null) return;
            for (int i = 0; i < wheels.Length && i < restRotations.Length; i++)
                if (wheels[i] != null) restRotations[i] = wheels[i].localRotation;
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            physicsStepIndex++;
            if (!initialized)
            {
                previousPosition = body.position;
                lastIntegratedPosition = previousPosition;
                initialized = true;
                return;
            }

            Vector3 delta = Vector3.ProjectOnPlane(body.position - previousPosition, supportNormal);
            previousPosition = body.position;
            lastIntegratedPosition = previousPosition;
            signedDistance = Vector3.Dot(delta, transform.right);
            if (supported)
            {
                float deltaRadians = signedDistance / Mathf.Max(0.001f, wheelRadius);
                rotationRadians += deltaRadians;
                angularSpeed = deltaRadians / Time.fixedDeltaTime;
            }
            else
            {
                rotationRadians += angularSpeed * Time.fixedDeltaTime;
                angularSpeed *= Mathf.Exp(-2.2f * Time.fixedDeltaTime);
            }
            ProbeGround();
        }

        private void LateUpdate()
        {
            ProbeGround();
            if (contactShadow != null && contactShadow.gameObject.activeSelf != supported)
                contactShadow.gameObject.SetActive(supported);
            if (wheels == null) return;
            for (int i = 0; i < wheels.Length && i < restRotations.Length; i++)
            {
                Transform wheel = wheels[i];
                if (wheel == null) continue;
                wheel.localRotation = Quaternion.AngleAxis(rotationRadians * Mathf.Rad2Deg, Vector3.forward) * restRotations[i];
            }
        }

        private void ProbeGround()
        {
            maximumContactGap = 0f;
            contactProbeCount = 0;
            int supportingWheels = 0;
            Vector3 normalSum = Vector3.zero;
            if (wheels != null)
            {
                for (int i = 0; i < wheels.Length && i < restRotations.Length; i++)
                {
                    Transform wheel = wheels[i];
                    if (wheel == null) continue;
                    Vector3 origin = wheel.position + transform.up * 0.30f;
                    int hitCount = Physics.RaycastNonAlloc(origin, -transform.up, contactHits, 0.50f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    RaycastHit hit = default;
                    bool foundGround = false;
                    for (int j = 0; j < hitCount; j++)
                    {
                        RaycastHit candidate = contactHits[j];
                        if (candidate.transform == null || candidate.transform.IsChildOf(transform) ||
                            (foundGround && candidate.distance >= hit.distance)) continue;
                        hit = candidate;
                        foundGround = true;
                    }
                    if (!foundGround) continue;
                    contactProbeCount++;
                    float gap = Vector3.Dot(wheel.position - transform.up * wheelRadius - hit.point, hit.normal);
                    maximumContactGap = Mathf.Max(maximumContactGap, Mathf.Abs(gap));
                    if (Mathf.Abs(gap) <= 0.01f)
                    {
                        supportingWheels++;
                        normalSum += hit.normal;
                    }
                }
            }
            supported = supportingWheels >= 2;
            if (supported && normalSum.sqrMagnitude > 0.01f) supportNormal = normalSum.normalized;
        }

        public static float RotationFromDistance(float distance, float radius)
        {
            return distance / Mathf.Max(0.001f, radius);
        }

        public void SetSupport(bool hasSupport, Vector3 normal)
        {
            supported = hasSupport;
            if (hasSupport && normal.sqrMagnitude > 0.01f) supportNormal = normal.normalized;
        }

        public void ResetTelemetry()
        {
            signedDistance = 0f;
            rotationRadians = 0f;
            angularSpeed = 0f;
            previousPosition = body != null ? body.position : transform.position;
            lastIntegratedPosition = previousPosition;
            initialized = body != null;
        }
    }
}
