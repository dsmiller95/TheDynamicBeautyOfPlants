using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Assets.Scripts.ReactiveInputExtensions
{
    public class ToggleWithEvent : MonoBehaviour
    {
        public InputActionReference toggleThing;

        public bool isToggledOn = true;
        public bool eventOnAwake = false;

        public UnityEvent onToggleOn;
        public UnityEvent onToggleOff;

        private void Awake()
        {
            toggleThing.ObservePerformed()
                .Select(x => isToggledOn = !isToggledOn)
                .Subscribe(x =>
                {
                    if (x)
                        onToggleOn?.Invoke();
                    else
                        onToggleOff?.Invoke();
                })
                .AddTo(this);
        }
    }
}
