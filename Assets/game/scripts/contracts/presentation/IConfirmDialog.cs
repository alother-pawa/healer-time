using UnityEngine;

namespace Game.Contracts.Presentation
{
    /// <summary>
    /// Generic modal confirmation (quit, delete a slot, reset settings). The dialog opens with the cancel button selected
    /// (the safe default); Cancel/Back (Escape, gamepad East) closes it with false. Requests made while a dialog is open
    /// wait in a queue and open one after another, so no answer is lost.
    /// </summary>
    public interface IConfirmDialog
    {
        /// <summary>True while a confirmation is shown.</summary>
        bool IsOpen { get; }

        /// <summary>
        /// Shows <paramref name="request"/>; completes with true when the player confirms, false when they cancel or the
        /// dialog is closed in any other way. The dialog is already closed when the awaiting code resumes.
        /// </summary>
        Awaitable<bool> ConfirmAsync(ConfirmRequest request);
    }
}
