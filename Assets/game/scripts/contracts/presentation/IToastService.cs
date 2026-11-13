namespace Game.Contracts.Presentation
{
    /// <summary>Short non-blocking messages shown one after another by the persistent UI scene.</summary>
    public interface IToastService
    {
        /// <summary>Queues a message; messages requested before the UI scene is loaded are shown once it is.</summary>
        void Show(string message);
    }
}
