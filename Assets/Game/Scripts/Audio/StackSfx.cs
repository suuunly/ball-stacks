using System.Collections.Generic;
using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Plays the stack sound effects. Every jump plays the jump clip matching
    /// how many balls ride on the jumper's stack — no stack is a single ball
    /// jumping, a full stack is the whole chorus — and a stack that built up
    /// and then collapses to nothing plays the stack-fall clip. Each play is
    /// slightly pitch-shifted so the small clip set never sounds repetitive.
    /// Stack sizes come from the scoreboard's scores-changed event; this
    /// component never counts balls itself.
    /// </summary>
    public class StackSfx : MonoBehaviour
    {
        [SerializeField] private StackSfxConfigSO _config;
        [SerializeField] private AudioPoolManager _audioPool;

        [Tooltip("Raised by PlayerController with itself as payload whenever a player actually jumps.")]
        [SerializeField] private GameEventPure _onBallJumped;

        [Tooltip("Raised by StackScoreboard with the score list (IReadOnlyList<PlayerScore> payload) whenever any stack changes.")]
        [SerializeField] private GameEventPure _onScoresChanged;

        private readonly Dictionary<PlayerController, int> _stackSizes = new Dictionary<PlayerController, int>();
        private readonly Dictionary<PlayerController, int> _peakStackSizes = new Dictionary<PlayerController, int>();

        private void Awake()
        {
            Assert.IsNotNull(_config, "StackSfx: _config is not assigned in the inspector!");
            Assert.IsNotNull(_audioPool, "StackSfx: _audioPool is not assigned in the inspector!");
            Assert.IsNotNull(_onBallJumped, "StackSfx: _onBallJumped is not assigned in the inspector!");
            Assert.IsNotNull(_onScoresChanged, "StackSfx: _onScoresChanged is not assigned in the inspector!");

            _onBallJumped.OnRaised += HandleBallJumped;
            _onScoresChanged.OnRaised += HandleScoresChanged;
        }

        private void OnDestroy()
        {
            if (_onBallJumped != null) { _onBallJumped.OnRaised -= HandleBallJumped; }
            if (_onScoresChanged != null) { _onScoresChanged.OnRaised -= HandleScoresChanged; }
        }

        private void HandleBallJumped(object payload)
        {
            if (payload is not PlayerController player) { return; }
            if (player.ControlledBall == null) { return; }

            // The scoreboard's last poll may lag the jump by a fraction of a
            // second — close enough for choosing a sound.
            _stackSizes.TryGetValue(player, out int stackSize);

            AudioClip clip = _config.GetJumpClip(stackSize);
            Play(clip, player.ControlledBall.transform.position, _config.JumpVolume);
        }

        private void HandleScoresChanged(object payload)
        {
            if (payload is not IReadOnlyList<PlayerScore> scores) { return; }

            foreach (PlayerScore score in scores)
            {
                TrackStack(score);
            }
        }

        // A collapse sheds balls over several score polls, so comparing
        // consecutive counts would miss it. Track the peak size each stack
        // reaches instead: hitting zero after a real stack existed is the
        // fall, however many polls the tumble took.
        private void TrackStack(PlayerScore score)
        {
            PlayerController player = score.Player;
            _stackSizes[player] = score.BallCount;

            _peakStackSizes.TryGetValue(player, out int peakSize);
            if (score.BallCount > peakSize)
            {
                _peakStackSizes[player] = score.BallCount;
                return;
            }

            bool stackIsGone = score.BallCount == 0;
            if (!stackIsGone) { return; }

            _peakStackSizes[player] = 0;

            bool wasARealStack = peakSize >= _config.MinStackForFall;
            if (!wasARealStack) { return; }
            if (player.ControlledBall == null) { return; }

            Play(_config.StackFallClip, player.ControlledBall.transform.position, _config.FallVolume);
        }

        private void Play(AudioClip clip, Vector3 position, float volume)
        {
            _audioPool.PlayOneShot(clip, position, volume, _config.NextPitch(), _config.SpatialBlend);
        }
    }
}
