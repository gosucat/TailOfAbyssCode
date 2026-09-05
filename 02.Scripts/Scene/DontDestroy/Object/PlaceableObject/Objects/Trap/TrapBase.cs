using System.Collections;
using UnityEngine;
using CatsWork;
using DG.Tweening;

public class TrapBase : PlaceableObjectBase
{
    [Header("덫 스펙")]
    [SerializeField] public int TrapDamage = 5;

    [Header("발동 횟수 (0 이하 = 무제한)")]
    [SerializeField] protected int triggerCount = 1;

    [Header("공격력 HUD (유닛의 Attack 행동 예고 HUD 재사용)")]
    [SerializeField] private float hudHeight = 0.5f;
    [SerializeField] private Vector2 hudOffset = Vector2.zero;

    [SerializeField] private ParticleSystem activateEffect; // 덫 발동 이펙트

    [Header("밟았을 때 데미지를 줄 대상")]
    [SerializeField] protected bool damagePlayer = true;
    [SerializeField] protected bool damageEnemy = true;

    [Header("다른 인장에 흡수될 때 연출 시간")]
    [SerializeField] private float absorbDuration = 0.3f;

    // 덫 전용 HUD. 아이콘은 유닛의 Attack 예고 HUD와 같지만 BehaviorType.TrapAttack 을 써서
    // 툴팁이 "덫"이라는 것을 알려준다. 행동 시스템에는 참여하지 않고 프리팹 비주얼만 재사용한다.
    protected BehaviorUI attackHud;

    public override void Init(Tile targetTile)
    {
        Type = PlaceableObjectType.Trap;

        IsBlocked = false;
        Durability = 1;

        // 부모의 초기화 로직 (위치 할당, 타일에 오브젝트 등록 등) 실행
        base.Init(targetTile);

        CreateAttackHud();
        RefreshHud();
    }

    private void CreateAttackHud()
    {
        if (attackHud != null) return;

        if (!BehaviorManager.Instance.BehaviorList.TryGetValue(BehaviorType.TrapAttack, out GameObject prefab))
            return;

        attackHud = Instantiate(prefab, BattleSceneManager.Instance.HudCanvasRect).GetComponent<BehaviorUI>();

        Transform followTarget = sr != null ? sr.transform : transform;
        attackHud.InitBehavior(followTarget, hudHeight, hudOffset);
        attackHud.Show();
    }

    public virtual void SetDamage(int damage)
    {
        TrapDamage = Mathf.Max(0, damage);
        RefreshHud();
    }

    public virtual void SetTriggerCount(int count)
    {
        triggerCount = count;
    }

    protected void RefreshHud()
    {
        if (attackHud == null || attackHud.ValueText == null) return;

        attackHud.ValueText.enabled = true;
        attackHud.ValueText.text = TrapDamage.ToString();
    }

    /// <summary>
    /// 이 덫 위에 새로운 덫이 설치될 때 호출된다. (새 덫은 별도로 생성되지 않는다)
    /// 기본 동작: 새 덫의 공격력을 이 덫에 합산한다.
    /// 개별 덫이 오버라이드하여, 위에 올라온 덫 종류(incomingPrefab)에 따라 다른 효과를 낼 수 있다.
    /// </summary>
    public virtual void OnTrapStacked(TrapBase incomingPrefab, int incomingDamage)
    {
        SetDamage(TrapDamage + incomingDamage);
    }

    /// <summary>
    /// 다른 인장에 흡수될 때 호출한다.
    /// 게임 로직에서의 분리(타일 해제 / 목록 제거)는 호출하는 쪽이 이미 끝냈다고 가정하며,
    /// 이 메서드는 대상 위치로 빨려들어가는 연출만 재생한 뒤 스스로 사라진다.
    /// </summary>
    public void AbsorbInto(Vector3 destination, float delay = 0f)
    {
        // HUD 는 트랜스폼을 따라다니는 별도 오브젝트다. 남겨두면 여러 인장의 숫자가
        // 한 칸에 겹치고, 오브젝트만 파괴할 경우 HudCanvas 에 영구히 남는다.
        if (attackHud != null)
        {
            attackHud.Destroy();
            attackHud = null;
        }

        StartCoroutine(AbsorbIntoCo(destination, delay));
    }

    private IEnumerator AbsorbIntoCo(Vector3 destination, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        transform.DOMove(destination, absorbDuration).SetEase(Ease.InQuad);

        Color originalColor = sr != null ? sr.color : Color.white;

        float t = 0f;
        while (t < absorbDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / absorbDuration);

            if (sr != null)
            {
                // 빨려들어가며 밝아지다가 마지막에 사라진다
                Color c = Color.Lerp(originalColor, Color.white, n);
                c.a = Mathf.Lerp(originalColor.a, 0f, n * n);
                sr.color = c;
            }

            yield return null;
        }

        transform.DOKill();
        Destroy(gameObject);
    }

    public override void OnObjectStepped(UnitBase unit)
    {
        base.OnObjectStepped(unit);

        if (unit == null) return;

        // 데미지 대상 필터
        bool isPlayer = unit.UnitType == UnitType.Player;
        if (isPlayer && !damagePlayer) return;
        if (!isPlayer && !damageEnemy) return;

        unit.TakeDamage(TrapDamage);

        // 발동 이펙트 재생
        if (activateEffect != null)
            activateEffect.Play();

        // 발동 횟수 소진 시 파괴. triggerCount <= 0 으로 설정된 덫은 무제한.
        if (triggerCount > 0)
        {
            triggerCount--;
            if (triggerCount <= 0)
                Durability = 0;
        }
    }

    public override void OnObjectHit()
    {
        //TODO : 특정 카드로만 부술 수 있음
    }

    protected override void Dispose()
    {
        if (attackHud != null)
            attackHud.Destroy();

        base.Dispose();
    }
}
