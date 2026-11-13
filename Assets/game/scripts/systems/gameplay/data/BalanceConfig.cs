using Paowa.Economy;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// What a run is made of: board size, gem kinds (board kind index = position in the list), monster kinds, item slots
    /// (in order), the coin currency, the revive price and the battle tuning. The builder writes the lists and references on
    /// every run; the numbers keep what was tuned in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Gameplay/Balance", fileName = "BalanceConfig")]
    internal sealed class BalanceConfig : ScriptableObject
    {
        [Range(5, 10)] [SerializeField] private int _rows = 8;
        [Range(5, 10)] [SerializeField] private int _columns = 8;
        [SerializeField] private GemDefinition[] _gems = new GemDefinition[0];
        [SerializeField] private MonsterDefinition[] _monsters = new MonsterDefinition[0];
        [SerializeField] private ItemDefinition[] _items = new ItemDefinition[0];
        [SerializeField] private CurrencyDefinition _coin;
        [Tooltip("Coins a revive costs (once per run).")]
        [Min(0)] [SerializeField] private int _reviveCost = 60;
        [SerializeField] private BattleTuning _tuning = new BattleTuning();

        internal int Rows => _rows;

        internal int Columns => _columns;

        internal GemDefinition[] Gems => _gems;

        internal MonsterDefinition[] Monsters => _monsters;

        internal ItemDefinition[] Items => _items;

        internal CurrencyDefinition Coin => _coin;

        internal int ReviveCost => _reviveCost;

        internal BattleTuning Tuning => _tuning ?? (_tuning = new BattleTuning());
    }
}
