using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Assets.Scripts.ReactiveInputExtensions
{
    public class InputToEvent : MonoBehaviour
    {
        public InputActionReference input;

        public UnityEvent onPerformed;

        private void Awake()
        {
            input.ObservePerformed()
                .Subscribe(x =>
                {
                    onPerformed?.Invoke();
                })
                .AddTo(this);
        }
    }
}
