using System;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TMPro.TMP_Text text;


    private DateTime started;
    private void Start()
    {
        started = DateTime.Now;
    }

    private void Update()
    {
        var delta = DateTime.Now - started;
        text.text = $"<mspace=17>{delta.ToString(@"mm")}</mspace>:<mspace=17>{delta.ToString(@"ss")}</mspace>";
    }
}

