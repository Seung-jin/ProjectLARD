using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Somggori.UI {
	[RequireComponent(typeof(Button))]
	public class UIButtonUtil : MonoBehaviour {
		[Header("Click"), SerializeField] private bool _isClickAnim;
		[SerializeField] private float _clickAnimSpeed = 0.1f;

		private Button _button;

		private void Awake() {
			_button = GetComponent<Button>();
		}

		private void Start() {
			if (_isClickAnim) {
				_button.onClick.AddListener(BtnClickAnim);
			}
		}

		private void BtnClickAnim() {
			_button.transform.DOKill();
			_button.transform.localScale = Vector3.one;
			_button.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), _clickAnimSpeed * 2);
		}
	}
}