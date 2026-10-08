using UnityEngine;

namespace LSJ.UI {
	public class UIBase : MonoBehaviour {
		protected UIType uiType;

		// 초기화 (UIManager가 프리팹을 생성할 때 호출).
		public virtual void Init(UIType type) {
			uiType = type;
		}

		// UI가 열릴 때 호출되는 함수 (파라미터 수신).
		// args가 없으면 null.
		public virtual void Show(params object[] args) {
			gameObject.SetActive(true);
		}

		// UI가 닫힐 때 호출.
		public virtual void Hide() {
			gameObject.SetActive(false);
			UIEventHandler.LastSelected = null;
		}

		public virtual void OnCancel() { }
	}
}