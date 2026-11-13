namespace Game.Systems.Gameplay
{
    /// <summary>What the pause menu popup asks of the gameplay screen that opened it (its payload).</summary>
    internal interface IPauseMenuHost
    {
        /// <summary>Resume button.</summary>
        void Resume();

        /// <summary>The menu's pause action (Start/Escape) was pressed while the menu owned focus.</summary>
        void OnResumePressed();

        /// <summary>Save button.</summary>
        void Save();

        /// <summary>Settings button.</summary>
        void OpenSettings();

        /// <summary>Quit to main menu button.</summary>
        void QuitToMenu();

        /// <summary>Quit to desktop button.</summary>
        void QuitToDesktop();

        /// <summary>The popup was hidden, by any path (it runs before the hide animation).</summary>
        void OnPauseMenuHidden();
    }
}
