using Game.Foundation.UI;
using NUnit.Framework;

namespace Game.Tests.Editor
{
    /// <summary>Text size math: multiplier applied to the builder size, clamped, invalid multipliers neutral.</summary>
    public sealed class TextScalingTests
    {
        [Test]
        public void MultipliesTheBuilderSize()
        {
            Assert.That(TextScaling.Size(30f, 1f), Is.EqualTo(30f));
            Assert.That(TextScaling.Size(30f, 1.3f), Is.EqualTo(39f).Within(1e-4f));
        }

        [Test]
        public void ClampsTheMultiplier()
        {
            Assert.That(TextScaling.Size(20f, 0.1f), Is.EqualTo(20f * TextScaling.MinScale));
            Assert.That(TextScaling.Size(20f, 9f), Is.EqualTo(20f * TextScaling.MaxScale));
        }

        [Test]
        public void InvalidMultipliersAreNeutral()
        {
            Assert.That(TextScaling.Sanitize(0f), Is.EqualTo(1f));
            Assert.That(TextScaling.Sanitize(-2f), Is.EqualTo(1f));
            Assert.That(TextScaling.Sanitize(float.NaN), Is.EqualTo(1f));
            Assert.That(TextScaling.Sanitize(float.PositiveInfinity), Is.EqualTo(1f));
        }

        [Test]
        public void ZeroBaseSizeStaysZero()
        {
            Assert.That(TextScaling.Size(0f, 1.2f), Is.Zero);
        }
    }
}
