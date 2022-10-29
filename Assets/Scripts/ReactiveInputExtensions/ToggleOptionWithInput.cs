using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.ReactiveInputExtensions
{
    public class ToggleOptionWithInput : MonoBehaviour
    {
        public InputActionReference toggleThing;

        public GameObject optionA;
        public GameObject optionB;

        private void Awake()
        {
            var isToggled = optionA.activeSelf;
            SetToggleState(isToggled);

            toggleThing.ObservePerformed()
                .Select(x => isToggled = !isToggled)
                .Subscribe(x => SetToggleState(x))
                .AddTo(this);
        }

        private void SetToggleState(bool isPrimary)
        {
            optionA.SetActive(isPrimary);
            optionB.SetActive(!isPrimary);
        }
    }
}
