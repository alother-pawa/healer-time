namespace Game.Contracts.Modules
{
    /// <summary>
    /// Initialization order of the game's modules. Core's modules run at 0–80 and plugin modules stay below 1000, so
    /// every game module starts at 1000 and may look up any of their services in <c>Initialize</c>.
    /// </summary>
    public static class GameModulePriorities
    {
        /// <summary>Priority of <see cref="GameModuleIds.Session"/>.</summary>
        public const int Session = 1000;

        /// <summary>Priority of <see cref="GameModuleIds.AnimationSettings"/>.</summary>
        public const int AnimationSettings = 1010;

        /// <summary>Priority of <see cref="GameModuleIds.SceneFlow"/>.</summary>
        public const int SceneFlow = 1020;

        /// <summary>Priority of <see cref="GameModuleIds.UIShell"/>.</summary>
        public const int UIShell = 1030;

        /// <summary>Priority of <see cref="GameModuleIds.Settings"/>.</summary>
        public const int Settings = 1040;

        /// <summary>Priority of <see cref="GameModuleIds.RenderSettings"/>.</summary>
        public const int RenderSettings = 1050;

        /// <summary>Priority of <see cref="GameModuleIds.Gameplay"/>.</summary>
        public const int Gameplay = 1080;
    }
}
