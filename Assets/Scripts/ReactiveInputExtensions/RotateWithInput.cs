using Cysharp.Threading.Tasks;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.ReactiveInputExtensions
{
    public class RotateWithInput : MonoBehaviour
    {
        public InputActionReference rotateAxis;

        private void Awake()
        {
            rotateAxis.ReadInputSticky<float>(PlayerLoopTiming.FixedUpdate, list => list.Average())
                .Subscribe(rotation => transform.rotation *= Quaternion.Euler(rotation, 0, 0))
                .AddTo(this);
        }
    }
}
