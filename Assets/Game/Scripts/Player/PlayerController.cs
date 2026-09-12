using Gaman;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BallStacks
{
    /// <summary>
    /// The player's brain. Drives whichever ball the player currently controls
    /// and shifts that control downward when the ball lands on top of an
    /// unoccupied ball: the landed-on ball becomes the new base of the
    /// player's tower and the old ball becomes magnet-held cargo above it.
    /// Occupied balls (controlled by anyone, or resting on another ball) can
    /// never be claimed.
    /// </summary>
    public class PlayerController : MonoBehaviour, IRuntime
    {
        private const float ProbeRadiusScale = 0.9f;
        private const float StopInputThresholdSqr = 0.01f;

        [SerializeField] private PlayerControlConfigSO _config;
        [SerializeField] private RuntimeSet _players;

        public Ball ControlledBall { get; private set; }

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private Color _auraColor;
        private BallOccupancy _controlledOccupancy;
        private CollisionRelay _ballCollisions;
        private BallOccupancy _pendingClaim;
        private float _pendingClaimSeatedTime;

        private void Awake()
        {
            Assert.IsNotNull(_config, "PlayerController: _config is not assigned in the inspector!");
            Assert.IsNotNull(_players, "PlayerController: _players is not assigned in the inspector!");

            _players.Add(this);
            UpdateControlState();
        }

        private void OnDestroy()
        {
            if (_players != null) { _players.Remove(this); }

            ReleaseControlledBall();
        }

        /// <summary>Hands this player its seat input and colour. Ball control starts at <see cref="Possess"/>.</summary>
        public void Initialize(InputAction moveAction, InputAction jumpAction, Color auraColor)
        {
            _moveAction = moveAction;
            _jumpAction = jumpAction;
            _auraColor = auraColor;
        }

        /// <summary>Takes control of a ball, releasing whichever ball was controlled before.</summary>
        public void Possess(Ball ball)
        {
            ReleaseControlledBall();
            CancelPendingClaim();

            ControlledBall = ball;
            _controlledOccupancy = ResolveOccupancy(ball);
            _controlledOccupancy.SetControlled(true);
            _ballCollisions = GetOrAddCollisionRelay(ball);
            _ballCollisions.CollisionEntered += HandleBallCollision;
            _ballCollisions.CollisionStayed += HandleBallCollision;
            _jumpAction.performed += HandleJumpPerformed;

            // Keep the player's aura on the ball they drive, so everyone can
            // see at a glance which ball belongs to whom.
            if (ball.TryGetComponent(out BallAura aura)) { aura.ShowControlled(_auraColor); }

            UpdateControlState();
        }

        private void FixedUpdate()
        {
            Assert.IsNotNull(ControlledBall, "PlayerController: FixedUpdate running without a controlled ball!");

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            ApplyMovement(moveInput);
            UpdatePendingClaim();
        }

        // Velocity command instead of force: the horizontal velocity ramps
        // toward the stick's target at a fixed rate, so friction from carried
        // cargo and the stack magnet's counter-force cannot eat into the
        // player's acceleration — movement feels the same at any tower height.
        private void ApplyMovement(Vector2 moveInput)
        {
            Rigidbody body = ControlledBall.Rigidbody;
            Vector3 velocity = body.linearVelocity;
            var horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            var targetVelocity = new Vector3(moveInput.x, 0f, moveInput.y) * _config.MaxMoveSpeed;

            // Stopping brakes gentler than accelerating: a full-rate stop
            // out-brakes the stack magnet and hurls carried balls off, so a
            // released stick eases the tower to a halt instead.
            float rampRate = _config.MoveAcceleration;
            bool isStopping = targetVelocity.sqrMagnitude < StopInputThresholdSqr;
            if (isStopping) { rampRate = _config.StopDeceleration; }

            float velocityStep = rampRate * Time.fixedDeltaTime;
            Vector3 rampedHorizontal = Vector3.MoveTowards(horizontalVelocity, targetVelocity, velocityStep);
            body.linearVelocity = new Vector3(rampedHorizontal.x, velocity.y, rampedHorizontal.z);
        }

        private void HandleJumpPerformed(InputAction.CallbackContext context)
        {
            Assert.IsNotNull(ControlledBall, "PlayerController: jump received without a controlled ball!");
            if (!HasSolidFootingBeneath()) { return; }

            JumpWithTower();
        }

        // The whole tower jumps together: giving only the carrier the jump
        // velocity slams it into its own cargo and momentum-sharing eats most
        // of the launch. Boosting the chain above keeps relative velocities
        // intact, so the stack rises as one. The walk stops at another
        // player's controlled ball — rivals don't get free launches.
        private void JumpWithTower()
        {
            Vector3 jumpVelocity = Vector3.up * _config.JumpSpeed;

            Ball current = ControlledBall;
            for (int depth = 0; depth <= _config.MaxTowerWalkDepth; depth++)
            {
                current.Rigidbody.AddForce(jumpVelocity, ForceMode.VelocityChange);

                if (!TryGetBallInDirection(current, Vector3.up, out Ball ballAbove)) { break; }
                if (ballAbove.Occupancy != null && ballAbove.Occupancy.IsControlled) { break; }

                current = ballAbove;
            }
        }

        // Landing on a claimable ball only STARTS a claim — control shifts
        // once the ball has stayed seated on it for the settle time. A
        // glancing landing that slides off is a failed stacking attempt and
        // the player keeps their current ball.
        //
        // Fired from both CollisionEntered and CollisionStayed: after a hop
        // off a nearby ball, physics can keep the old contact pair alive, so
        // landing back on top never raises a fresh enter — only stay.
        private void HandleBallCollision(Collision collision)
        {
            Rigidbody otherBody = collision.rigidbody;
            bool hasBody = otherBody != null;
            if (!hasBody) { return; }
            if (!otherBody.TryGetComponent(out Ball _)) { return; }
            if (!LandedOnTopOf(collision)) { return; }
            if (!otherBody.TryGetComponent(out BallOccupancy occupancy)) { return; }
            if (occupancy.IsOccupied) { return; }

            // Stay events repeat every physics step — re-arming would reset
            // the settle timer forever and the claim would never confirm.
            bool alreadyTrackingThisBall = _pendingClaim == occupancy;
            if (alreadyTrackingThisBall) { return; }

            _pendingClaim = occupancy;
            _pendingClaimSeatedTime = 0f;
        }

        private void UpdatePendingClaim()
        {
            if (_pendingClaim == null) { return; }

            bool stillSeatedOnCandidate = TryGetBallBeneath(out Ball ballBeneath) && ballBeneath == _pendingClaim.Ball;
            if (!stillSeatedOnCandidate)
            {
                CancelPendingClaim();
                return;
            }

            _pendingClaimSeatedTime += Time.fixedDeltaTime;
            bool hasSettled = _pendingClaimSeatedTime >= _config.ClaimSettleTime;
            if (!hasSettled) { return; }

            if (_pendingClaim.IsOccupied)
            {
                CancelPendingClaim();
                return;
            }

            Ball claimedBall = _pendingClaim.Ball;
            CancelPendingClaim();
            Possess(claimedBall);
        }

        private void CancelPendingClaim()
        {
            _pendingClaim = null;
            _pendingClaimSeatedTime = 0f;
        }

        private bool TryGetBallBeneath(out Ball ballBeneath)
        {
            return TryGetBallInDirection(ControlledBall, Vector3.down, out ballBeneath);
        }

        private bool TryGetBallInDirection(Ball fromBall, Vector3 direction, out Ball foundBall)
        {
            foundBall = null;

            bool hitSomething = ProbeFromBall(fromBall, direction, out RaycastHit hit);
            if (!hitSomething) { return false; }

            Rigidbody hitBody = hit.rigidbody;
            bool hasBody = hitBody != null;
            if (!hasBody) { return false; }

            return hitBody.TryGetComponent(out foundBall);
        }

        private bool LandedOnTopOf(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                // The contact normal points toward our ball, so a mostly-up
                // normal means the other ball is beneath us.
                bool contactIsBeneath = collision.GetContact(i).normal.y >= _config.MinLandingNormalY;
                if (contactIsBeneath) { return true; }
            }

            return false;
        }

        private bool HasSolidFootingBeneath()
        {
            return ProbeFromBall(ControlledBall, Vector3.down, out _);
        }

        // Casts a slightly-smaller sphere outward from the ball's centre. The
        // ball's own collider fully overlaps the cast at its start, so physics
        // ignores it and only whatever lies just beyond its surface is reported.
        private bool ProbeFromBall(Ball fromBall, Vector3 direction, out RaycastHit hit)
        {
            float ballRadius = _controlledOccupancy.Radius;
            float probeRadius = ballRadius * ProbeRadiusScale;
            float probeDistance = (ballRadius - probeRadius) + _config.GroundProbeDistance;

            return Physics.SphereCast(
                fromBall.Rigidbody.position, probeRadius, direction, out hit,
                probeDistance, _config.GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void ReleaseControlledBall()
        {
            if (_ballCollisions != null)
            {
                _ballCollisions.CollisionEntered -= HandleBallCollision;
                _ballCollisions.CollisionStayed -= HandleBallCollision;
            }
            if (_controlledOccupancy != null) { _controlledOccupancy.SetControlled(false); }
            if (_jumpAction != null && ControlledBall != null) { _jumpAction.performed -= HandleJumpPerformed; }
            if (ControlledBall != null && ControlledBall.TryGetComponent(out BallAura aura)) { aura.Hide(); }

            _ballCollisions = null;
            _controlledOccupancy = null;
            ControlledBall = null;
        }

        private void UpdateControlState()
        {
            bool hasBallToControl = ControlledBall != null;
            enabled = hasBallToControl;
        }

        private static BallOccupancy ResolveOccupancy(Ball ball)
        {
            ball.TryGetComponent(out BallOccupancy occupancy);
            Assert.IsNotNull(occupancy, "PlayerController: possessed ball has no BallOccupancy component!");
            return occupancy;
        }

        private static CollisionRelay GetOrAddCollisionRelay(Ball ball)
        {
            if (ball.TryGetComponent(out CollisionRelay relay)) { return relay; }

            return ball.gameObject.AddComponent<CollisionRelay>();
        }
    }
}
