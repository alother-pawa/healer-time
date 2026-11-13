using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// A bar over a character in the world: a sliced fill sprite (pivot on its left edge) whose width follows a 0–1 value.
    /// REFERENCES: <c>_fill</c> is the fill renderer (Draw Mode Sliced); <c>_width</c> its full width in units.
    /// </summary>
    internal sealed class WorldBar : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _fill;
        [SerializeField] private float _width = 1.2f;

        internal void Set(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            _fill.enabled = ratio > 0f;
            _fill.size = new Vector2(_width * ratio, _fill.size.y);
        }
    }
}
