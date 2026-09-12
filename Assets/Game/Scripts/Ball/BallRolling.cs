using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Fakes rolling for a ball whose physical rotation must stay fixed. The
    /// rigidbody's rotation is frozen so a moving ball's surface never drags
    /// the ball stacked on top of it, and the visual child is spun instead.
    ///
    /// While supported, spin follows the velocity relative to the supporting
    /// surface — a ball rolling on the ground spins, a ball carried on a
    /// moving stack does not. While airborne the last spin is kept, mimicking
    /// conserved angular momentum: a ball rolling off a ledge keeps spinning
    /// as it falls.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class BallRolling : MonoBehaviour
    {
        private const float RollVelocityThresholdSqr = 0.0001f;
        private const float SupportNormalMinY = 0.3f;
        private const float SupportGraceSeconds = 0.1f;

        [SerializeField] private Transform _visual;

        [Tooltip("Physical radius of the ball (m). Used to match spin speed to travel speed.")]
        [SerializeField] private float _radius = 0.5f;

        private Ball _ball;
        private Vector3 _spinVelocity;
        private Vector3 _supportVelocity;
        private float _lastSupportTime;

        private void Awake()
        {
            _ball = GetComponent<Ball>();

            Assert.IsNotNull(_visual, "BallRolling: _visual is not assigned in the inspector!");
            Assert.IsTrue(_radius > 0f, "BallRolling: _radius must be positive!");
        }

        private void Start()
        {
            // Frozen here rather than on the prefab so the constraint and the
            // reason for it live together: real rotation would let a moving
            // ball's surface fling off the ball stacked on top of it.
            _ball.Rigidbody.freezeRotation = true;
        }

        private void OnCollisionStay(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);

                // Only surfaces under the ball count as support — walls and
                // balls resting on top of us must not drive the spin.
                bool isSupport = contact.normal.y >= SupportNormalMinY;
                if (!isSupport) { continue; }

                Rigidbody supportBody = collision.rigidbody;
                if (supportBody != null)
                {
                    _supportVelocity = supportBody.linearVelocity;
                }
                else
                {
                    _supportVelocity = Vector3.zero;
                }

                _lastSupportTime = Time.time;
                return;
            }
        }

        private void Update()
        {
            bool isSupported = Time.time - _lastSupportTime <= SupportGraceSeconds;
            if (isSupported)
            {
                Vector3 relativeVelocity = _ball.Rigidbody.linearVelocity - _supportVelocity;
                relativeVelocity.y = 0f;
                _spinVelocity = relativeVelocity;
            }
            // Airborne: _spinVelocity is deliberately left as-is — a free ball
            // keeps the spin it left the surface with.

            bool isRolling = _spinVelocity.sqrMagnitude > RollVelocityThresholdSqr;
            if (!isRolling) { return; }

            Vector3 rollAxis = Vector3.Cross(Vector3.up, _spinVelocity.normalized);
            float rollDegrees = _spinVelocity.magnitude / _radius * Mathf.Rad2Deg * Time.deltaTime;
            _visual.Rotate(rollAxis, rollDegrees, Space.World);
        }
    }
}
