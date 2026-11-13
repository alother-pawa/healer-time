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

        /// <summary>Toggle: show hints and tutorials (<see cref="IGameSettings.ShowHints"/>).</summary>
        public const string ShowHints = "gameplay.show-hints";

        /// <summary>Slider: mouse look multiplier (<see cref="IGameSettings.MouseSensitivity"/>).</summary>
        public const string MouseSensitivity = "controls.mouse-sensitivity";

        /// <summary>Slider: stick look multiplier (<see cref="IGameSettings.StickSensitivity"/>).</summary>
        public const string StickSensitivity = "controls.stick-sensitivity";

        /// <summary>Toggle: invert the vertical look axis (<see cref="IGameSettings.InvertY"/>).</summary>
        public const string InvertY = "controls.invert-y";

        /// <summary>Slider: scale of every game canvas, 1 = reference size (<see cref="IGameSettings.UIScale"/>).</summary>
        public const string UIScale = "graphics.ui-scale";

        /// <summary>Slider: text size multiplier of every scaled text, 1 = builder size (<see cref="IGameSettings.TextScale"/>).</summary>
        public const string TextScale = "gameplay.text-size";

        /// <summary>Toggle: drop flashes and pulses (<see cref="IGameSettings.ReduceFlashing"/>).</summary>
        public const string ReduceFlashing = "gameplay.reduce-flashing";

        /// <summary>Choice <see cref="PressModeHold"/>/<see cref="PressModeToggle"/>: how Sprint works (<see cref="IGameSettings.ToggleSprint"/>).</summary>
        public const string SprintMode = "gameplay.sprint-mode";

        /// <summary>Choice <see cref="PressModeHold"/>/<see cref="PressModeToggle"/>: how Crouch works (<see cref="IGameSettings.ToggleCrouch"/>).</summary>
        public const string CrouchMode = "gameplay.crouch-mode";

        /// <summary>Toggle: subtitles on/off (<see cref="IGameSettings.Subtitles"/>); a value only until the game shows subtitles.</summary>
        public const string Subtitles = "gameplay.subtitles";

        /// <summary>Value of <see cref="SprintMode"/>/<see cref="CrouchMode"/>: active while the button is held.</summary>
        public const string PressModeHold = "hold";

        /// <summary>Value of <see cref="SprintMode"/>/<see cref="CrouchMode"/>: each press switches it on or off.</summary>
        public const string PressModeToggle = "toggle";
    }
}
