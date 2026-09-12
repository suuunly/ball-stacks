using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Fakes rolling for a ball whose physical rotation must stay fixed. The
    /// rigidbody's rotation is frozen so anything parented to the ball (like
    /// the stack magnet zone) always stays directly above it, and the visual
    /// child is spun from the ball's velocity instead so it still reads as a
    /// rolling ball.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class BallRolling : MonoBehaviour
    {
        private const float RollVelocityThresholdSqr = 0.0001f;

        [SerializeField] private Transform _visual;

        [Tooltip("Physical radius of the ball (m). Used to match spin speed to travel speed.")]
        [SerializeField] private float _radius = 0.5f;

        private Ball _ball;

        private void Awake()
        {
            _ball = GetComponent<Ball>();

            Assert.IsNotNull(_visual, "BallRolling: _visual is not assigned in the inspector!");
            Assert.IsTrue(_radius > 0f, "BallRolling: _radius must be positive!");
        }

        private void Start()
        {
            // Frozen here rather than on the prefab so the constraint and the
            // reason for it live together: zones parented to the ball must not
            // rotate with it.
            _ball.Rigidbody.freezeRotation = true;
        }

        private void Update()
        {
            Vector3 horizontalVelocity = _ball.Rigidbody.linearVelocity;
            horizontalVelocity.y = 0f;

            bool isRolling = horizontalVelocity.sqrMagnitude > RollVelocityThresholdSqr;
            if (!isRolling) { return; }

            Vector3 rollAxis = Vector3.Cross(Vector3.up, horizontalVelocity.normalized);
            float rollDegrees = horizontalVelocity.magnitude / _radius * Mathf.Rad2Deg * Time.deltaTime;
            _visual.Rotate(rollAxis, rollDegrees, Space.World);
        }
    }
}
