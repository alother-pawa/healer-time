namespace Game.Foundation.UI
{
    /// <summary>
    /// Font size math of the text size setting: the builder's size times the multiplier, kept inside
    /// [<see cref="MinScale"/>, <see cref="MaxScale"/>] so a hand-edited save never shrinks text to nothing or blows it up.
    /// A missing or invalid multiplier (zero, negative, NaN, infinite) counts as 1.
    /// </summary>
    public static class TextScaling
    {
        /// <summary>Smallest multiplier applied.</summary>
        public const float MinScale = 0.5f;

        /// <summary>Largest multiplier applied.</summary>
        public const float MaxScale = 2f;

        /// <summary>The multiplier actually applied for <paramref name="scale"/>.</summary>
        public static float Sanitize(float scale)
        {
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            {
                return 1f;
            }

            return scale < MinScale ? MinScale : scale > MaxScale ? MaxScale : scale;
        }

        /// <summary>Font size for a text built at <paramref name="baseSize"/> under the multiplier <paramref name="scale"/>.</summary>
        public static float Size(float baseSize, float scale)
        {
            return baseSize <= 0f ? baseSize : baseSize * Sanitize(scale);
        }
    }
}
