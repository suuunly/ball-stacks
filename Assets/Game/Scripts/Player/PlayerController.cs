using Gaman;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BallStacks
{
    /// <summary>
    /// The player's brain. Drives whichever ball the player currently controls
    /// and shifts that control downward when the ball lands on top of a free
    /// ball: the landed-on ball becomes the new base of the player's tower and
    /// the old ball becomes magnet-held cargo above it. Balls that belong to a
    /// player (controlled, or resting in a chain above a controlled ball) can
    /// never be claimed.
    /// </summary>
    public class PlayerController : MonoBehaviour, IRuntime
    {
        private const float ProbeRadiusScale = 0.9f;
        private const float FallbackBallRadius = 0.5f;

        [SerializeField] private PlayerControlConfigSO _config;
        [SerializeField] private RuntimeSet _players;

        public Ball ControlledBall { get; private set; }

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private CollisionRelay _ballCollisions;
        private float _ballRadius;

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
            if (_jumpAction != null) { _jumpAction.performed -= HandleJumpPerformed; }

            ReleaseControlledBall();
        }

        /// <summary>Hands this player its seat input and the first ball it controls.</summary>
        public void Initialize(InputAction moveAction, InputAction jumpAction, Ball startingBall)
        {
            _moveAction = moveAction;
            _jumpAction = jumpAction;
            _jumpAction.performed += HandleJumpPerformed;

            Possess(startingBall);
        }

        private void FixedUpdate()
        {
            Assert.IsNotNull(ControlledBall, "PlayerController: FixedUpdate running without a controlled ball!");

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            ApplyMovement(moveInput);
        }

        private void ApplyMovement(Vector2 moveInput)
        {
            Rigidbody body = ControlledBall.Rigidbody;
            var pushDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            body.AddForce(pushDirection * _config.MoveAcceleration, ForceMode.Acceleration);

            ClampHorizontalSpeed(body);
        }

        private void ClampHorizontalSpeed(Rigidbody body)
        {
            Vector3 velocity = body.linearVelocity;
            var horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            bool isWithinLimit = horizontalVelocity.sqrMagnitude <= _config.MaxMoveSpeed * _config.MaxMoveSpeed;
            if (isWithinLimit) { return; }

            Vector3 limitedHorizontal = horizontalVelocity.normalized * _config.MaxMoveSpeed;
            body.linearVelocity = new Vector3(limitedHorizontal.x, velocity.y, limitedHorizontal.z);
        }

        private void HandleJumpPerformed(InputAction.CallbackContext context)
        {
            Assert.IsNotNull(ControlledBall, "PlayerController: jump received without a controlled ball!");
            if (!HasSolidFootingBeneath(ControlledBall)) { return; }

            ControlledBall.Rigidbody.AddForce(Vector3.up * _config.JumpSpeed, ForceMode.VelocityChange);
        }

        private void HandleBallCollision(Collision collision)
        {
            Rigidbody otherBody = collision.rigidbody;
            bool hasBody = otherBody != null;
            if (!hasBody) { return; }
            if (!otherBody.TryGetComponent(out Ball landedOnBall)) { return; }
            if (!LandedOnTopOf(collision)) { return; }
            if (!IsBallFree(landedOnBall)) { return; }

            Possess(landedOnBall);
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

        private bool IsBallFree(Ball ball)
        {
            if (IsControlledByAnyPlayer(ball)) { return false; }

            return !IsPartOfAPlayerTower(ball);
        }

        private bool IsControlledByAnyPlayer(Ball ball)
        {
            foreach (IRuntime item in _players.List)
            {
                if (item is PlayerController player && player.ControlledBall == ball) { return true; }
            }

            return false;
        }

        // A ball is part of a tower when the chain of balls physically beneath
        // it ends at some player's controlled ball. Checking the physical chain
        // instead of keeping books means a ball knocked off a tower is free
        // again the moment it no longer rests on that tower.
        private bool IsPartOfAPlayerTower(Ball ball)
        {
            Ball current = ball;
            for (int depth = 0; depth < _config.MaxTowerWalkDepth; depth++)
            {
                Ball ballBeneath = FindBallBeneath(current);
                bool restsOnGround = ballBeneath == null;
                if (restsOnGround) { return false; }
                if (IsControlledByAnyPlayer(ballBeneath)) { return true; }

                current = ballBeneath;
            }

            // Chain deeper than the safety cap — treat as owned rather than
            // letting a player steal the middle of an absurdly tall tower.
            return true;
        }

        private Ball FindBallBeneath(Ball ball)
        {
            bool hitSomething = ProbeBeneath(ball, out RaycastHit hit);
            if (!hitSomething) { return null; }

            Rigidbody bodyBeneath = hit.rigidbody;
            bool hasBody = bodyBeneath != null;
            if (!hasBody) { return null; }

            bodyBeneath.TryGetComponent(out Ball ballBeneath);
            return ballBeneath;
        }

        private bool HasSolidFootingBeneath(Ball ball)
        {
            return ProbeBeneath(ball, out _);
        }

        // Casts a slightly-smaller sphere downward from the ball's centre. The
        // ball's own collider fully overlaps the cast at its start, so physics
        // ignores it and only whatever lies beneath is reported.
        private bool ProbeBeneath(Ball ball, out RaycastHit hit)
        {
            Vector3 origin = ball.Rigidbody.position;
            float probeRadius = _ballRadius * ProbeRadiusScale;
            float probeDistance = (_ballRadius - probeRadius) + _config.GroundProbeDistance;

            return Physics.SphereCast(
                origin, probeRadius, Vector3.down, out hit,
                probeDistance, _config.GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void Possess(Ball ball)
        {
            ReleaseControlledBall();

            ControlledBall = ball;
            _ballRadius = ResolveBallRadius(ball);
            _ballCollisions = GetOrAddCollisionRelay(ball);
            _ballCollisions.CollisionEntered += HandleBallCollision;

            UpdateControlState();
        }

        private void ReleaseControlledBall()
        {
            if (_ballCollisions != null) { _ballCollisions.CollisionEntered -= HandleBallCollision; }

            _ballCollisions = null;
            ControlledBall = null;
        }

        private void UpdateControlState()
        {
            bool hasBallToControl = ControlledBall != null;
            enabled = hasBallToControl;
        }

        private static CollisionRelay GetOrAddCollisionRelay(Ball ball)
        {
            if (ball.TryGetComponent(out CollisionRelay relay)) { return relay; }

            return ball.gameObject.AddComponent<CollisionRelay>();
        }

        private static float ResolveBallRadius(Ball ball)
        {
            SphereCollider[] spheres = ball.GetComponentsInChildren<SphereCollider>();
            foreach (SphereCollider sphere in spheres)
            {
                if (sphere.isTrigger) { continue; }

                return sphere.radius * sphere.transform.lossyScale.x;
            }

            Assert.IsTrue(false, "PlayerController: controlled ball has no solid SphereCollider!");
            return FallbackBallRadius;
        }
    }
}
