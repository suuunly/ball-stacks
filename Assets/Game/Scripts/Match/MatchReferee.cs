using System.Collections.Generic;
using Gaman;
using UnityEngine;

namespace BallStacks
{
    /// <summary>
    /// Decides the winner when the match ends: takes one final authoritative
    /// recount from the scoreboard, finds the tallest stack in centimetres,
    /// and raises the winner-decided pure event with a
    /// <see cref="MatchResult"/> payload. Every player tied for the tallest
    /// stack is a winner — more than one means a draw.
    /// </summary>
    public class MatchReferee : MonoBehaviour
    {
        [SerializeField] private MatchTimer _timer;
        [SerializeField] private StackScoreboard _scoreboard;

        [Tooltip("Raised once at match end with the final result (MatchResult payload).")]
        [SerializeField] private GameEventPure _onWinnerDecided;

        private void Awake()
        {
            Assert.IsNotNull(_timer, "MatchReferee: _timer is not assigned in the inspector!");
            Assert.IsNotNull(_scoreboard, "MatchReferee: _scoreboard is not assigned in the inspector!");
            Assert.IsNotNull(_onWinnerDecided, "MatchReferee: _onWinnerDecided is not assigned in the inspector!");

            _timer.MatchEnded += HandleMatchEnded;
        }

        private void OnDestroy()
        {
            if (_timer != null) { _timer.MatchEnded -= HandleMatchEnded; }
        }

        private void HandleMatchEnded()
        {
            _scoreboard.Recount();

            MatchResult result = BuildResult(_scoreboard.Scores);
            _onWinnerDecided.Raise(result);
        }

        private static MatchResult BuildResult(IReadOnlyList<PlayerScore> finalScores)
        {
            var winners = new List<PlayerScore>();

            int tallestCentimetres = 0;
            foreach (PlayerScore score in finalScores)
            {
                if (score.HeightCentimetres > tallestCentimetres) { tallestCentimetres = score.HeightCentimetres; }
            }

            foreach (PlayerScore score in finalScores)
            {
                if (score.HeightCentimetres == tallestCentimetres) { winners.Add(score); }
            }

            return new MatchResult(winners, finalScores);
        }
    }
}
