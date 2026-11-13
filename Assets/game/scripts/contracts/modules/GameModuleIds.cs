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

        /// <summary>
        /// Save slot panel, slot metadata, playtime and checkpoints (<c>IPlayMenu</c>, <c>IPlaytime</c>, <c>ICheckpoint</c>).
        /// </summary>
        public const string SaveSlots = "game-save-slots";

        /// <summary>
        /// Per-action input state and recent input history of the gameplay actions (<c>IInputActivity</c>); game-agnostic, a
        /// candidate for a package.
        /// </summary>
        public const string InputActivity = "game-input-activity";

        /// <summary>Gameplay scene support: the pause menu popup.</summary>
        public const string Gameplay = "game-gameplay";

        /// <summary>Credits popup (<c>ICreditsMenu</c>) scrolling the credits data.</summary>
        public const string Credits = "game-credits";

        /// <summary>First-launch flag (<c>IFirstLaunch</c>) and the first-launch screen.</summary>
        public const string FirstLaunch = "game-first-launch";

        /// <summary>
        /// Achievements popup (<c>IAchievementsMenu</c>), the demo's unlock rules, the offline unlock toast and rich presence.
        /// </summary>
        public const string Achievements = "game-achievements";
    }
}
