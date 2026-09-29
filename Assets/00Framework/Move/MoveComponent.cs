using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class MoveComponent : MonoBehaviour {
	[SerializeField] protected float _moveSpeed = 5f;

	protected Rigidbody2D _rigidbody;
	public float MoveSpeed => _moveSpeed;

	protected virtual void Awake() {
		_rigidbody = GetComponent<Rigidbody2D>();
	}

	private void FixedUpdate() {
		Move();
	}

	public abstract void Move();
	public abstract void Stop();

	public void SetMoveSpeed(float moveSpeed) {
		_moveSpeed = Mathf.Max(0f, moveSpeed);
	}
}
