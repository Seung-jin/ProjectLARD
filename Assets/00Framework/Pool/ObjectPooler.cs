using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Unity 오브젝트 풀을 기반으로 프리팹 인스턴스의 생성, 재사용, 반환과 정리를 관리한다.
/// </summary>
public class ObjectPooler<T> where T : PoolingObject<T> {
	private readonly T _prefab;
	private readonly Transform _parent;
	private readonly IObjectPool<T> _pool;

	public ObjectPooler(T prefab, Transform parent = null) {
		_prefab = prefab;
		_parent = parent;

		_pool = new ObjectPool<T>(CreateObject, OnGetObject, OnReleaseObject, OnDestroyObject);
	}

	/// <summary>풀에서 오브젝트를 꺼내고 대여 시 동작을 실행한다.</summary>
	public T Get() {
		return _pool.Get();
	}

	public T Get(Vector3 position, Quaternion rotation) {
		T poolingObject = Get();
		poolingObject.transform.SetPositionAndRotation(position, rotation);
		return poolingObject;
	}

	/// <summary>사용이 끝난 오브젝트를 연결된 풀로 반환한다.</summary>
	public void Release(T poolingObject) {
		if (poolingObject == null) {
			return;
		}

		poolingObject.Release();
	}

	/// <summary>풀에 보관 중인 오브젝트를 정리한다.</summary>
	public void Clear() {
		_pool.Clear();
	}

	/// <summary>재사용할 오브젝트가 없으면 프리팹을 생성하고 풀을 연결한다.</summary>
	private T CreateObject() {
		T poolingObject = Object.Instantiate(_prefab, _parent);
		poolingObject.SetPool(_pool);
		poolingObject.SetInPool(false);
		poolingObject.gameObject.SetActive(false);
		return poolingObject;
	}

	/// <summary>대여 상태로 전환하고 오브젝트의 활성화 동작을 호출한다.</summary>
	private void OnGetObject(T poolingObject) {
		poolingObject.SetPool(_pool);
		poolingObject.SetInPool(false);
		poolingObject.OnGetFromPool();
	}

	/// <summary>반환 상태로 전환하고 오브젝트의 정리 동작을 호출한다.</summary>
	private void OnReleaseObject(T poolingObject) {
		poolingObject.SetInPool(true);
		poolingObject.OnReleaseToPool();
	}

	/// <summary>풀이 정리할 오브젝트의 파괴 동작을 호출한다.</summary>
	private void OnDestroyObject(T poolingObject) {
		poolingObject.OnDestroyFromPool();
	}
}
