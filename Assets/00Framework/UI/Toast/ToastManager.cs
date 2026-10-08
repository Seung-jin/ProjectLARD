using System.Collections.Generic;
using DG.Tweening;
using LSJ.Util;
using UnityEngine;

namespace Somggori.UI {
	/// <summary>
	/// 토스트 메시지를 관리하는 싱글톤 매니저
	/// 최대 3개의 토스트 메시지를 동시에 표시할 수 있습니다
	/// </summary>
	public class ToastManager : Singleton<ToastManager> {
		[Header("Settings")]
		[SerializeField] private GameObject _toastPrefab;

		[SerializeField] private Transform _toastContainer;
		[SerializeField] private int _maxToastCount = 3;
		[SerializeField] private float _toastSpacing = 80f;

		private Queue<ToastMessage> _toastPool = new Queue<ToastMessage>();
		private List<ToastMessage> _activeToasts = new List<ToastMessage>();

		/// <summary>
		/// 토스트 메시지를 표시합니다
		/// </summary>
		/// <param name="message">표시할 메시지</param>
		/// <param name="duration">메시지가 표시될 시간</param>
		public void ShowToast(string message, float duration = 1f) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}

			// 최대 개수를 초과하면 가장 오래된 토스트를 제거
			if (_activeToasts.Count >= _maxToastCount) {
				var oldestToast = _activeToasts[0];
				oldestToast.ForceHide();
				ReturnToPool(oldestToast);
			}

			// 풀에서 토스트 가져오기 또는 새로 생성
			ToastMessage toast = GetToastFromPool();
			if (toast == null) {
				return;
			}

			// 활성 토스트 리스트에 추가
			_activeToasts.Add(toast);

			// 위치 업데이트
			UpdateToastPositions();

			// 토스트 표시
			toast.Show(message, duration);
		}

		/// <summary>
		/// 풀에서 토스트를 가져오거나 새로 생성합니다
		/// </summary>
		private ToastMessage GetToastFromPool() {
			ToastMessage toast;

			if (_toastPool.Count > 0) {
				toast = _toastPool.Dequeue();
				toast.gameObject.SetActive(true);
			} else {
				if (_toastPrefab == null) {
					Debug.LogError("[ToastManager] Toast Prefab이 설정되지 않았습니다!");
					return null;
				}

				GameObject toastObj = Instantiate(_toastPrefab, _toastContainer);
				toast = toastObj.GetComponent<ToastMessage>();

				if (toast == null) {
					Debug.LogError("[ToastManager] Toast Prefab에 ToastMessage 컴포넌트가 없습니다!");
					Destroy(toastObj);
					return null;
				}
			}

			return toast;
		}

		/// <summary>
		/// 토스트를 풀로 반환합니다
		/// </summary>
		public void ReturnToPool(ToastMessage toast) {
			if (toast == null) {
				return;
			}

			// 활성 리스트에서 제거
			_activeToasts.Remove(toast);

			// 풀로 반환
			toast.gameObject.SetActive(false);
			_toastPool.Enqueue(toast);

			// 남은 토스트들의 위치 업데이트
			UpdateToastPositions();
		}

		/// <summary>
		/// 활성화된 모든 토스트의 위치를 업데이트합니다
		/// 아래에서 위로 쌓이는 형태로 배치됩니다
		/// </summary>
		private void UpdateToastPositions() {
			for (int i = 0; i < _activeToasts.Count; i++) {
				if (_activeToasts[i] == null) {
					continue;
				}

				RectTransform rectTransform = _activeToasts[i].GetComponent<RectTransform>();
				if (rectTransform != null) {
					// 아래에서 위로 쌓이도록 Y 위치 설정
					float targetY = i * _toastSpacing;

					// DoTween을 사용하여 부드럽게 위치 이동
					rectTransform.DOAnchorPosY(targetY, 0.3f).SetEase(Ease.OutQuad);
				}
			}
		}

		/// <summary>
		/// 모든 활성 토스트를 즉시 제거합니다
		/// </summary>
		public void ClearAllToasts() {
			for (int i = _activeToasts.Count - 1; i >= 0; i--) {
				if (_activeToasts[i] != null) {
					_activeToasts[i].ForceHide();
					ReturnToPool(_activeToasts[i]);
				}
			}

			_activeToasts.Clear();
		}

		protected override void OnDestroy() {
			base.OnDestroy();

			ClearAllToasts();
		}
	}
}