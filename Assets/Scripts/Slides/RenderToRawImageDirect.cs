using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Slides
{
    [RequireComponent(typeof(Camera))]
    public class RenderToRawImageDirect: MonoBehaviour
    {
        public RawImage targetImage;
        public Camera referenceCamera;

        private RenderTexture myRenderTexture;

        private void OnEnable()
        {
            
        }
    }
}
