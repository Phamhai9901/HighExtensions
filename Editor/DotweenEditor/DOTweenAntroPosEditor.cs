using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class DOTweenAntroPosEditor : DOTweenEditorBase
{
    [SerializeField] protected Vector2 PosDefaut;
    [SerializeField] protected float TimeDelay;
    [SerializeField] protected bool PosX;
    [SerializeField] protected bool unScaleTime = true;
    protected RectTransform rectTransform;
    private void Awake()
    {
        rectTransform = transform.GetComponent<RectTransform>();
    }
    protected override void OnTweenSingle()
    {
        StopTween();
        rectTransform.anchoredPosition = PosDefaut;
        if (PosX)
            rectTransform.DOAnchorPosX(ValueTween.x, duration).SetEase(ease).SetDelay(TimeDelay).SetUpdate(unScaleTime);
        else
            rectTransform.DOAnchorPosY(ValueTween.y, duration).SetEase(ease).SetDelay(TimeDelay).SetUpdate(unScaleTime);

    }
    public override void StopTween()
    {
        rectTransform.DOKill();
    }
}
