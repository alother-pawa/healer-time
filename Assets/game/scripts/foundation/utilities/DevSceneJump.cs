using System;
using Game.Contracts.Navigation;
using Paowa.Core.Foundation.Services;
using Paowa.DevConsole;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Foundation.Utilities
{
    /// <summary>
    /// Scene switch for dev console commands (<c>goto.*</c>). The console turns uGUI navigation off while it is open and
    /// puts its own saved value back when it closes; a switch started from inside a command would let that late restore
    /// land on the next scene. So the console closes first (its value goes back now), navigation is switched on for the
    /// menus (the loading screen records and restores it around the switch), then the switch starts. Nothing is saved.
    /// </summary>
    public static class DevSceneJump
    {
        /// <summary>Starts the switch to <paramref name="scene"/>; returns the line the command prints.</summary>
        public static string GoTo(SceneKey scene)
        {
            if (scene == null)
            {
                return "No scene key is configured for this command.";
            }

            if (!CoreServiceLocator.TryGet(out IGameNavigator navigator))
            {
                return "The scene flow module is not running.";
            }

            if (navigator.IsNavigating)
            {
                return "A scene switch is already running.";
            }

            if (CoreServiceLocator.TryGet(out IDevConsole console))
            {
                console.Close();
            }

            EventSystem current = EventSystem.current;
            if (current != null)
            {
                current.sendNavigationEvents = true;
            }

            _ = RunAsync(navigator, scene);
            return $"Switching to {scene.SceneName} (nothing saved).";
        }

        private static async Awaitable RunAsync(IGameNavigator navigator, SceneKey scene)
        {
            try
            {
                if (!await navigator.GoToAsync(scene))
                {
                    Debug.LogWarning($"[Game] goto: the switch to {scene.SceneName} did not start (another switch is running).");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
