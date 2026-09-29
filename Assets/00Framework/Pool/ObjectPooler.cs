using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Unity IObjectPool을 감싼 간단한 제너릭 풀러.
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

	public T Get() {
		return _pool.Get();
	}

	public T Get(Vector3 position, Quaternion rotation) {
		T poolingObject = Get();
		poolingObject.transform.SetPositionAndRotation(position, rotation);
		return poolingObject;
	}

	public void Release(T poolingObject) {
		if (poolingObject == null) {
			return;
		}

		poolingObject.Release();
	}

	public void Clear() {
		_pool.Clear();
	}

	// 풀이 비어 있을 때 새 오브젝트 생성.
	private T CreateObject() {
		T poolingObject = Object.Instantiate(_prefab, _parent);
		poolingObject.SetPool(_pool);
		poolingObject.SetInPool(false);
		poolingObject.gameObject.SetActive(false);
		return poolingObject;
	}

	// 풀에서 꺼낼 때 호출.
	private void OnGetObject(T poolingObject) {
		poolingObject.SetPool(_pool);
		poolingObject.SetInPool(false);
		poolingObject.OnGetFromPool();
	}

	// 풀로 돌려보낼 때 호출.
	private void OnReleaseObject(T poolingObject) {
		poolingObject.SetInPool(true);
		poolingObject.OnReleaseToPool();
	}

	// 풀이 정리될 때 실제 오브젝트 제거.
	private void OnDestroyObject(T poolingObject) {
		poolingObject.OnDestroyFromPool();
	}
}
