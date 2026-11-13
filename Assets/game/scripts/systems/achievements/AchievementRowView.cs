using Game.Foundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Achievements
{
    /// <summary>
    /// One achievement row, selectable so keyboard and gamepad navigation walk the list (it scrolls to the selection).
    /// Locked rows dim their content and show the lock; a locked hidden achievement shows no icon.
    /// REFERENCES: <c>_selectable</c> is the row's navigation target; <c>_content</c> holds the visible parts (dimmed while
    /// locked); <c>_icon</c> the achievement icon; <c>_lockIcon</c> the lock shown while locked; <c>_name</c>,
    /// <c>_description</c>, <c>_status</c> the three localized texts.
    /// </summary>
    internal sealed class AchievementRowView : MonoBehaviour
    {
        [SerializeField] private Selectable _selectable;
        [SerializeField] private CanvasGroup _content;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private LocalizedLabel _name;
        [SerializeField] private LocalizedLabel _description;
        [SerializeField] private LocalizedLabel _status;
        [Range(0f, 1f)] [SerializeField] private float _lockedAlpha = 0.55f;

        internal Selectable Selectable => _selectable;

        internal void Show(AchievementDisplay display, Sprite icon)
        {
            _name.SetKey(display.NameKey);
            _description.SetKey(display.DescriptionKey);
            _status.SetKey(display.StatusKey);
            _content.alpha = display.Unlocked ? 1f : _lockedAlpha;
            bool showIcon = display.ShowIcon && icon != null;
            _icon.gameObject.SetActive(showIcon);
            _icon.sprite = icon;
            _lockIcon.SetActive(!display.Unlocked);
        }
    }
}
