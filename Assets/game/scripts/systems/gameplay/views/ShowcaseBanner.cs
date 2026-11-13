using DG.Tweening;
using Game.Contracts.Presentation;
using Game.Foundation.Localization;
using Game.Foundation.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The big caption over the dimmed board while a gem kind's effect plays ("Attack ×12", "Combo ×3"): it pops in and
    /// fades out. Nothing shows with animations off (the board is never dimmed then either).
    /// REFERENCES: <c>_group</c> fades the banner; <c>_icon</c> shows the gem; <c>_label</c> the caption; <c>_combo</c> the combo
    /// caption under it.
    /// </summary>
    internal sealed class ShowcaseBanner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Image _icon;
        [SerializeField] private LocalizedLabel _label;
        [SerializeField] private LocalizedLabel _combo;

        private IAnimationSettings _animations;

        internal void Init(IAnimationSettings animations)
        {
            _animations = animations;
            Hide();
        }

        internal void Show(Sprite icon, string key, object amount, int combo, string comboKey, Color color)
        {
            if (_animations != null && !_animations.Enabled)
            {
                Hide();
                return;
            }

            _icon.sprite = icon;
            _icon.gameObject.SetActive(icon != null);
            _label.SetFormat(key, amount);
            _label.GetComponent<TMPro.TMP_Text>().color = color;
            _combo.gameObject.SetActive(combo > 1);
            if (combo > 1)
            {
                _combo.SetFormat(comboKey, combo);
            }

            Transform t = transform;
            DOTween.Kill(t);
            DOTween.Kill(_group);
            _group.alpha = 1f;
            t.localScale = Vector3.one * 0.5f;
            t.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetTarget(t).SetLink(gameObject).Gate(_animations);
        }

        internal void Hide()
        {
            DOTween.Kill(transform);
            DOTween.Kill(_group);
            _group.alpha = 0f;
        }

        internal void FadeOut()
        {
            DOTween.Kill(_group);
            _group.DOFade(0f, 0.2f).SetTarget(_group).SetLink(gameObject).Gate(_animations);
        }
    }
}
