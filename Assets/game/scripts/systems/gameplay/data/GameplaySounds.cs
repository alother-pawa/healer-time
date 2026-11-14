using System;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>The sounds of the gameplay scene (Tools/AudioGen declares every file; unset clips play nothing).</summary>
    [Serializable]
    internal sealed class GameplaySounds
    {
        [SerializeField] private AudioClip _music;
        [SerializeField] private AudioClip _gemSwap;
        [SerializeField] private AudioClip _gemInvalid;
        [SerializeField] private AudioClip _gemMatch;
        [SerializeField] private AudioClip _shuffle;
        [SerializeField] private AudioClip _bomb;
        [SerializeField] private AudioClip _heroAttack;
        [SerializeField] private AudioClip _heroStrike;
        [SerializeField] private AudioClip _monsterAttack;
        [SerializeField] private AudioClip _hit;
        [SerializeField] private AudioClip _heal;
        [SerializeField] private AudioClip _shield;
        [SerializeField] private AudioClip _magicCharge;
        [SerializeField] private AudioClip _healerSkill;
        [SerializeField] private AudioClip _monsterDeath;
        [SerializeField] private AudioClip _bossAppear;
        [SerializeField] private AudioClip _heroDeath;
        [SerializeField] private AudioClip _coin;
        [SerializeField] private AudioClip _click;

        internal AudioClip Music => _music;

        internal AudioClip GemSwap => _gemSwap;

        internal AudioClip GemInvalid => _gemInvalid;

        internal AudioClip GemMatch => _gemMatch;

        internal AudioClip Shuffle => _shuffle;

        internal AudioClip Bomb => _bomb;

        internal AudioClip HeroAttack => _heroAttack;

        internal AudioClip HeroStrike => _heroStrike;

        internal AudioClip MonsterAttack => _monsterAttack;

        internal AudioClip Hit => _hit;

        internal AudioClip Heal => _heal;

        internal AudioClip Shield => _shield;

        internal AudioClip MagicCharge => _magicCharge;

        internal AudioClip HealerSkill => _healerSkill;

        internal AudioClip MonsterDeath => _monsterDeath;

        internal AudioClip BossAppear => _bossAppear;

        internal AudioClip HeroDeath => _heroDeath;

        internal AudioClip Coin => _coin;

        internal AudioClip Click => _click;
    }
}
