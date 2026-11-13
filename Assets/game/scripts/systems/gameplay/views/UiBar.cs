using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// A UI bar whose fill stretches from the left edge to a 0–1 value (the fill keeps its sliced ends at any length).
    /// REFERENCES: <c>_fill</c> is stretched over the track and cut by its right anchor.
    /// </summary>
    internal sealed class UiBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;

        internal void Set(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            _fill.gameObject.SetActive(ratio > 0f);
            _fill.anchorMax = new Vector2(ratio, _fill.anchorMax.y);
        }
    }
}
