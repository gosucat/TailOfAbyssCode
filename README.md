# Tail Of Abyss

타일 그리드 기반 **턴제 로그라이크 덱빌딩 게임**입니다.
적이 다음 턴 행동을 미리 예고하고, 플레이어는 그것을 읽고 카드로 이동·공격하며 위치를 잡습니다.

## 폴더 구조

```
02.Scripts/
│
├── CatsWork/                  프로젝트에 종속되지 않는 공용 라이브러리. 타 프로젝트에서도 사용
│   ├── Pathfinding/           A* 길찾기와 이진 힙 우선순위 큐 (직접 구현)
│   └── Tile.cs                타일 한 칸의 상태와 점유 유닛·오브젝트
│
├── Scene/
│   │
│   ├── BattleScene/           전투 씬에서만 쓰이는 코드
│   │   ├── Manager/           씬 초기화, 턴 흐름 제어
│   │   │   └── CardBattleManager/   손패·덱·버린 더미 등 카드 흐름 전반
│   │   ├── Object/            유닛 공통 베이스 (체력·버프 / 행동 / 길찾기로 분할)
│   │   │   └── Enemy/         적 유닛. 특수 패턴 위주로 발췌 (분열, 확산)
│   │   └── UI/
│   │       ├── CardInstance/  카드 오브젝트의 드래그·홀드 등 조작
│   │       └── HUD/           체력·마나, 행동 예고 표시 (+ UI 회전 셰이더)
│   │
│   └── DontDestroy/           씬이 바뀌어도 유지되는 전역 시스템
│       ├── Manager/           씬 전환, 필드(타일) 관리
│       │   └── BehaviorSystem/     적의 다음 턴 행동을 미리 계산·예고
│       ├── Object/
│       │   ├── Card/          카드 데이터와 카드별 효과
│       │   │   └── CardFunction/   효과 인터페이스·베이스·자동 등록 팩토리
│       │   │       └── CardFunctions/  대표 카드 효과 (강화형 A/B 포함)
│       │   └── PlaceableObject/    필드 배치물 베이스와 덫
│       └── UI/                툴팁
│
└── Utility/                   로컬라이징(한/영/일), 범용 헬퍼
    └── DialogueSystem/        대사 재생, 호감도 분기

08.SO/                         ScriptableObject 데이터 정의
├── CardEntitySO/              카드 (코스트·수치·사거리·효과 범위·강화 연결)
├── EnemyEntitySO/             적 (스탯·행동 우선권·예고 트리거 범위)
└── DialogueSO/                대사 시퀀스
```