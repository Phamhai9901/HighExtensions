using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DORotate360Editor : DOTweenEditorBase
{
    protected TweenerCore<Quaternion, Vector3, QuaternionOptions> tween;
    protected override void OnTweenSingle()
    {
        ValueTween = new Vector3(0, 0, 360);
        tween = transform.DOLocalRotate(ValueTween, duration, RotateMode.FastBeyond360).SetEase(ease);
    }
    protected override void OnTweenLoop()
    {
        ValueTween = new Vector3(0, 0, 360);
        tween = transform.DOLocalRotate(ValueTween, duration, RotateMode.FastBeyond360).SetLoops(-1, LoopType).SetEase(ease);
    }
    public override void StopTween()
    {
        tween.Kill();
    }
}
