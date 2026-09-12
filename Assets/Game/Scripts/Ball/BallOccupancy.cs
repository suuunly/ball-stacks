using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Tracks whether this ball is taken: controlled by a player, or resting
    /// on top of another ball. Registers into the balls RuntimeSet so other
    /// systems (like ball selection) can discover every ball in the scene.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class BallOccupancy : MonoBehaviour, IRuntime
    {
        private const float ProbeRadiusScale = 0.9f;
        private const float FallbackRadius = 0.5f;

        [SerializeField] private RuntimeSet _balls;

        [Tooltip("How far below the ball's surface to look for a supporting ball (m).")]
        [SerializeField] private float _probeDistance = 0.1f;

        [Tooltip("Layers the support probe checks — the balls at minimum.")]
        [SerializeField] private LayerMask _probeLayers = ~0;

        public bool IsControlled { get; private set; }
        public bool IsOccupied => IsControlled || IsRestingOnAnotherBall();

        private Ball _ball;
        private float _radius;

        // Lazily re-acquired instead of cached in Awake: a mid-play domain
        // reload (script recompile while testing) wipes non-serialized state
        // and Awake does not run again.
        public Ball Ball
        {
            get
            {
                if (_ball == null) { _ball = GetComponent<Ball>(); }
                return _ball;
            }
        }

        public float Radius
        {
            get
            {
                bool radiusWasLost = _radius <= 0f;
                if (radiusWasLost) { _radius = ResolveRadius(); }
                return _radius;
            }
        }

        private void Awake()
        {
            Assert.IsNotNull(_balls, "BallOccupancy: _balls is not assigned in the inspector!");
        }

        private void OnEnable()
        {
            _balls.Add(this);
        }

        private void OnDisable()
        {
            if (_balls) { _balls.Remove(this); }
        }

        public void SetControlled(bool isControlled)
        {
            IsControlled = isControlled;
        }

        // Casts a slightly-smaller sphere downward from the ball's centre. The
        // ball's own collider fully overlaps the cast at its start, so physics
        // ignores it and only whatever lies beneath is reported.
        /// <summary>Finds the ball this ball currently rests on, if any.</summary>
        public bool TryGetSupportingBall(out Ball supportingBall)
        {
            supportingBall = null;

            float ballRadius = Radius;
            float probeRadius = ballRadius * ProbeRadiusScale;
            float probeDistance = (ballRadius - probeRadius) + _probeDistance;

            bool hitSomething = Physics.SphereCast(
                transform.position, probeRadius, Vector3.down, out RaycastHit hit,
                probeDistance, _probeLayers, QueryTriggerInteraction.Ignore);
            if (!hitSomething) { return false; }

            Rigidbody bodyBeneath = hit.rigidbody;
            bool hasBody = bodyBeneath != null;
            return hasBody && bodyBeneath.TryGetComponent(out supportingBall);
        }

        private bool IsRestingOnAnotherBall()
        {
            return TryGetSupportingBall(out _);
        }

        private float ResolveRadius()
        {
            SphereCollider[] spheres = GetComponentsInChildren<SphereCollider>();
            foreach (SphereCollider sphere in spheres)
            {
                if (sphere.isTrigger) { continue; }

                return sphere.radius * sphere.transform.lossyScale.x;
            }

            Assert.IsTrue(false, "BallOccupancy: ball has no solid SphereCollider!");
            return FallbackRadius;
        }
    }
}
