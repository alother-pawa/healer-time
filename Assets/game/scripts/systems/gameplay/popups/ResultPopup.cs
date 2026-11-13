using Game.Foundation.Localization;
using Game.Foundation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The end of a run, in the middle of the gameplay screen: score (with the best score, and a badge for a new best),
    /// monsters beaten and coins earned; Play again or Main menu. Closing it any other way (Cancel/Back) goes to the menu.
    /// REFERENCES: the labels; <c>_newBest</c> is the badge; the two buttons; the root carries the <c>FocusScope</c> (Play
    /// again first).
    /// </summary>
    internal sealed class ResultPopup : PopupBase
    {
        [SerializeField] private LocalizedLabel _score;
        [SerializeField] private LocalizedLabel _best;
        [SerializeField] private LocalizedLabel _kills;
        [SerializeField] private LocalizedLabel _coins;
        [SerializeField] private GameObject _newBest;
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private string _scoreKey = "gameplay.result.score";
        [SerializeField] private string _bestKey = "gameplay.result.best";
        [SerializeField] private string _killsKey = "gameplay.result.kills";
        [SerializeField] private string _coinsKey = "gameplay.result.coins";

        private IResultPopupHost _host;
        private bool _answered;

        protected override void Awake()
        {
            base.Awake();
            _replayButton.onClick.AddListener(() => Answer(true));
            _menuButton.onClick.AddListener(() => Answer(false));
        }

        protected override void OnShown(object payload)
        {
            _host = payload as IResultPopupHost;
            _answered = false;
            if (_host == null)
            {
                Debug.LogError($"[Game] {name} opened without a run; closing it.", this);
                Close();
                return;
            }

            RunResult result = _host.Result;
            _score.SetFormat(_scoreKey, result.Score);
            _best.SetFormat(_bestKey, result.Best);
            _kills.SetFormat(_killsKey, result.Kills);
            _coins.SetFormat(_coinsKey, result.Coins);
            _newBest.SetActive(result.NewBest);
        }

        protected override void OnHidden()
        {
            IResultPopupHost host = _host;
            _host = null;
            if (host != null && !_answered)
            {
                host.GoToMenu();
            }
        }

        private void Answer(bool replay)
        {
            if (_host == null || _answered)
            {
                return;
            }

            _answered = true;
            IResultPopupHost host = _host;
            Close();
            if (replay)
            {
                host.Replay();
            }
            else
            {
                host.GoToMenu();
            }
        }
    }
}
