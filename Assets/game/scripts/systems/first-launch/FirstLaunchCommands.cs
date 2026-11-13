using Game.Contracts.Session;
using Paowa.Core.Foundation.Services;
using Paowa.DevConsole;
using UnityEngine.Scripting;

namespace Game.Systems.FirstLaunch
{
    /// <summary>Dev console command of the first launch (development builds and the Editor only, like the console).</summary>
    internal static class FirstLaunchCommands
    {
        [Preserve]
        [DevCommand("firstlaunch.reset", "Shows the first-launch screen again after the next splash")]
        private static string ResetFirstLaunch()
        {
            if (!CoreServiceLocator.TryGet(out IFirstLaunch firstLaunch))
            {
                return "The first-launch module is not running.";
            }

            firstLaunch.Reset();
            return "The first-launch screen shows after the next splash.";
        }
    }
}
