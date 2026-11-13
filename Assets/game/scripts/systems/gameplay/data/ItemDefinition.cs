using Paowa.Economy;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// A consumable item of the item bar: an inventory item of the economy package (count saved per install) with what it
    /// does. <see cref="Restock"/> is the shop product the empty slot's "+" buys. A new item is a new asset in the balance
    /// config's item list (one slot each, in order).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Gameplay/Item", fileName = "Item")]
    internal sealed class ItemDefinition : InventoryItem
    {
        [SerializeField] private ItemEffectKind _effect;
        [Tooltip("Health or armor given (Heal, Armor).")]
        [Min(0f)] [SerializeField] private float _amount = 40f;
        [Tooltip("Cells around the target that a bomb clears (1 = 3×3).")]
        [Min(0)] [SerializeField] private int _radius = 1;
        [Tooltip("Product bought by the empty slot's + button.")]
        [SerializeField] private ShopProduct _restock;
        [SerializeField] private string _nameKey;

        internal ItemEffectKind Effect => _effect;

        internal float Amount => _amount;

        internal int Radius => _radius;

        internal ShopProduct Restock => _restock;

        internal string NameKey => _nameKey;
    }
}
