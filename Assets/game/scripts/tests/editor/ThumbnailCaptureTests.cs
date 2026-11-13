using Game.Systems.SaveSlots;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Thumbnail encoding is guarded and deterministic: missing source or size gives null without throwing, a texture becomes
    /// a JPG of the target size, the scale keeps the centred crop of the target's aspect ratio, and capturing never throws
    /// (null in batch mode or without a graphics device).
    /// </summary>
    public sealed class ThumbnailCaptureTests
    {
        [Test]
        public void NoSourceOrNoSizeGivesNullWithoutThrowing()
        {
            Texture2D source = Solid(8, 8, Color.red);
            try
            {
                Assert.That(ThumbnailCapture.TryEncodeJpg(null, 320, 180, 75), Is.Null);
                Assert.That(ThumbnailCapture.TryEncodeJpg(source, 0, 180, 75), Is.Null);
                Assert.That(ThumbnailCapture.TryEncodeJpg(source, 320, -1, 75), Is.Null);
            }
            finally
            {
                ThumbnailCapture.Release(source);
            }
        }

        [Test]
        public void ATextureBecomesAJpgOfTheTargetSize()
        {
            Texture2D source = Solid(64, 48, Color.green);
            var decoded = new Texture2D(2, 2);
            try
            {
                byte[] jpg = ThumbnailCapture.TryEncodeJpg(source, 32, 18, 75);

                Assert.That(jpg, Is.Not.Null.And.Not.Empty);
                Assert.That(decoded.LoadImage(jpg), Is.True);
                Assert.That(decoded.width, Is.EqualTo(32));
                Assert.That(decoded.height, Is.EqualTo(18));
            }
            finally
            {
                ThumbnailCapture.Release(source);
                ThumbnailCapture.Release(decoded);
            }
        }

        [Test]
        public void ScalingKeepsTheCentredCropOfTheTargetAspect()
        {
            // Eight columns: red, red, green ×4, blue, blue. A 2:1 target of this 4:1 strip keeps the four green columns.
            var strip = new Color32[16];
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    strip[y * 8 + x] = x < 2 ? new Color32(255, 0, 0, 255) : x < 6 ? new Color32(0, 255, 0, 255) : new Color32(0, 0, 255, 255);
                }
            }

            Color32[] result = ThumbnailCapture.Scale(strip, 8, 2, 2, 1);

            Assert.That(result, Has.Length.EqualTo(2));
            foreach (Color32 pixel in result)
            {
                Assert.That(pixel.r, Is.Zero);
                Assert.That(pixel.g, Is.EqualTo(255));
                Assert.That(pixel.b, Is.Zero);
            }
        }

        [Test]
        public void CapturingNeverThrows()
        {
            // Outside a rendered frame Unity may log about reading the screen; the method must still return, not throw.
            LogAssert.ignoreFailingMessages = true;
            byte[] jpg = null;
            Assert.DoesNotThrow(() => jpg = ThumbnailCapture.TryCaptureJpg(32, 18, 75));
            if (!ThumbnailCapture.CanCapture)
            {
                Assert.That(jpg, Is.Null, "Batch mode or no graphics device: no capture.");
            }
        }

        private static Texture2D Solid(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
