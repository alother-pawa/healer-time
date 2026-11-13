using System;
using System.Collections.Generic;
using Game.Contracts.Presentation;
using Game.Foundation.UI;
using Paowa.Core.Foundation.Services;
using Paowa.Core.Messaging;
using Paowa.Input;
using Paowa.Save;
using Paowa.UI.Navigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Systems.SaveSlots
{
    /// <summary>
    /// The save slot panel opened by Play: one card per slot id of <see cref="SaveSlotsData"/>, a Close button and a prompt
    /// row (Confirm, Delete, Back). Every card is redrawn from Core's listing whenever it may have changed (opening, any
    /// action, <see cref="SaveSlotDeletedEvent"/>, <see cref="SaveCompletedEvent"/>, <see cref="SaveSlotLoadedEvent"/>,
    /// <see cref="SaveFailedEvent"/>, <see cref="SaveSlotRestoredEvent"/>), never from an animation, so the panel is the same
    /// with animations on or off. Navigation is rewritten from state on every redraw: Left/Right between cards (same row,
    /// clamped), Up/Down through a card's actions, down to Close. The scope's default selection follows the focused card, so
    /// after a dialog the selection lands on the same button with animations on or off. Opening starts on the most recently
    /// saved slot, else the first empty one. <c>Menu/Delete</c> (Delete key, gamepad West) deletes the selected card's slot
    /// after a confirmation; its prompt shows only when that is possible. Actions run through <see cref="SaveSlotFlow"/>,
    /// created once and kept with the panel instance, so a failed Continue is remembered across openings.
    /// REFERENCES: <c>_cardPrefab</c> is instantiated under <c>_cardRow</c> (a horizontal layout) once per slot;
    /// <c>_closeButton</c> hides the panel; <c>_deletePrompt</c> is the Delete prompt; the <c>_*Key</c> fields are the texts
    /// of the confirmations and notices.
    /// </summary>
    internal sealed class SaveSlotPanel : PopupBase
    {
        [SerializeField] private SaveSlotCardView _cardPrefab;
        [SerializeField] private RectTransform _cardRow;
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _deletePrompt;
        [SerializeField] private string _deleteAction = "Menu/Delete";
        [SerializeField] private string _cancelKey;
        [SerializeField] private string _overwriteTitleKey;
        [SerializeField] private string _overwriteMessageKey;
        [SerializeField] private string _overwriteConfirmKey;
        [SerializeField] private string _deleteTitleKey;
        [SerializeField] private string _deleteMessageKey;
        [SerializeField] private string _deleteConfirmKey;
        [SerializeField] private string _deleteNewerMessageKey;
        [SerializeField] private string _errorTitleKey;
        [SerializeField] private string _errorConfirmKey;
        [SerializeField] private string _unreadableMessageKey;
        [SerializeField] private string _migrationFailedMessageKey;
        [SerializeField] private string _newerVersionMessageKey;
        [SerializeField] private string _createFailedMessageKey;
        [SerializeField] private string _deleteFailedMessageKey;

        private readonly List<SaveSlotCardView> _cards = new List<SaveSlotCardView>();
        private readonly List<SlotCardModel> _models = new List<SlotCardModel>();
        private readonly List<Texture2D> _thumbnails = new List<Texture2D>();
        private readonly List<List<Selectable>> _cardButtons = new List<List<Selectable>>();
        private readonly List<IDisposable> _bindings = new List<IDisposable>();
        private SaveSlotPanelContext _context;
        private SaveSlotFlow _flow;
        private IUIFocusService _focus;
        private IInteractionModeService _modes;
        private IInputService _input;
        private IConfirmDialog _confirm;
        private FocusScope _scope;
        private bool _isOpen;
        private bool _shortcutValid;
        private int _focusedCard = -1;
        private int _closeTargetCard = -2;

        protected override void Awake()
        {
            base.Awake();
            CoreServiceLocator.TryGet(out _focus);
            CoreServiceLocator.TryGet(out _modes);
            CoreServiceLocator.TryGet(out _input);
            CoreServiceLocator.TryGet(out _confirm);
            _scope = GetComponent<FocusScope>();
            _shortcutValid = _input != null && _input.HasAction(_deleteAction);
            _closeButton.onClick.AddListener(Close);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ReleaseThumbnails();
        }

        protected override void OnShown(object payload)
        {
            var context = payload as SaveSlotPanelContext;
            if (context == null)
            {
                Debug.LogError($"[Game] {name} opened without the save slot module's context; closing it.", this);
                Close();
                return;
            }

            if (_flow == null || _context != context)
            {
                _context = context;
                _flow = new SaveSlotFlow(context.Save, context.Writer, context.Data.SlotIds, context.Data.DataVersion,
                    context.Data.NewGameLabelKey, Confirm, DialogKeys(), context.EnterGameplay);
                _flow.Changed += Redraw;
                BuildCards();
            }

            _isOpen = true;
            if (CoreServiceLocator.TryGet(out IEventBus events))
            {
                _bindings.Add(events.Subscribe<SaveSlotDeletedEvent>(_ => Redraw()));
                _bindings.Add(events.Subscribe<SaveCompletedEvent>(_ => Redraw()));
                _bindings.Add(events.Subscribe<SaveSlotLoadedEvent>(_ => Redraw()));
                _bindings.Add(events.Subscribe<SaveFailedEvent>(_ => Redraw()));
                _bindings.Add(events.Subscribe<SaveSlotRestoredEvent>(_ => Redraw()));
            }

            // Opening starts on the most recently saved slot, else the first empty one (Navigation mode; none in Pointer mode).
            _focusedCard = -1;
            Redraw();
            _focusedCard = SaveSlotFlow.PreferredIndex(_models);
            Selectable first = LinkCloseAndDefault();

            // The focus service may already have applied the scope's previous default this frame: select it explicitly.
            if (_modes != null && _modes.Mode == InteractionMode.Navigation)
            {
                Select(first.gameObject);
            }
            else if (_focus != null)
            {
                _focus.Refocus();
            }
        }

        protected override void OnHidden()
        {
            _isOpen = false;
            foreach (IDisposable binding in _bindings)
            {
                binding.Dispose();
            }

            _bindings.Clear();
            // Thumbnails stay until the next redraw or OnDestroy: OnHidden runs before the hide animation.
        }

        private void Update()
        {
            if (!_isOpen || _flow == null)
            {
                return;
            }

            int focused = SelectedCard();
            if (focused >= 0)
            {
                _focusedCard = focused;
            }

            if (_focusedCard != _closeTargetCard)
            {
                LinkCloseAndDefault();
            }

            bool canDelete = focused >= 0 && _models[focused].CanDelete && !_flow.IsBusy;
            if (_deletePrompt.activeSelf != canDelete)
            {
                _deletePrompt.SetActive(canDelete);
            }

            if (canDelete && _shortcutValid && OwnsFocus() && _input.WasPressedThisFrame(_deleteAction))
            {
                _ = RunAsync(_flow.DeleteAsync(focused));
            }
        }

        private void BuildCards()
        {
            foreach (SaveSlotCardView card in _cards)
            {
                Destroy(card.gameObject);
            }

            _cards.Clear();
            _cardButtons.Clear();
            ReleaseThumbnails();
            _thumbnails.Clear();
            for (int i = 0; i < _flow.Count; i++)
            {
                SaveSlotCardView card = Instantiate(_cardPrefab, _cardRow);
                card.name = _context.Data.SlotIds[i];
                int index = i;
                card.ContinueButton.onClick.AddListener(() => _ = RunAsync(_flow.ContinueAsync(index)));
                card.NewGameButton.onClick.AddListener(() => _ = RunAsync(_flow.NewGameAsync(index)));
                card.DeleteButton.onClick.AddListener(() => _ = RunAsync(_flow.DeleteAsync(index)));
                _cards.Add(card);
                _cardButtons.Add(new List<Selectable>());
                _thumbnails.Add(null);
            }
        }

        // Every card from Core's listing, then every navigation link and the default selection (the focused card's first
        // button); a selection inside the panel that the redraw hid moves there.
        private void Redraw()
        {
            if (!_isOpen || _flow == null)
            {
                return;
            }

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            int selectedCard = CardOf(selected);
            if (selectedCard >= 0)
            {
                _focusedCard = selectedCard;
            }

            _models.Clear();
            for (int i = 0; i < _cards.Count; i++)
            {
                SlotCardModel model = _flow.Describe(i);
                _models.Add(model);
                ReleaseThumbnail(i);
                _thumbnails[i] = model.IsUsed ? LoadThumbnail(model.SlotId) : null;
                _cards[i].Draw(model, _thumbnails[i]);
                _cards[i].GetVisibleButtons(_cardButtons[i]);
            }

            LinkCards();
            _closeTargetCard = -2;
            Selectable fallback = LinkCloseAndDefault();

            // Only a selection inside the panel that the redraw hid is moved, the same way with animations on or off; while a
            // dialog holds the selection the focus service returns to the scope's default (FirstSelected) when it closes.
            if (selected != null && selected.transform.IsChildOf(transform) && !IsUsable(selected))
            {
                Select(fallback.gameObject);
            }
        }

        private void LinkCards()
        {
            for (int card = 0; card < _cardButtons.Count; card++)
            {
                List<Selectable> buttons = _cardButtons[card];
                for (int row = 0; row < buttons.Count; row++)
                {
                    SetNavigation(buttons[row],
                        row > 0 ? buttons[row - 1] : null,
                        row < buttons.Count - 1 ? buttons[row + 1] : _closeButton,
                        Neighbour(card, -1, row),
                        Neighbour(card, 1, row));
                }
            }
        }

        // The focused card's first button (else the preferred card's, else Close) is both the scope's default selection and
        // the target of Up from Close; returns it.
        private Selectable LinkCloseAndDefault()
        {
            _closeTargetCard = _focusedCard;
            int card = _focusedCard >= 0 && _focusedCard < _cardButtons.Count && _cardButtons[_focusedCard].Count > 0
                ? _focusedCard
                : SaveSlotFlow.PreferredIndex(_models);
            Selectable target = card >= 0 && _cardButtons[card].Count > 0 ? _cardButtons[card][0] : null;
            SetNavigation(_closeButton, target, null, null, null);
            Selectable fallback = target != null ? target : _closeButton;
            if (_scope != null)
            {
                _scope.FirstSelected = fallback;
            }

            return fallback;
        }

        // The button at the same row (or the last one) of the nearest card in that direction that has any action.
        private Selectable Neighbour(int card, int direction, int row)
        {
            for (int other = card + direction; other >= 0 && other < _cardButtons.Count; other += direction)
            {
                List<Selectable> buttons = _cardButtons[other];
                if (buttons.Count > 0)
                {
                    return buttons[Math.Min(row, buttons.Count - 1)];
                }
            }

            return null;
        }

        private int SelectedCard()
        {
            EventSystem eventSystem = EventSystem.current;
            return CardOf(eventSystem != null ? eventSystem.currentSelectedGameObject : null);
        }

        private int CardOf(GameObject target)
        {
            if (target == null)
            {
                return -1;
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                if (target.transform.IsChildOf(_cards[i].transform))
                {
                    return i;
                }
            }

            return -1;
        }

        private Texture2D LoadThumbnail(string slotId)
        {
            try
            {
                if (!_context.Save.TryReadSlotAttachment(slotId, SlotSaveWriter.ThumbnailName, out byte[] jpg) || jpg == null || jpg.Length == 0)
                {
                    return null;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (texture.LoadImage(jpg))
                {
                    return texture;
                }

                ThumbnailCapture.Release(texture);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Game] The thumbnail of save slot '{slotId}' could not be shown: {exception.Message}", this);
            }

            return null;
        }

        private void ReleaseThumbnail(int index)
        {
            if (index < _thumbnails.Count && _thumbnails[index] != null)
            {
                ThumbnailCapture.Release(_thumbnails[index]);
                _thumbnails[index] = null;
            }
        }

        private void ReleaseThumbnails()
        {
            for (int i = 0; i < _thumbnails.Count; i++)
            {
                ReleaseThumbnail(i);
            }
        }

        // True while this popup's scope is on top: no dialog over it.
        private bool OwnsFocus()
        {
            if (_focus == null)
            {
                return true;
            }

            FocusScope top = _focus.TopScope;
            return top != null && top.transform.IsChildOf(transform);
        }

        private void Select(GameObject target)
        {
            if (_focus != null)
            {
                _focus.Select(target);
            }
            else if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }
        }

        private static bool IsUsable(GameObject target)
        {
            var selectable = target.GetComponent<Selectable>();
            return target.activeInHierarchy && (selectable == null || selectable.IsInteractable());
        }

        private static void SetNavigation(Selectable selectable, Selectable up, Selectable down, Selectable left, Selectable right)
        {
            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = left;
            navigation.selectOnRight = right;
            selectable.navigation = navigation;
        }

        private async Awaitable RunAsync(Awaitable action)
        {
            try
            {
                await action;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private Awaitable<bool> Confirm(ConfirmRequest request)
        {
            if (_confirm != null)
            {
                return _confirm.ConfirmAsync(request);
            }

            // Without the dialog nothing destructive happens: every question answers no.
            var declined = new AwaitableCompletionSource<bool>();
            declined.SetResult(false);
            return declined.Awaitable;
        }

        private SaveSlotDialogKeys DialogKeys()
        {
            return new SaveSlotDialogKeys
            {
                Cancel = _cancelKey,
                OverwriteTitle = _overwriteTitleKey,
                OverwriteMessage = _overwriteMessageKey,
                OverwriteConfirm = _overwriteConfirmKey,
                DeleteTitle = _deleteTitleKey,
                DeleteMessage = _deleteMessageKey,
                DeleteConfirm = _deleteConfirmKey,
                DeleteNewerMessage = _deleteNewerMessageKey,
                ErrorTitle = _errorTitleKey,
                ErrorConfirm = _errorConfirmKey,
                UnreadableMessage = _unreadableMessageKey,
                MigrationFailedMessage = _migrationFailedMessageKey,
                NewerVersionMessage = _newerVersionMessageKey,
                CreateFailedMessage = _createFailedMessageKey,
                DeleteFailedMessage = _deleteFailedMessageKey,
            };
        }
    }
}
