using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 풀링 대상 오브젝트의 공통 베이스.
/// </summary>
public abstract class PoolingObject<T> : MonoBehaviour where T : PoolingObject<T> {
	private IObjectPool<T> _pool;

	public bool IsInPool { get; private set; }

	internal void SetPool(IObjectPool<T> pool) {
		_pool = pool;
	}

	internal void SetInPool(bool isInPool) {
		IsInPool = isInPool;
	}

	// 자기 자신을 연결된 풀로 반환.
	public void Release() {
		if (IsInPool) {
			return;
		}

		if (_pool == null) {
			gameObject.SetActive(false);
			return;
		}

		_pool.Release((T)this);
	}

	// 풀에서 꺼내졌을 때의 기본 처리.
	public virtual void OnGetFromPool() {
		gameObject.SetActive(true);
	}

	// 풀로 반환될 때의 기본 처리.
	public virtual void OnReleaseToPool() {
		gameObject.SetActive(false);
	}

	// 풀 자체가 정리될 때의 기본 처리.
	public virtual void OnDestroyFromPool() {
		if (this == null) {
			return;
		}

		Destroy(gameObject);
	}
}
