using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Tunable values for how a player drives their controlled ball. Shared by
    /// every player so the feel is tuned in one place.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerControlConfig", menuName = "Ball Stacks/Player Control Config")]
    public class PlayerControlConfigSO : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Horizontal acceleration (m/s²) applied while the stick is held.")]
        [SerializeField] private float _moveAcceleration = 20f;

        [Tooltip("Top horizontal speed (m/s) the controls will push the ball to. External shoves can still exceed it.")]
        [SerializeField] private float _maxMoveSpeed = 6f;

        [Tooltip("Braking rate (m/s²) when the stick is released. Keep this WELL below the stack magnet's max pull (its effective hold is weaker than the max across most of the cone) or stopping hurls the carried balls off the tower. 10 vs a max pull of 30 is a good pairing.")]
        [SerializeField] private float _stopDeceleration = 10f;

        [Header("Jumping")]
        [Tooltip("Upward velocity (m/s) added when jumping.")]
        [SerializeField] private float _jumpSpeed = 6.5f;

        [Tooltip("How far below the ball's surface the ground probe reaches (m). Bigger = more forgiving jumps.")]
        [SerializeField] private float _groundProbeDistance = 0.1f;

        [Tooltip("Layers that count as solid footing for jumps — the arena and the balls.")]
        [SerializeField] private LayerMask _groundLayers = ~0;

        [Header("Stack Shift")]
        [Tooltip("How vertical a landing contact must be to count as landing on top of a ball (0 = any touch, 1 = perfectly above).")]
        [Range(0f, 1f)]
        [SerializeField] private float _minLandingNormalY = 0.6f;

        [Tooltip("Safety cap on how many balls the ownership check walks down through when deciding whether a landed-on ball belongs to a player's tower.")]
        [SerializeField] private int _maxTowerWalkDepth = 12;

        [Tooltip("How long (s) the ball must stay seated on a landed-on ball before control shifts into it. Glancing landings that slide off within this time are failed stacking attempts, not a switch.")]
        [SerializeField] private float _claimSettleTime = 0.3f;

        public float MoveAcceleration => _moveAcceleration;
        public float MaxMoveSpeed => _maxMoveSpeed;
        public float StopDeceleration => _stopDeceleration;
        public float ClaimSettleTime => _claimSettleTime;
        public float JumpSpeed => _jumpSpeed;
        public float GroundProbeDistance => _groundProbeDistance;
        public LayerMask GroundLayers => _groundLayers;
        public float MinLandingNormalY => _minLandingNormalY;
        public int MaxTowerWalkDepth => _maxTowerWalkDepth;
    }
}
