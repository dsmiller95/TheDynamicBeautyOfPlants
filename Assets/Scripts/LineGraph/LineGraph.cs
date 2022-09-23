using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LineGraph
{

    [RequireComponent(typeof(RectTransform))]

    public class LineGraph : MonoBehaviour
    {
        public List<float> data;
        public LineRenderer graphRenderer;

        void Start()
        {
        }

        void Update()
        {
            UpdateLineRender();
        }

        void UpdateLineRender()
        {
            var rect = GetComponent<RectTransform>().rect;
            var root = new Vector3(rect.min.x, rect.min.y, 0);
            var xVect = new Vector3(rect.size.x, 0, 0);
            var yVect = new Vector3(0, rect.size.y, 0);

            root = transform.TransformPoint(root);
            xVect = transform.TransformVector(xVect);
            yVect = transform.TransformVector(yVect);

            var maxValue = data.Max();

            graphRenderer.positionCount = data.Count;
            for (int i = 0; i < data.Count; i++)
            {
                var normalizedX = (float)i / data.Count;
                var normalizedY = (float)data[i] / maxValue;
                var newPosition = root + xVect * normalizedX + yVect * normalizedY;
                graphRenderer.SetPosition(i, newPosition);
            }
        }
    }
}
