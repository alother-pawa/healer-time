using Game.Systems.Credits;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>Credits scroll: start delay, auto scroll to the end, manual input and wheel, animations off, clamping.</summary>
    public sealed class CreditsScrollTests
    {
        // 10 px/s auto, 100 px/s manual, 30 px per wheel step, 1 s start delay, 2 s resume delay.
        private static CreditsScroll Create(float content = 500f, float viewport = 100f)
        {
            var scroll = new CreditsScroll(10f, 100f, 30f, 1f, 2f);
            scroll.SetExtent(content, viewport);
            return scroll;
        }

        [Test]
        public void WaitsAtTheTopThenScrollsByItself()
        {
            CreditsScroll scroll = Create();

            scroll.Tick(0.5f, 0f, 0f, true);
            Assert.That(scroll.Offset, Is.Zero);
            scroll.Tick(0.5f, 0f, 0f, true);
            scroll.Tick(1f, 0f, 0f, true);

            Assert.That(scroll.Offset, Is.EqualTo(10f).Within(1e-4f));
        }

        [Test]
        public void StopsAtTheEnd()
        {
            CreditsScroll scroll = Create();
            scroll.Tick(1f, 0f, 0f, true);
            for (int i = 0; i < 100; i++)
            {
                scroll.Tick(1f, 0f, 0f, true);
            }

            Assert.That(scroll.Offset, Is.EqualTo(400f));
            Assert.That(scroll.IsAtEnd, Is.True);
        }

        [Test]
        public void AnimationsOff_StaysAtTheTop_ButScrollsByHand()
        {
            CreditsScroll scroll = Create();
            for (int i = 0; i < 10; i++)
            {
                scroll.Tick(1f, 0f, 0f, false);
            }

            Assert.That(scroll.Offset, Is.Zero);

            scroll.Tick(0.5f, 1f, 0f, false);
            Assert.That(scroll.Offset, Is.EqualTo(50f).Within(1e-4f));
            scroll.Tick(1f, 0f, 0f, false);
            Assert.That(scroll.Offset, Is.EqualTo(50f).Within(1e-4f));
        }

        [Test]
        public void ManualInputHoldsTheAutoScrollForTheResumeDelay()
        {
            CreditsScroll scroll = Create();
            scroll.Tick(1f, 0f, 0f, true);
            scroll.Tick(0.1f, 1f, 0f, true);
            float afterManual = scroll.Offset;

            scroll.Tick(1.5f, 0f, 0f, true);
            Assert.That(scroll.Offset, Is.EqualTo(afterManual));
            scroll.Tick(0.5f, 0f, 0f, true);
            scroll.Tick(1f, 0f, 0f, true);
            Assert.That(scroll.Offset, Is.EqualTo(afterManual + 10f).Within(1e-4f));
        }

        [Test]
        public void WheelStepsAndClampingAtBothEnds()
        {
            CreditsScroll scroll = Create();

            scroll.Tick(0.016f, 0f, 2f, true);
            Assert.That(scroll.Offset, Is.EqualTo(60f).Within(1e-4f));
            scroll.Tick(0.016f, 0f, -5f, true);
            Assert.That(scroll.Offset, Is.Zero);
            scroll.Tick(10f, 1f, 0f, true);
            Assert.That(scroll.Offset, Is.EqualTo(400f));
        }

        [Test]
        public void ShortContentNeverScrolls_AndShrinkingContentClamps()
        {
            CreditsScroll scroll = Create(80f, 100f);
            scroll.Tick(5f, 1f, 3f, true);
            Assert.That(scroll.Offset, Is.Zero);
            Assert.That(scroll.MaxOffset, Is.Zero);

            CreditsScroll tall = Create();
            tall.Tick(10f, 1f, 0f, true);
            tall.SetExtent(250f, 100f);
            Assert.That(tall.Offset, Is.EqualTo(150f));
        }

        [Test]
        public void ResetGoesBackToTheTopWithTheStartDelay()
        {
            CreditsScroll scroll = Create();
            scroll.Tick(10f, 1f, 0f, true);

            scroll.Reset();
            scroll.Tick(0.9f, 0f, 0f, true);

            Assert.That(scroll.Offset, Is.Zero);
        }

        [Test]
        public void InvalidDeltaTimeDoesNotMove()
        {
            CreditsScroll scroll = Create();
            scroll.Tick(float.NaN, 1f, 0f, true);
            scroll.Tick(-1f, 1f, 0f, true);

            Assert.That(scroll.Offset, Is.Zero);
        }
    }
}
