using UnityEngine;

namespace SkaterSeoul
{
    public sealed class SkaterSeoulRiderIkRelay : MonoBehaviour
    {
        public SkaterSeoulRiderMotion motion;

        private void OnAnimatorIK(int layerIndex)
        {
            if (motion == null) return;
            motion.ApplyBodyPose();
            motion.ApplyFeetIk();
        }
    }
}
