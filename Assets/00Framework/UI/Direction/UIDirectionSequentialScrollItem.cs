using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Somggori.UI {
	/// <summary>
	/// 스크롤에 있는 아이템들을 하나씩 나타나게 연출
	/// </summary>
	[RequireComponent(typeof(ScrollRect))]
	public class UIDirectionSequentialScrollItem : UIDirectionSequentialItem {
		private ScrollRect _scrollRect;

		[SerializeField, Header("스크롤 끝으로 이동 여부")]
		private bool _isMoveToLast;

		private void Awake() {
			_scrollRect = GetComponent<ScrollRect>();
		}

		protected override void GetChildTransforms() {
			_childTransformList.Clear();
			foreach (Transform child in _scrollRect.content) {
				_childTransformList.Add(child);
			}
		}

		protected override void OnActiveChild(Transform child) {
			if (_isMoveToLast) {
				MoveToLastAsync().Forget();
			}
		}

		private async UniTaskVoid MoveToLastAsync() {
			await UniTask.NextFrame(cancellationToken: _cancellationToken.Token);

			if (_scrollRect.vertical) {
				_scrollRect.verticalNormalizedPosition = 0f;
			}

			if (_scrollRect.horizontal) {
				_scrollRect.horizontalNormalizedPosition = 1f;
			}
		}

		public override void Skip() {
			base.Skip();

			if (_isMoveToLast) {
				if (_scrollRect.vertical) {
					_scrollRect.verticalNormalizedPosition = 1f;
				}

				if (_scrollRect.horizontal) {
					_scrollRect.horizontalNormalizedPosition = 1f;
				}
			}
		}
	}
}