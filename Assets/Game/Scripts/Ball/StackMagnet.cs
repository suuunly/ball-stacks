using System.Collections.Generic;
using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Weak magnetic seat on top of a ball. Balls entering the trigger zone are
    /// pulled toward the point directly above the owning ball with a strength
    /// set by where they sit in a hold cone: strongest at the seat, fading
    /// laterally toward the cone wall and vertically toward the cone top along
    /// two designer curves. A carefully balanced stack holds together, sloppy
    /// movement lets balls slip away gradually, and balls falling in from a
    /// jump get a gentle guiding pull. Only horizontal motion is affected —
    /// gravity keeps jumps and landings natural.
    ///
    /// The trigger zone must be centred on the ball so it stays put while the
    /// ball rolls; the cone test decides who actually counts as "resting on top".
    /// </summary>
    public class StackMagnet : MonoBehaviour
    {
        private const float CentreDeadzoneMetres = 0.05f;

        [SerializeField] private StackMagnetConfigSO _config;

        private Ball _ownerBall;
        private SphereCollider _zone;
        private readonly List<Ball> _capturedBalls = new List<Ball>();
        private Vector3 _lastOwnerPosition;
        private Vector3 _ownerVelocity;

        private void Awake()
        {
            AcquireReferences();

            Assert.IsNotNull(_config, "StackMagnet: _config is not assigned in the inspector!");
            Assert.IsNotNull(_ownerBall, "StackMagnet: no Ball component found on this object or its parents!");
            Assert.IsNotNull(_zone, "StackMagnet: no SphereCollider trigger zone on this object!");

            UpdateMagnetState();
        }

        private void OnEnable()
        {
            // A mid-play domain reload (script recompile while testing) wipes
            // every non-serialized field but keeps the component enabled, and
            // OnEnable is the only callback that runs again — recover here.
            AcquireReferences();

            // Restart velocity tracking from here — a stale position from before
            // the magnet was disabled would read as one huge velocity spike.
            _lastOwnerPosition = _ownerBall.Rigidbody.position;
            _ownerVelocity = Vector3.zero;

            bool lostCaptures = _capturedBalls.Count == 0;
            if (lostCaptures)
            {
                // Balls already inside the zone fire no new OnTriggerEnter after
                // a reload, so rebuild the captured list by scanning the zone.
                RecaptureOverlappingBalls();
                UpdateMagnetState();
            }
        }

        private void AcquireReferences()
        {
            if (_ownerBall == null) { _ownerBall = GetComponentInParent<Ball>(); }
            if (_zone == null) { _zone = GetComponent<SphereCollider>(); }
        }

        private void RecaptureOverlappingBalls()
        {
            Vector3 zoneCentre = transform.TransformPoint(_zone.center);
            Vector3 scale = transform.lossyScale;
            float zoneRadius = _zone.radius * Mathf.Max(scale.x, scale.y, scale.z);

            Collider[] overlapping = Physics.OverlapSphere(zoneCentre, zoneRadius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            foreach (Collider other in overlapping)
            {
                TryCapture(other);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Other magnet zones reach into this zone from far away — only solid
            // colliders mean the ball itself is actually here.
            if (other.isTrigger) { return; }

            TryCapture(other);
        }

        private void TryCapture(Collider other)
        {
            Rigidbody otherBody = other.attachedRigidbody;
            bool isForeignBody = otherBody != null && otherBody != _ownerBall.Rigidbody;
            if (!isForeignBody) { return; }
            if (!otherBody.TryGetComponent(out Ball capturedBall)) { return; }
            if (_capturedBalls.Contains(capturedBall)) { return; }

            _capturedBalls.Add(capturedBall);
            UpdateMagnetState();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.isTrigger) { return; }

            Rigidbody otherBody = other.attachedRigidbody;
            bool hasBody = otherBody != null;
            if (!hasBody) { return; }
            if (!otherBody.TryGetComponent(out Ball releasedBall)) { return; }

            _capturedBalls.Remove(releasedBall);
            UpdateMagnetState();
        }

        private void FixedUpdate()
        {
            Assert.IsNotEmpty(_capturedBalls, "StackMagnet: FixedUpdate running with no captured balls!");

            UpdateOwnerVelocity();
            PruneDestroyedBalls();

            foreach (Ball capturedBall in _capturedBalls)
            {
                Vector3 offsetFromOwner = capturedBall.Rigidbody.position - _ownerBall.Rigidbody.position;
                if (!TryGetHoldFraction(offsetFromOwner, out float holdFraction)) { continue; }

                PullTowardRestingPoint(capturedBall, holdFraction);
            }

            UpdateMagnetState();
        }

        private void UpdateOwnerVelocity()
        {
            // Derived from position rather than Rigidbody.linearVelocity so the
            // magnet still carries the stack when the owner is moved without
            // physics velocity (scene-view gizmo drags, teleports, MovePosition).
            Vector3 ownerPosition = _ownerBall.Rigidbody.position;
            Vector3 instantaneousVelocity = (ownerPosition - _lastOwnerPosition) / Time.fixedDeltaTime;
            _lastOwnerPosition = ownerPosition;

            // Smoothed because choppy movement sources teleport the owner only
            // every few physics steps — the raw estimate would read as motion
            // spikes separated by standstill, braking the carried ball.
            float smoothingTime = Mathf.Max(_config.OwnerVelocitySmoothingTime, Time.fixedDeltaTime);
            float blendWeight = Time.fixedDeltaTime / smoothingTime;
            _ownerVelocity = Vector3.Lerp(_ownerVelocity, instantaneousVelocity, blendWeight);
        }

        private bool TryGetHoldFraction(Vector3 offsetFromOwner, out float holdFraction)
        {
            holdFraction = 0f;

            bool isAbove = offsetFromOwner.y > 0f;
            if (!isAbove) { return false; }

            float lateralT = Vector3.Angle(Vector3.up, offsetFromOwner) / _config.ConeHalfAngle;
            float heightT = offsetFromOwner.y / _config.ConeHeight;
            bool isInsideCone = lateralT < 1f && heightT < 1f;
            if (!isInsideCone) { return false; }

            holdFraction = _config.LateralFalloff.Evaluate(lateralT) * _config.HeightFalloff.Evaluate(heightT);
            return true;
        }

        private void PullTowardRestingPoint(Ball capturedBall, float holdFraction)
        {
            Vector3 towardOwnerAxis = _ownerBall.Rigidbody.position - capturedBall.Rigidbody.position;
            towardOwnerAxis.y = 0f;

            // Full strength right on the axis would flip direction every physics
            // step and jitter a perfectly stacked ball; fade the pull in over
            // the last few centimetres instead.
            float centreFade = Mathf.Min(towardOwnerAxis.magnitude / CentreDeadzoneMetres, 1f);
            Vector3 pull = towardOwnerAxis.normalized * (_config.MaxPullAcceleration * holdFraction * centreFade);

            Vector3 relativeVelocity = capturedBall.Rigidbody.linearVelocity - _ownerVelocity;
            relativeVelocity.y = 0f;

            // Damping scales with the hold too — at the cone fringe a ball
            // should roll off freely, not have its escape braked in slow motion.
            Vector3 damping = relativeVelocity * (_config.HorizontalDamping * holdFraction);
            Vector3 totalPull = pull - damping;
            Vector3 cappedPull = Vector3.ClampMagnitude(totalPull, _config.MaxPullAcceleration);

            // Acceleration mode so heavy and light balls feel the same hold strength.
            capturedBall.Rigidbody.AddForce(cappedPull, ForceMode.Acceleration);

            // Newton's third law. Without the counter-force the magnet + contact
            // pair generates net momentum and a leaning stack propels the ball
            // beneath it forever. It also makes a wobbling stack tug the player
            // carrying it — the core balancing mechanic.
            Vector3 counterForce = -cappedPull * capturedBall.Rigidbody.mass;
            _ownerBall.Rigidbody.AddForce(counterForce, ForceMode.Force);
        }

        private void PruneDestroyedBalls()
        {
            _capturedBalls.RemoveAll(static capturedBall => capturedBall == null);
        }

        private void UpdateMagnetState()
        {
            bool hasCapturedBalls = _capturedBalls.Count > 0;
            enabled = hasCapturedBalls;
        }

#if UNITY_EDITOR
        private static readonly Color StrongHoldColour = new Color(0.2f, 1f, 0.4f, 0.9f);
        private static readonly Color WeakHoldColour = new Color(1f, 0.35f, 0.2f, 0.35f);
        private static readonly Color CaptureZoneColour = new Color(0.4f, 0.7f, 1f, 0.15f);
        private const int ConeRingCount = 5;
        private const int RingSegments = 32;
        private const int ConeSlantLineCount = 8;
        private const float MaxDrawableHalfAngle = 89f;

        private void OnDrawGizmosSelected()
        {
            DrawMagnetGizmos();
        }

        /// <summary>
        /// Also called by <see cref="Ball"/> so the cone shows when the ball
        /// root is selected, not just this child.
        /// </summary>
        public void DrawMagnetGizmos()
        {
            AcquireReferences();

            bool canDraw = _config != null && _ownerBall != null;
            if (!canDraw) { return; }

            Vector3 apex = _ownerBall.transform.position;
            DrawCaptureZone();
            DrawHoldCone(apex);
            DrawCapturedBallPulls(apex);
        }

        private void DrawCaptureZone()
        {
            if (_zone == null) { return; }

            Vector3 zoneCentre = transform.TransformPoint(_zone.center);
            Vector3 scale = transform.lossyScale;
            float zoneRadius = _zone.radius * Mathf.Max(scale.x, scale.y, scale.z);

            Gizmos.color = CaptureZoneColour;
            Gizmos.DrawWireSphere(zoneCentre, zoneRadius);
        }

        private void DrawHoldCone(Vector3 apex)
        {
            float halfAngle = Mathf.Min(_config.ConeHalfAngle, MaxDrawableHalfAngle);
            float rimRadius = _config.ConeHeight * Mathf.Tan(halfAngle * Mathf.Deg2Rad);

            for (int ring = 1; ring <= ConeRingCount; ring++)
            {
                float heightT = (float)ring / ConeRingCount;
                float height = _config.ConeHeight * heightT;
                float radius = rimRadius * heightT;
                float strength = _config.HeightFalloff.Evaluate(heightT);

                Gizmos.color = Color.Lerp(WeakHoldColour, StrongHoldColour, strength);
                DrawRing(apex + Vector3.up * height, radius);
            }

            Gizmos.color = Color.Lerp(WeakHoldColour, StrongHoldColour, _config.LateralFalloff.Evaluate(1f));
            Vector3 rimCentre = apex + Vector3.up * _config.ConeHeight;
            for (int line = 0; line < ConeSlantLineCount; line++)
            {
                float angle = line * (2f * Mathf.PI / ConeSlantLineCount);
                Vector3 rimPoint = rimCentre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * rimRadius;
                Gizmos.DrawLine(apex, rimPoint);
            }
        }

        private void DrawCapturedBallPulls(Vector3 apex)
        {
            if (!Application.isPlaying) { return; }

            foreach (Ball capturedBall in _capturedBalls)
            {
                if (capturedBall == null) { continue; }

                Vector3 ballPosition = capturedBall.transform.position;
                Vector3 offsetFromOwner = ballPosition - apex;
                if (!TryGetHoldFraction(offsetFromOwner, out float holdFraction)) { continue; }

                Gizmos.color = Color.Lerp(WeakHoldColour, StrongHoldColour, holdFraction);
                Gizmos.DrawLine(apex, ballPosition);
                UnityEditor.Handles.Label(ballPosition, $"hold {holdFraction:P0}");
            }
        }

        private static void DrawRing(Vector3 centre, float radius)
        {
            Vector3 previousPoint = centre + new Vector3(radius, 0f, 0f);
            for (int segment = 1; segment <= RingSegments; segment++)
            {
                float angle = segment * (2f * Mathf.PI / RingSegments);
                Vector3 nextPoint = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
#endif
    }
}
