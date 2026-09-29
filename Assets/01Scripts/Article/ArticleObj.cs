using System.Collections.Generic;
using LAD.Battle;
using LAD.Stat;
using LAD.Skill;
using LAD.Map;
using LAD.Accessory;
using LAD.Player;
using UnityEngine;

namespace LAD.Article {
    /// <summary>
    /// 플레이어가 소환하는 기물 오브젝트다.
    /// </summary>
    [RequireComponent(typeof(ArticleAttackComponent), typeof(SkillController))]
    public class ArticleObj : BattleObj {
		private static readonly Dictionary<int, ArticleObj> instanceObjects = new Dictionary<int, ArticleObj>();
		private static readonly HashSet<ArticleObj> battlefieldArticles = new HashSet<ArticleObj>();
		public bool IsOnBattlefield { get; private set; }
		public float BasicAttackDamage => skillController != null ? skillController.BasicAttackDamage :
			BattleStatLogic.GetStat(this, StatType.Strength);
		public static IReadOnlyList<ArticleObj> GetBattlefieldArticles() => new List<ArticleObj>(battlefieldArticles);

		private void SetBattlefieldPresence(bool present) {
			if (IsOnBattlefield == present) return;
			IsOnBattlefield = present;
			if (present) battlefieldArticles.Add(this);
			else battlefieldArticles.Remove(this);
			BattleManager.Current?.Observer.TriggerEvent(present ? BattleManagerEventType.ArticleEntered :
				BattleManagerEventType.ArticleLeft, this);
		}

        [Header("Combat")]
        [SerializeField] private ArticleAttackComponent attackComponent;
        [SerializeField] private SkillController skillController;

		private Transform defaultParent;
		private bool hasDefaultParent;
		private int grade = 1;
		private readonly object accessoryModifierSource = new object();
		private PlayerAccessoryCollection accessoryCollection;

		public PlayerArticle PlayerArticle { get; private set; }
		public override int Grade => grade;
		public override bool ShowHealthUI => false;
		public static bool TryGet(int instanceId, out ArticleObj articleObj) {
			return instanceObjects.TryGetValue(instanceId, out articleObj);
		}

        public void Initialize(int id) {
			UnregisterPlayerArticle();
			Initialize(id, 1, null);
		}

		internal void Initialize(int id, SkillFactory skillFactory) {
			UnregisterPlayerArticle();
			Initialize(id, 1, skillFactory);
		}

		private void Initialize(int id, int grade, SkillFactory skillFactory) {
			SetBattlefieldPresence(false);
			accessoryCollection?.Observer.RemoveListener(PlayerAccessoryEventType.Changed, OnAccessoryChanged);
			accessoryCollection = null;
			this.grade = grade;
			if (!hasDefaultParent) {
				defaultParent = transform.parent;
				hasDefaultParent = true;
			}
            ArticleModel model = ArticleDataSet.Get(id);
            if (model == null) {
                Debug.LogError($"ID가 {id}인 ArticleModel을 불러오지 못했습니다.", this);
                return;
            }

            InitializeStats(model.GetStats(grade));
			accessoryCollection ??= PlayerDataSet.PlayerAccessories;
			accessoryCollection?.Observer.AddListener(PlayerAccessoryEventType.Changed, OnAccessoryChanged);
			RefreshAccessoryEffects();

            if (moveComponent != null) {
                SetMoveSpeed(StatController.GetStat(StatType.MoveSpeed));
            }

            if (attackComponent == null) {
                attackComponent = GetComponent<ArticleAttackComponent>();
            }

            if (attackComponent == null) {
                Debug.LogError($"{name}에 ArticleAttackComponent가 지정되지 않았습니다.", this);
                return;
            }

            attackComponent.Initialize(StatController);

			if (graphicController == null) {
				graphicController = GetComponentInChildren<GraphicController>(true);
			}

            if (skillController == null) {
                skillController = GetComponent<SkillController>();
            }

            if (skillController == null) {
                Debug.LogError($"{name}\uc5d0 SkillController\uac00 \uc9c0\uc815\ub418\uc9c0 \uc54a\uc558\uc2b5\ub2c8\ub2e4.", this);
                return;
            }

            skillController.Initialize(this, attackComponent, StatController, graphicController,
                                       model, grade, skillFactory);
			SetBattlefieldPresence(attackComponent.enabled);
        }

		public void Initialize(PlayerArticle playerArticle) {
			Initialize(playerArticle, null);
		}

		internal void Initialize(PlayerArticle playerArticle, SkillFactory skillFactory) {
			if (playerArticle == null) {
				Debug.LogError("ArticleObj에 연결할 플레이어 기물 데이터가 없습니다.", this);
				return;
			}

			UnregisterPlayerArticle();
			PlayerArticle = playerArticle;
			instanceObjects[playerArticle.InstanceId] = this;
			Initialize(playerArticle.ArticleId, playerArticle.Grade, skillFactory);
		}

		public void PlaceOnBench(Transform benchSlot) {
			SetBattlefieldPresence(false);
			skillController?.CancelPendingSkill();
			transform.SetParent(benchSlot, false);
			transform.localPosition = Vector3.zero;
			if (attackComponent != null) {
				attackComponent.enabled = false;
			}
			graphicController?.PlayIdle();
		}

		/// <summary>현재 장착 정보를 기준으로 악세사리 스탯 보정치를 다시 적용합니다.</summary>
		public void RefreshAccessoryEffects() {
			if (StatController == null) return;
			StatController.RemoveModifiers(accessoryModifierSource);
			if (PlayerArticle == null) return;
			StatController.AddModifiers(accessoryModifierSource,
			                            AccessoryEffectLogic.BuildModifiers(PlayerArticle.InstanceId));
		}

		public void PlaceOnBattlefield(Vector3 worldPosition) {
			transform.SetParent(defaultParent, true);
			transform.position = worldPosition;
			if (attackComponent != null) {
				attackComponent.enabled = true;
			}
			graphicController?.PlayIdle();
			SetBattlefieldPresence(true);
		}

		public void PlaceOnTile(MapTile tile) {
			PlaceOnBattlefield(tile.transform.position);
		}

        public override void OnReleaseToPool() {
			SetBattlefieldPresence(false);
			skillController?.CancelPendingSkill();
			UnregisterPlayerArticle();
			transform.SetParent(defaultParent, true);
            attackComponent?.ClearTarget();
            base.OnReleaseToPool();
        }

		protected override void OnDestroy() {
			SetBattlefieldPresence(false);
			accessoryCollection?.Observer.RemoveListener(PlayerAccessoryEventType.Changed, OnAccessoryChanged);
			accessoryCollection = null;
			UnregisterPlayerArticle();
			base.OnDestroy();
		}

		private void OnAccessoryChanged(params object[] args) {
			if (args.Length > 0 && args[0] is LAD.Accessory.Accessory accessory && accessory.ArticleId == PlayerArticle?.InstanceId)
				RefreshAccessoryEffects();
		}

		private void UnregisterPlayerArticle() {
			if (PlayerArticle != null) {
				instanceObjects.Remove(PlayerArticle.InstanceId);
				PlayerArticle = null;
			}
		}
    }
}
