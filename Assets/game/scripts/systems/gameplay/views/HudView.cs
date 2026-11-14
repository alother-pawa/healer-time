using System;
using Game.Foundation.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// The top bar of gameplay: score, monsters beaten, coins earned this run, and the pause button; drawn from a
    /// <see cref="BattleSnapshot"/> (texts are localization formats).
    /// REFERENCES: the three labels and the pause button; the <c>*Key</c> fields are their formats ({0} = the number).
    /// </summary>
    internal sealed class HudView : MonoBehaviour
    {
        [SerializeField] private LocalizedLabel _scoreLabel;
        [SerializeField] private LocalizedLabel _killsLabel;
        [SerializeField] private LocalizedLabel _coinsLabel;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private string _scoreKey = "gameplay.hud.score";
        [SerializeField] private string _killsKey = "gameplay.hud.kills";
        [SerializeField] private string _coinsKey = "gameplay.hud.coins";

        private int _score = -1;
        private int _kills = -1;
        private int _coins = -1;

        internal event Action PauseRequested;

        internal void Show(BattleSnapshot snapshot)
        {
            if (snapshot.Score != _score)
            {
                _score = snapshot.Score;
                _scoreLabel.SetFormat(_scoreKey, _score);
            }

            if (snapshot.Kills != _kills)
            {
                _kills = snapshot.Kills;
                _killsLabel.SetFormat(_killsKey, _kills);
            }

            if (snapshot.Coins != _coins)
            {
                _coins = snapshot.Coins;
                _coinsLabel.SetFormat(_coinsKey, _coins);
            }
        }

        private void Awake()
        {
            _pauseButton.onClick.AddListener(() => PauseRequested?.Invoke());
        }
    }
}
