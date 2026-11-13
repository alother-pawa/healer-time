using Game.Systems.Gameplay;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>Hold-to-toggle state: hold follows the button, toggle flips per press, mode switches start from the button.</summary>
    public sealed class HoldToggleTests
    {
        [Test]
        public void HoldMode_IsActiveOnlyWhileHeld()
        {
            var state = new HoldToggle();

            state.Tick(false, true, true);
            Assert.That(state.IsActive, Is.True);
            state.Tick(false, false, true);
            Assert.That(state.IsActive, Is.True);
            state.Tick(false, false, false);
            Assert.That(state.IsActive, Is.False);
        }

        [Test]
        public void ToggleMode_FlipsOnEachPress_AndIgnoresRelease()
        {
            var state = new HoldToggle();
            state.Tick(true, false, false);

            state.Tick(true, true, true);
            Assert.That(state.IsActive, Is.True);
            state.Tick(true, false, false);
            Assert.That(state.IsActive, Is.True, "Releasing keeps it on.");
            state.Tick(true, true, true);
            Assert.That(state.IsActive, Is.False);
        }

        [Test]
        public void SwitchingTheMode_StartsFromTheButton()
        {
            var state = new HoldToggle();
            state.Tick(true, false, false);
            state.Tick(true, true, true);
            state.Tick(true, false, false);
            Assert.That(state.IsActive, Is.True);

            state.Tick(false, false, false);
            Assert.That(state.IsActive, Is.False, "A latched toggle does not survive the switch to hold.");

            state.Tick(true, false, true);
            Assert.That(state.IsActive, Is.True, "Held while switching to toggle: on until the next press.");
            state.Tick(true, true, true);
            Assert.That(state.IsActive, Is.False);
        }

        [Test]
        public void ResetTurnsItOff()
        {
            var state = new HoldToggle();
            state.Tick(true, false, false);
            state.Tick(true, true, true);

            state.Reset();

            Assert.That(state.IsActive, Is.False);
        }
    }
}
