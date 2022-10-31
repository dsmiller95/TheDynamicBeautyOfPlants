using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.ReactiveInputExtensions
{
    public class IconFollowOffCamera : MonoBehaviour
    {
        public GameObject OffCameraObject;
        public Camera renderingCamera;
        public Canvas renderingCanvas;

        public float edgePadding = 0.1f;
        [Range(1, 100)]
        public int roundedness = 3;


        private void LateUpdate()
        {
            var camRect = new Rect(Vector2.zero, Vector2.one);
            var canvasPlane = new Plane(renderingCanvas.worldCamera.transform.forward, renderingCanvas.transform.position);

            var offCamObjectViewport = renderingCamera.WorldToViewportPoint(
                canvasPlane.ClosestPointOnPlane(
                    OffCameraObject.transform.position
                    )
                );
            var directionalVector = ((Vector2)offCamObjectViewport - camRect.center).normalized;

            var theta = Vector2.SignedAngle(Vector2.right, directionalVector);
            var radius = EvaluatePolarRoundedRectangle(Mathf.Deg2Rad * theta, (camRect.width - edgePadding) / 2f, (camRect.height - edgePadding) / 2f, roundedness);
            var positionalOffset = directionalVector * radius;
            var targetPositionViewport = camRect.center + positionalOffset;

            var targetPosition = renderingCamera.ViewportToWorldPoint(new Vector3(
                targetPositionViewport.x,
                targetPositionViewport.y,
                renderingCanvas.planeDistance));
            transform.position = targetPosition;

            transform.localEulerAngles = new Vector3(0, 0, theta);
        }

        /// <summary>
        /// evaluates the radius of a rounded rectangle at a given theta. AKA a superellipse
        /// </summary>
        /// <param name="theta"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="curveFactor"></param>
        /// <returns></returns>
        private static float EvaluatePolarRoundedRectangle(float theta, float a, float b, int curveFactor)
        {
            curveFactor *= 2;
            var aPrime = Mathf.Pow(Mathf.Cos(theta) / a, curveFactor);
            var bPrime = Mathf.Pow(Mathf.Sin(theta) / b, curveFactor);

            return 1f / Mathf.Pow(aPrime + bPrime, 1f / curveFactor);
        }
    }
}
