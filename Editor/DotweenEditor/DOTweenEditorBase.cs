using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DOTweenEditorBase : MonoBehaviour
{
    [SerializeField] protected bool StartInAwake = true;
    [SerializeField] protected float duration = 1f;
    [SerializeField] protected Ease ease = Ease.Linear;
    
    [SerializeField] protected bool IsLooping = true;
    [SerializeField] protected LoopType LoopType = LoopType.Incremental;
    [SerializeField] protected Vector2 ValueTween;

    private void OnEnable()
    {
        if (StartInAwake)
        {
            OnTween();
        }
    }
    public virtual void Active()
    {
        OnTween();
    }
    protected virtual void OnTween()
    {
        if (!IsLooping)
        {
            OnTweenSingle();
        }
        else
        {
            OnTweenLoop();
        }
    }
    protected virtual void OnTweenSingle()
    { 
    
    }
    protected virtual void OnTweenLoop()
    {

    }
    public virtual void StopTween()
    {

    }
}
