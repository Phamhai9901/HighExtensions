using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DOTweenSizeEditor : DOTweenEditorBase
{
    [SerializeField] protected int LoopCount = -1;
    [SerializeField] protected Vector3 SizeDefaut;
    [SerializeField] protected float TimeDelay;
    [SerializeField] protected bool unScaleTime;
    protected TweenerCore<Vector3, Vector3, VectorOptions> tween;
    
    protected override void OnTweenSingle()
    {
        tween.Kill();
        transform.localScale = SizeDefaut;
        tween = transform.DOScale(ValueTween, duration).SetEase(ease).SetDelay(TimeDelay).SetUpdate(unScaleTime);
    }
    protected override void OnTweenLoop()
    {
        tween = transform.DOScale(ValueTween, duration).SetLoops(LoopCount, LoopType).SetEase(ease).SetDelay(TimeDelay).SetUpdate(unScaleTime);
    }
    public override void StopTween()
    {
        tween.Kill();
    }
}
