using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Somggori.UI {
	public class UIDirection : MonoBehaviour {
		[SerializeField] protected bool _playOnEnabled;
		[SerializeField] protected UnityEvent _endCb;

		protected CancellationTokenSource _cancellationToken;

		public bool IsPlaying { get; protected set; }

		private void OnEnable() {
			if (_playOnEnabled) {
				Play();
			}
		}

		public virtual void Play() {
			IsPlaying = true;
			_cancellationToken = new CancellationTokenSource();
			PlayAsync().Forget();
		}

		protected virtual async UniTask PlayAsync() {
			await UniTask.CompletedTask;
		}

		public virtual void Skip() {
			if (!IsPlaying) {
				return;
			}

			IsPlaying = false;
			_endCb?.Invoke();
			if (_cancellationToken != null) {
				_cancellationToken.Cancel();
				_cancellationToken.Dispose();
				_cancellationToken = null;
			}
		}

		public void AddEndCb(UnityAction callback) {
			if (_endCb == null) {
				_endCb = new UnityEvent();
			}

			_endCb.AddListener(callback);
		}

		public void RemoveEndCb(UnityAction callback) {
			if (_endCb == null) {
				return;
			}

			_endCb.RemoveListener(callback);
		}
	}
}