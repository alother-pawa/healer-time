using System;
using UnityEngine;

namespace Game.Systems.Gameplay
{
    /// <summary>The item slots, in the balance config's item order; reports which slot was pressed.</summary>
    internal sealed class ItemBarView : MonoBehaviour
    {
        [SerializeField] private ItemSlotView[] _slots = new ItemSlotView[0];

        internal event Action<int> Pressed;

        internal int Count => _slots.Length;

        internal ItemSlotView Slot(int index)
        {
            return _slots[index];
        }

        private void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                int index = i;
                _slots[i].Pressed += () => Pressed?.Invoke(index);
            }
        }
    }
}
