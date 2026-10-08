using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Somggori.UI {
	public class UIPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
		public Action OnPress;
		public Action OnRelease;
		private bool _isPressed;

		public bool IsPressed => _isPressed;

		public void OnPointerDown(PointerEventData eventData) {
			_isPressed = true;
			OnPress?.Invoke();
		}

		public void OnPointerUp(PointerEventData eventData) {
			_isPressed = false;
			OnRelease?.Invoke();
		}

		private void OnDisable() {
			_isPressed = false;
		}
	}
}