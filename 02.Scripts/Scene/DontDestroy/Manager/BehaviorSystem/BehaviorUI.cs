using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BehaviorUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public BehaviorType BehaviorType;
    public TMP_Text ValueText { get; private set; }
    public RectTransform RectTransform { get; private set; }

    protected CanvasGroup canvasGroup;

    Coroutine co;
    const float DURATION = 0.4f;
    const float DURATION_HIDE = 0.25f;

    Transform target;
    float height;
    Vector2 followOffset;

    public float bobbingSpeed = 2f;
    private float bobbingAmount = 0.07f;
    private float randomOffset;

    protected virtual void Awake()
    {
        ValueText = GetComponentInChildren<TMP_Text>();
        RectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        // 시작할 때 전체 투명도를 0으로 초기화
        SetAlpha(0f);
        randomOffset = Random.Range(0f, 100f);
    }

    protected virtual void Update()
    {
        // 대상 따라다니기 (부모 오브젝트 전체가 이동함)
        if (target != null)
        {
            float bobbingOffset = Mathf.Sin((Time.time + randomOffset) * bobbingSpeed) * bobbingAmount;

            transform.position = new Vector3(
                target.transform.position.x + followOffset.x,
                target.transform.position.y + height + followOffset.y + bobbingOffset
            );
        }
    }

    /// <summary>
    /// Behavior 전용
    /// </summary>
    public void InitBehavior(Transform target, float height, Vector2 offset = default)
    {
        this.target = target;
        this.height = height;
        followOffset = offset;
    }

    public void Show()
    {
        StartFade(1f, DURATION);
    }

    public void BehaviorPlayed()
    {
        StartCoroutine(BehaviorPlayedCo());
    }

    public void Destroy()
    {
        if (co != null) StopCoroutine(co);
        StartCoroutine(DestroySelf());
    }

    void StartFade(float targetAlpha, float duration)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Fade(targetAlpha, duration));
    }

    IEnumerator BehaviorPlayedCo()
    {
        yield return null;
        Destroy();
    }

    IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, k));

            yield return null;
        }

        SetAlpha(targetAlpha);
        co = null;
    }

    IEnumerator DestroySelf()
    {
        yield return StartCoroutine(Fade(0f, DURATION_HIDE));
        Destroy(gameObject);
    }

    Coroutine scaleCo;
    Vector3 hoverScale = new(1.1f, 1.1f, 1.0f);

    IEnumerator ScaleCo(Vector3 targetScale)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    bool isHovering = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleCo(hoverScale));

        // 마우스 오버 시 약간 어두워지는 효과 (전체 알파값 조절)
        SetAlpha(0.7f);

        TooltipManager.Instance.SetTooltip(transform, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(ScaleCo(Vector3.one));

        // 원래 알파값 복구
        SetAlpha(1f);

        TooltipManager.Instance.HideTooltips();
    }

    void OnDisable()
    {
        if (isHovering)
        {
            TooltipManager.Instance.HideTooltips();
        }
    }

    protected virtual void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }
}