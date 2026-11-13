using UnityEngine;

namespace Game.Contracts.Navigation
{
    /// <summary>Moves between content scenes through Core's scene service; the persistent UI scene covers each switch.</summary>
    public interface IGameNavigator
    {
        /// <summary>True while a scene switch is running.</summary>
        bool IsNavigating { get; }

        /// <summary>Loads a scene that stays loaded across switches (the shared UI scene).</summary>
        Awaitable LoadPersistentAsync(SceneKey scene);

        /// <summary>
        /// Switches to <paramref name="scene"/>; reloads it when it is already open. Returns false without doing anything
        /// while another switch is running (a double-tapped button).
        /// </summary>
        Awaitable<bool> GoToAsync(SceneKey scene);
    }
}
