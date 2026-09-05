using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.CanvasScaler;

public enum KeywordType
{
    K_Consumable,     //소모성
    K_Chaos,          //혼돈
    K_Discard,        //버리기
    K_Opening,        //개시
    K_Adrenaline,     //아드레날린
    K_Focus,
    K_Footwork,       //리비엘식 보법
    K_FootworkA,      //리비엘식 보법
    K_Haste,          //기민함
    K_Neutral,        //중립 (플레이어도 피해를 받을 수 있음)
}

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    //프리뷰가 있는 카드의 경우를 대비해 CardPreviewInstance를 하나 사용합니다.
    public CardUIInstance cardUIInstance;

    [SerializeField] RectTransform tooltipParent;
    [SerializeField] GameObject tooltipPrefab;

    [Header("Canvas/Sorting")]
    [SerializeField] Canvas canvas;

    //남는 tooltip 큐
    Queue<Tooltip> tooltipPool = new();
    //활성화된 tooltip들
    List<Tooltip> activeTooltips = new();

    ////현재 선택된 카드의 툴팁을 표시할겁니다.
    //현재 선택된 오브젝트의 툴팁을 표시
    Transform selectedTransform;
    public Transform SelectedTransform => selectedTransform;

    float tooltipWidth;
    Coroutine tooltipCoroutine;
    //bool isCoroutineEnabled = false; // 지연 효과에서 코루틴 검사를 위해 사용

    int uiSortingLayerID;
    int tooltipSortingLayerID;

    private float verticalSpacing = 7.5f;        // 툴팁 간격
    private float screenMargin = 8f;           // 화면 가장자리 여백
    private float minTopPadding = 8f;          // 화면 상단 패딩
    private float minBottomPadding = 8f;       // 화면 하단 패딩

    const float OBJ_CARD_OFFSET_X = 300f;
    const float OBJ_CARD_OFFSET_Y = 370f;
    const float UI_CARD_OFFSET_X = 200f;
    const float UI_CARD_OFFSET_Y = 70f;
    const float UNIT_OFFSET_X = 200f;
    const float UNIT_OFFSET_Y = 200f;
    const float RELIC_OFFSET_X = 50f;
    const float RELIC_OFFSET_Y = 50f;

    const int ATTACK_RANGE_IDX = 12;
    const int MOVE_RANGE_IDX = 0;

    void Awake()
    {
        //싱글톤
        if (Instance == null)
            Instance = this;

        uiSortingLayerID = SortingLayer.NameToID("UI");
        tooltipSortingLayerID = SortingLayer.NameToID("Tooltip");
        //툴팁 풀링
        for (int i = 0; i < 3; i++)
        {
            Tooltip tooltip = Instantiate(tooltipPrefab, tooltipParent).GetComponent<Tooltip>();
            tooltipPool.Enqueue(tooltip);

            if (i == 0)
                tooltipWidth = tooltip.GetWidth();
        }

    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            HideTooltips();
    }

    /// <summary>
    /// 로딩시 1회 수행
    /// </summary>
    public void SetCanvasCameraToUI()
    {
        canvas.worldCamera = CinemachineManager.Instance.UICamera;
    }

    public void HideTooltips()
    {
        selectedTransform = null;

        // 코루틴이 이미 돌고 있다면 중단
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }

        foreach (Tooltip tooltip in activeTooltips)
        {
            tooltipPool.Enqueue(tooltip);
            tooltip.HideTooltip();
        }

        activeTooltips.Clear();
    }

    #region 공통 코루틴
    /// <summary>
    /// 중복되던 코루틴 호출부와 지연 로직을 하나로 통합했습니다.
    /// </summary>
    private void StartTooltipRoutine(Transform target, float waitTime, Action onShowTooltip)
    {
        // 코루틴이 이미 돌고 있다면 중단
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }

        selectedTransform = target;
        tooltipCoroutine = StartCoroutine(WaitAndShowTooltip(target, waitTime, onShowTooltip));
    }

    private IEnumerator WaitAndShowTooltip(Transform target, float waitTime, Action onShowTooltip)
    {
        float elapsed = 0f;

        while (elapsed < waitTime)
        {
            // 마우스가 다른 카드로 이동했거나 벗어났다면 중단
            if (selectedTransform != target)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        //기존 툴팁 초기화
        HideTooltips();

        // 설정된 Action(툴팁 텍스트 세팅 및 배치 로직) 실행
        onShowTooltip?.Invoke();
    }
    #endregion

    #region 툴팁 셋팅 (오버로딩)
    public void SetTooltip(CardInstance card)
    {
        StartTooltipRoutine(card.transform, 0.3f, () =>
        {
            //키워드정보
            foreach (KeywordType keywordType in card.KeywordTypes)
            {
                string localizeKey = keywordType.ToString();
                string keyword = Localization.Instance.GetLocalizedText(localizeKey);
                string info = Localization.Instance.GetLocalizedText($"{localizeKey}Info");

                SetTooltipByData(info, keyword);
            }
            // 툴팁정보
            foreach (KeywordType keywordType in card.OriginalData.CardEntitySO.TooltipInfo)
            {
                string localizeKey = keywordType.ToString();
                string keyword = Localization.Instance.GetLocalizedText(localizeKey);
                string info = Localization.Instance.GetLocalizedText($"{localizeKey}Info");

                SetTooltipByData(info, keyword);
            }

            //카드를 생성하는 카드의 경우, 미리보기를 보여줍니다.
            if (card.OriginalData.CardEntitySO.PreviewCard != null)
            {
                SetTooltipByCardPreview(card.OriginalData.CardEntitySO.PreviewCard);
            }

            selectedTransform = card.transform;
            //카드
            SetTooltipParentPosition(selectedTransform, false, new(OBJ_CARD_OFFSET_X, OBJ_CARD_OFFSET_Y));
        });
    }

    //주로 ui환경에서 사용할듯, 따라서 order를 좀 변경해주자
    public void SetTooltip(Transform transform, CardEntitySO cardData)
    {
        StartTooltipRoutine(transform, 0.3f, () =>
        {
            //키워드정보
            foreach (KeywordType keywordType in cardData.KeywordTypes)
            {
                string localizeKey = keywordType.ToString();
                string keyword = Localization.Instance.GetLocalizedText(localizeKey);
                string info = Localization.Instance.GetLocalizedText($"{localizeKey}Info");

                SetTooltipByData(info, keyword, true);
            }
            // 툴팁정보
            foreach (KeywordType keywordType in cardData.TooltipInfo)
            {
                string localizeKey = keywordType.ToString();
                string keyword = Localization.Instance.GetLocalizedText(localizeKey);
                string info = Localization.Instance.GetLocalizedText($"{localizeKey}Info");

                SetTooltipByData(info, keyword, true);
            }

            //카드를 생성하는 카드의 경우, 미리보기를 보여줍니다.
            if (cardData.PreviewCard != null)
            {
                SetTooltipByCardPreview(cardData.PreviewCard, true);
            }

            selectedTransform = transform;
            SetTooltipParentPosition(selectedTransform, false, new(UI_CARD_OFFSET_X, UI_CARD_OFFSET_Y));
        });
    }

    //유닛에 마우스 올렸을때 사용(버프 표시)
    public void SetTooltip(Transform transform, UnitBase unit)
    {
        StartTooltipRoutine(transform, 0.3f, () =>
        {
            //유닛의 이름과 사거리, 이동력을 표시합니다.
            EnemyEntitySO so = unit.GetTargetEnemySO();
            if (so == null) return;
            string name = Localization.Instance.GetLocalizedText(so.name);

            string unitInfo = $"<size=38><sprite={ATTACK_RANGE_IDX}></size><size=30>{unit.Range}</size> <size=38><sprite={MOVE_RANGE_IDX}></size><size=30>{unit.MoveRange}</size>";
            SetTooltipByData(unitInfo, name);

            ////우선 유닛의 행동이 존재하면 행동을 설명합니다.

            selectedTransform = transform;
            SetTooltipParentPosition(selectedTransform, true, new(UNIT_OFFSET_X, UNIT_OFFSET_Y));
        });
    }

    //버프ui에 마우스 올렸을 때 사용
    public void SetTooltip(Transform transform, BuffUI buffUI)
    {
        StartTooltipRoutine(transform, 0.3f, () =>
        {
            string key;
            string keyword;
            string info;
            //오브젝트 하단 버프류
            if (buffUI.BuffType != BuffType.None)
            {
                key = $"K_{buffUI.BuffType}";
                keyword = Localization.Instance.GetLocalizedText(key);
                info = Localization.Instance.GetLocalizedText($"{key}Info");
            }
            else
            {
                return;
            }

            SetTooltipByData(info, keyword, false);

            selectedTransform = transform;
            SetTooltipParentPosition(selectedTransform, true, new(RELIC_OFFSET_X, RELIC_OFFSET_Y));
        });
    }

    public void SetTooltip(Transform transform, BehaviorUI behaviorUI)
    {
        StartTooltipRoutine(transform, 0.3f, () =>
        {
            string key;
            string keyword;
            string info;
            //오브젝트 상단 행동류
            if (behaviorUI.BehaviorType != BehaviorType.None)
            {
                key = $"K_{behaviorUI.BehaviorType}";
                keyword = Localization.Instance.GetLocalizedText(key);
                info = Localization.Instance.GetLocalizedText($"{key}Info");
            }
            else
            {
                return;
            }

            SetTooltipByData(info, keyword, false);

            selectedTransform = transform;
            SetTooltipParentPosition(selectedTransform, true, new(RELIC_OFFSET_X, RELIC_OFFSET_Y));
        });
    }

    //유물에 마우스 올렸을때 사용(ui)
    public void SetTooltip(Transform transform, RelicEntitySO relicSO)
    {
        StartTooltipRoutine(transform, 0.3f, () =>
        {
            string keyword = Localization.Instance.GetLocalizedText(relicSO.NameKey);
            string info = Localization.Instance.GetLocalizedText($"{relicSO.NameKey}Info");
            //유물은 해당 유물의 Awake에서 이미 한글로 넣어뒀음
            SetTooltipByData(info, keyword, true);

            selectedTransform = transform;
            SetTooltipParentPosition(selectedTransform, false, new(RELIC_OFFSET_X, RELIC_OFFSET_Y));
        });
    }

    /// <summary>
    /// 대화가 미리 지정된 툴팁(UI용)
    /// </summary>
    public void SetTooltip(Transform targetPos, string contentKey, string titleKey = null, Vector2? offset = null)
    {
        StartTooltipRoutine(targetPos, 0.1f, () =>
        {
            string title = null;
            string content;
            //오브젝트 하단 버프류
            content = Localization.Instance.GetLocalizedText(contentKey);
            if (titleKey != null)
                title = Localization.Instance.GetLocalizedText(titleKey);

            SetTooltipByData(content, title);

            selectedTransform = targetPos;

            //따로 정해준 오프셋이 없으면 그대로
            if(offset != null)
                SetTooltipParentPosition(selectedTransform, false, offset.Value);
            else
                SetTooltipParentPosition(selectedTransform, false, new(RELIC_OFFSET_X, RELIC_OFFSET_Y));
        });
    }

    /// <summary>
    /// 대화형 툴팁. 매니저 전용(튜토리얼)
    /// </summary>
    public Tooltip SetTooltip(RectTransform tooltipPos, string content, string title = null, bool isManager = false)
    {
        // 코루틴이 이미 돌고 있다면 중단
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        //기존 툴팁 초기화
        HideTooltips();

        selectedTransform = tooltipPos;
        return SetTooltipByAdmin(tooltipPos, content, title, true);
    }
    #endregion

    #region 툴팁 데이터 삽입
    void SetTooltipByData(string content, string title = null, bool isOverUI = false)
    {
        //툴팁은 여러개가 동시에 존재할 수 있어야합니다.
        //툴팁을 여러개 표시할 경우, 툴팁의 크기를 고려해서 아래에 배치하도록.

        Tooltip tooltip;
        //1. 툴팁을 생성하고 크기를 정합니다.
        if (tooltipPool.Count > 0)
            tooltip = tooltipPool.Dequeue();
        else
        {
            tooltip = Instantiate(tooltipPrefab, tooltipParent).GetComponent<Tooltip>();
        }

        tooltip.SetTooltip(content, title);
        SetOrderToOverUI(isOverUI);

        //2. 생성한 툴팁을 배치합니다.

        //기존에 툴팁이 존재하면, 마지막 툴팁의 하단에 배치합니다.
        if (activeTooltips.Count > 0)
            tooltip.gameObject.transform.position = activeTooltips[activeTooltips.Count - 1].GetBottomPosition();

        activeTooltips.Add(tooltip);
        tooltip.ShowTooltip();
    }

    /// <summary>
    /// 프리뷰 생성 툴팁 전용
    /// </summary>
    void SetTooltipByCardPreview(CardEntitySO previewCard, bool isOverUI = false)
    {
        cardUIInstance.Initialize(previewCard, CardUISelectType.PreviewTooltip);

        //1. 툴팁을 생성하고 크기를 정합니다.

        Tooltip tooltip;
        if (tooltipPool.Count > 0)
            tooltip = tooltipPool.Dequeue();
        else
        {
            tooltip = Instantiate(tooltipPrefab, tooltipParent).GetComponent<Tooltip>();
        }
        tooltip.SetTooltipByCardPreview(cardUIInstance);
        SetOrderToOverUI(isOverUI);

        //2. 생성한 툴팁을 배치합니다.
        //기존에 툴팁이 존재하면, 마지막 툴팁의 하단에 배치합니다.
        if (activeTooltips.Count > 0)
            tooltip.gameObject.transform.position = activeTooltips[activeTooltips.Count - 1].GetBottomPosition();

        activeTooltips.Add(tooltip);
        tooltip.ShowTooltip();
    }

    /// <summary>
    /// 대화형 툴팁 전용
    /// </summary>
    Tooltip SetTooltipByAdmin(RectTransform tooltipPos, string content, string title = null, bool isOverUI = false)
    {
        Tooltip tooltip;
        //1. 툴팁을 생성하고 크기를 정합니다.
        if (tooltipPool.Count > 0)
            tooltip = tooltipPool.Dequeue();
        else
        {
            tooltip = Instantiate(tooltipPrefab, tooltipParent).GetComponent<Tooltip>();
        }

        tooltip.SetTooltip(content, title);
        SetOrderToOverUI(isOverUI);

        //2. 생성한 툴팁을 배치합니다.

        //기존에 툴팁이 존재하면, 마지막 툴팁의 하단에 배치합니다.
        if (activeTooltips.Count > 0)
            tooltip.gameObject.transform.position = activeTooltips[activeTooltips.Count - 1].GetBottomPosition();

        tooltipParent.anchoredPosition = tooltipPos.anchoredPosition;

        activeTooltips.Add(tooltip);
        tooltip.ShowTooltip();
        return tooltip;
    }

    #endregion

    /// <summary>
    /// targetTransform 기준으로 툴팁 부모를 배치한다.
    /// 기본은 오른쪽에 붙이고, 오른쪽이 넘치면 왼쪽에 붙인다.
    /// 툴팁은 부모 top에서 아래로 쌓이며, 세로는 화면 안으로 수직 보정한다.
    /// </summary>
    public void SetTooltipParentPosition(Transform targetTransform, bool isUnit = false, Vector2 offset = default)
    {
        if (targetTransform == null)
            return;

        Camera uc = CinemachineManager.Instance.UICamera;
        Camera wc = CinemachineManager.Instance.FieldCamera;

        // 1) 타겟 스크린 좌표
        Vector3 targetScreen = isUnit
            ? wc.WorldToScreenPoint(targetTransform.position)
            : uc.WorldToScreenPoint(targetTransform.position);

        // 2) offset을 "참조 해상도 단위 → 실제 스크린 픽셀" 로 변환
        // (OBJ_CARD_OFFSET_X 등은 1920x1080 기준 값이라고 가정)
        float scaleFactor = canvas.scaleFactor;
        Vector2 offsetScreen = offset * scaleFactor;

        // 3) 활성 툴팁들의 전체 높이/최대 너비를 스크린 픽셀 기준으로 계산
        float totalHeightScreen = 0f;
        float maxWidthScreen = tooltipWidth * scaleFactor; // fallback

        if (activeTooltips != null && activeTooltips.Count > 0)
        {
            for (int i = 0; i < activeTooltips.Count; i++)
            {
                float hLocal = Mathf.Max(0f, activeTooltips[i].GetHeight());
                float wLocal = activeTooltips[i].GetWidth();

                float hScreen = hLocal * scaleFactor;
                float wScreen = wLocal * scaleFactor;

                totalHeightScreen += hScreen;
                maxWidthScreen = Mathf.Max(maxWidthScreen, wScreen);

                if (i < activeTooltips.Count - 1)
                    totalHeightScreen += verticalSpacing * scaleFactor;
            }
        }

        // 4) 좌/우 배치 결정 (오른쪽 기본, 넘치면 왼쪽)
        float desiredLeftIfRight = targetScreen.x + offsetScreen.x;
        bool placeOnRight = (desiredLeftIfRight + maxWidthScreen) <= (Screen.width - screenMargin);

        float leftEdge;
        if (placeOnRight)
        {
            leftEdge = Mathf.Clamp(
                desiredLeftIfRight,
                screenMargin,
                Screen.width - screenMargin - maxWidthScreen
            );
        }
        else
        {
            float desiredLeftIfLeft = targetScreen.x - offsetScreen.x - maxWidthScreen;
            leftEdge = Mathf.Clamp(
                desiredLeftIfLeft,
                screenMargin,
                Screen.width - screenMargin - maxWidthScreen
            );
        }

        // 5) 수직 위치 계산 (스크린 픽셀 기준)
        float topY = targetScreen.y + offsetScreen.y;
        topY = Mathf.Min(topY, Screen.height - minTopPadding);

        float bottomY = topY - totalHeightScreen;
        if (bottomY < minBottomPadding)
        {
            float delta = minBottomPadding - bottomY;
            topY = Mathf.Min(topY + delta, Screen.height - minTopPadding);
        }

        // 6) 스크린 → 로컬(캔버스) 변환
        RectTransform parentSpace = tooltipParent.parent as RectTransform;
        if (parentSpace == null)
            parentSpace = tooltipParent;

        Vector2 screenPoint = new Vector2(leftEdge, topY);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentSpace,
            screenPoint,
            uc,
            out localPoint
        );
        tooltipParent.anchoredPosition = localPoint;

        // 7) 툴팁들을 부모 기준으로 위에서 아래로 쌓기 (로컬 단위)
        if (activeTooltips != null && activeTooltips.Count > 0)
        {
            float cursorY = 0f;
            for (int i = 0; i < activeTooltips.Count; i++)
            {
                var tip = activeTooltips[i];
                float hLocal = Mathf.Max(0f, tip.GetHeight());

                var tr = tip.transform as RectTransform;
                tr.anchoredPosition = new Vector2(0f, -cursorY);

                cursorY += hLocal;
                if (i < activeTooltips.Count - 1)
                    cursorY += verticalSpacing;
            }
        }
    }

    /// <summary>
    /// UI 오브젝트 위에 툴팁을 표시합니다.
    /// </summary>
    void SetOrderToOverUI(bool IsOverUI)
    {
        if (IsOverUI)
        {
            canvas.sortingLayerID = uiSortingLayerID;
        }
        else
        {
            canvas.sortingLayerID = tooltipSortingLayerID;
        }
    }
}