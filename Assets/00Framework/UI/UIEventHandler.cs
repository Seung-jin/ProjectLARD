using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LSJ.UI {
	[RequireComponent(typeof(Selectable))]
	public class UIEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
	                              ISelectHandler, IDeselectHandler, ISubmitHandler {
		public Action OnPointerEnterAction;
		public Action OnPointerExitAction;
		public Action OnClickAction;
		public Action OnSelectAction;
		public Action OnDeselectAction;

		private Selectable _selectable;

		private static GameObject _lastSelected;

		/// <summary>
		/// 마지막으로 선택된 UI 오브젝트. 변경 시 자동으로 EventSystem에 반영됩니다.
		/// </summary>
		public static GameObject LastSelected {
			get => _lastSelected;
			set {
				_lastSelected = value;
				if (EventSystem.current != null && _lastSelected != null && _lastSelected.activeInHierarchy) {
					if (EventSystem.current.currentSelectedGameObject != _lastSelected) {
						EventSystem.current.SetSelectedGameObject(_lastSelected);
					}
				}
			}
		}

		private void Awake() {
			_selectable = GetComponent<Selectable>();
		}

		private void Update() {
			if (EventSystem.current == null) {
				return;
			}

			// 전역 상태 관리를 위해, 현재 컴포넌트가 LastSelected일 때만 갱신을 수행하여 중복 호출을 방지합니다.
			if (LastSelected == gameObject) {
				if (EventSystem.current.currentSelectedGameObject == null && gameObject.activeInHierarchy) {
					EventSystem.current.SetSelectedGameObject(gameObject);
				}
			}
		}

		public void OnPointerEnter(PointerEventData eventData) {
			if (!_selectable.interactable) {
				return;
			}

			// 마우스 오버 시 자동으로 해당 오브젝트를 선택 상태로 변경 (키보드/패드와 싱크)
			if (EventSystem.current != null) {
				if (EventSystem.current.currentSelectedGameObject != gameObject) {
					EventSystem.current.SetSelectedGameObject(gameObject);
				}
			}

			OnPointerEnterAction?.Invoke();
		}

		public void OnPointerExit(PointerEventData eventData) {
			OnPointerExitAction?.Invoke();
		}

		public void OnPointerClick(PointerEventData eventData) {
			if (!_selectable.interactable) {
				return;
			}

			OnClickAction?.Invoke();
		}

		public void OnSelect(BaseEventData eventData) {
			_lastSelected = gameObject; // SetSelectedGameObject 무한 루프를 막기 위해 필드에 직접 할당
			OnSelectAction?.Invoke();
		}

		public void OnDeselect(BaseEventData eventData) {
			OnDeselectAction?.Invoke();
		}

		public void OnSubmit(BaseEventData eventData) {
			if (!_selectable.interactable) {
				return;
			}

			OnClickAction?.Invoke();
		}
	}
}