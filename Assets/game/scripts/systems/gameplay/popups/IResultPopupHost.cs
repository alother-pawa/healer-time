namespace Game.Systems.Gameplay
{
    /// <summary>What the result popup asks of the run that opened it (its payload).</summary>
    internal interface IResultPopupHost
    {
        RunResult Result { get; }

        void Replay();

        void GoToMenu();
    }
}
