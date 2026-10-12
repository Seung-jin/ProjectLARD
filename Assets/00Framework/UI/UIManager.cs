using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LSJ.Util;
using UnityEngine;

namespace LSJ.UI {
	public enum UIType {
		None,

		// Battle
		Battle,
		Shop,
		ArticleInfo,
	}

	public class UIManager : Singleton<UIManager> {
		// [인스턴스] 현재 생성되어 있는 UI 오브젝트 캐싱
		private Dictionary<UIType, UIBase> _activeUIDict = new Dictionary<UIType, UIBase>();

		// [스택] 현재 활성화된 UI 스택 (Cancel 처리용)
		private List<UIBase> _uiStack = new List<UIBase>();

		// [필수] UI가 생성될 부모 캔버스 (씬마다 달라짐)
		[SerializeField] private Transform _uiRoot;

		// Addressable 기본 경로 : UI/Prefab/
		// 경로 추가 시 오브젝트 이름 앞에 나머지 경로 추가
		// 파일 확장자인 .prefab 입력은 필요하지 않음
		private static Dictionary<UIType, string> _prefabKeyDic = new() {

			{ UIType.Battle, "Battle/UIBattle" },
			{ UIType.Shop, "Shop/UIShop" },
		};

		// 데이터 로드 완료 여부
		public bool IsLoaded { get; private set; } = false;

		/// <summary>Show
		/// UI 열기 (비동기)
		/// </summary>
		public void Show(UIType type, params object[] args) {
			ShowAsync(type, args).Forget();
		}

		public async UniTask ShowAsync(UIType type, params object[] args) {
			// 1. 이미 생성되어 있는지 확인 (캐싱)
			if (_activeUIDict.TryGetValue(type, out UIBase activeUI)) {
				// 오브젝트가 파괴되지 않았는지 확인
				if (activeUI != null) {
					activeUI.gameObject.SetActive(true);
					activeUI.transform.SetAsLastSibling();
					activeUI.Show(args);

					// 스택 관리
					_uiStack.Remove(activeUI);
					_uiStack.Add(activeUI);
					return;
				} else {
					_activeUIDict.Remove(type);
				}
			}

			// 2. Enum을 Addressable Key로 변환
			// 예: UIType.Inventory -> "UI_Inventory"
			string addressKey = $"UI/Prefab/{_prefabKeyDic[type]}.prefab";

			// 3. Addressables를 통해 바로 인스턴스화 (비동기)
			try {
				// InstantiateAsync는 내부적으로 로드 + 생성을 수행함
				GameObject go = await AddressableManager.InstantiateAsync(addressKey, _uiRoot);

				if (go == null) {
					return;
				}

				UIBase newUI = go.GetComponent<UIBase>();
				if (newUI == null) {
					return;
				}

				// 초기화
				newUI.Init(type);
				newUI.Show(args);

				// 딕셔너리에 등록
				_activeUIDict.Add(type, newUI);

				// 스택 등록
				_uiStack.Add(newUI);
			} catch (System.Exception e) {
			}

			await UniTask.Yield();
		}

		/// <summary>
		/// UI 닫기 (비활성화)
		/// </summary>
		public void Hide(UIType type) {
			if (_activeUIDict.TryGetValue(type, out UIBase ui) && ui != null) {
				_uiStack.Remove(ui);
				ui.Hide();
			}
		}

		/// <summary>
		/// 열려있는 모든 UI 닫기
		/// </summary>
		public void HideAll() {
			foreach (var ui in _uiStack) {
				if (ui != null) {
					ui.Hide();
				}
			}

			_uiStack.Clear();
		}

		public T GetUI<T>(UIType type) where T : UIBase {
			if (_activeUIDict.TryGetValue(type, out UIBase ui) && ui != null) {
				return ui as T;
			}

			return null;
		}

		public void OnCancel() {
			// 가장 최근에 활성화된(스택의 마지막) UI부터 Cancel 처리
			if (_uiStack.Count > 0) {
				var topUI = _uiStack[^1];
				if (topUI != null && topUI.gameObject.activeInHierarchy) {
					topUI.OnCancel();
				}
			}
		}
	}
}
