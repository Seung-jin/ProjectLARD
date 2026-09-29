using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace LSJ.Util {
	public static class AddressableManager {
		// 초기화 여부 체크
		private static bool _isInit = false;
		private static Dictionary<string, SpriteAtlas> _atlasCache = new Dictionary<string, SpriteAtlas>();

		public static async UniTask Init() {
			if (_isInit) {
				return;
			}

			await Addressables.InitializeAsync().ToUniTask();
			_isInit = true;
		}

		/// <summary>
		/// 불러올 리소스들의 키 리스트를 받아서 한번에 로드.
		/// </summary>
		public static async UniTask<IList<TObject>> LoadAssetAsyncList<TObject>(IEnumerable<string> keyList, Action<TObject> callback) {
			float startAt = Time.realtimeSinceStartup;
			try {
				var op = Addressables.LoadAssetsAsync(keyList, callback, Addressables.MergeMode.Union);
				await op;
				Debug.LogWarning($"[AddressableManager] LoadAssetAsync {op.Status} : {(Time.realtimeSinceStartup - startAt)}sec.\nCount : {op.Result.Count} / {keyList.Count()}");
				return op.Result;
			} catch (Exception e) {
				Debug.LogWarning("[AddressableManager] LoadAssetAsync Exception : " + e.Message);
				return null;
			}
		}

		/// <summary>
		/// 라벨을 통해 에셋들(SO)을 한 번에 로드.
		/// </summary>
		public static async UniTask<IList<T>> LoadAssetsByLabelAsync<T>(string label) {
			if (!_isInit) {
				await Init();
			}

			// 핸들을 통해 로드하고 결과를 UniTask로 기다림.
			var handle = Addressables.LoadAssetsAsync<T>(label, null);
			IList<T> result = await handle.ToUniTask();

			return result;
		}

		// [신규] 주소(Path 또는 Name)로 단일 에셋 로드
		// key 예시: "Assets/UIData/InventoryData.asset" 또는 "InventoryData"
		public static async UniTask<T> LoadAssetAsync<T>(string key) {
			if (!_isInit) {
				await Init();
			}

			try {
				// 에셋 존재 여부 확인 (선택 사항이지만 안전을 위해 추천)
				var locations = await Addressables.LoadResourceLocationsAsync(key).ToUniTask();
				if (locations == null || locations.Count == 0) {
					return default;
				}

				// 실제 로드
				return await Addressables.LoadAssetAsync<T>(key).ToUniTask();
			} catch (System.Exception e) {
				return default;
			}
		}

		/// <summary>
		/// 핸들을 포함하여 에셋을 로드합니다. (핸들 관리가 직접 필요할 때 사용)
		/// </summary>
		public static AsyncOperationHandle<T> LoadAssetHandle<T>(string key) {
			if (!_isInit) {
				Addressables.InitializeAsync();
				_isInit = true;
			}

			return Addressables.LoadAssetAsync<T>(key);
		}

		/// <summary>
		/// 키를 통해 프리팹을 로드하고 인스턴스화하여 반환
		/// </summary>
		/// <param name="key">Addressables에 등록된 키 (경로 또는 이름)</param>
		/// <param name="parent">부모 Transform (null 가능)</param>
		/// <returns>생성된 GameObject</returns>
		public static async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null) {
			if (!_isInit) {
				await Init();
			}

			try {
				return await Addressables.InstantiateAsync(key, parent).ToUniTask();
			} catch (System.Exception e) {
				return null;
			}
		}

		/// <summary>
		/// 아틀라스를 로드(또는 캐시 사용)하고 특정 스프라이트를 반환
		/// </summary>
		public static async UniTask<Sprite> GetSpriteFromAtlasAsync(string atlasKey, string spriteName) {
			SpriteAtlas atlas = null;

			// 1. 캐시에 있는지 확인
			if (_atlasCache.ContainsKey(atlasKey)) {
				atlas = _atlasCache[atlasKey];
			} else {
				// 2. 캐시에 없으면 새로 로드
				atlas = await LoadAssetAsync<SpriteAtlas>(atlasKey);

				if (atlas != null) {
					// 로드 성공 시 캐시에 등록
					_atlasCache.Add(atlasKey, atlas);
				} else {
					return null;
				}
			}

			// 3. 스프라이트 추출
			Sprite sprite = atlas.GetSprite(spriteName);
			if (sprite == null) {
			}

			return sprite;
		}

		public static void Release<T>(AsyncOperationHandle<T> handle) {
			if (handle.IsValid()) {
				handle.Release();
			}
		}

		/// <summary>
		/// 특정 아틀라스 캐시 해제 (메모리 정리용)
		/// 사용이 끝난 아틀라스(예: 씬 전환, UI 닫기)는 반드시 해제해 주세요.
		/// </summary>
		public static void ReleaseAtlas(string atlasKey) {
			if (_atlasCache.ContainsKey(atlasKey)) {
				SpriteAtlas atlas = _atlasCache[atlasKey];

				// Addressables에서 메모리 해제
				if (atlas != null) {
					Addressables.Release(atlas);
				}

				// 캐시 목록에서 제거
				_atlasCache.Remove(atlasKey);
			}
		}

		/// <summary>
		/// 모든 아틀라스 캐시 해제 (씬 전환 시 등에 사용)
		/// </summary>
		public static void ReleaseAllAtlases() {
			foreach (var kvp in _atlasCache) {
				if (kvp.Value != null) {
					Addressables.Release(kvp.Value);
				}
			}

			_atlasCache.Clear();
		}
	}
}