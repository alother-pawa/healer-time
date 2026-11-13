using Paowa.Gamepad;
namespace Game.Contracts.Settings
{
    /// <summary>
    /// Committed values of the game's own settings (<see cref="GameSettingIds"/>), for gameplay code that only reads them
    /// (camera, input look, canvases; controller vibration is the gamepad package's <c>IRumbleService</c>). Values are cheap to read every frame; only
    /// <see cref="UIScale"/> and <see cref="TextScale"/> have change events (<see cref="UIScaleChangedEvent"/>,
    /// <see cref="TextScaleChangedEvent"/>) because canvases and texts react to them. Without the settings service, or for
    /// an id missing from the catalog, the neutral value documented on each property is returned.
    /// </summary>
    public interface IGameSettings
    {
        /// <summary>Camera shake strength, 0–1 (neutral 1).</summary>
        float CameraShake { get; }

        /// <summary>Whether hints and tutorials show (neutral true).</summary>
        bool ShowHints { get; }

        /// <summary>Mouse look multiplier (neutral 1).</summary>
        float MouseSensitivity { get; }

        /// <summary>Stick look multiplier (neutral 1).</summary>
        float StickSensitivity { get; }

        /// <summary>Whether the vertical look axis is inverted (neutral false).</summary>
        bool InvertY { get; }

        /// <summary>Scale of the game's canvases, 1 = reference size (neutral 1).</summary>
        float UIScale { get; }

        /// <summary>Text size multiplier, 1 = the size the builders set (neutral 1).</summary>
        float TextScale { get; }

        /// <summary>Whether flashes and pulses are dropped (neutral false).</summary>
        bool ReduceFlashing { get; }

        /// <summary>True when each Sprint press switches sprinting on or off; false when sprinting lasts while held (neutral false).</summary>
        bool ToggleSprint { get; }

        /// <summary>True when each Crouch press switches crouching on or off; false when crouching lasts while held (neutral false).</summary>
        bool ToggleCrouch { get; }

        /// <summary>Whether subtitles show (neutral true). Nothing shows subtitles yet: the value is ready for the game's dialogue.</summary>
        bool Subtitles { get; }
    }
}
