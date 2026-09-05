using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


//현재 캐릭터, 카드의 종류에 따라 맞는 모양이 나오도록 합시다
public class CardUIInstance : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public CardEntitySO CardEntitySO { get; set; }
    public CardData CardData { get; set; }

    public bool IsCardSold { get; set; }

    [HideInInspector] public CardFlip CardFlip;

    [SerializeField] Image cardImage;
    [SerializeField] RectTransform cardBodyRect;
    [SerializeField] CardRarityIcon cardRarity;

    [Header("EdgeGlow")]
    [SerializeField] Image cardEdge;
    [SerializeField] Color enhanceTargetColor;
    [SerializeField] Color normalColor;
    [SerializeField] Color rareColor;
    [SerializeField] Color epicColor;

    [Header("Range")]
    [SerializeField] Image rangePanel;
    [SerializeField] Image rangeIcon;
    [SerializeField] Sprite selectableIcon;
    [SerializeField] Sprite unSelectableIcon;

    [Header("Texts")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text infoText;
    [SerializeField] TMP_Text costText;
    [SerializeField] TMP_Text rangeText;
    [SerializeField] TMP_Text typeText;

    [Header("CanvasGroup")]
    [SerializeField] CanvasGroup canvasGroup;

    Vector3 originalScale;

    CardInstance cardInstance;
    StringBuilder sb = new();

    CardShake cardShake;

    private void Awake()
    {
        originalScale = transform.localScale;
        cardShake = GetComponent<CardShake>();
        CardFlip = GetComponent<CardFlip>();
    }

    private void OnEnable()
    {
        transform.localScale = originalScale;
    }

    /// <summary>
    /// 현재 변경사항이 있을 수 있는 실제 인스턴스화된 카드의 데이터를 가져옵니다.
    /// </summary>
    /// <param name="card"></param>
    public void Initialize(CardInstance card, CardUISelectType cardUISelectType = CardUISelectType.DefaultScrollView, Rarity colorType = Rarity.Basic)
    {
        CardEntitySO = card.OriginalData.CardEntitySO;
        CardData = null;
        cardInstance = card;
        this.cardUISelectType = cardUISelectType;
        cardImage.sprite = card.OriginalData.CardImage;

        nameText.SetText(Localization.Instance.GetLocalizedCardName(card.OriginalData.CardName));

        
        sb.Clear();
        foreach (KeywordType keyword in card.KeywordTypes)
        {
            string localized = Localization.Instance.GetLocalizedText(keyword.ToString());
            if (sb.Length > 0)
                sb.Append(", "); // 키워드 사이에 쉼표 추가

            sb.Append($"{localized}");
        }
        if (sb.Length > 0)
            sb.Append('\n');

        string mainInfo = string.Format(Localization.Instance.GetLocalizedCardInfo(CardEntitySO), card.OriginalData.Value);

        sb.Append(mainInfo);

        infoText.SetText(sb.ToString());
        costText.SetText("{0}", card.Cost);
        rangeText.SetText("{0}", card.Range);
        typeText.SetText(Localization.Instance.GetLocalizedText(card.CardType.ToString()));

        //선택 타입에 따라 아이콘 변경
        if (card.OriginalData.CardEntitySO.IsSelectable)
            rangeIcon.sprite = selectableIcon;
        else
            rangeIcon.sprite = unSelectableIcon;

        //사거리가 존재하지 않는 카드일 경우 사거리를 표기하지 않습니다.
        if (card.OriginalData.CardEntitySO.Range <= 0)
        {
            rangeIcon.enabled = false;
            rangePanel.enabled = false;
            rangeText.enabled = false;
        }
        else
        {
            rangeIcon.enabled = true;
            rangePanel.enabled = true;
            rangeText.enabled = true;
        }
        cardRarity.SetImage(CardEntitySO.Rarity);
        SetEdgeColor(colorType);

        IsCardSold = false;
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 현재 변경사항이 있을 수 있지만 생성하지 않은 카드로 초기화합니다.
    /// </summary>
    /// <param name="data"></param>
    public void Initialize(CardData data, CardUISelectType cardUISelectType = CardUISelectType.DefaultScrollView, Rarity colorType = Rarity.Basic)
    {
        CardEntitySO = data.CardEntitySO;
        CardData = data;
        cardInstance = null;
        this.cardUISelectType = cardUISelectType;
        cardImage.sprite = data.CardImage;

        nameText.SetText(Localization.Instance.GetLocalizedCardName(data.CardName));
        sb.Clear();
        foreach (KeywordType keyword in data.KeywordTypes)
        {
            string localized = Localization.Instance.GetLocalizedText(keyword.ToString());
            if (sb.Length > 0)
                sb.Append(", "); // 키워드 사이에 쉼표 추가

            sb.Append(localized);
        }
        if (sb.Length > 0)
            sb.Append('\n');

        string mainInfo = string.Format(Localization.Instance.GetLocalizedCardInfo(CardEntitySO), data.Value);

        sb.Append(mainInfo);
        infoText.SetText(sb.ToString());
        costText.SetText("{0}", data.Cost);
        rangeText.SetText("{0}", CardEntitySO.Range);
        typeText.SetText(Localization.Instance.GetLocalizedText(CardEntitySO.CardType.ToString()));
        //선택 타입에 따라 아이콘 변경
        if (data.CardEntitySO.IsSelectable)
            rangeIcon.sprite = selectableIcon;
        else
            rangeIcon.sprite = unSelectableIcon;

        //사거리가 존재하지 않는 카드일 경우 사거리를 표기하지 않습니다.
        if (data.CardEntitySO.Range <= 0)
        {
            rangeIcon.enabled = false;
            rangePanel.enabled = false;
            rangeText.enabled = false;
        }
        else
        {
            rangeIcon.enabled = true;
            rangePanel.enabled = true;
            rangeText.enabled = true;
        }
        cardRarity.SetImage(CardEntitySO.Rarity);
        SetEdgeColor(colorType);

        IsCardSold = false;
        canvasGroup.alpha = 1f;
    }



    /// <summary>
    /// 변경이 없는 카드의 원형 데이터를 기반으로 카드를 초기화합니다.
    /// </summary>
    public void Initialize(CardEntitySO data, CardUISelectType cardUISelectType = CardUISelectType.DefaultScrollView, Rarity colorType = Rarity.Basic)
    {
        CardEntitySO = data;
        CardData = null;
        cardInstance = null;
        this.cardUISelectType = cardUISelectType;
        cardImage.sprite = data.CardImage;

        nameText.SetText(Localization.Instance.GetLocalizedCardName(data.CardName));

        sb.Clear();
        foreach (KeywordType keyword in data.KeywordTypes)
        {
            string localized = Localization.Instance.GetLocalizedText(keyword.ToString());
            if (sb.Length > 0)
                sb.Append(", "); // 키워드 사이에 쉼표 추가

            sb.Append($"{localized}");
        }
        if (sb.Length > 0)
            sb.Append('\n');

        string mainInfo = string.Format(Localization.Instance.GetLocalizedCardInfo(CardEntitySO), data.Value);

        sb.Append(mainInfo);
        infoText.SetText(sb.ToString());
        costText.SetText("{0}", data.Cost);
        rangeText.SetText("{0}", data.Range);

        typeText.SetText(Localization.Instance.GetLocalizedText(data.CardType.ToString()));

        //선택 타입에 따라 아이콘 변경
        if (data.IsSelectable)
            rangeIcon.sprite = selectableIcon;
        else
            rangeIcon.sprite = unSelectableIcon;

        //사거리가 존재하지 않는 카드일 경우 사거리를 표기하지 않습니다.
        if (data.Range <= 0)
        {
            rangeIcon.enabled = false;
            rangePanel.enabled = false;
            rangeText.enabled = false;
        }
        else
        {
            rangeIcon.enabled = true;
            rangePanel.enabled = true;
            rangeText.enabled = true;
        }


            cardRarity.SetImage(data.Rarity);
        SetEdgeColor(colorType);
        IsCardSold = false;
        canvasGroup.alpha = 1f;
    }

    public float GetWidth()
    {
        return cardBodyRect.rect.width;
    }

    public float GetHeight()
    {
        return cardBodyRect.rect.height;
    }


    #region 마우스 상호작용
    bool isHovering = false;
    CardUISelectType cardUISelectType = CardUISelectType.DefaultScrollView;
    Vector3 hoverScale = new(1.1f, 1.1f, 1.0f);
    Coroutine co;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cardUISelectType == CardUISelectType.PreviewTooltip) return;

        isHovering = true;
        TooltipManager.Instance.SetTooltip(transform, CardEntitySO);

        if (co != null)
        {
            StopCoroutine(co);
        }
        co = StartCoroutine(ScaleCo(hoverScale));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (cardUISelectType == CardUISelectType.PreviewTooltip) return;

        isHovering = false;
        TooltipManager.Instance.HideTooltips();
        if (co != null)
        {
            StopCoroutine(co);
        }
        co = StartCoroutine(ScaleCo(originalScale));
    }

    float duration = 0.12f;
    IEnumerator ScaleCo(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float smoothT = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(start, target, smoothT);
            yield return null;
        }

        transform.localScale = target;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardUISelectType == CardUISelectType.PreviewTooltip) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick(eventData);
            return;
        }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick(eventData);
            return;
        }
    }

    public void OnLeftClick(PointerEventData eventData)
    {
        //카드 선택창에서 카드를 누르면, 해당 카드를 선택했다고 알려줘야해요!
        if(cardUISelectType == CardUISelectType.SelectScrollView)
        {
            if (cardInstance != null)
            {
                CardBattleManager.Instance.ScrollView.OnCardSelected?.Invoke(cardInstance);
            }
            else if(CardData != null)
            {
                CardBattleManager.Instance.ScrollView.OnCardSelectedUI?.Invoke(CardData);
            }
            else
            {
                Debug.LogWarning("CardUIInstance : cardInstance is Null");
            }
            TooltipManager.Instance.HideTooltips();
        }
        else if(cardUISelectType == CardUISelectType.ShopTablePanel)
        {
            if (IsCardSold)
                return;

            if (CardTableUI.Instance.TryBuyCard(this))
            {
                //판매에 성공했다면
                //외곽선 없애고
                SetEdgeColor(Rarity.Basic);

                IsCardSold = true;
                //없애버리기
                gameObject.SetActive(false);
            }
            else
            {
                if(cardShake != null) 
                    cardShake.Shake();
            }
            
        }
        else if (cardUISelectType == CardUISelectType.EnhanceScrollView)
        {
            //이 상태는 카드의 강화 대상을 스크롤 뷰에서 보여줄 때 입니다.
            //일반 스크롤뷰와 다른점은, 클릭시 강화 형태만 보여주는 것이 아닌, 강화가 실제로 진행될 수 있도록 체크해줘야합니다.
            //강화 대상이 된 카드 UI는 EnhanceScrollView 타입이 아닌 EnhanceTarget 으로 합니다.
            if(CardData != null)
                CardEnhanceUI.Instance.ShowEnhancePreviewWithButton(CardData);
        }
        else if(cardUISelectType == CardUISelectType.EnhanceTarget)
        {
            CardEnhanceUI.Instance.SelectCardToEnhance(this);
        }
        else if (cardUISelectType == CardUISelectType.DefaultScrollView)
        {
            //일반 상태. 카드 선택시 강화 형태를 보여줍니다.
            CardEnhanceUI.Instance.ShowEnhancePreview(CardEntitySO);
        }
        else if(cardUISelectType == CardUISelectType.CardReward)
        {
            CardReward.Instance.SelectRewardCard(this);
        }

    }

    public void OnRightClick(PointerEventData eventData)
    {
        if (cardUISelectType == CardUISelectType.ShopTablePanel)
        {
            CardEnhanceUI.Instance.ShowEnhancePreview(CardEntitySO);
        }
        else if (cardUISelectType == CardUISelectType.DefaultScrollView)
        {
            CardEnhanceUI.Instance.ShowEnhancePreview(CardEntitySO);
        }

    }

    #endregion

    public void SetEdgeColor(Rarity colorType)
    {
        switch (colorType)
        {
            case Rarity.Basic:
                cardEdge.enabled = false;
                break;
            case Rarity.Common:
                cardEdge.enabled = true;
                cardEdge.color = normalColor;
                break;
            case Rarity.Rare:
                cardEdge.enabled = true;
                cardEdge.color = rareColor; 
                break;
            case Rarity.Unique:
                cardEdge.enabled = true;
                cardEdge.color = epicColor;
                break;
            default:
                cardEdge.enabled = true;
                cardEdge.color = enhanceTargetColor;
                break;
        }

    }


    void OnDisable()
    {
        //이 오브젝트와 마우스 상호작용중 사라졌을때의 예외처리
        if (isHovering)
        {
            TooltipManager.Instance.HideTooltips();
        }
    }
}
public enum CardUISelectType
{
    DefaultScrollView,            //통상
    PreviewTooltip,         //툴팁에만 표시되는 카드 이미지
    ShopTablePanel,         //카드 구매
    SelectScrollView,       //가지고 있는 카드덱이나 카드더미에서 선택가능
    EnhanceScrollView,      // 카드 강화 스크롤뷰 위 카드
    EnhanceTarget,           // 카드 강화 스크롤뷰 선택 후 대상 카드
    CardReward,
    DontInteract
}

public enum CardEdgeColor
{
    None,
    Normal,
    Rare,
    Epic,
    EnhanceTarget,

}