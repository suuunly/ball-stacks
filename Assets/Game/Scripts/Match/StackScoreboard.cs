using System.Collections.Generic;
using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Keeps the live score of every player: the stacked height (in
    /// scoreboard centimetres) of the balls sitting on top of the ball they
    /// control — each ball contributes its own diameter, so bigger balls are
    /// worth more. Recounts on a poll interval by probing which ball supports
    /// which (the same physics truth the stack magnet uses), and raises a
    /// pure event with the score list whenever anything changed — displays
    /// subscribe to that instead of polling themselves.
    ///
    /// A rival's controlled ball parked on someone's tower ends that tower's
    /// measurement — it is the rival's base, not the owner's cargo.
    /// </summary>
    public class StackScoreboard : MonoBehaviour
    {
        private const int MaxTowerWalkDepth = 32;

        [SerializeField] private MatchConfigSO _config;
        [SerializeField] private RuntimeSet _players;
        [SerializeField] private RuntimeSet _balls;

        [Tooltip("Raised with the score list (IReadOnlyList<PlayerScore> payload) whenever any player's stack height changes.")]
        [SerializeField] private GameEventPure _onScoresChanged;

        public IReadOnlyList<PlayerScore> Scores => _scores;

        private List<PlayerScore> _scores = new List<PlayerScore>();
        private List<PlayerScore> _scratchScores = new List<PlayerScore>();
        private readonly Dictionary<Ball, BallOccupancy> _ballAbove = new Dictionary<Ball, BallOccupancy>();
        private float _timeUntilNextPoll;
        private bool _hasCountedOnce;

        private void Awake()
        {
            Assert.IsNotNull(_config, "StackScoreboard: _config is not assigned in the inspector!");
            Assert.IsNotNull(_players, "StackScoreboard: _players is not assigned in the inspector!");
            Assert.IsNotNull(_balls, "StackScoreboard: _balls is not assigned in the inspector!");
            Assert.IsNotNull(_onScoresChanged, "StackScoreboard: _onScoresChanged is not assigned in the inspector!");
        }

        private void Update()
        {
            _timeUntilNextPoll -= Time.deltaTime;

            bool pollIsDue = _timeUntilNextPoll <= 0f;
            if (!pollIsDue) { return; }

            _timeUntilNextPoll = _config.ScorePollInterval;
            Recount();
        }

        /// <summary>Recounts every player's stack right now and raises the scores-changed event if anything moved.</summary>
        public void Recount()
        {
            CountAllStacks(_scratchScores);

            bool scoresChanged = !_hasCountedOnce || !ScoresMatch(_scratchScores, _scores);
            if (!scoresChanged) { return; }

            (_scores, _scratchScores) = (_scratchScores, _scores);
            _hasCountedOnce = true;
            _onScoresChanged.Raise(Scores);
        }

        private void CountAllStacks(List<PlayerScore> results)
        {
            BuildSupportMap();

            results.Clear();
            foreach (IRuntime item in _players.List)
            {
                if (item is not PlayerController player) { continue; }

                results.Add(MeasureStack(player));
            }
        }

        // One support probe per ball answers "who rests on whom" for the whole
        // arena; each tower is then a walk up the resulting chain.
        private void BuildSupportMap()
        {
            _ballAbove.Clear();
            foreach (IRuntime item in _balls.List)
            {
                if (item is not BallOccupancy occupancy) { continue; }
                if (!occupancy.TryGetSupportingBall(out Ball supportingBall)) { continue; }

                _ballAbove[supportingBall] = occupancy;
            }
        }

        // Each stacked ball contributes its physical diameter; the sum stays
        // in Unity units until the very end, then converts to whole
        // scoreboard centimetres in one place.
        private PlayerScore MeasureStack(PlayerController player)
        {
            Ball baseBall = player.ControlledBall;

            bool playerIsStillPickingABall = baseBall == null;
            if (playerIsStillPickingABall) { return new PlayerScore(player, 0, 0); }

            int ballCount = 0;
            float heightUnits = 0f;
            Ball current = baseBall;
            for (int depth = 0; depth < MaxTowerWalkDepth; depth++)
            {
                if (!_ballAbove.TryGetValue(current, out BallOccupancy above)) { break; }
                if (above.IsControlled) { break; }

                ballCount++;
                heightUnits += above.Radius * 2f;
                current = above.Ball;
            }

            int heightCentimetres = Mathf.RoundToInt(heightUnits * _config.CentimetresPerUnit);
            return new PlayerScore(player, ballCount, heightCentimetres);
        }

        private static bool ScoresMatch(List<PlayerScore> left, List<PlayerScore> right)
        {
            if (left.Count != right.Count) { return false; }

            for (int i = 0; i < left.Count; i++)
            {
                bool entryMatches = left[i].Player == right[i].Player
                    && left[i].BallCount == right[i].BallCount
                    && left[i].HeightCentimetres == right[i].HeightCentimetres;
                if (!entryMatches) { return false; }
            }

            return true;
        }
    }
}
