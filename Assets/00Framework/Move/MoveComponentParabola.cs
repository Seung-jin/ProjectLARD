using UnityEngine;

/// <summary>
/// 포물선으로 이동
/// </summary>
public class MoveComponentParabola : MoveComponent {
	[SerializeField] private float _height = 2f;

	private Vector2 _destination;
	private Vector2 _startPosition;
	private float _elapsedTime;
	private float _duration;
	private bool _hasDestination;
	private bool _isArrived;

	private void OnEnable() {
		ResetMove();
	}

	public void SetDestination(Vector2 destination) {
		_destination = destination;
		_hasDestination = true;
		ResetMove();
	}

	public override void Move() {
		if (!_hasDestination || _isArrived) {
			Stop();
			return;
		}

		_elapsedTime += Time.fixedDeltaTime;

		float progress = Mathf.Clamp01(_elapsedTime / _duration);
		Vector2 nextPosition = Vector2.Lerp(_startPosition, _destination, progress);
		nextPosition.y += _height * 4f * progress * (1f - progress);

		_rigidbody.MovePosition(nextPosition);

		if (progress >= 1f) {
			_isArrived = true;
			_rigidbody.position = _destination;
			Stop();
		}
	}

	public override void Stop() {
		_rigidbody.linearVelocity = Vector2.zero;
	}

	private void ResetMove() {
		if (_rigidbody == null) {
			return;
		}

		_startPosition = _rigidbody.position;
		_elapsedTime = 0f;
		_isArrived = false;

		if (!_hasDestination) {
			_duration = 1f;
			return;
		}

		float distance = Vector2.Distance(_startPosition, _destination);
		float moveSpeed = Mathf.Max(_moveSpeed, 0.001f);
		_duration = Mathf.Max(distance / moveSpeed, Time.fixedDeltaTime);
	}
}