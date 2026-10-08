# 토스트 메시지 시스템 사용 가이드

## 개요
DoTween을 사용한 자연스러운 애니메이션의 토스트 메시지 시스템입니다.
최대 3개의 메시지를 동시에 표시할 수 있으며, 오브젝트 풀링을 통해 효율적으로 관리됩니다.

## Unity 에디터 설정

### 1. Toast Prefab 생성

#### 1-1. 새 GameObject 생성
- Hierarchy에서 우클릭 → UI → Panel
- 이름을 "ToastMessage"로 변경

#### 1-2. ToastMessage 오브젝트 설정
- **RectTransform 설정:**
  - Width: 400
  - Height: 60
  - Anchor: Bottom Center (또는 원하는 위치)
  
- **Image 컴포넌트 설정:**
  - Color: 반투명 검은색 (R:0, G:0, B:0, A:200)
  - 또는 원하는 배경 색상

#### 1-3. Text 추가
- ToastMessage 오브젝트 하위에 TextMeshProUGUI 추가
  - 우클릭 → UI → Text - TextMeshPro
  - 이름: "MessageText"
  
- **TextMeshProUGUI 설정:**
  - Anchor: Stretch (전체 영역)
  - Margins: Left:10, Right:10, Top:5, Bottom:5
  - Alignment: Center & Middle
  - Font Size: 18-24
  - Color: White
  - Overflow: Ellipsis (긴 텍스트 처리)

#### 1-4. CanvasGroup 추가
- ToastMessage 오브젝트에 CanvasGroup 컴포넌트 추가
  - Add Component → Canvas Group

#### 1-5. ToastMessage 스크립트 추가
- ToastMessage 오브젝트에 ToastMessage.cs 스크립트 추가
- Inspector에서 참조 연결:
  - Message Text: MessageText 오브젝트 드래그
  - Canvas Group: 자동으로 연결되거나 수동으로 드래그
  - Rect Transform: 자동으로 연결되거나 수동으로 드래그

#### 1-6. Prefab으로 저장
- ToastMessage 오브젝트를 Project 창의 적절한 폴더로 드래그하여 Prefab 생성
  - 권장 경로: Assets/Prefabs/UI/ToastMessage.prefab
- Hierarchy에서 ToastMessage 오브젝트 삭제

### 2. ToastManager 설정

#### 2-1. Canvas에 ToastManager 추가
- 씬의 Canvas 오브젝트 선택
- Add Component → ToastManager 스크립트 추가

#### 2-2. Toast Container 생성
- Canvas 하위에 빈 GameObject 생성
  - 이름: "ToastContainer"
  - RectTransform 설정:
    - Anchor: Bottom Center (또는 원하는 위치)
    - Pivot: (0.5, 0)
    - Position Y: 100 (화면 하단에서 100픽셀 위)

#### 2-3. ToastManager Inspector 설정
- **Toast Prefab:** 앞서 만든 ToastMessage Prefab 드래그
- **Toast Container:** ToastContainer 오브젝트 드래그
- **Max Toast Count:** 3 (기본값, 동시에 표시할 최대 개수)
- **Toast Spacing:** 80 (토스트 간 간격, 픽셀 단위)

## 코드 사용 예제

### 기본 사용법

```csharp
using DnFLuckyDefense.UI;

// 기본 토스트 표시 (2초 동안 표시)
ToastManager.Instance.ShowToast("골드를 획득했습니다!");

// 표시 시간 지정 (3초 동안 표시)
ToastManager.Instance.ShowToast("레벨 업!", 3f);
```

### 게임 로직에서 사용 예제

```csharp
using DnFLuckyDefense.UI;
using UnityEngine;

public class GameController : MonoBehaviour {
    public void OnPlayerLevelUp(int level) {
        ToastManager.Instance.ShowToast($"레벨 {level} 달성!");
    }
    
    public void OnGoldEarned(int amount) {
        ToastManager.Instance.ShowToast($"골드 +{amount}");
    }
    
    public void OnItemAcquired(string itemName) {
        ToastManager.Instance.ShowToast($"{itemName}을(를) 획득했습니다!", 2.5f);
    }
    
    public void OnError(string errorMessage) {
        ToastManager.Instance.ShowToast($"오류: {errorMessage}", 3f);
    }
}
```

### 업그레이드 시스템 연동 예제

```csharp
using DnFLuckyDefense.UI;
using DnFLuckyDefense.Player;

public class UpgradeSystem : MonoBehaviour {
    public void OnUpgradeSuccess(UpgradeType type, int level) {
        string message = type switch {
            UpgradeType.AttackPower => $"공격력 업그레이드 Lv.{level}",
            UpgradeType.AttackSpeed => $"공격속도 업그레이드 Lv.{level}",
            UpgradeType.CriticalRate => $"치명타율 업그레이드 Lv.{level}",
            _ => "업그레이드 완료!"
        };
        
        ToastManager.Instance.ShowToast(message);
    }
    
    public void OnUpgradeFailed(string reason) {
        ToastManager.Instance.ShowToast($"업그레이드 실패: {reason}", 2.5f);
    }
}
```

### 모든 토스트 제거

```csharp
// 씬 전환 시 또는 필요한 경우 모든 토스트를 즉시 제거
ToastManager.Instance.ClearAllToasts();
```

## 애니메이션 상세

### 페이드 인 (0.3초)
- 알파값: 0 → 1 (Ease.OutQuad)
- 스케일: 0.8 → 1.0 (Ease.OutBack) - 약간 튀는 효과

### 유지 (기본 2초)
- 메시지가 화면에 표시되는 시간
- ShowToast의 duration 파라미터로 조절 가능

### 페이드 아웃 (0.3초)
- 알파값: 1 → 0 (Ease.InQuad)

### 위치 이동
- 새 토스트가 추가되면 기존 토스트들이 위로 부드럽게 이동
- 최대 3개까지만 표시되며, 초과 시 가장 오래된 토스트가 자동으로 제거됨

## 커스터마이징

### 최대 토스트 개수 변경
ToastManager Inspector에서 "Max Toast Count" 값을 변경하세요.

### 토스트 간격 조절
ToastManager Inspector에서 "Toast Spacing" 값을 변경하세요.

### 애니메이션 커스터마이징
ToastMessage.cs의 Show() 메서드에서 DOTween 애니메이션을 수정할 수 있습니다:
- 페이드 인/아웃 시간 조절
- Ease 타입 변경
- 추가 애니메이션 효과 (회전, 이동 등)

### 스타일 변경
ToastMessage Prefab의 Image, TextMeshProUGUI 컴포넌트를 수정하여:
- 배경 색상/이미지 변경
- 텍스트 폰트, 크기, 색상 변경
- 크기 및 모양 조절

## 주의사항

1. **DoTween 필수**: 이 시스템은 DOTween을 사용합니다. 프로젝트에 DOTween이 설치되어 있어야 합니다.

2. **Singleton 패턴**: ToastManager는 싱글톤으로 구현되어 있으며, 씬에 하나만 존재해야 합니다.

3. **Canvas 필수**: ToastManager는 Canvas 하위에 있어야 정상적으로 작동합니다.

4. **TextMeshPro**: TextMeshProUGUI를 사용하므로 TextMeshPro 패키지가 필요합니다.

5. **오브젝트 풀링**: 토스트는 자동으로 풀링되어 재사용되므로, 성능 걱정 없이 자주 호출할 수 있습니다.

## 트러블슈팅

### 토스트가 표시되지 않는 경우
1. ToastManager의 Toast Prefab이 설정되어 있는지 확인
2. Toast Container가 올바르게 연결되어 있는지 확인
3. Canvas의 Render Mode가 올바르게 설정되어 있는지 확인
4. ToastMessage Prefab의 참조들이 모두 연결되어 있는지 확인

### 애니메이션이 작동하지 않는 경우
1. DOTween이 프로젝트에 설치되어 있는지 확인
2. ToastMessage의 CanvasGroup과 RectTransform이 연결되어 있는지 확인

### 위치가 이상한 경우
1. ToastContainer의 Anchor와 Pivot 설정 확인
2. Toast Spacing 값 조절
3. ToastMessage Prefab의 Anchor 설정 확인