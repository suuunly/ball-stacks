using System.Collections.Generic;

namespace BallStacks
{
    /// <summary>
    /// The outcome of a finished match: everyone's final score and whoever
    /// stacked highest. More than one winner means the match is a draw.
    /// </summary>
    public class MatchResult
    {
        public IReadOnlyList<PlayerScore> Winners { get; }
        public IReadOnlyList<PlayerScore> FinalScores { get; }
        public bool IsDraw => Winners.Count > 1;

        public MatchResult(IReadOnlyList<PlayerScore> winners, IReadOnlyList<PlayerScore> finalScores)
        {
            Winners = winners;
            FinalScores = finalScores;
        }
    }
}
