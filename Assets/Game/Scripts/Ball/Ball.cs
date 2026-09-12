using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Identity component for a stackable ball. Systems recognise balls by this
    /// component (never by tag) and reach the physics body through it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Ball : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private BallOccupancy _occupancy;

        // Lazily re-acquired instead of cached in Awake: a mid-play domain
        // reload (script recompile while testing) wipes the cache and Awake
        // does not run again.
        public Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null) { _rigidbody = GetComponent<Rigidbody>(); }
                return _rigidbody;
            }
        }

        public BallOccupancy Occupancy
        {
            get
            {
                if (_occupancy == null) { _occupancy = GetComponent<BallOccupancy>(); }
                return _occupancy;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // The magnet lives on a child, but people select the ball root —
            // forward so the hold cone shows either way.
            StackMagnet magnet = GetComponentInChildren<StackMagnet>();
            if (magnet != null) { magnet.DrawMagnetGizmos(); }
        }
#endif
    }
}
