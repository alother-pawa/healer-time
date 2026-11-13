using Game.Systems.SaveSlots;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Playtime counting rules: only between start and stop, never while paused, at most a capped amount per frame (a stalled
    /// frame is not play), invalid frame times ignored, and a restored value is clean.
    /// </summary>
    public sealed class PlaytimeTrackerTests
    {
        private bool _paused;
        private PlaytimeTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _paused = false;
            _tracker = new PlaytimeTracker(() => _paused);
        }

        [Test]
        public void NothingCountsBeforeStart()
        {
            _tracker.Advance(1f);

            Assert.That(_tracker.Seconds, Is.Zero);
            Assert.That(_tracker.IsCounting, Is.False);
        }

        [Test]
        public void FramesCountWhileStartedAndNotAfterStop()
        {
            _tracker.StartCounting();
            _tracker.Advance(0.25f);
            _tracker.Advance(0.25f);
            _tracker.StopCounting();
            _tracker.Advance(0.25f);

            Assert.That(_tracker.Seconds, Is.EqualTo(0.5d).Within(1e-6));
        }

        [Test]
        public void PausedFramesDoNotCount()
        {
            _tracker.StartCounting();
            _tracker.Advance(0.1f);
            _paused = true;
            _tracker.Advance(0.1f);
            _paused = false;
            _tracker.Advance(0.1f);

            Assert.That(_tracker.Seconds, Is.EqualTo(0.2d).Within(1e-6));
            Assert.That(_tracker.IsCounting, Is.True, "Pausing does not stop counting, it only skips time.");
        }

        [Test]
        public void AStalledFrameCountsAtMostTheCap()
        {
            _tracker.StartCounting();
            _tracker.Advance(120f);

            Assert.That(_tracker.Seconds, Is.EqualTo(PlaytimeTracker.MaxFrameSeconds).Within(1e-6));
        }

        [Test]
        public void InvalidFrameTimesAreIgnored()
        {
            _tracker.StartCounting();
            _tracker.Advance(-1f);
            _tracker.Advance(float.NaN);
            _tracker.Advance(0f);

            Assert.That(_tracker.Seconds, Is.Zero);
        }

        [Test]
        public void RestoringSetsTheValueAndRejectsInvalidOnes()
        {
            _tracker.Restore(90d);
            Assert.That(_tracker.Seconds, Is.EqualTo(90d));

            _tracker.Restore(-3d);
            Assert.That(_tracker.Seconds, Is.Zero);

            _tracker.Restore(double.PositiveInfinity);
            Assert.That(_tracker.Seconds, Is.Zero);
        }

        [Test]
        public void WithoutAPauseSourceNothingIsPaused()
        {
            var tracker = new PlaytimeTracker(null);
            tracker.StartCounting();
            tracker.Advance(0.3f);

            Assert.That(tracker.Seconds, Is.EqualTo(0.3d).Within(1e-6));
        }
    }
}
