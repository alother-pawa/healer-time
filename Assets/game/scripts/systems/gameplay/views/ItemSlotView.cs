using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.Gameplay
{
    /// <summary>
    /// One slot of the item bar: the item's icon and count, a "+" when none is left (pressing it buys one), a frame while
    /// the item waits for a target, and the key that uses it.
    /// REFERENCES: <c>_button</c> is the whole slot; <c>_icon</c>, <c>_count</c>, <c>_plus</c>, <c>_active</c> (targeting
    /// frame) and <c>_key</c> (key hint) are its parts.
    /// </summary>
    internal sealed class ItemSlotView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private GameObject _plus;
        [SerializeField] private GameObject _active;
        [SerializeField] private TMP_Text _key;

        internal event Action Pressed;

        internal void Show(Sprite icon, int count, bool usable, bool active, string key)
        {
            _icon.sprite = icon;
            bool owned = count > 0;
            _icon.color = owned && usable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            _count.text = owned ? count.ToString() : string.Empty;
            _plus.SetActive(!owned);
            _active.SetActive(active);
            _key.text = key;
        }

        internal void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }

        private void Awake()
        {
            _button.onClick.AddListener(() => Pressed?.Invoke());
        }
    }
}
