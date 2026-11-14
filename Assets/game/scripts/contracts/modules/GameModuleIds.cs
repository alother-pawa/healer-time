namespace Game.Contracts.Modules
{
    /// <summary>
    /// Ids of the game's Core modules, so a module can declare <c>DependsOn</c> another without referencing its assembly.
    /// </summary>
    public static class GameModuleIds
    {
        /// <summary>Cross-scene session data (<c>ISessionContext</c>).</summary>
        public const string Session = "game-session";

        /// <summary>Global animation switch (<c>IAnimationSettings</c>).</summary>
        public const string AnimationSettings = "game-animation-settings";

        /// <summary>Scene navigation (<c>IGameNavigator</c>).</summary>
        public const string SceneFlow = "game-scene-flow";

        /// <summary>
        /// Persistent UI scene: root canvas, loading transition, toasts (<c>IToastService</c>) and confirmation dialog
        /// (<c>IConfirmDialog</c>).
        /// </summary>
        public const string UIShell = "game-ui-shell";

        /// <summary>Settings popup and the game's setting appliers (<c>ISettingsMenu</c>, <c>IGameSettings</c>).</summary>
        public const string Settings = "game-settings";

        /// <summary>Render scale and brightness on Universal RP (compiled only when URP is installed).</summary>
        public const string RenderSettings = "game-render-settings";

        /// <summary>Heroes, healers and the player's choice of each (<c>IRoster</c>).</summary>
        public const string Roster = "game-roster";

        /// <summary>Gameplay scene support: the pause menu and result popups.</summary>
        public const string Gameplay = "game-gameplay";
    }
}
