using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUD : MonoBehaviour
{
    [SerializeField] Image mainImage;
    [SerializeField] Image easeImage;
    [SerializeField] TMP_Text text;


    [Tooltip("Visual Customize")]
    [SerializeField] bool DoNotUseEase = false;
    [SerializeField] bool OnlyShowCurrentValue = false;

    float easeSpeed = 1.9f;   // 이 값이 클수록 빨리 따라감
    float epsilon = 0.003f;
    //ease가 따라붙는 유예시간
    float delayTime = 0.4f;

    //타이머
    float easeDelayTimer = 0f;

    public int MaxValue
    {
        get => maxValue;
        set
        {
            maxValue = Mathf.Max(1, value);
            CurrentValue = Mathf.Clamp(currentValue, 0, maxValue);
        }
    } int maxValue;

    public int CurrentValue
    {
        get => currentValue;
        set
        {
            int newValue = Mathf.Clamp(value, 0, MaxValue);
            if (currentValue == newValue) return;

            currentValue = newValue;
            float targetFill = currentValue / (float)MaxValue;
            mainImage.fillAmount = targetFill;

            if(OnlyShowCurrentValue)
                text.text = $"{currentValue}";
            else
                text.text = $"{currentValue} / {maxValue}";

            if (co != null)
                StopCoroutine(co);
            co = StartCoroutine(AnimateTextScale());

            if (DoNotUseEase) return;
            // 증가한 경우 easeimage 보충
            if (easeImage.fillAmount < targetFill)
            {
                easeImage.fillAmount = targetFill;
                //Debug.Log("체력증가");
            }
            else if (easeImage.fillAmount > targetFill)
            {
                easeDelayTimer = delayTime;
            }
        }
    } int currentValue;
    Coroutine co;

    protected virtual void Update()
    {
        if (DoNotUseEase) return;

        float target = mainImage.fillAmount;
        float current = easeImage.fillAmount;

        // 타이머 카운트다운
        if (easeDelayTimer > 0f)
        {
            easeDelayTimer -= Time.deltaTime;
            return; // 아직 유예 중이면 아무 것도 안 함
        }

        // 감소 중일 때만 부드럽게 따라가게 함
        if (current > target + epsilon)
        {
            // Lerp 대신 상수 속도 접근으로 끝부분 느려짐 제거
            float maxDelta = easeSpeed * Time.deltaTime; // easeSpeed: fillAmount/초
            easeImage.fillAmount = Mathf.MoveTowards(current, target, maxDelta);

            // 거의 같으면 스냅
            if (Mathf.Abs(easeImage.fillAmount - target) <= epsilon)
            {
                easeImage.fillAmount = target;
            }
        }
    }


    float scaleUpFactor = 1.5f;   // 커질 크기 비율
    Vector3 textOriginalScale = Vector3.one;
    IEnumerator AnimateTextScale()
    {
        text.transform.localScale = textOriginalScale;
        Vector3 targetScale = textOriginalScale * scaleUpFactor;

        // 🔹 커지는 구간
        float t = 0f;
        while (t < 0.03f)
        {
            t += Time.deltaTime;
            float progress = t / 0.03f;
            text.transform.localScale = Vector3.Lerp(textOriginalScale, targetScale, progress);
            yield return null;
        }

        // 🔹 줄어드는 구간
        t = 0f;
        while (t < 0.23f)
        {
            t += Time.deltaTime;
            float progress = t / 0.23f;
            text.transform.localScale = Vector3.Lerp(targetScale, textOriginalScale, progress);
            yield return null;
        }

        text.transform.localScale = textOriginalScale;
    }
}