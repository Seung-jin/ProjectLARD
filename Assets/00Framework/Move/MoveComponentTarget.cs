using UnityEngine;

/// <summary>
/// 지정된 타겟을 따라 이동
/// </summary>
public class MoveComponentTarget : MoveComponent {
	[SerializeField] private float _stopDistance = 1f;

	private Transform _target;

	public void SetTarget(Transform target) {
		_target = target;
	}

	public override void Move() {
		if (_target == null) {
			Stop();
			return;
		}

		Vector2 currentPosition = _rigidbody.position;
		Vector2 targetPosition = _target.position;
		Vector2 direction = targetPosition - currentPosition;

		if (direction.sqrMagnitude <= _stopDistance * _stopDistance) {
			Stop();
			return;
		}

		_rigidbody.linearVelocity = direction.normalized * _moveSpeed;
	}

	public override void Stop() {
		_rigidbody.linearVelocity = Vector2.zero;
	}
}