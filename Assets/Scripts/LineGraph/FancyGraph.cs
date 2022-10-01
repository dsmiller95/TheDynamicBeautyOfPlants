using Dman.LSystem.SystemRuntime.NativeCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FancyGraph : MonoBehaviour
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct DataPoint
    {
        public float value;
        public float time;
        public static DataPoint INVALID => new DataPoint
        {
            time = -1,
            value = -1
        };

        public static float GetInterpolatedValue(DataPoint a, DataPoint b, float time)
        {
            return math.lerp(a.value, b.value, (time - a.time) / (b.time - a.time));
        }

        public static bool operator ==(DataPoint a, DataPoint b)
        {
            return a.time == b.time;
        }
        public static bool operator !=(DataPoint a, DataPoint b)
        {
            return a.time != b.time;
        }
    }

    [SerializeField]
    List<DataPoint> data;


    public int graphTextureWidth;

    Texture2DArray graphs;

    public Material graphMaterial;

    private void Awake()
    {
        AllocateGraphs();
        UpdateRender();
    }

    private void Update()
    {
        UpdateRender();
    }

    public int DataLength()
    {
        return data.Count;
    }

    public void SetData(List<DataPoint> newData)
    {
        data = newData;
        UpdateRender();
    }

    public void AppendData(DataPoint data)
    {
        this.data.Add(data);
        UpdateRender();
    }

    private void UpdateRender()
    {
        AllocateGraphs();
        WriteGraphs();
    }

    private void AllocateGraphs()
    {
        if (graphs != null && graphTextureWidth == graphs.width)
        {
            return;
        }
        if (graphs != null)
        {
            Destroy(graphs);
        }
        graphs = new Texture2DArray(
            100, 1, 1,
            TextureFormat.ARGB32,
            mipCount: 0,
            linear: true);
        graphs.wrapMode = TextureWrapMode.Clamp;
        graphMaterial.SetTexture("_graphTextures", graphs);
    }

    private void WriteGraphs()
    {
        data.Sort((a, b) => a.time.CompareTo(b.time));
        var maxValue = data.Max(x => x.value);
        var minValue = data.Min(x => x.value);
        var maxTime = data[data.Count - 1].time;
        var minTime = data[0].time;

        var dataSequences = new DataPoint[][] { data.ToArray() };
        var dataSequencesNative = new JaggedNativeArray<DataPoint>(dataSequences, Allocator.TempJob);
        using var colorDataOutput = new NativeArray<Color32>(graphs.width * graphs.height * graphs.depth, Allocator.TempJob);

        var renderJob = new RenderGraphTextureJob
        {
            graphWidth = graphs.width,
            minTime = minTime,
            maxTime = maxTime,
            minValue = minValue,
            maxValue = maxValue,

            dataSequences = dataSequencesNative,
            colorData = colorDataOutput
        };

        var dep = renderJob.Schedule(dataSequencesNative.Length, 1);
        dep = dataSequencesNative.Dispose(dep);
        dep.Complete();
        for (int graphIndex = 0; graphIndex < dataSequences.Length; graphIndex++)
        {
            graphs.SetPixelData<Color32>(colorDataOutput,
                mipLevel: 0,
                element: graphIndex,
                sourceDataStartIndex: graphIndex * graphs.width);
        }
        graphs.Apply();

        var colorData = graphs.GetPixels(0);

        //Debug.Log(colorData.Length);
    }

    struct RenderGraphTextureJob : IJobParallelFor
    {
        public int graphWidth;

        public float minTime;
        public float maxTime;
        public float minValue;
        public float maxValue;

        public JaggedNativeArray<DataPoint> dataSequences;

        [NativeDisableParallelForRestriction]
        public NativeArray<Color32> colorData;


        private int graphIndex;
        public void Execute(int graphIndex)
        {
            this.graphIndex = graphIndex;
            var dataPoints = dataSequences[graphIndex];

            var lastPoint = DataPoint.INVALID;
            var nextPointIndex = 0;
            var nextPoint = dataSequences[dataPoints, nextPointIndex];
            for (int i = 0; i < graphWidth; i++)
            {
                var timeValue = minTime + i * (maxTime - minTime) / graphWidth;
                if (nextPoint.time < timeValue)
                {
                    nextPointIndex++;
                    lastPoint = nextPoint;
                    if (nextPointIndex >= dataPoints.length)
                    {
                        nextPoint = DataPoint.INVALID;
                    }
                    else
                    {
                        nextPoint = dataSequences[dataPoints, nextPointIndex];
                    }
                }

                if (nextPointIndex <= 0 || nextPointIndex >= dataPoints.length)
                {
                    WriteColorData(new Color32(0, 0, 0, 0), i);
                }
                else
                {
                    var interpolated = DataPoint.GetInterpolatedValue(lastPoint, nextPoint, timeValue);
                    var normalizedValue = (interpolated - minValue) / (maxValue - minValue);
                    var byteValue = (byte)(normalizedValue * byte.MaxValue);
                    WriteColorData(new Color32(byteValue, byteValue, byteValue, byteValue), i);
                }

                //Vector3.Angle

            }
        }

        private void WriteColorData(Color32 color, int point)
        {
            colorData[point + graphIndex * graphWidth] = color;
        }

    }
}
