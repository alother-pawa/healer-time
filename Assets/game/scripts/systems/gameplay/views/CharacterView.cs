using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// A character on the battlefield: its sheet's animator (states named after the sheet's tags), its tint and size, and its
    /// bars. Action animations (attack, hurt, cast) go back to idle when they end; death stays on its last frame. The
    /// character faces right unless <c>_faceLeft</c> (the hero stands on the right). Bars sit just above the frame.
    /// REFERENCES: <c>_body</c> draws the sheet; <c>_animator</c> plays it; <c>_health</c> and <c>_armor</c> are its bars (the
    /// armor bar is optional).
    /// </summary>
    internal sealed class CharacterView : MonoBehaviour
    {
        internal const string Idle = "idle";
        internal const string Walk = "run";
        internal const string Attack = "attack";
        internal const string AttackHeavy = "attack-heavy";
        internal const string Hurt = "hurt";
        internal const string Death = "death";
        internal const string Cast = "cast";
        private const float BarGap = 0.1f;

        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Animator _animator;
        [SerializeField] private WorldBar _health;
        [SerializeField] private WorldBar _armor;
        [SerializeField] private bool _faceLeft;

        private string _state = Idle;
        private int _stateHash = Animator.StringToHash(Idle);
        private string _loop = Idle;
        private bool _barsPlaced;

        /// <summary>Size of the body relative to its sheet (bars keep their size and sit over the head).</summary>
        internal float Scale => _body.transform.localScale.y;

        /// <summary>The look of a character: animator, tint and scale (1 = sheet size).</summary>
        internal void Setup(RuntimeAnimatorController controller, Color tint, float scale)
        {
            _animator.runtimeAnimatorController = controller;
            _body.color = tint;
            _body.flipX = _faceLeft;
            _body.transform.localScale = new Vector3(scale, scale, 1f);
            _barsPlaced = false;
            _loop = Idle;
            Play(Idle);
        }

        internal void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>The looping state shown between actions (idle, or walk while approaching).</summary>
        internal void SetLoop(string state)
        {
            if (_loop == state)
            {
                return;
            }

            _loop = state;
            if (_state == Idle || _state == Walk)
            {
                Play(state);
            }
        }

        /// <summary>Plays a state from its start; an action returns to the loop state when it ends.</summary>
        internal void Play(string state)
        {
            if (_animator.runtimeAnimatorController == null || !isActiveAndEnabled)
            {
                return;
            }

            int hash = Animator.StringToHash(state);
            if (!_animator.HasState(0, hash))
            {
                return;
            }

            _state = state;
            _stateHash = hash;
            _animator.Play(hash, 0, 0f);
        }

        /// <summary>The death state at its last frame, without playing it (animations off).</summary>
        internal void ShowDead()
        {
            if (_animator.runtimeAnimatorController != null && isActiveAndEnabled
                && _animator.HasState(0, Animator.StringToHash(Death)))
            {
                _state = Death;
                _stateHash = Animator.StringToHash(Death);
                _animator.Play(Death, 0, 1f);
            }
        }

        internal void ShowBars(float health, float maxHealth, float armor)
        {
            if (_health != null)
            {
                _health.Set(maxHealth > 0f ? health / maxHealth : 0f);
            }

            if (_armor != null)
            {
                _armor.Set(maxHealth > 0f ? armor / maxHealth : 0f);
            }
        }

        internal void ShowBarsVisible(bool visible)
        {
            if (_health != null)
            {
                _health.gameObject.SetActive(visible);
            }

            if (_armor != null)
            {
                _armor.gameObject.SetActive(visible);
            }
        }

        // Bars sit just above the character's frame (its sprite's top), once the animator has shown a first frame.
        private void LateUpdate()
        {
            if (_barsPlaced || _body.sprite == null)
            {
                return;
            }

            _barsPlaced = true;
            float top = _body.sprite.bounds.max.y * Scale + BarGap;
            PlaceBar(_health, top);
            PlaceBar(_armor, top);
        }

        private static void PlaceBar(WorldBar bar, float y)
        {
            if (bar != null)
            {
                Transform t = bar.transform;
                t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);
            }
        }

        private void Update()
        {
            if (_state == Idle || _state == Walk || _state == Death || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            // Only the action's own state counts: right after Play the animator still reports the previous (looping) state.
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == _stateHash && info.normalizedTime >= 1f && !_animator.IsInTransition(0))
            {
                Play(_loop);
            }
        }
    }
}
