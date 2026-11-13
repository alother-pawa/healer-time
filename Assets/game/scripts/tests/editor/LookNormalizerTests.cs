using Game.Systems.Gameplay;
using NUnit.Framework;
using Paowa.Input;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Look normalization: mouse pixels scale per pixel and ignore the frame time; a stick scales per second with the frame
    /// time, after a radial dead zone and response curve; each sensitivity applies to its own source only; invert Y flips
    /// both; invalid input, a paused frame and negative sensitivity turn nothing; the source follows the device in use; the
    /// demo angle is clamped per axis.
    /// </summary>
    public sealed class LookNormalizerTests
    {
        private const float Tolerance = 1e-4f;

        // 0.1°/px, 180°/s, inner dead zone 0.2, outer 1, linear.
        private static readonly LookTuning Tuning = new LookTuning(0.1f, 180f, 0.2f, 1f, 1f);

        [Test]
        public void MousePixelsScalePerPixelAndIgnoreTheFrameTime()
        {
            Vector2 slow = LookNormalizer.Normalize(new Vector2(10f, -20f), LookSource.Mouse, 1f / 30f, Tuning, 2f, 1f, false);
            Vector2 fast = LookNormalizer.Normalize(new Vector2(10f, -20f), LookSource.Mouse, 1f / 240f, Tuning, 2f, 1f, false);

            Assert.That(slow.x, Is.EqualTo(2f).Within(Tolerance));
            Assert.That(slow.y, Is.EqualTo(-4f).Within(Tolerance));
            Assert.That(fast, Is.EqualTo(slow), "A delta is a distance: the frame rate does not change it.");
        }

        [Test]
        public void AStickScalesPerSecondTimesTheFrameTime()
        {
            Vector2 degrees = LookNormalizer.Normalize(new Vector2(1f, 0f), LookSource.Stick, 0.5f, Tuning, 1f, 2f, false);

            Assert.That(degrees.x, Is.EqualTo(180f).Within(Tolerance), "Full deflection, 180°/s × 2 × 0.5 s.");
            Assert.That(degrees.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void EachSensitivityScalesOnlyItsOwnSource()
        {
            Vector2 mouse = LookNormalizer.Normalize(new Vector2(10f, 0f), LookSource.Mouse, 0.1f, Tuning, 1f, 3f, false);
            Vector2 stick = LookNormalizer.Normalize(new Vector2(1f, 0f), LookSource.Stick, 0.1f, Tuning, 3f, 1f, false);

            Assert.That(mouse.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(stick.x, Is.EqualTo(18f).Within(Tolerance));
        }

        [Test]
        public void TheStickDeadZoneRescalesTheRestOfTheRange()
        {
            Assert.That(LookNormalizer.ApplyStickCurve(new Vector2(0.15f, 0f), Tuning), Is.EqualTo(Vector2.zero));
            Vector2 half = LookNormalizer.ApplyStickCurve(new Vector2(0f, 0.6f), Tuning);
            Assert.That(half.y, Is.EqualTo(0.5f).Within(Tolerance), "0.6 is half-way between 0.2 and 1.");
            Assert.That(half.x, Is.EqualTo(0f).Within(Tolerance), "The direction is kept.");
            Assert.That(LookNormalizer.ApplyStickCurve(new Vector2(0f, 1.2f), Tuning).y, Is.EqualTo(1f).Within(Tolerance));

            var curved = new LookTuning(0.1f, 180f, 0.2f, 1f, 2f);
            Assert.That(LookNormalizer.ApplyStickCurve(new Vector2(0.6f, 0f), curved).x, Is.EqualTo(0.25f).Within(Tolerance));
        }

        [Test]
        public void InvertYFlipsTheVerticalAxisOfBothSources()
        {
            Vector2 mouse = LookNormalizer.Normalize(new Vector2(10f, 10f), LookSource.Mouse, 0.1f, Tuning, 1f, 1f, true);
            Vector2 stick = LookNormalizer.Normalize(new Vector2(0f, 1f), LookSource.Stick, 0.1f, Tuning, 1f, 1f, true);

            Assert.That(mouse.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(mouse.y, Is.EqualTo(-1f).Within(Tolerance));
            Assert.That(stick.y, Is.EqualTo(-18f).Within(Tolerance));
        }

        [Test]
        public void InvalidInputPausedFramesAndNegativeSensitivityTurnNothing()
        {
            Assert.That(LookNormalizer.Normalize(new Vector2(1f, 0f), LookSource.Stick, 0f, Tuning, 1f, 1f, false), Is.EqualTo(Vector2.zero));
            Assert.That(LookNormalizer.Normalize(new Vector2(1f, 0f), LookSource.Stick, float.NaN, Tuning, 1f, 1f, false), Is.EqualTo(Vector2.zero));
            Assert.That(LookNormalizer.Normalize(new Vector2(float.NaN, 0f), LookSource.Mouse, 0.1f, Tuning, 1f, 1f, false), Is.EqualTo(Vector2.zero));
            Assert.That(LookNormalizer.Normalize(new Vector2(5f, 0f), LookSource.Mouse, 0.1f, Tuning, -1f, 1f, false), Is.EqualTo(Vector2.zero));
            Assert.That(LookNormalizer.Normalize(new Vector2(5f, 0f), LookSource.Mouse, 0.1f, null, 1f, 1f, false), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TheSourceFollowsTheDeviceInUse()
        {
            Vector2 small = new Vector2(0.5f, 0.5f);
            Assert.That(LookNormalizer.SourceOf(InputDeviceKind.KeyboardMouse, small), Is.EqualTo(LookSource.Mouse));
            Assert.That(LookNormalizer.SourceOf(InputDeviceKind.Gamepad, small), Is.EqualTo(LookSource.Stick));
            Assert.That(LookNormalizer.SourceOf(InputDeviceKind.Other, small), Is.EqualTo(LookSource.Stick));
            Assert.That(LookNormalizer.SourceOf(InputDeviceKind.None, small), Is.EqualTo(LookSource.Mouse));
            Assert.That(LookNormalizer.SourceOf(InputDeviceKind.Gamepad, new Vector2(1f, 0f)), Is.EqualTo(LookSource.Stick),
                "Full deflection is still a stick.");
        }

        [Test]
        public void AValueLargerThanAStickIsAMouseDeltaEvenWhileTheGamepadIsTheDeviceInUse()
        {
            // Core switches the device to keyboard and mouse only after 3 px; a 2 px move right after the gamepad.
            Assert.That(LookNormalizer.SourceOf(InputDeviceKind.Gamepad, new Vector2(2f, 0f)), Is.EqualTo(LookSource.Mouse));
            Assert.That(LookNormalizer.SourceOf(InputDeviceKind.Gamepad, new Vector2(0.9f, 0.9f)), Is.EqualTo(LookSource.Mouse),
                "Magnitude 1.27: no stick reads that.");
        }

        [Test]
        public void MisconfiguredDeadZonesFallBackToSaneValues()
        {
            var tuning = new LookTuning(-1f, -5f, 0.5f, 0.3f, 0.5f);

            Assert.That(tuning.MouseDegreesPerPixel, Is.EqualTo(0f));
            Assert.That(tuning.StickDegreesPerSecond, Is.EqualTo(0f));
            Assert.That(tuning.StickOuterDeadZone, Is.EqualTo(1f), "Outer below inner falls back to 1.");
            Assert.That(tuning.StickExponent, Is.EqualTo(1f));
        }

        [Test]
        public void TheDemoAngleIsClampedPerAxis()
        {
            Vector2 clamped = LookCrosshairView.Clamp(new Vector2(100f, -50f), new Vector2(45f, 30f));

            Assert.That(clamped, Is.EqualTo(new Vector2(45f, -30f)));
        }
    }
}
