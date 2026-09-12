using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Tunable values for the stack magnet — how strongly a ball holds the
    /// ball resting on top of it. The hold is a cone rising from the ball:
    /// strength is strongest at the seat, fading laterally toward the cone
    /// wall and vertically toward the cone top, each along a designer-drawn
    /// curve. Shared by every ball so the feel is tuned in one place.
    /// </summary>
    [CreateAssetMenu(fileName = "StackMagnetConfig", menuName = "Ball Stacks/Stack Magnet Config")]
    public class StackMagnetConfigSO : ScriptableObject
    {
        [Tooltip("Half-angle (degrees) of the hold cone above the ball. A ball tilted outside the cone gets no pull at all.")]
        [SerializeField] private float _coneHalfAngle = 60f;

        [Tooltip("Height (m) of the hold cone above the ball's centre. No pull above this.")]
        [SerializeField] private float _coneHeight = 3f;

        [Tooltip("Pull strength across the cone. X: 0 = central axis, 1 = cone wall. Y: fraction of Max Pull Acceleration.")]
        [SerializeField] private AnimationCurve _lateralFalloff = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.1f));

        [Tooltip("Pull strength along the cone's height. X: 0 = the ball's centre, 1 = the cone top. Y: fraction of Max Pull Acceleration. Keep some strength near the top so balls falling in from a jump are guided onto the stack.")]
        [SerializeField] private AnimationCurve _heightFalloff = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.1f));

        [Tooltip("The most acceleration (m/s²) the magnet can ever apply — the single strength knob. Violent movement the magnet cannot match breaks the stack.")]
        [SerializeField] private float _maxPullAcceleration = 30f;

        [Tooltip("How much of a held ball's weight the magnet carries (0 = none, 1 = full levitation). Sheds cargo weight off the ball beneath so movement stays load-independent. Keep slightly below 1 so residual weight presses the ball into its seat — at exactly 1 the ball is neutrally buoyant and pogos on any bounce. Only applies while the held ball is seated on another ball AND this ball is itself supported — airborne towers fly under true gravity.")]
        [Range(0f, 1f)]
        [SerializeField] private float _weightSupport = 0.9f;

        [Tooltip("How strongly vertical wobble between a seated ball and its carrier is damped. Kills the bounce cycle a weightless seat would otherwise pogo on.")]
        [SerializeField] private float _verticalSeatDamping = 6f;

        [Tooltip("When on, a magnet only holds while its ball is part of a player-controlled tower (the ball itself, or a ball beneath it in the stack, is possessed). Loose balls landing on unclaimed balls simply roll off.")]
        [SerializeField] private bool _requirePlayerAnchor = true;

        [Tooltip("How strongly a CLAIMABLE (unoccupied) ball guides a player's ball onto its seat, as a fraction of normal hold. Makes jump-landings forgiving without making rivals' towers sticky — occupied balls give no help, so rolling off an opponent's stack stays free. 0 = landings are pure skill.")]
        [Range(0f, 1f)]
        [SerializeField] private float _landingAssist = 0.6f;

        [Tooltip("How strongly horizontal wobble relative to the ball below is damped. Higher = settles faster and the ball on top is carried more firmly.")]
        [SerializeField] private float _horizontalDamping = 8f;

        [Tooltip("Seconds over which the carrying ball's velocity is smoothed. Evens out choppy movement sources (editor gizmo drags, teleports) so the ball on top is carried steadily.")]
        [SerializeField] private float _ownerVelocitySmoothingTime = 0.2f;

        public float ConeHalfAngle => _coneHalfAngle;
        public float ConeHeight => _coneHeight;
        public AnimationCurve LateralFalloff => _lateralFalloff;
        public AnimationCurve HeightFalloff => _heightFalloff;
        public float MaxPullAcceleration => _maxPullAcceleration;
        public float WeightSupport => _weightSupport;
        public float VerticalSeatDamping => _verticalSeatDamping;
        public bool RequirePlayerAnchor => _requirePlayerAnchor;
        public float LandingAssist => _landingAssist;
        public float HorizontalDamping => _horizontalDamping;
        public float OwnerVelocitySmoothingTime => _ownerVelocitySmoothingTime;
    }
}
