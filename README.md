# Project LARD

Unity 2D 스테이지 기반 자동 전투 프로젝트입니다. 플레이어는 기물을 소환하고 배치하며, 승급과 종족 강화를 통해 제한 시간 안에 몬스터를 처치합니다.

## 게임 흐름

1. 실버를 사용해 기물을 소환하고 전장 안에 배치합니다.
2. **START BATTLE**을 누르면 몬스터가 경로를 따라 등장하고 기물이 자동으로 공격합니다.
3. 몬스터 처치 보상으로 실버와 골드를 얻어 기물 승급, 추가 소환, 종족 강화에 사용합니다.
4. 제한 시간 내에 몬스터를 모두 처치하지 못하면 패배하며, 마지막 스테이지까지 완료하면 승리합니다.

### 조작

| 입력 | 동작 |
| --- | --- |
| 마우스 왼쪽 클릭 | 기물 선택 |
| 마우스 왼쪽 드래그 | 범위 내 기물 선택 |
| 마우스 오른쪽 클릭 | 선택한 기물 이동 |
| 전투 UI 버튼 | 소환, 기물 승급, 종족 강화, 전투 시작 및 재시작 |

## 주요 구현

- **전투 진행:** [`BattleManager`](Assets/01Scripts/Battle/BattleManager.cs)가 준비와 승패 상태를 관리하고, [`StageBattleLogic`](Assets/01Scripts/Stage/StageBattleLogic.cs)이 몬스터 소환과 제한 시간을 진행합니다.
- **기물과 이동:** [`StageArticleLogic`](Assets/01Scripts/Stage/StageArticleLogic.cs)이 기물 선택·배치·편대 이동을 담당합니다. [`ArticleObj`](Assets/01Scripts/Article/ArticleObj.cs)는 이동·그래픽·스킬 컴포넌트를 묶고, [`MoveComponentDestination`](Assets/01Scripts/Article/MoveComponentDestination.cs)은 목적지와 장애물 우회를 처리합니다.
- **소환과 성장:** [`ShopLogic`](Assets/01Scripts/Shop/ShopLogic.cs)이 확률에 따라 기물을 소환하며, [`ArticleLogic`](Assets/01Scripts/Article/ArticleLogic.cs)이 기물 승급과 판매를 처리합니다.
- **스킬과 효과:** [`SkillBase`](Assets/01Scripts/Skill/SkillBase.cs)와 [`EffectData`](Assets/01Scripts/Skill/EffectData.cs)를 통해 스킬의 대상, 적용 시점과 효과 값을 구성합니다.
- **데이터와 재사용:** [`DataManager`](Assets/01Scripts/Data/DataManager.cs)가 Addressables 데이터셋을 로드하고, [`ObjectPooler`](Assets/00Framework/Pool/ObjectPooler.cs)가 전투 오브젝트를 재사용합니다.

## 실행 방법

1. Unity Hub에서 이 폴더를 프로젝트로 엽니다. 사용한 에디터 버전은 **Unity 6000.6.0f1**입니다.
2. [`Assets/02Scenes/Battle.unity`](Assets/02Scenes/Battle.unity)을 열고 Play 모드를 시작합니다. 전투 UI 씬과 데이터는 시작 과정에서 로드됩니다.
3. 기물을 소환·배치한 뒤 **START BATTLE** 버튼을 누릅니다.

주요 패키지는 Unity Input System, Addressables, UniTask입니다. 프로젝트에는 Odin Inspector와 DOTween 플러그인도 포함되어 있습니다.

## 자동 전투 테스트

Unity 메뉴 **Tools > LARD > 자동 전투 테스트**에서 시드, 전략, 실행 횟수와 배속을 설정해 반복 전투를 실행할 수 있습니다. 테스트는 실제 전투 씬을 사용하며 실행별·스테이지별 결과를 기록합니다. 사용 방법과 결과 형식은 [`AUTO_BATTLE_TEST.md`](AUTO_BATTLE_TEST.md)에 정리했습니다.
