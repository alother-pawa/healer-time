using System.Collections.Generic;
using DG.Tweening;
using Game.Contracts.Presentation;
using Game.Contracts.Roster;
using Game.Foundation.Tweening;
using TMPro;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The battlefield in the world: hero (right, facing left), monster (walking in from the left), healer (beside the
    /// board), their bars, floating numbers, bursts and flying orbs. State is drawn by <see cref="ShowMonster"/> and
    /// <see cref="ShowStats"/> from the battle's values; everything else is a transition and is skipped when animations
    /// are off (<see cref="Animate"/>). Two monster views take turns, so a dying monster can fall while the next walks in.
    /// REFERENCES: <c>_hero</c>, <c>_healer</c>, <c>_monsters</c> (two views); <c>_heroSpot</c>, <c>_monsterSpot</c>,
    /// <c>_monsterEntry</c> place them; <c>_textTemplate</c> and <c>_fxTemplate</c> are inactive templates cloned into
    /// pools; the <c>*Fx</c> sprites are the bursts; <c>_orb</c> is the flying orb; <c>_cameraPivot</c> is shaken.
    /// </summary>
    internal sealed class BattlefieldView : MonoBehaviour
    {
        private const float DeathFadeSeconds = 0.9f;

        [SerializeField] private CharacterView _hero;
        [SerializeField] private CharacterView _healer;
        [SerializeField] private CharacterView[] _monsters = new CharacterView[2];
        [SerializeField] private Transform _heroSpot;
        [SerializeField] private Transform _monsterSpot;
        [SerializeField] private Transform _monsterEntry;
        [SerializeField] private TextMeshPro _textTemplate;
        [SerializeField] private SpriteRenderer _fxTemplate;
        [SerializeField] private Sprite _hitFx;
        [SerializeField] private Sprite _healFx;
        [SerializeField] private Sprite _shieldFx;
        [SerializeField] private Sprite _magicFx;
        [SerializeField] private Sprite _orb;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Color _damageColor = new Color(1f, 0.93f, 0.8f);
        [SerializeField] private Color _hurtColor = new Color(1f, 0.35f, 0.3f);
        [SerializeField] private Color _healColor = new Color(0.45f, 1f, 0.45f);
        [SerializeField] private Color _armorColor = new Color(0.5f, 0.75f, 1f);
        [SerializeField] private Color _magicColor = new Color(0.8f, 0.6f, 1f);

        private readonly List<TextMeshPro> _texts = new List<TextMeshPro>();
        private readonly List<SpriteRenderer> _fx = new List<SpriteRenderer>();
        private MonsterDefinition[] _looks = new MonsterDefinition[0];
        private IAnimationSettings _animations;
        private int _current;
        private int _shownIndex = -1;

        internal enum Target
        {
            Hero,
            Monster,
            Healer,
        }

        internal Color DamageColor => _damageColor;

        internal Color HurtColor => _hurtColor;

        internal Color HealColor => _healColor;

        internal Color ArmorColor => _armorColor;

        internal Color MagicColor => _magicColor;

        internal Sprite HitFx => _hitFx;

        internal Sprite HealFx => _healFx;

        internal Sprite ShieldFx => _shieldFx;

        internal Sprite MagicFx => _magicFx;

        internal Transform CameraPivot => _cameraPivot;

        internal CharacterView Hero => _hero;

        internal CharacterView Healer => _healer;

        internal CharacterView Monster => _monsters[_current];

        private bool Animate => _animations == null || _animations.Enabled;

        internal void Init(HeroDefinition hero, HealerDefinition healer, MonsterDefinition[] looks, IAnimationSettings animations)
        {
            _looks = looks;
            _animations = animations;
            _hero.Setup(hero.Controller, hero.Tint, hero.Scale);
            _healer.Setup(healer.Controller, healer.Tint, healer.Scale);
            foreach (CharacterView monster in _monsters)
            {
                monster.SetVisible(false);
            }

            _shownIndex = -1;
        }

        /// <summary>
        /// The monster on the field: a new index sends the previous view to fall (or hides it with animations off) and dresses
        /// the other view in the new kind's look; its place follows the approach (0 at the entry, 1 at the fight spot).
        /// </summary>
        internal void ShowMonster(int index, int kind, float approach)
        {
            if (index < 0 || kind < 0 || kind >= _looks.Length)
            {
                return;
            }

            if (index != _shownIndex)
            {
                CharacterView previous = _monsters[_current];
                if (_shownIndex >= 0 && previous.gameObject.activeSelf)
                {
                    Fall(previous);
                }

                _current = (_current + 1) % _monsters.Length;
                CharacterView next = _monsters[_current];
                DOTween.Kill(next.transform);
                next.SetVisible(true);
                next.ShowBarsVisible(true);
                MonsterDefinition look = _looks[kind];
                next.Setup(look.Controller, look.Tint, look.Scale);
                _shownIndex = index;
            }

            CharacterView monster = _monsters[_current];
            monster.transform.position = Vector3.Lerp(_monsterEntry.position, _monsterSpot.position, Mathf.Clamp01(approach));
            monster.SetLoop(approach < 1f ? CharacterView.Walk : CharacterView.Idle);
        }

        /// <summary>Bars of the hero and the monster on the field.</summary>
        internal void ShowStats(BattleSnapshot snapshot)
        {
            _hero.ShowBars(snapshot.HeroHealth, snapshot.HeroMaxHealth, snapshot.Armor);
            _monsters[_current].ShowBars(snapshot.MonsterHealth, snapshot.MonsterMaxHealth, 0f);
        }

        /// <summary>Plays an action state on a character (nothing with animations off).</summary>
        internal void Act(Target target, string state)
        {
            if (Animate)
            {
                ViewOf(target).Play(state);
            }
        }

        /// <summary>The hero's death pose (played, or shown at its end with animations off).</summary>
        internal void HeroDown()
        {
            if (Animate)
            {
                _hero.Play(CharacterView.Death);
            }
            else
            {
                _hero.ShowDead();
            }
        }

        internal void HeroUp()
        {
            _hero.Play(CharacterView.Idle);
        }

        /// <summary>A number rising from a character (nothing with animations off).</summary>
        internal void Number(Target target, string text, Color color, float size = 1f)
        {
            if (!Animate || string.IsNullOrEmpty(text))
            {
                return;
            }

            TextMeshPro label = Take(_texts, _textTemplate);
            label.text = text;
            label.color = color;
            label.alpha = 1f;
            Transform t = label.transform;
            t.position = Above(target) + new Vector3(Random.Range(-0.25f, 0.25f), 0f, 0f);
            t.localScale = Vector3.one * (0.6f * size);
            DOTween.Kill(t);
            DOTween.Kill(label);
            DOTween.Sequence().SetTarget(t)
                .Append(t.DOScale(size, 0.12f).SetEase(Ease.OutBack))
                .Join(t.DOMove(t.position + Vector3.up * 0.9f, 0.8f).SetEase(Ease.OutCubic))
                .Insert(0.45f, DOTween.To(() => label.alpha, value => label.alpha = value, 0f, 0.35f))
                .OnComplete(() => label.gameObject.SetActive(false))
                .SetLink(label.gameObject)
                .Gate(_animations);
        }

        /// <summary>A burst on a character (nothing with animations off).</summary>
        internal void Burst(Target target, Sprite sprite, float size = 1f)
        {
            if (!Animate || sprite == null)
            {
                return;
            }

            SpriteRenderer fx = Take(_fx, _fxTemplate);
            fx.sprite = sprite;
            fx.color = Color.white;
            Transform t = fx.transform;
            t.position = Middle(target);
            t.localScale = Vector3.one * (0.4f * size);
            t.rotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));
            DOTween.Kill(t);
            DOTween.Kill(fx);
            DOTween.Sequence().SetTarget(t)
                .Append(t.DOScale(1.3f * size, 0.25f).SetEase(Ease.OutBack))
                .Append(fx.DOFade(0f, 0.25f))
                .OnComplete(() => fx.gameObject.SetActive(false))
                .SetLink(fx.gameObject)
                .Gate(_animations);
        }

        /// <summary>An orb flying from <paramref name="from"/> to a character; completes when it lands (at once when off).</summary>
        internal Awaitable Orb(Vector3 from, Target to, float seconds)
        {
            if (!Animate || _orb == null)
            {
                return TweenGateExtensions.AsAwaitable(null);
            }

            SpriteRenderer orb = Take(_fx, _fxTemplate);
            orb.sprite = _orb;
            orb.color = Color.white;
            Transform t = orb.transform;
            from.z = 0f;
            t.position = from;
            t.localScale = Vector3.one * 0.7f;
            Vector3 target = Middle(to);
            Vector3 direction = target - from;
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 45f);
            DOTween.Kill(t);
            Tween tween = t.DOJump(target, 1.2f, 1, seconds).SetEase(Ease.InQuad)
                .OnComplete(() => orb.gameObject.SetActive(false))
                .SetLink(orb.gameObject)
                .Gate(_animations);
            return tween.AsAwaitable();
        }

        /// <summary>World position of a character's middle (for effects that start or end there).</summary>
        internal Vector3 Middle(Target target)
        {
            return ViewOf(target).transform.position + Vector3.up * 0.55f * ViewOf(target).Scale;
        }

        /// <summary>Everything transient stops and hides at once (animations switched off, leaving the scene).</summary>
        internal void ClearTransients()
        {
            foreach (TextMeshPro text in _texts)
            {
                DOTween.Kill(text.transform);
                DOTween.Kill(text);
                text.gameObject.SetActive(false);
            }

            foreach (SpriteRenderer fx in _fx)
            {
                DOTween.Kill(fx.transform);
                DOTween.Kill(fx);
                fx.gameObject.SetActive(false);
            }

            for (int i = 0; i < _monsters.Length; i++)
            {
                if (i != _current)
                {
                    DOTween.Kill(_monsters[i].transform);
                    _monsters[i].SetVisible(false);
                }
            }
        }

        private void Fall(CharacterView monster)
        {
            monster.ShowBarsVisible(false);
            if (!Animate)
            {
                monster.SetVisible(false);
                return;
            }

            monster.Play(CharacterView.Death);
            DOTween.Kill(monster.transform);
            DOTween.Sequence().SetTarget(monster.transform)
                .AppendInterval(DeathFadeSeconds)
                .OnComplete(() => monster.SetVisible(false))
                .SetLink(monster.gameObject)
                .Gate(_animations);
        }

        private CharacterView ViewOf(Target target)
        {
            switch (target)
            {
                case Target.Hero:
                    return _hero;
                case Target.Healer:
                    return _healer;
                default:
                    return _monsters[_current];
            }
        }

        private Vector3 Above(Target target)
        {
            return ViewOf(target).transform.position + Vector3.up * 1.25f * ViewOf(target).Scale;
        }

        private static T Take<T>(List<T> pool, T template) where T : Component
        {
            foreach (T item in pool)
            {
                if (!item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                    return item;
                }
            }

            T created = Instantiate(template, template.transform.parent.parent);
            created.gameObject.SetActive(true);
            pool.Add(created);
            return created;
        }
    }
}
