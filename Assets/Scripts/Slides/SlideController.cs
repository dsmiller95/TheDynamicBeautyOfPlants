using Assets.Scripts.ReactiveInputExtensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Dman.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using static System.TimeZoneInfo;

public class SlideController : MonoBehaviour
{
    public SlidePrefab[] slides;
    public Camera renderCamera;

    public Material surfaceMaterial;
    public RenderTexture renderTextureTemplate;

    public float transitionTime = 2f;


    public int currentSlideIndex = 0;

    public InputActionReference nextSlide;
    public InputActionReference previousSlide;

    private BehaviorSubject<int> slideIndex;

    private SlidePrefab currentSlide = null;

    private GameObject slideDisplayer = null;
    private RenderTexture activeRenderTex = null;

    private CancellationTokenSource nextSlideAdvancement;

    private void Awake()
    {
        var pendingSlide = UniTask.FromCanceled();

        slideIndex = new BehaviorSubject<int>(0);

        nextSlide.ObservePerformed()
            .Do(x => Debug.Log("next slide event"))
            .Select(x => 1)
            .Subscribe(x => AdvanceSlide(x))
            .AddTo(this);
        previousSlide.ObservePerformed()
            .Do(x => Debug.Log("previous slide event"))
            .Select(x => -1)
            .Subscribe(x => AdvanceSlide(x))
            .AddTo(this);

        slideIndex
            .Pairwise()
            .Subscribe(slideIndexPair =>
            {
                var diff = slideIndexPair.Current - slideIndexPair.Previous;
                if (diff == 0 && currentSlide != null) return;
                pendingSlide = SwitchToSlide(
                    slideIndexPair.Current,
                    transitionTime,
                    diff > 0,
                    TaskUtil.RefreshToken(ref nextSlideAdvancement));
            }).AddTo(this);
        slideIndex.AddTo(this);
    }


    public void AdvanceSlide(int slidesForward)
    {
        slideIndex.OnNext((slideIndex.Value + slidesForward + slides.Length) % slides.Length);
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

    private async UniTask SwitchToSlide(
        int slideIndex, 
        float transitionTime,
        bool forward,
        CancellationToken cancel)
    {
        await UniTask.NextFrame(cancel);
        var nextSlide = Instantiate(slides[slideIndex]);

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
        newRenderTexture.width = renderCamera.pixelWidth;
        newRenderTexture.height = renderCamera.pixelHeight;
        newRenderTexture.Create();
        nextSlide.SetRenderTarget(newRenderTexture);

        {
            var renderer = nextQuad.GetComponent<MeshRenderer>();
            renderer.material = surfaceMaterial;
            renderer.material.mainTexture = newRenderTexture;
            renderer.material.color = Color.white;
        }

        var enterRotation = Quaternion.Euler(new Vector3(0, 90, 0));
        var exitRotation = Quaternion.Euler(new Vector3(0, 270, 0));
        if (!forward)
        {
            (enterRotation, exitRotation) = (exitRotation, enterRotation);
        }
        renderCubeCenter.transform.localRotation = enterRotation;
        //var tweens = new List<Tween>();
        //DOTween.To<Vector3>(vec => { renderCubeCenter.transform.eulerAngles = vec}, enterRotation, Vector3.zero, transitionTime);
        
        var newInTween = renderCubeCenter.transform.DOLocalRotateQuaternion(Quaternion.identity, transitionTime);
        Tween oldOutTween = null;
        
        if (currentSlide != null)
        {
            oldOutTween = slideDisplayer.transform.DOLocalRotateQuaternion(exitRotation, transitionTime);
        }

        try
        {
            await UniTask.WhenAll(
                newInTween.ToUniTask(TweenCancelBehaviour.CompleteAndCancelAwait, cancellationToken: cancel),
                oldOutTween?.ToUniTask(TweenCancelBehaviour.CompleteAndCancelAwait, cancellationToken: cancel) ?? UniTask.FromResult(0));
        }
        catch
        {
            // oldOutTween.Restart();

            // nextSlide.slideRenderer.targetTexture = null;
            // Destroy(renderCubeCenter);
            // Destroy(nextSlide.gameObject);
            // newRenderTexture.Release();
            // Destroy(newRenderTexture);
            // throw;
        }
        finally
        {
            newInTween.Kill();
            oldOutTween?.Kill();

            if (activeRenderTex != null)
            {
                activeRenderTex.Release();
                Destroy(activeRenderTex);
            }
            activeRenderTex = newRenderTexture;
            if (currentSlide != null)
            {
                Destroy(currentSlide.gameObject);
                Destroy(slideDisplayer);
            }
            currentSlide = nextSlide;
            slideDisplayer = renderCubeCenter;

            currentSlideIndex = slideIndex;
        }
    }
}
