using System;
using UnityEngine;
using UnityEngine.Rendering;
using CoreSaveSlots = Paowa.Save.SaveSlots;
using Object = UnityEngine.Object;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// Slot thumbnails: a screenshot of the game view, cropped to the thumbnail's aspect ratio around its centre, scaled
    /// down on the CPU (bilinear, no GPU work, so it also runs in Edit Mode tests) and encoded as JPG. Nothing here throws:
    /// without a graphics device (batch mode, headless server) or when capturing or encoding fails, the result is null and
    /// the save goes on without a new thumbnail.
    /// </summary>
    internal static class ThumbnailCapture
    {
        /// <summary>False in batch mode and without a graphics device, where the screen cannot be captured.</summary>
        internal static bool CanCapture => !Application.isBatchMode && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        /// <summary>
        /// Captures the screen and returns it as a <paramref name="width"/>×<paramref name="height"/> JPG, or null. Call it
        /// at the end of a frame (after rendering), before any pause UI is drawn.
        /// </summary>
        internal static byte[] TryCaptureJpg(int width, int height, int quality)
        {
            if (!CanCapture)
            {
                return null;
            }

            Texture2D screenshot = null;
            try
            {
                screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                return TryEncodeJpg(screenshot, width, height, quality);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Game] The slot thumbnail could not be captured: {exception.Message}");
                return null;
            }
            finally
            {
                Release(screenshot);
            }
        }

        /// <summary>
        /// Crops <paramref name="source"/> (readable) to the target aspect ratio, scales it to
        /// <paramref name="width"/>×<paramref name="height"/> and encodes it; null for a missing or unreadable source, a
        /// non-positive size, a result above Core's attachment limit, or any failure.
        /// </summary>
        internal static byte[] TryEncodeJpg(Texture2D source, int width, int height, int quality)
        {
            if (source == null || width <= 0 || height <= 0 || source.width <= 0 || source.height <= 0)
            {
                return null;
            }

            Texture2D scaled = null;
            try
            {
                Color32[] pixels = Scale(source.GetPixels32(), source.width, source.height, width, height);
                scaled = new Texture2D(width, height, TextureFormat.RGB24, false);
                scaled.SetPixels32(pixels);
                scaled.Apply(false);
                byte[] jpg = scaled.EncodeToJPG(Mathf.Clamp(quality, 1, 100));
                return jpg != null && jpg.Length > 0 && jpg.Length <= CoreSaveSlots.MaxAttachmentBytes ? jpg : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Game] The slot thumbnail could not be encoded: {exception.Message}");
                return null;
            }
            finally
            {
                Release(scaled);
            }
        }

        /// <summary>Bilinear scale of the centred crop with the target's aspect ratio (rows bottom to top, as Unity stores them).</summary>
        internal static Color32[] Scale(Color32[] source, int sourceWidth, int sourceHeight, int width, int height)
        {
            float targetAspect = (float)width / height;
            float cropWidth = sourceWidth;
            float cropHeight = sourceHeight;
            if (cropWidth / cropHeight > targetAspect)
            {
                cropWidth = cropHeight * targetAspect;
            }
            else
            {
                cropHeight = cropWidth / targetAspect;
            }

            float left = (sourceWidth - cropWidth) * 0.5f;
            float bottom = (sourceHeight - cropHeight) * 0.5f;
            var result = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float sourceY = Mathf.Clamp(bottom + (y + 0.5f) * cropHeight / height - 0.5f, 0f, sourceHeight - 1);
                int y0 = (int)sourceY;
                int y1 = Math.Min(y0 + 1, sourceHeight - 1);
                float ty = sourceY - y0;
                for (int x = 0; x < width; x++)
                {
                    float sourceX = Mathf.Clamp(left + (x + 0.5f) * cropWidth / width - 0.5f, 0f, sourceWidth - 1);
                    int x0 = (int)sourceX;
                    int x1 = Math.Min(x0 + 1, sourceWidth - 1);
                    float tx = sourceX - x0;
                    Color32 bottomRow = Color32.Lerp(source[y0 * sourceWidth + x0], source[y0 * sourceWidth + x1], tx);
                    Color32 topRow = Color32.Lerp(source[y1 * sourceWidth + x0], source[y1 * sourceWidth + x1], tx);
                    result[y * width + x] = Color32.Lerp(bottomRow, topRow, ty);
                }
            }

            return result;
        }

        /// <summary>Destroys a temporary texture (immediately outside Play Mode, where <c>Destroy</c> is not allowed).</summary>
        internal static void Release(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
