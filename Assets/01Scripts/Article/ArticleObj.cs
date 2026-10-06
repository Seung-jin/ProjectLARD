using UnityEngine;

namespace LARD.Article {
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(SkillController), typeof(GraphicController))]
    public class ArticleObj : PoolingObject<ArticleObj> {
        [SerializeField] private GraphicController _graphicController;
        [SerializeField] private MoveComponent _moveComponent;
        [SerializeField] private SkillController _skillController;
        [SerializeField] private UIArticleObj _uiArticleObj;

        public int Id { get; private set; }
        public int Tier { get; private set; }
        public ArticleModel Model { get; private set; }
        public MoveComponent MoveComponent => _moveComponent;
        public SkillController SkillController => _skillController;
        public GraphicController GraphicController => _graphicController;

        private void Awake() {
            _moveComponent ??= GetComponent<MoveComponent>();
            _skillController ??= GetComponent<SkillController>();
            _graphicController ??= GetComponent<GraphicController>();
            _uiArticleObj ??= GetComponentInChildren<UIArticleObj>(true);
        }

        internal void Initialize(ArticleModel model, int id, int tier) {
            Model = model;
            Id = id;
            Tier = tier;
            _moveComponent?.SetMoveSpeed(model.MoveSpeed);
            _uiArticleObj?.SetInfo(model.Race, tier);
        }

        public override void OnReleaseToPool() {
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
