using Sirenix.OdinInspector;
using UnityEngine;
using LARD.Effects;

namespace LARD.Article {
    // 기물 상태와 구성 요소 관리
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(SkillController))]
    public class ArticleObj : PoolingObject<ArticleObj> {
        [ShowInInspector] public int Id { get; private set; }

        [SerializeField] private GraphicController _graphicController;
        [SerializeField] private MoveComponent _moveComponent;
        [SerializeField] private SkillController _skillController;
        [SerializeField] private UIArticleObj _uiArticleObj;

        public int Tier { get; private set; }
        public EffectController EffectController { get; } = new();
        public ArticleModel Model { get; private set; }
        public MoveComponent MoveComponent => _moveComponent;
        public SkillController SkillController => _skillController;
        public GraphicController GraphicController => _graphicController;

        private void Awake() {
            _moveComponent ??= GetComponent<MoveComponent>();
            _skillController ??= GetComponent<SkillController>();
            _graphicController ??= GetComponentInChildren<GraphicController>(true);
            _uiArticleObj ??= GetComponentInChildren<UIArticleObj>(true);
        }

        private void LateUpdate() {
            if (_moveComponent != null && _graphicController != null) {
                _graphicController.FaceDirection(_moveComponent.MovementDirection);
            }
        }

        internal void Init(ArticleModel model, int id, int tier) {
            EffectController.Clear();
            Model = model;
            Id = id;
            Tier = tier;
            _moveComponent?.SetMoveSpeed(model.MoveSpeed);
            _graphicController?.ResetFacing();
            _uiArticleObj?.SetInfo(model.Race, tier);
        }

        public override void OnReleaseToPool() {
            EffectController.Clear();
            if (_moveComponent != null) {
                _moveComponent.Stop();
            }

            _skillController?.ClearSkill();
            _uiArticleObj?.Clear();

            Model = null;
            Id = 0;
            Tier = 0;
            base.OnReleaseToPool();
        }
    }

}
