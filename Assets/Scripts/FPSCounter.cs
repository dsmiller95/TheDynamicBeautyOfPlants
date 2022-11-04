using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public Queue<float> fpsMeasurements = new Queue<float>();
    public int frameDepth = 20;
    public TMPro.TMP_Text text;


    private int frameCount = 0;
    private float nextUpdate = 0.0f;
    public float updateRate = 4;  // 4 updates per sec.

    private void Update()
    {
        frameCount++;
        if (Time.time <= nextUpdate)
        {
            return;
        }

        nextUpdate += 1 / updateRate;
        fpsMeasurements.Enqueue(frameCount * updateRate);
        if (fpsMeasurements.Count > frameDepth) fpsMeasurements.Dequeue();
        frameCount = 0;
        UpdateFrameText();

    }

    private void UpdateFrameText()
    {
        var min = float.MaxValue;
        var max = float.MinValue;

        foreach (var fps in fpsMeasurements)
        {
            min = Mathf.Min(fps, min);
            max = Mathf.Max(fps, min);
        }


        var fpsMax = Mathf.RoundToInt(max);
        var fpsMid = Mathf.RoundToInt(fpsMeasurements.Peek());
        var fpsMin = Mathf.RoundToInt(min);

        text.text = $"<mspace=17>{fpsMin,3}:{fpsMid,3}:{fpsMax,3}</mspace>";
    }
}

