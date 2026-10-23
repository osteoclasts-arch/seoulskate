using UnityEngine;

namespace SkaterSeoul
{
    public sealed class SkaterSeoulCameraRig : MonoBehaviour
    {
        public Rigidbody target;
        public float lookAhead = 1.8f;
        public float followHeight = 1.72f;
        public float depth = -18f;
        public float damping = 0.16f;

        private Vector3 velocity;
        private Camera view;

        private void Awake()
        {
            view = GetComponent<Camera>();
            if (view != null)
            {
                view.orthographic = true;
                view.orthographicSize = 2.2f;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float direction = target.transform.right.x < 0f ? -1f : 1f;
            Vector3 goal = new Vector3(target.position.x + lookAhead * direction,
                Mathf.Max(followHeight, target.position.y + 1.5f), depth);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity,
                Mathf.Max(0.03f, damping), Mathf.Infinity, Time.deltaTime);
        }
    }
}
