using Game.Foundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The monster's health bar on the HUD: the full bar drawn over the empty one and cut at the health left (the art's
    /// filled part runs from <c>_fillStart</c> to <c>_fillEnd</c> of its width; the head before it always shows), with the
    /// monster's name over it (a boss caption for bosses).
    /// REFERENCES: <c>_full</c> is an Image of type Filled (horizontal, from the left) over the empty bar; <c>_name</c> shows the
    /// monster's name; <c>_bossKey</c> formats a boss's name ({0} = the name).
    /// </summary>
    internal sealed class EnemyBarView : MonoBehaviour
    {
        [SerializeField] private Image _full;
        [SerializeField] private LocalizedLabel _name;
        [Range(0f, 1f)] [SerializeField] private float _fillStart = 25f / 112f;
        [Range(0f, 1f)] [SerializeField] private float _fillEnd = 96f / 112f;
        [SerializeField] private string _bossKey = "gameplay.enemy.boss";

        private string _shownName;
        private bool _shownBoss;

        internal void Show(float health, float maxHealth, string nameKey, bool boss)
        {
            float ratio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;
            _full.fillAmount = Mathf.Lerp(_fillStart, _fillEnd, ratio);
            if (nameKey == _shownName && boss == _shownBoss)
            {
                return;
            }

            _shownName = nameKey;
            _shownBoss = boss;
            if (boss)
            {
                _name.SetFormat(_bossKey, Loc.Get(nameKey));
            }
            else
            {
                _name.SetKey(nameKey);
            }
        }
    }
}
