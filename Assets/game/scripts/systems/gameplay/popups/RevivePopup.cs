using Game.Foundation.Localization;
using Game.Foundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// Shown when the hero falls and a revive is left: the price in coins and the balance; Revive (only when affordable)
    /// or Give up. Closing it any other way (Cancel/Back) counts as giving up.
    /// REFERENCES: <c>_message</c> shows the price; <c>_balance</c> the coins owned; the two buttons; the root carries the
    /// <c>FocusScope</c> (Revive first).
    /// </summary>
    internal sealed class RevivePopup : PopupBase
    {
        [SerializeField] private LocalizedLabel _message;
        [SerializeField] private LocalizedLabel _balance;
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _giveUpButton;
        [SerializeField] private string _messageKey = "gameplay.revive.message";
        [SerializeField] private string _balanceKey = "gameplay.revive.balance";

        private IRevivePopupHost _host;
        private bool _answered;

        protected override void Awake()
        {
            base.Awake();
            _reviveButton.onClick.AddListener(OnRevive);
            _giveUpButton.onClick.AddListener(OnGiveUp);
        }

        protected override void OnShown(object payload)
        {
            _host = payload as IRevivePopupHost;
            _answered = false;
            if (_host == null)
            {
                Debug.LogError($"[Game] {name} opened without a run; closing it.", this);
                Close();
                return;
            }

            _message.SetFormat(_messageKey, _host.ReviveCost);
            _balance.SetFormat(_balanceKey, _host.Coins);
            _reviveButton.interactable = _host.Coins >= _host.ReviveCost;
        }

        protected override void OnHidden()
        {
            IRevivePopupHost host = _host;
            _host = null;
            if (host != null && !_answered)
            {
                host.GiveUp();
            }
        }

        private void OnRevive()
        {
            if (_host != null && !_answered && _host.Revive())
            {
                _answered = true;
                Close();
            }
        }

        private void OnGiveUp()
        {
            if (_host != null && !_answered)
            {
                _answered = true;
                IRevivePopupHost host = _host;
                Close();
                host.GiveUp();
            }
        }
    }
}
