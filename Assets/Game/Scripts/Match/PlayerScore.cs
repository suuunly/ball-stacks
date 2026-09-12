namespace BallStacks
{
    /// <summary>
    /// One player's current score: the stacked height of the balls sitting on
    /// top of the ball they control, in scoreboard centimetres — plus the raw
    /// ball count for displays that want it. Bigger balls are worth more.
    /// </summary>
    public readonly struct PlayerScore
    {
        public PlayerController Player { get; }
        public int BallCount { get; }
        public int HeightCentimetres { get; }

        public PlayerScore(PlayerController player, int ballCount, int heightCentimetres)
        {
            Player = player;
            BallCount = ballCount;
            HeightCentimetres = heightCentimetres;
        }
    }
}
