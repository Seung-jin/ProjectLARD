using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Globalization;

namespace LSJ.Battle {
	public class DamageText : PoolingObject<DamageText> {
		[SerializeField] private TextMeshPro _text;
		[SerializeField] private float _duration = 1f;
		[SerializeField] private float _moveDistance = 1f;

		private Color _startColor;
		private Vector3 _startScale;
		private Sequence _tweenSequence;

		private void Awake() {
			if (_text != null) {
				_startColor = _text.color;
			}

			_startScale = transform.localScale;
		}

		public void Build(params object[] param) {
			if (param.Length > 0 && param[0] is string text) {
				_text.text = text;
			} else if (param.Length > 0 && param[0] is int damage) {
				_text.text = damage.ToString("#,##0", CultureInfo.CurrentCulture);
			}

			Vector3 randomOffset = (Vector3)(Random.insideUnitCircle * 0.1f);
			if (param.Length > 1 && param[1] is Vector3 position) {
				transform.position = position + randomOffset;
			}

			// 부모 설정 (args[2]가 Transform인 경우)
			if (param.Length > 2 && param[2] is Transform parent) {
				transform.SetParent(parent, false);
				transform.localPosition = randomOffset;
			}

			if (_text != null) {
				_text.color = _startColor;
			}

			transform.localScale = _startScale;

			// 기존 트윈 제거
			_tweenSequence?.Kill();

			// DOTween 시퀀스 생성
			_tweenSequence = DOTween.Sequence();
			_tweenSequence.Append(transform.DOLocalMoveY(_moveDistance, _duration)
			                               .SetEase(Ease.OutQuad))
			              .Join(_text.DOFade(0f, _duration).SetEase(Ease.InQuad))
			              .OnComplete(Release);
		}

		public void OnDeactivate() {
			// 트윈 정리
			_tweenSequence?.Kill();
			_tweenSequence = null;
		}

		private void OnDestroy() {
			_tweenSequence?.Kill();
			_tweenSequence = null;
		}
	}
}