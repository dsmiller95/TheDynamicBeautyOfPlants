using Assets.Scripts.ReactiveInputExtensions;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class Timer : MonoBehaviour
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

