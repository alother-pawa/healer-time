namespace Game.Contracts.Save
{
    /// <summary>
    /// Playtime of the game being played. It counts unscaled seconds only between <see cref="StartCounting"/> and
    /// <see cref="StopCounting"/> (gameplay calls them when it starts and ends) and never while the game is paused. Loading a
    /// slot restores the slot's playtime and a new game starts at zero; every slot save stores the current value.
    /// </summary>
    public interface IPlaytime
    {
        /// <summary>Seconds played in the current game.</summary>
        double Seconds { get; }

        /// <summary>True between <see cref="StartCounting"/> and <see cref="StopCounting"/> (paused or not).</summary>
        bool IsCounting { get; }

        /// <summary>Gameplay started: playtime runs while the game is not paused. Calling it again does nothing.</summary>
        void StartCounting();

        /// <summary>Gameplay ended (back to the menu, quitting): playtime stops. Calling it again does nothing.</summary>
        void StopCounting();
    }
}
