using Game.Foundation.Utilities;
using Paowa.DevConsole;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Systems.Gameplay
{
    /// <summary>Dev console commands of the gameplay module (development builds and the Editor only, like the console).</summary>
    internal static class GameplayCommands
    {
        [Preserve]
        [DevCommand("goto.menu", "Switches to the main menu without saving")]
        private static string GoToMenu()
        {
            var config = Resources.Load<GameplayConfig>(GameplayConfig.ResourcePath);
            return DevSceneJump.GoTo(config != null ? config.MenuScene : null);
        }
    }
}
