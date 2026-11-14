using System;
using System.Collections.Generic;
using System.Linq;
using Game.Contracts.Presentation;
using Game.Contracts.Roster;
using Game.Contracts.Settings;
using Game.Foundation.Localization;
using Paowa.Audio;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Core.Persistence;
using Paowa.Economy;
using Paowa.GameFeel;
using Paowa.Input;
using Paowa.UI;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Runs one game: owns the <see cref="Run"/> (board + battle, the only game state), feeds it time, the player's swaps and
    /// items, and draws the views from it. The battle never stops for the board. A board action resolves the board at once
    /// (state first); the board then replays its steps and each step's gems reach the battle the moment it pops, with their
    /// effects shown right then (coins earned go to the wallet at once). When the replay ends, is skipped, fails or animations
    /// are off, the steps not applied yet are applied together, and every view is drawn from the run each frame, so the screen
    /// always shows the run's state. Hero down: the result popup (Play again restarts in this scene; Main
    /// menu leaves).
    /// REFERENCES: <c>_screen</c> hosts pause and leaving; <c>_config</c> holds balance, popups and sounds; the views;
    /// <c>_battlefield</c> is the world part of the scene; <c>_itemActions</c> are the item keys in slot order; the <c>*Key</c>
    /// fields are texts.
    /// </summary>
    internal sealed class RunController : MonoBehaviour, IResultPopupHost
    {
        private const string BestScoreKey = "game.best-score";
        private const float StuckSeconds = 20f;
        private const float ResultDelaySeconds = 1.4f;

        [SerializeField] private GameplayScreen _screen;
        [SerializeField] private GameplayConfig _config;
        [SerializeField] private BoardView _board;
        [SerializeField] private HudView _hud;
        [SerializeField] private EnemyBarView _enemyBar;
        [SerializeField] private ItemBarView _items;
        [SerializeField] private ShowcaseBanner _banner;
        [SerializeField] private BattlefieldView _battlefield;
        [SerializeField] private string[] _itemActions = { "Gameplay/Item1", "Gameplay/Item2", "Gameplay/Item3", "Gameplay/Item4" };
        [SerializeField] private string[] _itemKeys = { "1", "2", "3", "4" };
        [SerializeField] private string _comboKey = "gameplay.showcase.combo";
        [SerializeField] private string _boughtKey = "gameplay.item.bought";
        [SerializeField] private string _noCoinsKey = "gameplay.item.no-coins";
        [SerializeField] private string _cannotUseKey = "gameplay.item.cannot-use";
        [SerializeField] private string _pickTargetKey = "gameplay.item.pick-target";

        private readonly List<BattleEvent> _tickEvents = new List<BattleEvent>();
        private readonly List<BattleEvent> _stepEvents = new List<BattleEvent>();
        private readonly List<IDisposable> _handles = new List<IDisposable>();
        private BalanceConfig _balance;
        private GameplaySounds _sounds;
        private IRoster _roster;
        private IAnimationSettings _animations;
        private IInputService _input;
        private IWallet _wallet;
        private IInventory _inventory;
        private IShopService _shop;
        private ISfxService _sfx;
        private IMusicService _music;
        private ICameraShake _shake;
        private IGameSettings _settings;
        private IUIManager _ui;
        private IGlobalStore _store;
        private IEventBus _events;
        private Run _run;
        private bool _busy;
        private bool _abort;
        private bool _ended;
        private bool _buying;
        private int _targetingItem = -1;
        private float _busySince;
        private int _generation;

        public RunResult Result { get; private set; }

        private bool Animate => _animations == null || _animations.Enabled;

        public void Replay()
        {
            StartRun();
        }

        public void GoToMenu()
        {
            _screen.GoToMenu();
        }

        private void Awake()
        {
            CoreServiceLocator.TryGet(out _roster);
            CoreServiceLocator.TryGet(out _animations);
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _wallet);
            CoreServiceLocator.TryGet(out _inventory);
            CoreServiceLocator.TryGet(out _shop);
            CoreServiceLocator.TryGet(out _sfx);
            CoreServiceLocator.TryGet(out _music);
            CoreServiceLocator.TryGet(out _shake);
            CoreServiceLocator.TryGet(out _settings);
            CoreServiceLocator.TryGet(out _ui);
            CoreServiceLocator.TryGet(out _store);
            CoreServiceLocator.TryGet(out _events);
            _balance = _config.Balance;
            _sounds = _config.Sounds;
            _board.SwapRequested += OnSwapRequested;
            _board.CellPicked += OnCellPicked;
            _board.StepPopped += OnStepPopped;
            _items.Pressed += OnItemPressed;
            _hud.PauseRequested += _screen.RequestPause;
            _banner.Init(_animations);
            Sprite[] sprites = _balance.Gems.Select(gem => gem.Sprite).ToArray();
            _board.Build(_balance.Rows, _balance.Columns, sprites, _animations);
        }

        private void OnEnable()
        {
            if (_events != null)
            {
                _handles.Add(_events.Subscribe<AnimationSettingsChangedEvent>(OnAnimationsChanged));
            }

            if (_shake != null && _battlefield.ShakeRoot != null)
            {
                _handles.Add(_shake.RegisterPivot(_battlefield.ShakeRoot));
            }
        }

        private void OnDisable()
        {
            foreach (IDisposable handle in _handles)
            {
                handle.Dispose();
            }

            _handles.Clear();
        }

        private void Start()
        {
            if (_music != null && _sounds.Music != null)
            {
                _music.PlayMusic(_sounds.Music, 1f);
            }

            StartRun();
        }

        // A new run: fresh board and battle, views drawn from them. Callbacks of the previous run's replay check the generation
        // and stop, so they never touch the new run's views.
        private void StartRun()
        {
            _generation++;
            _board.StopAnimations();
            _battlefield.ClearTransients();
            _busy = false;
            _abort = false;
            _ended = false;
            _targetingItem = -1;
            _board.SetTargeting(false);
            _banner.Hide();
            _screen.PauseBlocked = false;

            HeroDefinition hero = _roster.SelectedHero;
            HealerDefinition healer = _roster.SelectedHealer;
            _run = RunFactory.Create(_balance, hero, healer, Environment.TickCount);
            _battlefield.Init(hero, healer, _balance.Monsters, _animations);
            _board.Render(_run.Board);
            RunStep start = _run.Start();
            foreach (BattleEvent e in start.Events)
            {
                PresentLive(e);
            }

            SyncViews();
        }

        private void Update()
        {
            if (_run == null)
            {
                return;
            }

            bool paused = _screen.IsPaused || _screen.IsLeaving;
            if (_busy && !paused && Time.unscaledTime - _busySince > StuckSeconds)
            {
                // Only a guard against a stuck replay: stop it; the flow then draws everything from the run.
                Debug.LogWarning("[Game] A board replay took too long; skipping to the end.", this);
                _abort = true;
                _board.StopAnimations();
                _battlefield.ClearTransients();
            }

            if (!paused && !_run.IsOver && !_ended)
            {
                if (!_busy)
                {
                    ReadItemKeys();
                }

                _tickEvents.Clear();
                _run.Tick(Time.deltaTime, _tickEvents);
                Credit(_tickEvents);
                foreach (BattleEvent e in _tickEvents)
                {
                    PresentLive(e);
                }

                if (_run.IsOver)
                {
                    OnHeroDown();
                }
            }

            _board.AcceptInput = !_busy && !paused && !_run.IsOver && !_ended;
            SyncViews();
        }

        // Every view from the run's state.
        private void SyncViews()
        {
            Battle battle = _run.Battle;
            BattleSnapshot snapshot = battle.Snapshot;
            _battlefield.ShowMonster(battle.MonsterIndex, battle.MonsterKind, battle.ApproachProgress);
            _battlefield.ShowStats(snapshot);
            _hud.Show(snapshot, battle.MonsterIsBoss);
            ShowEnemy(snapshot);
            RenderItems();
        }

        private void RenderItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                ItemSlotView slot = _items.Slot(i);
                if (i >= _balance.Items.Length)
                {
                    slot.gameObject.SetActive(false);
                    continue;
                }

                ItemDefinition item = _balance.Items[i];
                int count = _inventory != null ? _inventory.GetCount(item) : 0;
                bool usable = !_run.IsOver && ItemEffects.For(item.Effect).CanUse(_run, item.Amount);
                slot.Show(item.Icon, count, usable, _targetingItem == i, i < _itemKeys.Length ? _itemKeys[i] : string.Empty);
                slot.SetInteractable(!_busy && !_run.IsOver && !_ended && !_buying);
            }
        }

        private void OnSwapRequested(Cell a, Cell b)
        {
            if (_busy || _run.IsOver || _ended)
            {
                return;
            }

            CancelTargeting();
            RunStep step = _run.Swap(a, b);
            _ = PlayStepAsync(step, a, b);
        }

        private void OnCellPicked(Cell cell)
        {
            if (_targetingItem < 0 || _busy)
            {
                return;
            }

            int index = _targetingItem;
            CancelTargeting();
            UseItem(index, cell);
        }

        private void OnItemPressed(int index)
        {
            if (_busy || _run.IsOver || _ended || _buying || index < 0 || index >= _balance.Items.Length)
            {
                return;
            }

            ItemDefinition item = _balance.Items[index];
            if (_inventory == null || _inventory.GetCount(item) <= 0)
            {
                CancelTargeting();
                _ = BuyAsync(item);
                return;
            }

            IItemEffect effect = ItemEffects.For(item.Effect);
            if (!effect.CanUse(_run, item.Amount))
            {
                Toast(_cannotUseKey);
                return;
            }

            if (effect.NeedsTarget)
            {
                bool start = _targetingItem != index;
                CancelTargeting();
                if (start)
                {
                    _targetingItem = index;
                    _board.SetTargeting(true);
                    Toast(_pickTargetKey);
                }

                return;
            }

            CancelTargeting();
            UseItem(index, default);
        }

        private void UseItem(int index, Cell target)
        {
            ItemDefinition item = _balance.Items[index];
            RunStep step = ItemEffects.For(item.Effect).Use(_run, item.Amount, item.Radius, target);
            if (!step.Done || !_inventory.TryConsume(item))
            {
                Toast(_cannotUseKey);
                return;
            }

            switch (item.Effect)
            {
                case ItemEffectKind.Bomb:
                    Play(_sounds.Bomb);
                    break;
                case ItemEffectKind.Shuffle:
                    Play(_sounds.Shuffle);
                    break;
            }

            if (step.Board != null)
            {
                _ = PlayStepAsync(step, target, target);
                return;
            }

            foreach (BattleEvent e in step.Events)
            {
                PresentLive(e);
            }
        }

        private async Awaitable BuyAsync(ItemDefinition item)
        {
            if (_shop == null || item.Restock == null)
            {
                return;
            }

            if (!_shop.CanBuy(item.Restock))
            {
                Toast(_noCoinsKey);
                return;
            }

            _buying = true;
            try
            {
                ShopPurchaseResult result = await _shop.BuyAsync(item.Restock);
                if (result.IsSuccess)
                {
                    Play(_sounds.Coin);
                    Toast(_boughtKey, Loc.Get(item.NameKey));
                }
                else
                {
                    Toast(_noCoinsKey);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                _buying = false;
            }
        }

        // The board replay; each popped step reaches the battle at once (OnStepPopped). Whatever the replay did not apply
        // (it ended, was stopped, failed or animations are off) is applied at the end.
        private async Awaitable PlayStepAsync(RunStep step, Cell a, Cell b)
        {
            int generation = _generation;
            _busy = true;
            _busySince = Time.unscaledTime;
            _abort = false;
            try
            {
                if (step.Board != null && !step.Board.Valid)
                {
                    Play(_sounds.GemInvalid);
                    await _board.PlayInvalidSwap(a, b, _run.Board);
                    return;
                }

                ShowEvents(step.Events);
                if (step.Board != null && step.Board.Swapped)
                {
                    Play(_sounds.GemSwap);
                }

                if (step.Board != null)
                {
                    await _board.PlayResolution(step.Board, _run.Board);
                    if (step.Board.Reshuffled)
                    {
                        Play(_sounds.Shuffle);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                if (generation == _generation && this != null)
                {
                    _stepEvents.Clear();
                    _run.ApplyPending(_stepEvents);
                    ShowEvents(_stepEvents);
                    _busy = false;
                    _banner.FadeOut();
                    _board.Render(_run.Board);
                    SyncViews();
                }
            }
        }

        // A cascade step popped: its gems reach the battle now, their effects play now, and a banner names the main gem.
        private void OnStepPopped(CascadeStep step)
        {
            if (_sfx != null && _sounds.GemMatch != null)
            {
                _sfx.PlaySfx(_sounds.GemMatch, AudioCategory.SFX, Mathf.Min(1f, 0.7f + 0.1f * step.Index));
            }

            if (_run == null || !_run.HasPendingSteps)
            {
                return;
            }

            _stepEvents.Clear();
            _run.ApplySteps(step.Index, _stepEvents);
            ShowEvents(_stepEvents);
            GemGroup main = step.Cleared.OrderByDescending(group => group.Count).FirstOrDefault();
            if (main != null && Animate && main.Kind < _balance.Gems.Length)
            {
                GemDefinition gem = _balance.Gems[main.Kind];
                float total = _stepEvents.Where(e => e.Gem == main.Kind && IsGemAmount(e)).Sum(e => e.Amount);
                _banner.Show(gem.Sprite, gem.ShowcaseKey, Mathf.RoundToInt(total), step.Index + 1, _comboKey, gem.Color);
            }
        }

        // Battle events caused by the player (board steps, items): coins go to the wallet, effects show, the run may end.
        private void ShowEvents(List<BattleEvent> events)
        {
            Credit(events);
            foreach (BattleEvent e in events)
            {
                PresentLive(e);
            }

            if (_run.IsOver && !_ended)
            {
                OnHeroDown();
            }
        }

        // Events of the running battle (and of items), shown at once; the state itself is drawn by SyncViews.
        private void PresentLive(BattleEvent e)
        {
            switch (e.Kind)
            {
                case BattleEventKind.HeroWindup:
                    _battlefield.Act(BattlefieldView.Target.Hero, CharacterView.Attack);
                    Play(_sounds.HeroAttack);
                    break;
                case BattleEventKind.HeroAttack:
                    _battlefield.Act(BattlefieldView.Target.Monster, CharacterView.Hurt);
                    _battlefield.Burst(BattlefieldView.Target.Monster, _battlefield.HitFx, 0.7f);
                    _battlefield.Number(BattlefieldView.Target.Monster, Mathf.RoundToInt(e.Amount).ToString(), _battlefield.DamageColor, 0.8f);
                    Play(_sounds.Hit);
                    Shake(0.08f, 0.12f);
                    break;
                case BattleEventKind.HeroStrike:
                    _battlefield.Burst(BattlefieldView.Target.Monster, _battlefield.HitFx, 1.3f);
                    _battlefield.Act(BattlefieldView.Target.Monster, CharacterView.Hurt);
                    _battlefield.Number(BattlefieldView.Target.Monster, Mathf.RoundToInt(e.Amount).ToString(), _battlefield.DamageColor, 1.5f);
                    Play(_sounds.Hit);
                    Shake(0.15f, 0.2f);
                    break;
                case BattleEventKind.HealerSkill:
                    _battlefield.Burst(BattlefieldView.Target.Monster, _battlefield.MagicFx, 1.6f);
                    _battlefield.Act(BattlefieldView.Target.Monster, CharacterView.Hurt);
                    _battlefield.Number(BattlefieldView.Target.Monster, Mathf.RoundToInt(e.Amount).ToString(), _battlefield.MagicColor, 1.8f);
                    Shake(0.3f, 0.35f);
                    break;
                case BattleEventKind.StrikeHeld:
                    // The strike waits for the monster: it shows as power gathered on the one who cast it.
                    bool skill = e.Value == (int)BattleEventKind.HealerSkill;
                    BattlefieldView.Target caster = skill ? BattlefieldView.Target.Healer : BattlefieldView.Target.Hero;
                    _battlefield.Burst(caster, skill ? _battlefield.MagicFx : _battlefield.HitFx, 1.1f);
                    _battlefield.Number(caster, Mathf.RoundToInt(e.Amount).ToString(), skill ? _battlefield.MagicColor : _battlefield.DamageColor);
                    if (e.Absorbed > 0f)
                    {
                        _battlefield.Number(BattlefieldView.Target.Hero, "+" + Mathf.RoundToInt(e.Absorbed), _battlefield.HealColor);
                    }

                    break;
                case BattleEventKind.StrikeWindup:
                    if ((e.Value & 1) != 0)
                    {
                        _battlefield.Act(BattlefieldView.Target.Hero, CharacterView.AttackHeavy);
                        Play(_sounds.HeroStrike);
                    }

                    if ((e.Value & 2) != 0)
                    {
                        _battlefield.Act(BattlefieldView.Target.Healer, CharacterView.Cast);
                        Play(_sounds.HealerSkill);
                        _ = _battlefield.Orb(_battlefield.Middle(BattlefieldView.Target.Healer), BattlefieldView.Target.Monster,
                            Mathf.Max(0.1f, _balance.Tuning.AttackWindup));
                    }

                    if (e.Absorbed > 0f)
                    {
                        _battlefield.Number(BattlefieldView.Target.Hero, "+" + Mathf.RoundToInt(e.Absorbed), _battlefield.HealColor);
                    }

                    break;
                case BattleEventKind.MagicCharged:
                    _battlefield.Burst(BattlefieldView.Target.Healer, _battlefield.MagicFx, 0.7f);
                    _battlefield.Number(BattlefieldView.Target.Healer, "+" + Mathf.RoundToInt(e.Amount), _battlefield.MagicColor);
                    Play(_sounds.MagicCharge);
                    break;
                case BattleEventKind.MonsterWindup:
                    _battlefield.Act(BattlefieldView.Target.Monster, CharacterView.Attack);
                    Play(_sounds.MonsterAttack);
                    break;
                case BattleEventKind.MonsterAttack:
                    Play(_sounds.Hit);
                    Shake(e.Amount > 0f ? 0.12f : 0.08f, e.Amount > 0f ? 0.15f : 0.12f);
                    if (e.Amount > 0f)
                    {
                        _battlefield.Act(BattlefieldView.Target.Hero, CharacterView.Hurt);
                        _battlefield.Number(BattlefieldView.Target.Hero, "-" + Mathf.RoundToInt(e.Amount), _battlefield.HurtColor);
                    }

                    if (e.Absorbed > 0f)
                    {
                        _battlefield.Number(BattlefieldView.Target.Hero, "-" + Mathf.RoundToInt(e.Absorbed), _battlefield.ArmorColor, 0.8f);
                    }

                    break;
                case BattleEventKind.MonsterDied:
                    Play(_sounds.MonsterDeath);
                    Play(_sounds.Coin);
                    _battlefield.Number(BattlefieldView.Target.Monster, "+" + (int)e.Amount + "c", new Color(1f, 0.85f, 0.3f), 1.2f);
                    break;
                case BattleEventKind.MonsterSpawned:
                    _battlefield.ShowMonster(e.After.MonsterIndex, e.Value, 0f);
                    if (e.Amount > 0f)
                    {
                        Play(_sounds.BossAppear);
                        Shake(0.35f, 0.5f);
                    }

                    break;
                case BattleEventKind.Healed:
                    _battlefield.Burst(BattlefieldView.Target.Hero, _battlefield.HealFx);
                    _battlefield.Number(BattlefieldView.Target.Hero, "+" + Mathf.RoundToInt(e.Amount), _battlefield.HealColor);
                    Play(_sounds.Heal);
                    break;
                case BattleEventKind.ArmorGained:
                    _battlefield.Burst(BattlefieldView.Target.Hero, _battlefield.ShieldFx);
                    _battlefield.Number(BattlefieldView.Target.Hero, "+" + Mathf.RoundToInt(e.Amount), _battlefield.ArmorColor);
                    Play(_sounds.Shield);
                    break;
                case BattleEventKind.HeroDied:
                    _battlefield.HeroDown();
                    Play(_sounds.HeroDeath);
                    Shake(0.4f, 0.5f);
                    break;
            }
        }

        private void ShowEnemy(BattleSnapshot snapshot)
        {
            Battle battle = _run.Battle;
            if (battle.MonsterKind >= 0 && battle.MonsterKind < _balance.Monsters.Length)
            {
                _enemyBar.Show(snapshot.MonsterHealth, snapshot.MonsterMaxHealth, _balance.Monsters[battle.MonsterKind].NameKey,
                    battle.MonsterIsBoss);
            }
        }

        // Coins earned are the run's state: they reach the wallet as soon as the battle reports the kill.
        private void Credit(List<BattleEvent> events)
        {
            if (_wallet == null || _balance.Coin == null)
            {
                return;
            }

            foreach (BattleEvent e in events)
            {
                if (e.Kind == BattleEventKind.MonsterDied && e.Amount > 0f)
                {
                    _wallet.Add(_balance.Coin, (long)e.Amount);
                }
            }
        }

        // The run is over at once (no ticking, no input, no pause menu); the result shows once the hero's fall has played.
        private void OnHeroDown()
        {
            if (_ended)
            {
                return;
            }

            _ended = true;
            CancelTargeting();
            _screen.PauseBlocked = true;
            BattleSnapshot snapshot = _run.Battle.Snapshot;
            int best = _store != null ? _store.Get(BestScoreKey, 0) : 0;
            bool newBest = snapshot.Score > best;
            if (newBest)
            {
                best = snapshot.Score;
                if (_store != null)
                {
                    _store.Set(BestScoreKey, best);
                    _store.Flush();
                }
            }

            Result = new RunResult(snapshot.Score, snapshot.Kills, snapshot.Coins, best, newBest);
            _ = ShowResultAsync(_generation);
        }

        private async Awaitable ShowResultAsync(int generation)
        {
            await DelayAsync(ResultDelaySeconds, generation);
            if (generation != _generation || this == null)
            {
                return;
            }

            if (_ui != null)
            {
                _ui.ShowPopup(_config.ResultPopup.ViewId, this);
            }
        }

        private void ReadItemKeys()
        {
            if (_input == null)
            {
                return;
            }

            for (int i = 0; i < _itemActions.Length; i++)
            {
                if (_input.HasAction(_itemActions[i]) && _input.WasPressedThisFrame(_itemActions[i]))
                {
                    OnItemPressed(i);
                }
            }
        }

        private void CancelTargeting()
        {
            if (_targetingItem >= 0)
            {
                _targetingItem = -1;
                _board.SetTargeting(false);
            }
        }

        private void OnAnimationsChanged(AnimationSettingsChangedEvent evt)
        {
            if (!evt.Enabled)
            {
                // Switched off mid-replay: everything jumps to the end and the flow carries on from the run's state.
                _board.StopAnimations();
                _battlefield.ClearTransients();
                _banner.Hide();
            }
        }

        // Waits game time (so a pause holds it); ends at once with animations off, a stuck guard or a new run.
        private async Awaitable DelayAsync(float seconds, int generation)
        {
            float end = Time.time + seconds;
            while (Continue(generation) && Animate && Time.time < end)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        private bool Continue(int generation)
        {
            return generation == _generation && !_abort && this != null;
        }

        private static bool IsGemAmount(BattleEvent e)
        {
            return e.Kind == BattleEventKind.HeroStrike || e.Kind == BattleEventKind.Healed
                   || (e.Kind == BattleEventKind.StrikeHeld && e.Value == (int)BattleEventKind.HeroStrike)
                   || (e.Kind == BattleEventKind.StrikeWindup && e.Value == 1) || e.Kind == BattleEventKind.ArmorGained
                   || e.Kind == BattleEventKind.MagicCharged;
        }

        private static Vector3 ScreenToWorld(Vector3 screen)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return Vector3.zero;
            }

            screen.z = -camera.transform.position.z;
            return camera.ScreenToWorldPoint(screen);
        }

        private void Shake(float amplitude, float seconds)
        {
            if (_shake == null || !Animate)
            {
                return;
            }

            float strength = _settings != null ? _settings.CameraShake : 1f;
            if (strength > 0f)
            {
                _shake.Shake(amplitude * strength, seconds);
            }
        }

        private void Play(AudioClip clip)
        {
            if (_sfx != null && clip != null)
            {
                _sfx.PlaySfx(clip);
            }
        }

        private static void Toast(string key, params object[] arguments)
        {
            if (!string.IsNullOrEmpty(key) && CoreServiceLocator.TryGet(out IToastService toasts))
            {
                toasts.Show(arguments.Length > 0 ? Loc.Format(key, arguments) : Loc.Get(key));
            }
        }
    }
}
