using System;
using Game.Contracts.Save;
using Paowa.Core.Foundation.Ticking;
using UnityEngine;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// Default <see cref="IPlaytime"/>: adds the frame's unscaled time while counting and not paused. A single frame adds at
    /// most <see cref="MaxFrameSeconds"/>, so the first frame after a stall (window in the background with the player loop
    /// stopped, a debugger break, a long load hitch) is not counted as play. The module restores the value when a slot loads
    /// and resets it for a new game.
    /// </summary>
    internal sealed class PlaytimeTracker : IPlaytime, ITickable
    {
        /// <summary>Longest frame counted, in seconds.</summary>
        internal const float MaxFrameSeconds = 0.5f;

        private readonly Func<bool> _isPaused;

        /// <param name="isPaused">True while the game is paused (pause service or Core's time scale); null: never paused.</param>
        internal PlaytimeTracker(Func<bool> isPaused)
        {
            _isPaused = isPaused;
        }

        public double Seconds { get; private set; }

        public bool IsCounting { get; private set; }

        public void StartCounting()
        {
            IsCounting = true;
        }

        public void StopCounting()
        {
            IsCounting = false;
        }

        public void Tick(float deltaTime)
        {
            Advance(Time.unscaledDeltaTime);
        }

        /// <summary>Counts <paramref name="unscaledSeconds"/> of one frame when the rules allow it.</summary>
        internal void Advance(float unscaledSeconds)
        {
            if (!IsCounting || (_isPaused != null && _isPaused()) || float.IsNaN(unscaledSeconds) || unscaledSeconds <= 0f)
            {
                return;
            }

            Seconds += Math.Min(unscaledSeconds, MaxFrameSeconds);
        }

        /// <summary>Sets the playtime of a loaded slot (negative or invalid values read as zero).</summary>
        internal void Restore(double seconds)
        {
            Seconds = double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0d ? 0d : seconds;
        }
    }
}
