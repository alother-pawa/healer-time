using Game.Contracts.Save;
using Game.Foundation.Utilities;
using Paowa.Core.Foundation.Services;
using Paowa.DevConsole;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// Dev console commands of the save slots (development builds and the Editor only, like the console). There is no
    /// checkpoint-with-thumbnail command: the console overlay draws above every canvas and would be in the screenshot;
    /// use the gameplay key Debug/Checkpoint (F5, d-pad down) for that.
    /// </summary>
    internal static class SaveSlotCommands
    {
        [Preserve]
        [DevCommand("goto.gameplay", "Switches to the gameplay scene with the current session (no slot loaded, nothing saved)")]
        private static string GoToGameplay()
        {
            var config = Resources.Load<SaveSlotsConfig>(SaveSlotsConfig.ResourcePath);
            return DevSceneJump.GoTo(config != null ? config.GameplayScene : null);
        }

        // Writes the current slot with its last label and no new thumbnail; it reads no input, so the console's input lock
        // does not matter.
        [Preserve]
        [DevCommand("save.now", "Saves the current slot (no new thumbnail)")]
        private static string SaveNow()
        {
            if (!CoreServiceLocator.TryGet(out ICheckpoint checkpoint))
            {
                return "The save slots module is not running.";
            }

            return checkpoint.Save() ? "Saved." : "Not saved: no current slot, or the write failed (see the log).";
        }
    }
}
