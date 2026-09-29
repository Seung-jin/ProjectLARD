using UnityEngine;

/// <summary>
/// 지정된 방향으로 이동
/// </summary>
public class MoveComponentDirection : MoveComponent {
	private Vector2 _direction;

	public void SetDirection(Vector2 direction) {
		_direction = direction.normalized;
	}

	public override void Move() {
		_rigidbody.linearVelocity = _direction * _moveSpeed;
	}

	public override void Stop() {
		_rigidbody.linearVelocity = Vector2.zero;
	}
}