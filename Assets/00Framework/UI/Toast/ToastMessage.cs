using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Somggori.UI {
	/// <summary>
	/// 개별 토스트 메시지 UI 컴포넌트
	/// </summary>
	public class ToastMessage : MonoBehaviour {
		[SerializeField] private TextMeshProUGUI _messageText;
		[SerializeField] private CanvasGroup _canvasGroup;
		[SerializeField] private RectTransform _rectTransform;

		private Sequence _animationSequence;
		private bool _isAnimating = false;

		/// <summary>
		/// 토스트 메시지를 표시합니다
		/// </summary>
		/// <param name="message">표시할 메시지</param>
		/// <param name="duration">메시지가 표시될 시간 (초)</param>
		public void Show(string message, float duration = 2f) {
			if (_isAnimating) {
				_animationSequence?.Kill();
			}

			_messageText.text = message;
			_isAnimating = true;

			// 초기 상태 설정
			_canvasGroup.alpha = 0f;
			_rectTransform.localScale = Vector3.one * 0.8f;

			// 애니메이션 시퀀스 생성
			_animationSequence = DOTween.Sequence();

			// 1. 페이드 인 + 스케일 업 (0.3초)
			_animationSequence.Append(_canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad));
			_animationSequence.Join(_rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

			// 2. 유지 (duration초)
			_animationSequence.AppendInterval(duration);

			// 3. 페이드 아웃 (0.3초)
			_animationSequence.Append(_canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad));

			// 4. 애니메이션 완료 후 처리
			_animationSequence.OnComplete(() => {
				_isAnimating = false;
				ToastManager.Instance.ReturnToPool(this);
			});
		}

		/// <summary>
		/// 애니메이션을 즉시 중단하고 초기화합니다
		/// </summary>
		public void ForceHide() {
			_animationSequence?.Kill();
			_isAnimating = false;
			_canvasGroup.alpha = 0f;
			_rectTransform.localScale = Vector3.one;
		}

		private void OnDestroy() {
			_animationSequence?.Kill();
		}
	}
}