using System.Collections.Generic;
using Game.Systems.Settings;
using NUnit.Framework;
using Paowa.Settings;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Stepper and slider arithmetic of the settings rows: left/right clamp at the ends while Submit wraps, a value outside
    /// the options steps to the first (right) or last (left) option, the arrows' state follows the clamp, and slider
    /// positions map one press to one definition step.
    /// </summary>
    public sealed class ChoiceStepperTests
    {
        [Test]
        public void ClampedStepsStopAtTheEnds()
        {
            Assert.That(ChoiceStepper.Step(0, 3, -1, false), Is.EqualTo(0));
            Assert.That(ChoiceStepper.Step(2, 3, 1, false), Is.EqualTo(2));
            Assert.That(ChoiceStepper.Step(1, 3, 1, false), Is.EqualTo(2));
            Assert.That(ChoiceStepper.Step(1, 3, -1, false), Is.EqualTo(0));
        }

        [Test]
        public void WrappingStepsGoAround()
        {
            Assert.That(ChoiceStepper.Step(2, 3, 1, true), Is.EqualTo(0));
            Assert.That(ChoiceStepper.Step(0, 3, -1, true), Is.EqualTo(2));
            Assert.That(ChoiceStepper.Step(0, 1, 1, true), Is.EqualTo(0));
        }

        [Test]
        public void AnUnknownValueStepsToTheFirstOrLastOption()
        {
            Assert.That(ChoiceStepper.Step(-1, 4, 1, false), Is.EqualTo(0));
            Assert.That(ChoiceStepper.Step(-1, 4, -1, false), Is.EqualTo(3));
            Assert.That(ChoiceStepper.Step(-1, 4, 1, true), Is.EqualTo(0));
            Assert.That(ChoiceStepper.Step(7, 4, -1, true), Is.EqualTo(3));
        }

        [Test]
        public void NoOptionsGiveNoIndex()
        {
            Assert.That(ChoiceStepper.Step(0, 0, 1, true), Is.EqualTo(-1));
            Assert.That(ChoiceStepper.CanStep(-1, 0, 1), Is.False);
        }

        [Test]
        public void TheArrowsFollowTheClamp()
        {
            Assert.That(ChoiceStepper.CanStep(0, 3, -1), Is.False);
            Assert.That(ChoiceStepper.CanStep(0, 3, 1), Is.True);
            Assert.That(ChoiceStepper.CanStep(2, 3, 1), Is.False);
            Assert.That(ChoiceStepper.CanStep(2, 3, -1), Is.True);
            Assert.That(ChoiceStepper.CanStep(-1, 3, -1), Is.True, "An unknown value can always step onto an option.");
        }

        [Test]
        public void IndexOfMatchesTheOptionValue()
        {
            var options = new List<SettingOption>
            {
                new SettingOption("1280x720", "1280 × 720", false), null, new SettingOption("1920x1080", "1920 × 1080", false),
            };

            Assert.That(ChoiceStepper.IndexOf(options, "1920x1080"), Is.EqualTo(2));
            Assert.That(ChoiceStepper.IndexOf(options, "2560x1440"), Is.EqualTo(-1));
            Assert.That(ChoiceStepper.IndexOf(options, string.Empty), Is.EqualTo(-1));
        }

        [Test]
        public void SliderPositionsFollowTheDefinitionStep()
        {
            Assert.That(SliderSteps.Count(0f, 1f, 0.05f), Is.EqualTo(20));
            Assert.That(SliderSteps.Count(0.1f, 3f, 0.1f), Is.EqualTo(29));
            Assert.That(SliderSteps.Count(0f, 1f, 0f), Is.EqualTo(SliderSteps.ContinuousSteps));
            Assert.That(SliderSteps.Count(1f, 1f, 0.1f), Is.EqualTo(1));
            Assert.That(SliderSteps.ToValue(10, 0f, 1f, 0.05f, 20), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(SliderSteps.ToIndex(0.8f, 0f, 1f, 0.05f, 20), Is.EqualTo(16));
            Assert.That(SliderSteps.ToIndex(5f, 0f, 1f, 0.05f, 20), Is.EqualTo(20), "Out-of-range values clamp to the ends.");
            Assert.That(SliderSteps.ToIndex(-1f, 0f, 1f, 0.05f, 20), Is.Zero);
            Assert.That(SliderSteps.ToIndex(1.3f, 0.1f, 3f, 0.1f, 29), Is.EqualTo(12));
            Assert.That(SliderSteps.ToValue(10, 0f, 1f, 0f, 20), Is.EqualTo(0.5f).Within(1e-5f), "Continuous positions are even.");
        }

        [Test]
        public void TheLastPositionIsTheMaximumWhenTheRangeIsNotAWholeNumberOfSteps()
        {
            // 0.1..1.0 by 0.4: positions 0.1, 0.5, 0.9, then 1.0.
            int count = SliderSteps.Count(0.1f, 1f, 0.4f);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(SliderSteps.ToValue(2, 0.1f, 1f, 0.4f, count), Is.EqualTo(0.9f).Within(1e-5f));
            Assert.That(SliderSteps.ToValue(count, 0.1f, 1f, 0.4f, count), Is.EqualTo(1f));
            Assert.That(SliderSteps.ToIndex(1f, 0.1f, 1f, 0.4f, count), Is.EqualTo(count));
            Assert.That(SliderSteps.ToIndex(0.9f, 0.1f, 1f, 0.4f, count), Is.EqualTo(2));
        }
    }
}
