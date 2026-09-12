using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Tunable values for the match flow — how long a round lasts, whether it
    /// starts by itself, and how often the scoreboard recounts the stacks.
    /// Shared by the match systems so the pacing is tuned in one place.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchConfig", menuName = "Ball Stacks/Match Config")]
    public class MatchConfigSO : ScriptableObject
    {
        [Tooltip("Length of a match (seconds). When the countdown reaches zero the match ends and a winner is decided.")]
        [SerializeField] private float _matchDurationSeconds = 120f;

        [Tooltip("When on, the countdown starts by itself on scene start. Turn off to start it manually — e.g. a GameEvent response or UI button calling MatchTimer.BeginMatch().")]
        [SerializeField] private bool _autoStartMatch = true;

        [Tooltip("How often (seconds) the scoreboard recounts every player's stack. Smaller = snappier score updates, more physics probes.")]
        [SerializeField] private float _scorePollInterval = 0.25f;

        [Tooltip("How many centimetres one Unity unit represents on the scoreboard. 100 = true scale (1 unit = 1 m). Lower it if the arena's balls are modelled chunkier than their real-world size.")]
        [Min(1f)]
        [SerializeField] private float _centimetresPerUnit = 100f;

        [Tooltip("How long (seconds) a LOWER stack reading must persist before the scoreboard believes it. Absorbs support-probe flicker from wobbling balls; genuine topples show after this delay. Gains always show instantly.")]
        [Min(0f)]
        [SerializeField] private float _scoreDropGraceSeconds = 0.4f;

        public float MatchDurationSeconds => _matchDurationSeconds;
        public bool AutoStartMatch => _autoStartMatch;
        public float ScorePollInterval => _scorePollInterval;
        public float CentimetresPerUnit => _centimetresPerUnit;
        public float ScoreDropGraceSeconds => _scoreDropGraceSeconds;
    }
}
