using System.Collections.Generic;

namespace Game.Contracts.Interaction
{
    /// <summary>
    /// What the player is pressing, sampled once per frame from the input service (tier 2 of input tracking): the state of
    /// every tracked action (pressed, value, press count, hold times) and a ring buffer of the most recent presses and
    /// releases with the device that made them. Every press and release is also published as
    /// <see cref="InputActivityRecordedEvent"/>. Views read the state every frame (it never depends on having seen an
    /// event); actions of an inactive or locked context read as released, like the input service.
    /// </summary>
    public interface IInputActivity
    {
        /// <summary>Tracked actions (<c>"Context/Action"</c>), in configuration order.</summary>
        IReadOnlyList<string> Actions { get; }

        /// <summary>
        /// State of <paramref name="action"/> at the last sample; for an action that is not tracked, a state whose
        /// <see cref="InputActionActivity.IsTracked"/> is false.
        /// </summary>
        InputActionActivity GetState(string action);

        /// <summary>Entries in the history, at most <see cref="HistoryCapacity"/>.</summary>
        int HistoryCount { get; }

        /// <summary>Size of the history ring buffer; the oldest entry is dropped when it is full.</summary>
        int HistoryCapacity { get; }

        /// <summary>History entry <paramref name="index"/>, 0 being the newest; throws outside 0..HistoryCount-1.</summary>
        InputActivityEntry GetHistory(int index);

        /// <summary>Increases whenever the history or the press counts change, so a view redraws only on change.</summary>
        int Version { get; }

        /// <summary>Clears the history and resets press counts and hold times (the pressed state is kept).</summary>
        void Clear();
    }
}
