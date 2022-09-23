using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class LineGraph : MonoBehaviour
{
    [SerializeField]
    List<float> data;
    [SerializeField]
    LineRenderer graphRenderer;

    public void SetData(List<float> newData)
    {
        data = newData;
        UpdateLineRender();
    }

    public void AppendData(float data)
    {
        this.data.Add(data);
        UpdateLineRender();
    }

    void Start()
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

        graphRenderer.positionCount = data.Count;
        if (data.Count <= 0)
        {
            return;
        }

        var maxValue = data.Max();
        for (int i = 0; i < data.Count; i++)
        {
            var normalizedX = (float)i / data.Count;
            var normalizedY = (float)data[i] / maxValue;
            var newPosition = root + xVect * normalizedX + yVect * normalizedY;
            graphRenderer.SetPosition(i, newPosition);
        }
    }
}
