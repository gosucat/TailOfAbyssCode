using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CatsWork;
using DG.Tweening;

public enum PlaceableObjectType
{
    None,
    Trap,
    Building

}

public class PlaceableObjectBase : MonoBehaviour
{
    public PlaceableObjectType Type { get; set; }
    public bool IsBlocked { get; set; }
    /// <summary>
    /// 그리드상 위치
    /// </summary>
    public Vector2Int GridPosition { get; set; }
    /// <summary>
    /// 실제 위치
    /// </summary>
    protected Vector3 CurrentPosition { get; set; }

    public int Durability
    {
        get { return durability; }
        set
        {
            durability = value;
            if (durability <= 0)
            {
                StartCoroutine(OnObjectDestroyCo());
            }
        }

    } int durability;

    [SerializeField] protected SpriteRenderer sr;

    [Header("기준 칸을 제외한 추가 칸(타일 점유)")]
    [SerializeField] protected List<Vector2Int> additionalGridPos;

    public virtual void Init(Tile targetTile)
    {
        targetTile.SetPlaceableObject(this);
        GridPosition = targetTile.GridPosition;
        CurrentPosition = FieldManager.Instance.GetTilePosition(targetTile);
        transform.position = CurrentPosition;

        //추가 공간을 타일에 직접 할당해줍니다.
        foreach (Vector2Int pos in additionalGridPos)
        {
            Vector2Int targetPos = GridPosition + pos;
            Tile addtionalTile = FieldManager.Instance.GetTileFromPosition(targetPos);
            if (addtionalTile != null)
                addtionalTile.SetPlaceableObject(this);
        }

        SpawnEffect(CurrentPosition);
    }

    protected virtual void SpawnEffect(Vector3 SpawnPosition)
    {
        StartCoroutine(SpawnEffectCo(SpawnPosition));
    }

    IEnumerator SpawnEffectCo(Vector3 SpawnPosition)
    {
        if (sr == null) yield break;

        float side;
        if (FieldManager.Instance.PlayerInstance.GridPosition.x <= GridPosition.x)
            side = 0.2f;
        else
            side = -0.2f;

        transform.position = SpawnPosition;

        Vector3 originalLocalPos = sr.transform.localPosition;
        Vector3 startLocalPos = originalLocalPos + new Vector3(side, 0f, 0f);

        sr.transform.localPosition = startLocalPos;

        Color originalColor = sr.color;
        Color startColor = new Color(0f, 0f, 0f, 0f);
        sr.color = startColor;

        float t = 0f;
        float duration = 0.5f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float k = Mathf.SmoothStep(0f, 1f, n * 3);

            sr.transform.localPosition = Vector3.Lerp(startLocalPos, originalLocalPos, k);
            sr.color = Color.Lerp(startColor, originalColor, k);

            yield return null;
        }

        sr.transform.localPosition = originalLocalPos;
        sr.color = originalColor;
    }

    public virtual void Move(Tile destTile)
    {
        //기존 타일 해제 작업
        FieldManager.Instance.Tiles[GridPosition].DisposePlaceableObject();

        //이동할 타일 작업
        destTile.SetPlaceableObject(this);
        CurrentPosition = FieldManager.Instance.GetTilePosition(destTile);
        GridPosition = destTile.GridPosition;
        PositionUpdate();
    }
    protected virtual void PositionUpdate()
    {
        gameObject.transform.DOMove(CurrentPosition, 0.1f).SetEase(Ease.OutQuad);
    }



    // 오브젝트가 맞을때

    float hitShakeDuration = 0.12f;   // 흔들리는 시간
    float hitShakeStrength = 0.11f;   // 흔들리는 거리(유닛)
    int hitShakeVibrato = 10;         // 흔들림 빈도(프레임 느낌)

    Coroutine hitShakeCo;
    public virtual void OnObjectHit()
    {
        Durability--;

        if (hitShakeCo != null)
        {
            StopCoroutine(hitShakeCo);
        }
        transform.localPosition = CurrentPosition;
        hitShakeCo = StartCoroutine(CoHitShake());
    }

    IEnumerator CoHitShake()
    {
        float t = 0f;

        while (t < hitShakeDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / hitShakeDuration);
            float damper = 1f - n;
            float x = Mathf.Sin(n * hitShakeVibrato * Mathf.PI * 2f) * hitShakeStrength * damper;

            transform.localPosition = CurrentPosition + new Vector3(x, 0f, 0f);
            yield return null;
        }

        transform.localPosition = CurrentPosition;
        hitShakeCo = null;
    }

    /// <summary>
    /// 오브젝트가 밟힐때
    /// </summary>
    public virtual void OnObjectStepped(UnitBase type)
    {
        

    }



    protected virtual IEnumerator OnObjectDestroyCo()
    {
        //실제 타일 간섭 구간
        Tile targetTile = FieldManager.Instance.Tiles[GridPosition];
        targetTile.DisposePlaceableObject();

        foreach (Vector2Int pos in additionalGridPos)
        {
            Vector2Int targetPos = GridPosition + pos;
            Tile addtionalTile = FieldManager.Instance.GetTileFromPosition(targetPos);
            if (addtionalTile != null)
                addtionalTile.DisposePlaceableObject();
        }

        BattleSceneManager.Instance.RemovePlaceableObject(this);

        if (TurnManager.Instance.IsMyTurn && IsBlocked)
            BehaviorManager.Instance.CalcAllEnemyBehavior();


        //연출 구간

        // 시작 시 색상 저장
        Color originalColor = sr.color;

        float t = 0f;
        float duration = 0.5f; // 연출 시간

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float k = Mathf.SmoothStep(0f, 1f, n);

            // 점점 어두워지고 투명해짐
            // originalColor에서 검은색(0,0,0)으로 보간하고, 동시에 알파는 0으로 감소
            Color targetColor = Color.Lerp(originalColor, Color.black, k);
            targetColor.a = Mathf.Lerp(originalColor.a, 0f, k);
            sr.color = targetColor;

            yield return null;
        }
        sr.color = new Color(0f, 0f, 0f, 0f);


        Dispose();
    }

    /// <summary>
    /// 초기화용
    /// </summary>
    public virtual void DestroyObject()
    {
        Tile targetTile = FieldManager.Instance.Tiles[GridPosition];
        targetTile.DisposePlaceableObject();

        foreach (Vector2Int pos in additionalGridPos)
        {
            Vector2Int targetPos = GridPosition + pos;
            Tile addtionalTile = FieldManager.Instance.GetTileFromPosition(targetPos);
            if (addtionalTile != null)
                addtionalTile.DisposePlaceableObject();
        }


        Dispose();
    }

    protected virtual void Dispose()
    {
        Destroy(gameObject);
    }
}
