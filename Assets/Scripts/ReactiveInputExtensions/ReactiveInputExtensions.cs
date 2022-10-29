using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniRx;
using UnityEngine.InputSystem;
using static UnityEngine.EventSystems.PointerEventData;

namespace Assets.Scripts.ReactiveInputExtensions
{
    public static class ReactiveInputExtensions
    {
        public static IObservable<InputAction.CallbackContext> ObservePerformed(this InputActionReference reference)
        {
            var action = reference.action;
            return Observable.FromEvent<InputAction.CallbackContext>(
                sub => action.performed += sub,
                sub => action.performed -= sub);
        }
    }
}
