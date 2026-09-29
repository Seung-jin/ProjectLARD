using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LAD.Data;
using LSJ.Util;
using UnityEngine;

namespace LAD.Battle {
	public sealed class BattleDataSet : IDataSet {
		public const string AddressableLabel = "Battle";

		public static BattleConstModel Const { get; private set; }

		public async UniTask Load() {
			Const = null;

			IList<ScriptableObject> assets = await AddressableManager.LoadAssetAsyncList<ScriptableObject>(
				new[] { AddressableLabel },
				AddAsset);

			if (assets == null) {
				Debug.LogError($"BattleDataSet 로드에 실패했습니다. Addressable Label: {AddressableLabel}");
				return;
			}

			if (Const == null) {
				Debug.LogError("BattleConstModel이 등록되지 않았습니다.");
			}
		}

		private static void AddAsset(ScriptableObject asset) {
			if (asset is not BattleConstModel constModel) {
				return;
			}

			if (Const != null) {
				Debug.LogError($"BattleConstModel이 중복되었습니다: {constModel.name}");
				return;
			}

			Const = constModel;
		}
	}
}
