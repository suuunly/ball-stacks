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
    ///
    /// Raw measurements twitch: wobbling balls slip in and out of the support
    /// probe, and a jumping tower rests on nothing at all mid-air. Reported
    /// scores are settled: the previous reading holds while the player's base
    /// ball is airborne, and a lower reading is only believed once it has
    /// persisted for the configured grace time. Gains always show instantly.
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
        private readonly Dictionary<PlayerController, PlayerScore> _reportedScores =
            new Dictionary<PlayerController, PlayerScore>();
        private readonly Dictionary<PlayerController, float> _dropPendingSince =
            new Dictionary<PlayerController, float>();
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

                results.Add(SettleScore(player, MeasureStack(player)));
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

        private PlayerScore SettleScore(PlayerController player, PlayerScore measured)
        {
            bool isFirstReading = !_reportedScores.TryGetValue(player, out PlayerScore reported);
            if (isFirstReading) { return AcceptScore(player, measured); }

            // A jumping tower rests on nothing — keep the pre-jump reading
            // until the base ball touches down and a real measure exists.
            if (IsAirborne(player))
            {
                _dropPendingSince.Remove(player);
                return reported;
            }

            bool isGainOrSteady = measured.HeightCentimetres >= reported.HeightCentimetres;
            if (isGainOrSteady)
            {
                _dropPendingSince.Remove(player);
                return AcceptScore(player, measured);
            }

            bool dropJustAppeared = !_dropPendingSince.TryGetValue(player, out float pendingSince);
            if (dropJustAppeared)
            {
                _dropPendingSince[player] = Time.time;
                return reported;
            }

            bool dropHasSettled = Time.time - pendingSince >= _config.ScoreDropGraceSeconds;
            if (!dropHasSettled) { return reported; }

            _dropPendingSince.Remove(player);
            return AcceptScore(player, measured);
        }

        private PlayerScore AcceptScore(PlayerController player, PlayerScore measured)
        {
            _reportedScores[player] = measured;
            return measured;
        }

        private static bool IsAirborne(PlayerController player)
        {
            Ball baseBall = player.ControlledBall;
            if (baseBall == null) { return false; }

            BallOccupancy occupancy = baseBall.Occupancy;
            return occupancy != null && !occupancy.HasSolidFootingBeneath();
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
