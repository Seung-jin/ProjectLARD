using UnityEngine;

namespace LSJ.Util {
	public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
		/// <summary>
		/// true일 경우 DontDestroyOnLoad가 적용되어 씬이 바뀌어도 유지됩니다.
		/// 하위 클래스에서 override하여 설정을 바꿀 수 있습니다.
		/// </summary>
		protected virtual bool IsPersistent => false;

		public static T Instance { get; protected set; }

		protected virtual void Awake() {
			if (Instance == null) {
				Instance = this as T;

				if (IsPersistent) {
					transform.SetParent(null); // 최상위 오브젝트여야 DontDestroyOnLoad가 가능합니다.
					DontDestroyOnLoad(gameObject);
				}
			} else if (Instance != this) {
				// 이미 인스턴스가 존재하는데 다른 오브젝트가 생성되었다면 중복이므로 파괴합니다.
				Destroy(gameObject);
			}
		}

		protected virtual void OnDestroy() {
			if (Instance == this) {
				Instance = null;
			}
		}
	}

	// public class SerializedSingleton<T> : SerializedMonoBehaviour where T : MonoBehaviour {
	// 	/// <summary>
	// 	/// true일 경우 DontDestroyOnLoad가 적용되어 씬이 바뀌어도 유지됩니다.
	// 	/// 하위 클래스에서 override하여 설정을 바꿀 수 있습니다.
	// 	/// </summary>
	// 	protected virtual bool IsPersistent => false;
	//
	// 	public static T Instance { get; protected set; }
	//
	// 	protected virtual void Awake() {
	// 		if (Instance == null) {
	// 			Instance = this as T;
	//
	// 			if (IsPersistent) {
	// 				transform.SetParent(null); // 최상위 오브젝트여야 DontDestroyOnLoad가 가능합니다.
	// 				DontDestroyOnLoad(gameObject);
	// 			}
	// 		} else if (Instance != this) {
	// 			// 이미 인스턴스가 존재하는데 다른 오브젝트가 생성되었다면 중복이므로 파괴합니다.
	// 			Destroy(gameObject);
	// 		}
	// 	}
	//
	// 	protected virtual void OnDestroy() {
	// 		if (Instance == this) {
	// 			Instance = null;
	// 		}
	// 	}
	// }
}