namespace Game.Systems.Gameplay
{
    /// <summary>What a finished run shows on the result popup.</summary>
    internal readonly struct RunResult
    {
        internal RunResult(int score, int kills, int coins, int best, bool newBest)
        {
            Score = score;
            Kills = kills;
            Coins = coins;
            Best = best;
            NewBest = newBest;
        }

        internal int Score { get; }

        internal int Kills { get; }

        /// <summary>Coins earned this run (already in the wallet).</summary>
        internal int Coins { get; }

        internal int Best { get; }

        internal bool NewBest { get; }
    }
}
