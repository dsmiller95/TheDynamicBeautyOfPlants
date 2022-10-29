using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlidePrefab : MonoBehaviour
{
    public Camera[] renderCameras;

    public void SetRenderTarget(RenderTexture texture)
    {
        foreach (var slideRenderer in renderCameras)
        {
            slideRenderer.targetTexture = texture;
            slideRenderer.forceIntoRenderTexture = true;
        }
    }



}
