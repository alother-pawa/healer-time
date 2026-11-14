using Game.Foundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The boss's health bar on the HUD (other monsters only have the bar over their head): the full bar drawn over the
    /// empty one and cut at the health left (the art's filled part runs from <c>_fillStart</c> to <c>_fillEnd</c> of its
    /// width; the head before it always shows), with the boss's name over it. Hidden while the monster on the field is not
    /// a boss.
    /// REFERENCES: <c>_full</c> is an Image of type Filled (horizontal, from the left) over the empty bar; <c>_name</c> shows the
    /// boss's name.
    /// </summary>
    internal sealed class EnemyBarView : MonoBehaviour
    {
        [SerializeField] private Image _full;
        [SerializeField] private LocalizedLabel _name;
        [Range(0f, 1f)] [SerializeField] private float _fillStart = 25f / 112f;
        [Range(0f, 1f)] [SerializeField] private float _fillEnd = 96f / 112f;

        private string _shownName;

        internal void Show(float health, float maxHealth, string nameKey, bool boss)
        {
            if (gameObject.activeSelf != boss)
            {
                gameObject.SetActive(boss);
            }

            if (!boss)
            {
                return;
            }

            float ratio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
            _full.fillAmount = Mathf.Lerp(_fillStart, _fillEnd, ratio);
            if (nameKey != _shownName)
            {
                _shownName = nameKey;
                _name.SetKey(nameKey);
            }
        }
    }
}
