using Assets.Scripts.ReactiveInputExtensions;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class OrbitingCameraController : MonoBehaviour
{
    public MeshRenderer orbitTarget;
    //public RectTransform UIFrame;

    public float lerpSpeed = 1;
    public float maxDistance = 10;

    public InputActionReference zoomAxis;
    public InputActionReference orbit2D;
    public InputActionReference rollAxis;
    public InputActionReference resetOrientationButton;

    private CameraState currentCamState;
    private CameraState targetCamState;

    public struct CameraState : IEquatable<CameraState>
    {
        private float distance;
        private float3 subjectPosition;
        private quaternion orientation;

        private static readonly float epsilon = 1e-5f;

        private readonly float maxDistance;

        public CameraState(
            Transform camera,
            Transform subject,
            float maxDistance)
        {
            this.maxDistance = maxDistance;
            this.distance = math.distance(camera.position, subject.position);
            this.subjectPosition = subject.position;
            this.orientation = Quaternion.identity;
        }

        private CameraState(CameraState other)
        {
            this.maxDistance = other.maxDistance;
            this.distance = other.distance;
            this.subjectPosition = other.subjectPosition;
            this.orientation = other.orientation;
        }

        public CameraState Zoomed(float zoomFactor)
        {
            return new CameraState(this)
            {
                distance = math.clamp(distance + zoomFactor, 0, maxDistance)
            };
        }
        public CameraState PointedAt(Bounds subjectBounds)
        {
            return new CameraState(this)
            {
                subjectPosition = subjectBounds.center
            };
        }
        public CameraState WithIdentityRotation()
        {
            return new CameraState(this)
            {
                orientation = Quaternion.identity
            };
        }
        public CameraState RotatedAround(float aroundRadians)
        {
            return new CameraState(this)
            {
                orientation = math.mul(orientation, quaternion.Euler(0, aroundRadians, 0))
            };
        }
        public CameraState RotatedUp(float raiseRadians)
        {
            return new CameraState(this)
            {
                orientation = math.mul(orientation, quaternion.Euler(raiseRadians, 0, 0))
            };
        }
        public CameraState Rolled(float rollRadians)
        {
            return new CameraState(this)
            {
                orientation = math.mul(orientation, quaternion.Euler(0, 0, rollRadians)) // quaternion.AxisAngle(math.forward(orientation), rollRadians))
            };
        }

        public CameraState Lerped(CameraState other, float lerpFactor)
        {
            return new CameraState(this)
            {
                distance = math.lerp(distance, other.distance, lerpFactor),
                subjectPosition = math.lerp(subjectPosition, other.subjectPosition, lerpFactor),
                orientation = math.slerp(orientation, other.orientation, lerpFactor)
            };
        }

        public void ApplyState(Transform camera)
        {
            var cameraLookVector = math.forward(orientation) * distance;
            var camPos = subjectPosition - cameraLookVector;
            camera.position = camPos;
            camera.rotation = orientation;
        }

        public bool Equals(CameraState other)
        {
            return
                math.distancesq(distance, other.distance) <= epsilon &&
                math.distancesq(subjectPosition, other.subjectPosition) < epsilon &&
                math.distancesq(orientation.value, other.orientation.value) < epsilon;
        }
    }

    private void Awake()
    {
        this.currentCamState = targetCamState = new CameraState(
            transform,
            orbitTarget.transform,
            maxDistance);
        zoomAxis.ReadInputSticky<float>(PlayerLoopTiming.FixedUpdate, list => list.Average())
            .Subscribe(zoomDiff => targetCamState = targetCamState.Zoomed(zoomDiff))
            .AddTo(this);

        orbit2D.ReadInputSticky<Vector2>(PlayerLoopTiming.FixedUpdate, AverageVector)
            .Subscribe(orbit => targetCamState = targetCamState
                .RotatedAround(math.radians(orbit.x))
                .RotatedUp(math.radians(-orbit.y))
             )
            .AddTo(this);

        rollAxis.ReadInputSticky<float>(PlayerLoopTiming.FixedUpdate, list => list.Average())
            .Subscribe(roll => targetCamState = targetCamState.Rolled(math.radians(roll)))
            .AddTo(this);

        resetOrientationButton.ObservePerformed()
            .Subscribe(_ => targetCamState = targetCamState .WithIdentityRotation())
            .AddTo(this);
    }

    private static Vector2 AverageVector(List<Vector2> input)
    {
        var res = Vector2.zero;
        foreach (var vec in input)
        {
            res += vec;
        }
        return res / input.Count;
    }

    private void FixedUpdate()
    {
        targetCamState = targetCamState.PointedAt(orbitTarget.bounds);
        if (currentCamState.Equals(targetCamState))
            return;
        currentCamState = currentCamState.Lerped(targetCamState, lerpSpeed * Time.fixedDeltaTime);
        currentCamState.ApplyState(transform);
    }
}

