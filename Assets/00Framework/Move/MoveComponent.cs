using UnityEngine;
using LARD.Article;

/// <summary>
/// Rigidbody2D 기반 이동을 고정 주기마다 실행하고 이동 속도를 관리하는 공통 컴포넌트다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class MoveComponent : MonoBehaviour {
	[SerializeField] protected float _moveSpeed = 5f;

	protected Rigidbody2D _rigidbody;
	private GraphicController _graphicController;
	private bool _wasMoving;
	public float MoveSpeed => _moveSpeed;
	public Vector2 MovementDirection => _rigidbody != null && _rigidbody.linearVelocity.sqrMagnitude > 0.0001f
		? _rigidbody.linearVelocity.normalized : Vector2.zero;

	protected virtual void Awake() {
		_rigidbody = GetComponent<Rigidbody2D>();
		_graphicController = GetComponentInChildren<GraphicController>(true);
	}

	private void FixedUpdate() {
		Move();
		bool isMoving = _rigidbody != null && _rigidbody.linearVelocity.sqrMagnitude > 0.0001f;
		if (isMoving == _wasMoving) return;
		_wasMoving = isMoving;
		_graphicController?.SetMoving(isMoving);
	}

	private void OnDisable() {
		_wasMoving = false;
		_graphicController?.SetMoving(false);
	}

	/// <summary>구체적인 이동 방식에 따라 속도 또는 위치를 갱신한다.</summary>
	public abstract void Move();
	/// <summary>현재 물리 이동 속도를 0으로 설정한다.</summary>
	public abstract void Stop();

	public void SetMoveSpeed(float moveSpeed) {
		_moveSpeed = Mathf.Max(0f, moveSpeed);
	}
}
