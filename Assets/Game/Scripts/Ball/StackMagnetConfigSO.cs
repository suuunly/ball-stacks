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

        [Tooltip("How strongly horizontal wobble relative to the ball below is damped. Higher = settles faster and the ball on top is carried more firmly.")]
        [SerializeField] private float _horizontalDamping = 8f;

        [Tooltip("Seconds over which the carrying ball's velocity is smoothed. Evens out choppy movement sources (editor gizmo drags, teleports) so the ball on top is carried steadily.")]
        [SerializeField] private float _ownerVelocitySmoothingTime = 0.2f;

        public float ConeHalfAngle => _coneHalfAngle;
        public float ConeHeight => _coneHeight;
        public AnimationCurve LateralFalloff => _lateralFalloff;
        public AnimationCurve HeightFalloff => _heightFalloff;
        public float MaxPullAcceleration => _maxPullAcceleration;
        public float HorizontalDamping => _horizontalDamping;
        public float OwnerVelocitySmoothingTime => _ownerVelocitySmoothingTime;
    }
}
