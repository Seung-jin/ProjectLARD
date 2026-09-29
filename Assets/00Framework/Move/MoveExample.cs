using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MoveExample : MonoBehaviour {
	[SerializeField] private List<MoveComponent> _moveComponents;

	[SerializeField] private Transform _target;
	[SerializeField] private Vector2 _parabolaPos;

	private Vector3[] _startPositions;
	private bool _isMoving;

	private void Awake() {
		if (_moveComponents == null || _moveComponents.Count == 0) {
			return;
		}

		_startPositions = new Vector3[_moveComponents.Count];

		for (int i = 0; i < _moveComponents.Count; i++) {
			if (_moveComponents[i] == null) {
				continue;
			}

			_startPositions[i] = _moveComponents[i].transform.position;
			_moveComponents[i].enabled = false;
		}
	}

	private void Start() {
		if (_moveComponents == null || _moveComponents.Count == 0) {
			return;
		}

		if (_moveComponents.Count > 0 && _moveComponents[0] is MoveComponentDirection moveComponentDirection) {
			moveComponentDirection.SetDirection(Vector2.right);
		}

		if (_moveComponents.Count > 1 && _moveComponents[1] is MoveComponentTarget moveComponentTarget) {
			moveComponentTarget.SetTarget(_target);
		}

		if (_moveComponents.Count > 2 && _moveComponents[2] is MoveComponentParabola moveComponentParabola) {
			moveComponentParabola.SetDestination(_parabolaPos);
		}
	}

	public void StartMove() {
		if (_isMoving || _moveComponents == null || _moveComponents.Count == 0) {
			return;
		}

		_isMoving = true;

		for (int i = 0; i < _moveComponents.Count; i++) {
			if (_moveComponents[i] == null) {
				continue;
			}

			_moveComponents[i].transform.position = _startPositions[i];
			_moveComponents[i].enabled = true;
			_moveComponents[i].Move();
		}

		StopMove().Forget();
	}

	private async UniTaskVoid StopMove() {
		await UniTask.WaitForSeconds(1f);

		foreach (var moveComponent in _moveComponents) {
			if (moveComponent == null) {
				continue;
			}

			moveComponent.Stop();
			moveComponent.enabled = false;
		}

		_isMoving = false;
	}
}