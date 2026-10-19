using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 풀링 대상의 반환 상태를 추적하고 대여, 반환, 파괴 시 공통 수명 주기를 제공한다.
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

	/// <summary>중복 반환을 막고 자신을 연결된 풀로 돌려보낸다.</summary>
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

	/// <summary>풀에서 대여될 때 오브젝트를 활성화한다.</summary>
	public virtual void OnGetFromPool() {
		gameObject.SetActive(true);
	}

	/// <summary>풀로 반환될 때 오브젝트를 비활성화한다.</summary>
	public virtual void OnReleaseToPool() {
		gameObject.SetActive(false);
	}

	/// <summary>풀에서 제거될 때 오브젝트를 파괴한다.</summary>
	public virtual void OnDestroyFromPool() {
		if (this == null) {
			return;
		}

		Destroy(gameObject);
	}
}
