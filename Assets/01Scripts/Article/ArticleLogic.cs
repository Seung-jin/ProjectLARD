using LAD.Player;
using LAD.Map;
using UnityEngine;
using BattleMap = LAD.Map.Map;

namespace LAD.Article {
	/// <summary>
	/// 플레이어 기물의 대기석 및 전투구역 배치를 담당합니다.
	/// </summary>
	public static class ArticleLogic {
		private static ArticleFactory articleFactory;
		private static BattleMap map;

		public static BattleMap CurrentMap => map;
		public static int DeployableArticleCapacity =>
			(map != null ? map.Tiles.Count : 0) + PlayerDataSet.Economy.DeployableArticleCountBonus;

		public static bool CanDeployMoreArticles() {
			PlayerArticleCollection collection = PlayerDataSet.PlayerArticles;
			return collection != null && collection.BattlefieldArticles.Count < DeployableArticleCapacity;
		}

		public static void Initialize(ArticleFactory factory, BattleMap battleMap) {
			articleFactory = factory;
			map = battleMap;
		}

		public static void Clear() {
			articleFactory = null;
			map = null;
		}

		public static bool SpawnOnBench(PlayerArticle article) {
			if (article == null || articleFactory == null || map == null) {
				Debug.LogError("기물 팩토리 또는 맵이 초기화되지 않아 기물을 생성할 수 없습니다.");
				return false;
			}

			if (!map.TryGetBenchSlot(article.BenchSlotIndex, out Transform benchSlot)) {
				Debug.LogError($"대기석 슬롯을 찾을 수 없습니다: {article.BenchSlotIndex}");
				return false;
			}

			ArticleObj articleObj = articleFactory.Spawn(article, benchSlot.position, benchSlot.rotation);
			if (articleObj == null) {
				return false;
			}

			articleObj.PlaceOnBench(benchSlot);
			return true;
		}

		public static void ReleaseObject(int instanceId) {
			if (ArticleObj.TryGet(instanceId, out ArticleObj articleObj) && articleObj != null) {
				articleObj.Release();
			}
		}

		/// <summary>
		/// 대기석에 기물을 추가로 배치할 수 있는지 확인합니다.
		/// </summary>
		public static bool HasEmptyBenchSlot() {
			PlayerArticleCollection collection = PlayerDataSet.PlayerArticles;
			if (collection == null) {
				return false;
			}

			return collection.HasEmptyBenchSlot;
		}

		/// <summary>
		/// 동일한 기물이 3개 이상 있어 다음 등급으로 합성할 수 있는지 확인합니다.
		/// </summary>
		public static bool CanCombine(int articleId, int grade) {
			PlayerArticleCollection collection = PlayerDataSet.PlayerArticles;
			if (collection == null || articleId <= 0 || grade < 1 || grade >= PlayerArticle.MaxGrade) {
				return false;
			}

			return collection.Count(articleId, grade) >= 3;
		}

		/// <summary>
		/// 지정한 기물이 다음 등급으로 합성 가능한지 확인합니다.
		/// </summary>
		public static bool CanCombine(PlayerArticle article) {
			return article != null && CanCombine(article.ArticleId, article.Grade);
		}

		public static bool MoveToBattlefield(int instanceId) {
			if (map == null) {
				return false;
			}

			for (int index = 0; index < map.Tiles.Count; index++) {
				MapTile tile = map.Tiles[index];
				if (tile != null && !tile.IsOccupied) {
					return MoveToTile(instanceId, tile, out _);
				}
			}

			return false;
		}

		public static bool MoveToTile(int instanceId, MapTile targetTile, out PlayerArticle swappedArticle) {
			swappedArticle = null;
			PlayerArticleCollection collection = PlayerDataSet.PlayerArticles;
			if (collection == null || map == null || targetTile == null ||
			    !collection.TryGet(instanceId, out PlayerArticle article) ||
			    !ArticleObj.TryGet(instanceId, out ArticleObj articleObj)) {
				return false;
			}

			map.TryGetTile(articleObj, out MapTile originTile);
			ArticleObj targetObj = targetTile.Occupant;
			if (targetObj == articleObj) {
				return true;
			}

			if (targetObj != null && targetObj.PlayerArticle != null) {
				swappedArticle = targetObj.PlayerArticle;
				if (article.Location == ArticleLocation.Bench) {
					int benchSlotIndex = article.BenchSlotIndex;
					if (!collection.TrySetLocation(instanceId, ArticleLocation.Battlefield) ||
					    !collection.TrySetBenchSlot(swappedArticle.InstanceId, benchSlotIndex)) {
						return false;
					}
					targetTile.SetOccupant(articleObj);
					return true;
				}

				originTile?.SetOccupant(targetObj);
				targetTile.SetOccupant(articleObj);
				return true;
			}

			if (!collection.TrySetLocation(instanceId, ArticleLocation.Battlefield)) {
				return false;
			}

			originTile?.ClearOccupant(articleObj);
			targetTile.SetOccupant(articleObj);
			return true;
		}

		public static bool MoveToBench(int instanceId) {
			PlayerArticleCollection collection = PlayerDataSet.PlayerArticles;
			if (collection != null && collection.TryGet(instanceId, out PlayerArticle article) &&
			    article.Location == ArticleLocation.Bench) {
				return true;
			}

			return Move(instanceId, ArticleLocation.Bench);
		}

		public static bool MoveToBenchSlot(int instanceId, int slotIndex, out PlayerArticle swappedArticle) {
			swappedArticle = null;
			PlayerArticleCollection collection = PlayerDataSet.PlayerArticles;
			if (collection == null || !collection.TryGet(instanceId, out PlayerArticle article)) {
				return false;
			}

			int previousSlotIndex = article.BenchSlotIndex;
			ArticleObj.TryGet(instanceId, out ArticleObj movingObj);
			MapTile originTile = null;
			if (map != null && movingObj != null) {
				map.TryGetTile(movingObj, out originTile);
			}
			if (collection.TryGetBenchOccupant(slotIndex, out PlayerArticle occupant) &&
			    occupant.InstanceId != instanceId) {
				swappedArticle = occupant;
				if (article.Location == ArticleLocation.Bench) {
					collection.TrySetBenchSlot(occupant.InstanceId, previousSlotIndex);
				} else {
					collection.TrySetLocation(occupant.InstanceId, ArticleLocation.Battlefield);
				}
			}

			bool moved = collection.TrySetBenchSlot(instanceId, slotIndex);
			if (moved) {
				if (swappedArticle != null && article.Location == ArticleLocation.Bench &&
				    ArticleObj.TryGet(swappedArticle.InstanceId, out ArticleObj swappedObj)) {
					originTile?.SetOccupant(swappedObj);
				} else {
					originTile?.ClearOccupant(movingObj);
				}
			}
			return moved;
		}

		public static bool SwapBenchAndBattlefield(int benchInstanceId, int battlefieldInstanceId) {
			PlayerArticleCollection collection = PlayerDataSet.PlayerArticles;
			if (collection == null || !collection.TryGet(benchInstanceId, out PlayerArticle benchArticle) ||
			    !collection.TryGet(battlefieldInstanceId, out PlayerArticle battlefieldArticle) ||
			    benchArticle.Location != ArticleLocation.Bench ||
			    battlefieldArticle.Location != ArticleLocation.Battlefield) {
				return false;
			}

			if (map == null || !ArticleObj.TryGet(battlefieldInstanceId, out ArticleObj battlefieldObj) ||
			    !map.TryGetTile(battlefieldObj, out MapTile battlefieldTile)) {
				return false;
			}

			return MoveToTile(benchInstanceId, battlefieldTile, out PlayerArticle swappedArticle) &&
			       swappedArticle?.InstanceId == battlefieldInstanceId;
		}

		private static bool Move(int instanceId, ArticleLocation location) {
			PlayerArticleCollection collection = PlayerDataSet.PlayerArticles;
			if (collection == null) {
				Debug.LogError("플레이어 기물 데이터가 초기화되지 않았습니다.");
				return false;
			}

			if (!collection.TryGet(instanceId, out _)) {
				Debug.LogError($"Instance ID가 {instanceId}인 플레이어 기물을 찾을 수 없습니다.");
				return false;
			}

			if (!collection.TrySetLocation(instanceId, location)) {
				Debug.Log("대기석에 빈칸이 없어 기물을 이동할 수 없습니다.");
				return false;
			}

			Debug.Log($"기물 위치를 변경했습니다. Instance ID: {instanceId}, 위치: {location}");
			return true;
		}
	}
}
