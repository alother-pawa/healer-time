using Game.Systems.Splash;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Splash sequencing: fade in → hold → fade out → wait for the preload; a skip shortens the animation but never the
    /// preload; early skips are dropped; with animations off the logo appears and disappears at once and the same steps
    /// follow.
    /// </summary>
    public sealed class SplashSequenceTests
    {
        private const float FadeIn = 0.5f;
        private const float Hold = 1f;
        private const float FadeOut = 0.5f;
        private const float SkipDelay = 0.3f;
        private const float Tolerance = 0.0001f;

        private SplashSequence _sequence;

        [SetUp]
        public void SetUp()
        {
            _sequence = new SplashSequence(FadeIn, Hold, FadeOut, SkipDelay);
        }

        [Test]
        public void PlaysEveryStepThenFinishesWhenThePreloadIsDone()
        {
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.FadeIn));
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0f));

            _sequence.Tick(0.25f, false, true);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.FadeIn));
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0.5f).Within(Tolerance));

            _sequence.Tick(0.25f, false, true);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.Hold));
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(1f));

            _sequence.Tick(1f, false, true);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.FadeOut));

            _sequence.Tick(0.25f, false, true);
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0.5f).Within(Tolerance));

            _sequence.Tick(0.25f, false, true);
            Assert.That(_sequence.IsWaitingForPreload, Is.True);
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0f));

            _sequence.Tick(5f, false, true);
            Assert.That(_sequence.IsFinished, Is.False, "The splash never ends before the preload.");

            _sequence.Tick(0f, true, true);
            Assert.That(_sequence.IsFinished, Is.True);
            Assert.That(_sequence.WasSkipped, Is.False);
        }

        [Test]
        public void AnEarlyPreloadDoesNotShortenTheAnimation()
        {
            _sequence.Tick(0.1f, true, true);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.FadeIn));

            _sequence.Tick(FadeIn + Hold, true, true);
            Assert.That(_sequence.IsFinished, Is.False);

            _sequence.Tick(FadeOut, true, true);
            Assert.That(_sequence.IsFinished, Is.True);
        }

        [Test]
        public void SkipStartsTheFadeOutFromTheCurrentAlphaButWaitsForThePreload()
        {
            _sequence.Tick(0.4f, false, true);
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0.8f).Within(Tolerance));

            _sequence.RequestSkip();
            _sequence.Tick(0f, false, true);

            Assert.That(_sequence.WasSkipped, Is.True);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.FadeOut));
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0.8f).Within(Tolerance), "The logo must not jump.");

            _sequence.Tick(FadeOut, false, true);
            Assert.That(_sequence.IsWaitingForPreload, Is.True);
            Assert.That(_sequence.IsFinished, Is.False, "A skip never skips the preload.");

            _sequence.Tick(0f, true, true);
            Assert.That(_sequence.IsFinished, Is.True);
        }

        [Test]
        public void ASkipBeforeTheDelayIsDropped()
        {
            _sequence.Tick(0.1f, false, true);
            _sequence.RequestSkip();
            _sequence.Tick(0.1f, false, true);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.FadeIn));

            // The dropped request does not come back later.
            _sequence.Tick(0.4f, false, true);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.Hold));
            Assert.That(_sequence.WasSkipped, Is.False);
        }

        [Test]
        public void ASkipDuringTheFadeOutChangesNothing()
        {
            _sequence.Tick(FadeIn + Hold + 0.25f, false, true);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.FadeOut));

            _sequence.RequestSkip();
            _sequence.Tick(0f, false, true);

            Assert.That(_sequence.WasSkipped, Is.False);
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void WithAnimationsOffTheLogoAppearsAtOnceHoldsAndDisappearsAtOnce()
        {
            _sequence.Tick(0.016f, false, false);
            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.Hold));
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(1f));

            _sequence.Tick(0.5f, false, false);
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(1f), "The hold is reading time and stays.");

            _sequence.Tick(0.5f, false, false);
            Assert.That(_sequence.IsWaitingForPreload, Is.True);
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0f));

            _sequence.Tick(0f, true, false);
            Assert.That(_sequence.IsFinished, Is.True);
        }

        [Test]
        public void WithAnimationsOffASkipGoesStraightToThePreloadWait()
        {
            _sequence.Tick(0.5f, false, false);
            _sequence.RequestSkip();
            _sequence.Tick(0f, false, false);

            Assert.That(_sequence.IsWaitingForPreload, Is.True);
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0f));
        }

        [Test]
        public void TurningAnimationsOffMidFadeCompletesThatFade()
        {
            _sequence.Tick(0.25f, false, true);
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(0.5f).Within(Tolerance));

            _sequence.Tick(0f, false, false);

            Assert.That(_sequence.Phase, Is.EqualTo(SplashPhase.Hold));
            Assert.That(_sequence.LogoAlpha, Is.EqualTo(1f));
        }

        [Test]
        public void BothModesReachTheSameEndState()
        {
            var animated = new SplashSequence(FadeIn, Hold, FadeOut, SkipDelay);
            var instant = new SplashSequence(FadeIn, Hold, FadeOut, SkipDelay);
            for (int frame = 0; frame < 300; frame++)
            {
                bool preloadDone = frame > 100;
                animated.Tick(0.016f, preloadDone, true);
                instant.Tick(0.016f, preloadDone, false);
            }

            Assert.That(animated.IsFinished, Is.True);
            Assert.That(instant.IsFinished, Is.True);
            Assert.That(animated.LogoAlpha, Is.EqualTo(instant.LogoAlpha));
        }
    }
}
