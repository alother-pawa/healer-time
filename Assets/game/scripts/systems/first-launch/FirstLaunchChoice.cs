using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems.FirstLaunch
{
    /// <summary>
    /// One choice button of the first-launch screen: a label and a marker shown while its value is the current one.
    /// REFERENCES: <c>_button</c>, <c>_label</c>, <c>_marker</c> (the "chosen" outline).
    /// </summary>
    internal sealed class FirstLaunchChoice : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _marker;

        internal Button Button => _button;

        internal void SetLabel(string text)
        {
            if (_label.text != text)
            {
                _label.text = text;
            }
        }

        internal void SetChosen(bool chosen)
        {
            if (_marker.activeSelf != chosen)
            {
                _marker.SetActive(chosen);
            }
        }
    }
}
