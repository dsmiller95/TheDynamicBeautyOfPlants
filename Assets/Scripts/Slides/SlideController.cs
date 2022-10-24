using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dman.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Threading;
using UniRx;
using UnityEngine;

public class SlideController : MonoBehaviour
{
    public SlidePrefab[] slides;
    public Camera renderCamera;

    public Material surfaceMaterial;
    public RenderTexture renderTextureTemplate;

    public float autoAdvanceTimer = 10f;


    public int currentSlideIndex = 0;
    private SlidePrefab currentSlide = null;

    private GameObject slideDisplayer = null;
    private RenderTexture activeRenderTex = null;

    private CancellationTokenSource nextSlideAdvancement;


    public void AdvanceSlide()
    {
        currentSlideIndex = (currentSlideIndex + 1) % slides.Length;
        var newSlide = Instantiate(slides[currentSlideIndex]);
        SwitchToSlide(newSlide, 5f, TaskUtil.RefreshToken(ref nextSlideAdvancement)).Forget();
    }

    private void OnDestroy()
    {
        nextSlideAdvancement?.Cancel();
        nextSlideAdvancement?.Dispose();

        if(currentSlide != null)
        {
            Destroy(currentSlide);
            Destroy(slideDisplayer);
        }
        if(activeRenderTex != null)
        {
            activeRenderTex.Release();
            Destroy(activeRenderTex);
        }
    }

    private async UniTask SwitchToSlide(SlidePrefab nextSlide, float transitionTime, CancellationToken cancel)
    {
        var renderCubeCenter = new GameObject("quad center");
        renderCubeCenter.transform.SetParent(renderCamera.transform, false);
        renderCubeCenter.transform.position = new Vector3(0, 0, 1);

        var corner1 = renderCamera.transform.InverseTransformPoint(renderCamera.ViewportToWorldPoint(new Vector3(0, 0, 1)));
        var corner2 = renderCamera.transform.InverseTransformPoint(renderCamera.ViewportToWorldPoint(new Vector3(1, 1, 1)));

        var size = (corner2 - corner1);

        var cubeOffset = size.x * 0.5f * Vector3.forward;
        renderCubeCenter.transform.localPosition = ((corner1 + corner2) / 2) + cubeOffset;

        var nextQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        nextQuad.transform.SetParent(renderCubeCenter.transform, false);
        nextQuad.transform.localScale = new Vector3(size.x, size.y, 1);
        nextQuad.transform.localPosition = -cubeOffset;


        //ScaleQuadToFit(corner1, corner2, nextQuad.transform);


        var newRenderTexture = new RenderTexture(renderTextureTemplate);
        newRenderTexture.width = nextSlide.slideRenderer.pixelWidth;
        newRenderTexture.height = nextSlide.slideRenderer.pixelHeight;
        newRenderTexture.Create();

        nextSlide.slideRenderer.targetTexture = newRenderTexture;
        nextSlide.slideRenderer.forceIntoRenderTexture = true;

        {
            var renderer = nextQuad.GetComponent<MeshRenderer>();
            renderer.material = surfaceMaterial;
            renderer.material.mainTexture = newRenderTexture;
            renderer.material.color = Color.white;
        }

        var enterRotation = Quaternion.Euler(new Vector3(0, 90, 0));
        var exitRotation = Quaternion.Euler(new Vector3(0, 270, 0));
        renderCubeCenter.transform.localRotation = enterRotation;
        var tweens = new List<Tween>();
        //DOTween.To<Vector3>(vec => { renderCubeCenter.transform.eulerAngles = vec}, enterRotation, Vector3.zero, transitionTime);
        
        tweens.Add(renderCubeCenter.transform.DOLocalRotateQuaternion(Quaternion.identity, transitionTime));
        
        if (currentSlide != null)
        {
            tweens.Add(slideDisplayer.transform.DOLocalRotateQuaternion(exitRotation, transitionTime));
        }

        try
        {
            await UniTask.WhenAll(tweens.Select(x => x.ToUniTask(cancellationToken: cancel)));
        }
        catch
        {
            newRenderTexture.Release();
            Destroy(newRenderTexture);
            Destroy(renderCubeCenter);
            throw;
        }
        finally
        {
            foreach (var tween in tweens)
            {
                tween.Complete();
                tween.Kill();
            }
        }

        if(activeRenderTex != null)
        {
            activeRenderTex.Release();
            Destroy(activeRenderTex);
        }
        activeRenderTex = newRenderTexture;
        if(currentSlide != null)
        {
            Destroy(currentSlide.gameObject);
            Destroy(slideDisplayer);
        }
        currentSlide = nextSlide;
        slideDisplayer = renderCubeCenter;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    float lastSwap = 0;
    // Update is called once per frame
    void Update()
    {
        if(lastSwap + autoAdvanceTimer < Time.unscaledTime)
        {
            lastSwap = Time.unscaledTime;
            AdvanceSlide();
        }
    }
}
