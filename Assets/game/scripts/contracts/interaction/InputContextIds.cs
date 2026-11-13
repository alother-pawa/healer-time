namespace Game.Contracts.Interaction
{
    /// <summary>Input contexts of the game's input actions (action map names).</summary>
    public static class InputContextIds
    {
        /// <summary>Tab switching (<c>Menu/TabPrevious</c>, <c>Menu/TabNext</c>) and <c>Menu/Pause</c> while a menu is shown.</summary>
        public const string Menu = "Menu";

        /// <summary>Gameplay controls.</summary>
        public const string Gameplay = "Gameplay";
    }
}
