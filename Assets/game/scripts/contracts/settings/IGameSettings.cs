namespace Game.Contracts.Settings
{
    /// <summary>
    /// Committed values of the game's own settings (<see cref="GameSettingIds"/>), for gameplay code that only reads them
    /// (camera shake, effects, canvases). Values are cheap to read every frame; only
    /// <see cref="UIScale"/> and <see cref="TextScale"/> have change events (<see cref="UIScaleChangedEvent"/>,
    /// <see cref="TextScaleChangedEvent"/>) because canvases and texts react to them. Without the settings service, or for
    /// an id missing from the catalog, the neutral value documented on each property is returned.
    /// </summary>
    public interface IGameSettings
    {
        /// <summary>Camera shake strength, 0–1 (neutral 1).</summary>
        float CameraShake { get; }





        /// <summary>Scale of the game's canvases, 1 = reference size (neutral 1).</summary>
        float UIScale { get; }

        /// <summary>Text size multiplier, 1 = the size the builders set (neutral 1).</summary>
        float TextScale { get; }

        /// <summary>Whether flashes and pulses are dropped (neutral false).</summary>
        bool ReduceFlashing { get; }



    }
}
