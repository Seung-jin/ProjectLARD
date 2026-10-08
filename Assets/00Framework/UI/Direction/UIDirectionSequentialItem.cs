using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Somggori.UI {
	/// <summary>
	/// 자식 오브젝트 아이템들을 하나씩 나타나게 연출
	/// </summary>
	public class UIDirectionSequentialItem : UIDirection {
		protected List<Transform> _childTransformList = new List<Transform>();

		[SerializeField, Header("다음 오브젝트 나오기까지 걸리는 시간")]
		protected float _sequentialDelay;

		protected override async UniTask PlayAsync() {
			GetChildTransforms();

			foreach (Transform childTransform in _childTransformList) {
				childTransform.gameObject.SetActive(false);
			}

			foreach (Transform childTransform in _childTransformList) {
				childTransform.gameObject.SetActive(true);
				OnActiveChild(childTransform);

				await UniTask.WaitForSeconds(_sequentialDelay, cancellationToken: _cancellationToken.Token);
			}

			IsPlaying = false;
			_endCb?.Invoke();
		}

		protected virtual void GetChildTransforms() {
			_childTransformList.Clear();
			foreach (Transform child in transform) {
				_childTransformList.Add(child);
			}
		}

		protected virtual void OnActiveChild(Transform child) { }

		public override void Skip() {
			if (!IsPlaying) {
				return;
			}

			base.Skip();
			foreach (Transform childTransform in _childTransformList) {
				childTransform.gameObject.SetActive(true);
			}
		}
	}
}