namespace Game.Contracts.Settings
{
    /// <summary>
    /// Ids of the game's own settings (built-in ids are in <c>Paowa.Settings.SettingIds</c> and
    /// <c>Paowa.Desktop.DesktopSettingIds</c>).
    /// A definition with one of these ids in the settings catalog is applied by the <c>game-settings</c> module and read
    /// through <see cref="IGameSettings"/>.
    /// </summary>
    public static class GameSettingIds
    {
        /// <summary>Toggle: animations on/off, backed by <c>IAnimationSettings</c> (its single source of truth).</summary>
        public const string Animations = "gameplay.animations";

        /// <summary>Slider 0–1: camera shake strength (<see cref="IGameSettings.CameraShake"/>).</summary>
        public const string CameraShake = "gameplay.camera-shake";





        /// <summary>Slider: scale of every game canvas, 1 = reference size (<see cref="IGameSettings.UIScale"/>).</summary>
        public const string UIScale = "graphics.ui-scale";

        /// <summary>Slider: text size multiplier of every scaled text, 1 = builder size (<see cref="IGameSettings.TextScale"/>).</summary>
        public const string TextScale = "gameplay.text-size";

        /// <summary>Toggle: drop flashes and pulses (<see cref="IGameSettings.ReduceFlashing"/>).</summary>
        public const string ReduceFlashing = "gameplay.reduce-flashing";





    }
}
