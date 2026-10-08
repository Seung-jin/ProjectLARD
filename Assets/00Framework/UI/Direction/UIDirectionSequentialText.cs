using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Somggori.UI {
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class UIDirectionTextSequential : UIDirection {
		private TextMeshProUGUI _text;

		[SerializeField, Header("다음 텍스트 나오기까지 걸리는 시간")]
		private float _sequentialDelay = 0.5f;

		private string _textToPlay;

		private void Awake() {
			_text = GetComponent<TextMeshProUGUI>();
		}

		public void Play(string textToPlay) {
			_textToPlay = textToPlay;
			base.Play();
		}

		public override void Play() {
			_textToPlay = _text.text;
			base.Play();
		}

		protected override async UniTask PlayAsync() {
			_text.text = string.Empty;
			foreach (char oneChar in _textToPlay) {
				_text.text += oneChar;

				await UniTask.WaitForSeconds(_sequentialDelay, cancellationToken: _cancellationToken.Token);
			}

			IsPlaying = false;
			_endCb?.Invoke();
		}

		public override void Skip() {
			base.Skip();
			_text.text = _textToPlay;
		}
	}
}