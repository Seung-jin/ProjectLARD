using LAD.Battle;
using LAD.Map;
using UnityEngine;
using UnityEngine.InputSystem;
using BattleMap = LAD.Map.Map;

namespace LAD.Article {
	[RequireComponent(typeof(Collider2D))]
	public sealed class ArticleDragComponent : MonoBehaviour {
		private static ArticleDragComponent activeDrag;

		[SerializeField] private ArticleObj articleObj;
		[SerializeField] private BattleManager battleManager;
		private Collider2D hitCollider;
		private Rigidbody2D rigidBody;
		private RigidbodyType2D originalBodyType;
		private Vector3 originPosition;
		private MapTile originTile;
		private ArticleLocation originLocation;
		private int originBenchSlot;
		private bool isDragging;

		private void Awake() {
			articleObj ??= GetComponent<ArticleObj>();
			battleManager ??= FindFirstObjectByType<BattleManager>();
			hitCollider = GetComponent<Collider2D>();
			rigidBody = GetComponent<Rigidbody2D>();
		}

		private void Update() {
			Mouse mouse = Mouse.current;
			Camera mainCamera = Camera.main;
			if (mouse == null || mainCamera == null || articleObj?.PlayerArticle == null) {
				return;
			}

			Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
			worldPosition.z = transform.position.z;

			if (!isDragging && activeDrag == null && mouse.leftButton.wasPressedThisFrame &&
			    hitCollider.OverlapPoint(worldPosition) && CanBeginDrag()) {
				BeginDrag();
			}

			if (!isDragging) {
				return;
			}

			transform.position = worldPosition;
			if (mouse.leftButton.wasReleasedThisFrame) {
				EndDrag(worldPosition);
			}
		}

		private bool CanBeginDrag() {
			return articleObj.PlayerArticle.Location != ArticleLocation.Battlefield ||
			       battleManager == null || battleManager.CurrentState != BattleStateType.Combat;
		}

		private void BeginDrag() {
			activeDrag = this;
			isDragging = true;
			originPosition = transform.position;
			originLocation = articleObj.PlayerArticle.Location;
			originBenchSlot = articleObj.PlayerArticle.BenchSlotIndex;
			FindFirstObjectByType<BattleMap>()?.TryGetTile(articleObj, out originTile);
			transform.SetParent(null, true);

			if (rigidBody != null) {
				originalBodyType = rigidBody.bodyType;
				rigidBody.bodyType = RigidbodyType2D.Kinematic;
				rigidBody.linearVelocity = Vector2.zero;
			}
		}

		private void EndDrag(Vector3 dropPosition) {
			BattleMap map = FindFirstObjectByType<BattleMap>();
			bool placed = map != null && (TryDropOnBench(map, dropPosition) || TryDropOnBattlefield(map, dropPosition));
			if (!placed) {
				RestoreOrigin(map);
			}

			if (rigidBody != null) {
				rigidBody.bodyType = originalBodyType;
			}

			isDragging = false;
			activeDrag = null;
		}

		private bool TryDropOnBench(BattleMap map, Vector3 dropPosition) {
			if (!map.TryGetBenchSlot(dropPosition, out int slotIndex, out Transform slot)) {
				return false;
			}

			if (!ArticleLogic.MoveToBenchSlot(articleObj.PlayerArticle.InstanceId, slotIndex,
			                                  out PlayerArticle swappedArticle)) {
				return false;
			}

			articleObj.PlaceOnBench(slot);
			if (swappedArticle != null && ArticleObj.TryGet(swappedArticle.InstanceId, out ArticleObj swappedObj)) {
				if (originLocation == ArticleLocation.Bench && map.TryGetBenchSlot(originBenchSlot, out Transform originSlot)) {
					swappedObj.PlaceOnBench(originSlot);
				} else {
					swappedObj.PlaceOnBattlefield(originPosition);
				}
			}

			return true;
		}

		private bool TryDropOnBattlefield(BattleMap map, Vector3 dropPosition) {
			if (!map.TryGetTile(dropPosition, out MapTile targetTile)) {
				return false;
			}

			ArticleObj target = targetTile.Occupant;
			if (!ArticleLogic.MoveToTile(articleObj.PlayerArticle.InstanceId, targetTile,
			                             out _)) {
				return false;
			}

			if (target != null && target != articleObj) {
				if (originLocation == ArticleLocation.Bench) {
					if (map.TryGetBenchSlot(originBenchSlot, out Transform originSlot)) {
						target.PlaceOnBench(originSlot);
					}
				} else if (originTile != null) {
					target.PlaceOnTile(originTile);
				}
			}

			articleObj.PlaceOnTile(targetTile);
			return true;
		}

		private void RestoreOrigin(BattleMap map) {
			if (originLocation == ArticleLocation.Bench && map != null &&
			    map.TryGetBenchSlot(originBenchSlot, out Transform originSlot)) {
				articleObj.PlaceOnBench(originSlot);
			} else {
				if (originTile != null) {
					articleObj.PlaceOnTile(originTile);
				} else {
					articleObj.PlaceOnBattlefield(originPosition);
				}
			}
		}
	}
}
