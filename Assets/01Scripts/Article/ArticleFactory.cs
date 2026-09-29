using System.Collections.Generic;
using LAD.Battle;
using LAD.Skill;
using UnityEngine;

namespace LAD.Article {
	public sealed class ArticleFactory {
		private readonly Dictionary<int, ObjectPooler<BattleObj>> pools = new Dictionary<int, ObjectPooler<BattleObj>>();
		private readonly Transform poolRoot;
		private readonly SkillFactory skillFactory;

		public ArticleFactory(Transform poolRoot = null) {
			this.poolRoot = poolRoot;
			skillFactory = new SkillFactory(poolRoot);
		}

		public ArticleObj Spawn(int id, Vector3 position, Quaternion rotation) {
			if (!TryGetPool(id, out ObjectPooler<BattleObj> pool)) {
				return null;
			}

			BattleObj battleObj = pool.Get(position, rotation);
			if (battleObj is ArticleObj articleObj) {
				articleObj.Initialize(id, skillFactory);
				return articleObj;
			}

			Debug.LogError($"ID가 {id}인 프리팹에 ArticleObj가 없습니다.");
			battleObj.Release();
			return null;
		}

		public ArticleObj Spawn(PlayerArticle playerArticle, Vector3 position, Quaternion rotation) {
			if (playerArticle == null || !TryGetPool(playerArticle.ArticleId, out ObjectPooler<BattleObj> pool)) {
				return null;
			}

			BattleObj battleObj = pool.Get(position, rotation);
			if (battleObj is ArticleObj articleObj) {
				articleObj.Initialize(playerArticle, skillFactory);
				return articleObj;
			}

			battleObj.Release();
			return null;
		}

		public void Clear() {
			foreach (ObjectPooler<BattleObj> pool in pools.Values) {
				pool.Clear();
			}

			pools.Clear();
			skillFactory.Clear();
		}

		private bool TryGetPool(int id, out ObjectPooler<BattleObj> pool) {
			if (pools.TryGetValue(id, out pool)) {
				return true;
			}

			ArticleModel model = ArticleDataSet.Get(id);
			if (model == null || model.Prefab == null) {
				Debug.LogError($"ID가 {id}인 기물 프리팹을 찾을 수 없습니다.");
				pool = null;
				return false;
			}

			pool = new ObjectPooler<BattleObj>(model.Prefab, poolRoot);
			pools.Add(id, pool);
			return true;
		}
	}
}
