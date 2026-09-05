using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Data;
using UnityEngine.PlayerLoop;
using UnityEngine.EventSystems;
using System.Text;


/// <summary>
/// CardInstance는 Card 프리팹에 붙어있는 스크립트로,
/// 실제 카드덱의 데이터를 기반으로 인게임(전투) 내에서 프리팹을 생성할때 사용합니다.
/// 인게임 내의 카드 데이터와 실제 덱의 데이터는 분리됩니다.
/// </summary>
public class CardInstance : MonoBehaviour
{
    public float EnlargeInfoTextSize = 3.8f;
    public string CardName
    {
        get { return cardName; }
        private set
        {
            cardName = value;

            // 끝이 대문자 A/B면 떼었다가 다시 붙여줍니다.
            nameText.text = Localization.Instance.GetLocalizedCardName(cardName);
        }
    } string cardName;
    public int Cost
    {
        get { return cost; }
        set
        {
            cost = Mathf.Max(0, value);
            costText.text = cost.ToString();
        }
    }
    int cost;
    public int Value
    {
        get 
        {
            return value; 
        }
        set
        {
            this.value = Mathf.Max(0, value);
            CardInfoUpdate();
        }
    }
    int value;
    /// <summary>
    /// 카드 정보를 보여줄때 값을 계산해서 보여줍니다.
    /// </summary>
    /// <returns></returns>
    private string GetVisualInfoValue()
    {
        //공격 카드가 보여질땐, 아드레날린 집중 수치를 반영해서 보여줍니다.
        if (OriginalData.CardEntitySO.IsDamageEnhanceCard)
        {
            if (CardType == CardType.Martial)
            {
                //물리 카드에 아드레날린 적용
                foreach (BuffBase buff in FieldManager.Instance.PlayerInstance.Buffs)
                {
                    if (buff.BuffType == BuffType.Adrenaline)
                    {
                        return $"<size={EnlargeInfoTextSize}>'{value + buff.Stack}'</size>";
                    }
                }
            }
            else if (CardType == CardType.Magic)
            {
                //마법 카드에 집중 적용
                foreach (BuffBase buff in FieldManager.Instance.PlayerInstance.Buffs)
                {
                    if (buff.BuffType == BuffType.Focus)
                    {
                        return $"<size={EnlargeInfoTextSize}>'{value + buff.Stack}'</size>";
                    }
                }
            }
        }
        return $"<size={EnlargeInfoTextSize}>{value}</size>";
    }
    public int Range
    {
        get { return range; }
        set
        {
            range = value;
            rangeText.text = range.ToString();
        }

    }
    int range;
    public CardType CardType
    {
        get { return cardType; }
        set
        {
            cardType = value;
            string localized = Localization.Instance.GetLocalizedText(cardType.ToString());
            typeText.SetText(localized);


        }
    }CardType cardType;

    /// <summary>
    /// 카드의 키워드들. AddKeyword, RemoveKeyword를 통해 추가/제거합니다.
    /// </summary>
    public List<KeywordType> KeywordTypes { get; set; } = new();

    public ICardFunction CardFunction { get; private set; }

    /// <summary>
    /// 카드 선택 UI 사용시 적용
    /// </summary>
    public bool IsSelected = false;

    /// <summary>
    /// 카드의 원형 데이터
    /// </summary>
    public CardData OriginalData;

    public TransformContainer OriginalTransform;
    public Renderer[] Renderers;
    public Order Order { get; private set; }
    PolygonCollider2D _collider;

    [Header("CardImage Renderer")]
    [SerializeField] SpriteRenderer imageRenderer;
    [Header("Range Renderer")]
    [SerializeField] SpriteRenderer rangePanelRenderer;
    [SerializeField] SpriteRenderer rangeIconRenderer;
    [Header("Range Icons")]
    [SerializeField] Sprite selectableIcon;
    [SerializeField] Sprite unSelectableIcon;

    [Header("Rarity Icon")]
    [SerializeField] CardRarityIcon rarityImage;

    [Header("Texts")]
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text infoText;
    [SerializeField] TMP_Text costText;
    [SerializeField] TMP_Text rangeText;
    [SerializeField] TMP_Text typeText;


    [Header("OutLine Renderer(Edge)")]

    [SerializeField] SpriteRenderer outlineRenderer;


    private void Awake()
    {
        Order = GetComponent<Order>();
        _collider = GetComponent<PolygonCollider2D>();

        _collider.enabled = false;
    }

    /// <summary>
    /// 카드 덱으로부터 현재 데이터를 받아 초기화합니다.
    /// </summary>
    public void Initialize(CardData data)
    {
        OriginalData = data;
        KeywordTypes = OriginalData.KeywordTypes;
        CardName = OriginalData.CardName;

        SetCardFunction();
        Cost = OriginalData.Cost;
        Value = OriginalData.Value;
        //사거리나 타입은 일단 변경 예정이 없으므로, SO에서 직접가져옵니다.
        Range = OriginalData.CardEntitySO.Range;
        CardType = OriginalData.CardEntitySO.CardType;
        //선택 타입에 따라 아이콘 변경
        if (data.CardEntitySO.IsSelectable)
            rangeIconRenderer.sprite = selectableIcon;
        else
            rangeIconRenderer.sprite = unSelectableIcon;
        //희귀도 타입에 따라 아이콘 변경
        rarityImage.SetImage(data.CardEntitySO.Rarity);

        for (int i=0; i<Renderers.Length; i++) 
        {
            Renderers[i].enabled = false;
        }

        imageRenderer.sprite = OriginalData.CardImage;

        mpb = new MaterialPropertyBlock();
    }

    private void SetCardFunction()
    {
        CardEntitySO so = OriginalData.CardEntitySO;

        //펑션키가 없으면 이름으로 검색합니다.
        if(string.IsNullOrWhiteSpace(so.CardFunctionKey))
        {
            // 끝이 대문자 A/B면 떼고 검색합니다.
            char last = CardName[CardName.Length - 1];
            if (last == 'A' || last == 'B')
            {
                string key = cardName.Substring(0, cardName.Length - 1);
                CardFunction = CardFunctionFactory.Create(key);
            }
            else
                CardFunction = CardFunctionFactory.Create(CardName);
        }
        else
        {
            CardFunction = CardFunctionFactory.Create(so.CardFunctionKey);
        }
    }

    /// <summary>
    /// 콜라이더 활성화/비활성화
    /// </summary>
    /// <param name="enabled"></param>
    public void SetCollider(bool enabled)
    {
        if (_collider == null)
            return;

        if (enabled)
        {
            if(TurnManager.Instance.IsMyTurn)
                _collider.enabled = true;
        }
        else
        {
            _collider.enabled = false;
        }

    }
    public void HideCard()
    {
        //만약 핸드에 들어올 카드라면 숨겨선 안됩니다.
        if (CardBattleManager.Instance.HandCards.Contains(this)) 
            return;

        for (int i = 0; i < Renderers.Length; i++)
        {
            Renderers[i].enabled = false;
        }
        outlineRenderer.enabled = false;
    }
    public void ShowCard()
    {
        for (int i = 0; i < Renderers.Length; i++)
        {
            Renderers[i].enabled = true;
        }

        //사거리가 존재하지 않는 카드일 경우 표기하지 않습니다.
        if (OriginalData.CardEntitySO.Range <= 0)
        {
            rangeIconRenderer.enabled = false;
            rangePanelRenderer.enabled = false;
            rangeText.enabled = false;
        }
    }

    StringBuilder sb = new();
    /// <summary>
    /// 카드 정보를 최신화합니다.
    /// </summary>
    public void CardInfoUpdate()
    {
        sb.Clear();

        foreach (KeywordType keyword in KeywordTypes)
        {
            string localized = Localization.Instance.GetLocalizedText(keyword.ToString());
            if (sb.Length > 0)
                sb.Append(", "); // 키워드 사이에 쉼표 추가

            sb.Append(localized);
        }

        if(sb.Length > 0)
            sb.Append('\n');

        string mainInfo = string.Format(Localization.Instance.GetLocalizedCardInfo(OriginalData.CardEntitySO), GetVisualInfoValue());
        sb.Append(mainInfo);

        string dynamicInfo = CardFunction.GetDynamicInfoValue(this);
        if(dynamicInfo != null)
            sb.Append($"({dynamicInfo})");

        infoText.SetText($"{sb}");

        UpdateOutline(true);
    }

    public void AddKeyword(KeywordType keyword)
    {
        if(KeywordTypes.Contains(keyword))
            return;

        KeywordTypes.Add(keyword);
        CardInfoUpdate();
    }

    public void RemoveKeyword(KeywordType keyword)
    {
        if (!KeywordTypes.Contains(keyword))
            return;

        KeywordTypes.Remove(keyword);
        CardInfoUpdate();
    }

    //updateOutline이 실행될때마다 카드에 이펙트를 주자

    /// <summary>
    /// 현재 카드를 낼 수 있냐 없냐에 따라 현재 외곽선을 결정합니다.
    /// false면 끕니다.
    /// </summary>
    public void UpdateOutline(bool enabled)
    {
        //카드 선택 모드 중에는 일반 외곽선 갱신을 무시합니다
        if (CardBattleManager.Instance.IsCardSelectMode)
            return;
        ////핸드에 없으면 실행 안함

        if (IsAvailable() && enabled == true)
        {
            outlineRenderer.enabled = true;
        }
        else
        {
            outlineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 카드 선택 모드 등에서 조건에 부합하는 카드의 외곽선만 강제로 켤 때 사용합니다.
    /// </summary>
    public void SetSelectableOutline(bool isSelectable)
    {
        outlineRenderer.enabled = isSelectable;
    }


    Tween outlineTween;
    Color originalColor;
    [Header("방해 카드가 보여질때 잠깐 활성화할 색"), SerializeField]
    Color negativeOutlineColor;
    public void StartNegativeOutline()
    {
        originalColor = outlineRenderer.color;
        Color c = negativeOutlineColor;
        c.a = 0f;
        outlineRenderer.color = c;
    }

    public void EndNegativeOutline()
    {
        outlineRenderer.color = originalColor;
        UpdateOutline(false);
    }

    public Tween OutlineFadeTo(bool isFadeIn, float duration)
    {
        outlineRenderer.enabled = true;

        float targetAlpha = 1f;
        if (!isFadeIn)
            targetAlpha = 0f;

        outlineTween = outlineRenderer.DOFade(targetAlpha, duration);
        return outlineTween;
    }


    /// <summary>
    /// 현재 카드를 사용할 수 있는 상태인지
    /// </summary>
    public bool IsAvailable()
    {
        //핸드에 없으면 false
        if (!CardBattleManager.Instance.HandCards.Contains(this))
            return false;

        //마나 요구조건
        if (FieldManager.Instance.PlayerInstance.CurrentMp < Cost) 
            return false;

        //큐 진행중이면 카드잠금
        if (CardBattleManager.Instance.IsQueueRunning)
            return false;

        //키워드 '개시' 조건 : 이번 턴의 첫번째 카드일경우 사용할 수 있습니다.
        if (KeywordTypes != null && KeywordTypes.Contains(KeywordType.K_Opening))
        {
            if (TurnManager.Instance != null)
            {
                if (TurnManager.Instance.UsedCardCountThisTurn > 0)
                {
                    // 이번 턴에 이미 다른 카드를 사용했으므로 개시 카드 잠금
                    return false;
                }
            }
        }

        //여기서 카드별 사용가능 검사를 진행합시다! ---------------------------------
        return CardFunction.UseValidateCheck(this);
    }

    /// <summary>
    /// 이 카드를 사용합니다.
    /// 사용하는데 성공하면 true를 반환합니다.
    /// </summary>
    public bool UseCard(CatsWork.Tile targetTile = null)
    {
        //마나 검사
        if (FieldManager.Instance.PlayerInstance.CurrentMp < Cost)
            return false;

        //카드 실행
        FieldManager.Instance.PlayerInstance.CurrentMp -= Cost;
        //사용한 카드는 ui 맨 뒤로 보내줍니다.
        Order.SetSortingLayerToDefault();

        CardFunction.OnUsed(this, targetTile); //혹시 나중에 카드 기능 2회 같은 기능을 넣으면 이쪽을 건드리면 될것같습니다

        //카드 소모 시도(구: TryConsumeCard)
        if (KeywordTypes.Contains(KeywordType.K_Consumable))
            CardBattleManager.Instance.ConsumeCard(this);
        else
            CardBattleManager.Instance.EnqueueSetCardToUsed(this);

        TurnManager.Instance.UsedCardCountThisTurn++;

        BattleSceneManager.Instance.UsedCardAmount++;
        BattleSceneManager.Instance.UsedManaAmount += Cost;

        return true;
    }


    private MaterialPropertyBlock mpb;
    /// <summary>
    /// 카드를 소멸시킵니다.
    /// </summary>
    public void ConsumeCard(float duration, Vector3? targetPosition = null)
    {
        Order.SetSortingLayerToDefault();
        StartCoroutine(ConsumeEffect(duration, targetPosition));
    }

    //커진 스케일
    Vector3 enlargeScale = new(0.42f, 0.42f, 0.42f);
    IEnumerator ConsumeEffect(float duration, Vector3? targetPosition = null)
    {
        Vector3 basePosition;
        if (targetPosition.HasValue)
            basePosition = targetPosition.Value;
        else
        {
            basePosition = transform.position;
        }

        yield return Utility.WaitForSeconds(0.3f);

        // 목표 위치 근처에서 연출하고 싶다면:
        Vector3 upOffset = new(0f, 0.5f, 0f);
        Vector3 newPosition1 = basePosition + upOffset;
        Vector3 newPosition2 = newPosition1 + new Vector3(0f, 0.05f, 0f);

        KillAllSequences();
        Sequence = DOTween.Sequence();

        Sequence
            // 살짝 커지면서 target 쪽으로 이동
            .Append(transform.DOScale(enlargeScale, 0.08f).SetEase(Ease.OutQuad))
            .Join(transform.DOMove(newPosition1, 0.5f).SetEase(Ease.OutQuad))
            .Join(transform.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutQuad))
            // 잠깐 멈춤
            .AppendInterval(0.08f)
            // 살짝 더 위로, 살짝 더 커짐
            .Append(transform.DOMove(newPosition2, 0.4f).SetEase(Ease.OutQuad))
            .Join(transform.DOScale(enlargeScale * 1.05f, 0.5f))
            // 마지막에 더 부풀면서 증발
            .Append(transform.DOScale(enlargeScale * 1.08f, 0.2f).SetEase(Ease.InQuad));

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            float fadeAmount = Mathf.Lerp(-0.1f, 1f, t);
            float glow = Mathf.Lerp(1f, 30f, timer);

            foreach (var r in Renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_FadeAmount", fadeAmount);
                mpb.SetFloat("_FadeBurnGlow", glow);
                r.SetPropertyBlock(mpb);
            }

            // 텍스트 숨기기 (원래 로직 유지하되 약간 정돈)
            if (timer < 0.5f)
            {
                float textAlpha = 1f - Mathf.Clamp01(timer * 2f); // 0.5초 동안 서서히 사라짐
                nameText.alpha = textAlpha;
                infoText.alpha = textAlpha;
                costText.alpha = textAlpha;
                typeText.alpha = textAlpha;
            }
            else if (nameText.gameObject.activeSelf)
            {
                nameText.gameObject.SetActive(false);
                infoText.gameObject.SetActive(false);
                costText.gameObject.SetActive(false);
                typeText.gameObject.SetActive(false);
            }

            yield return null;
        }

        yield return Sequence.WaitForCompletion();
        Destroy(gameObject);
    }

    #region 카드 상호작용

    public DG.Tweening.Sequence Sequence;
    private void OnMouseEnter()
    {
        isHovering = true;
        CardBattleManager.Instance.CardMouseEnter(this);
    }

    private void OnMouseExit()
    {
        isHovering = false;
        CardBattleManager.Instance.CardMouseExit(this);
    }


    private void OnMouseDown()
    {
        CardBattleManager.Instance.CardMouseDown(this);
    }

    private void OnMouseOver()
    {
        CardBattleManager.Instance.CardMouseOver(this);
    }


    private void OnMouseUp()
    {
        CardBattleManager.Instance.CardMouseUp();
    }


    public void KillAllSequences()
    {
        if (Sequence != null)
        {
            Sequence.Kill();
            Sequence = null;
        }
    }

    bool isHovering = false;
    void OnDisable()
    {
        //이 오브젝트와 마우스 상호작용중 사라졌을때의 예외처리
        if (isHovering)
        {
            TooltipManager.Instance.HideTooltips();
        }
    }

    #endregion


}