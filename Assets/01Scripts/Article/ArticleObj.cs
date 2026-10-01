using UnityEngine;

namespace LARD.Article {
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(SkillController), typeof(GraphicController))]
    public class ArticleObj : PoolingObject<ArticleObj> {
        [SerializeField] private GraphicController _graphicController;
        [SerializeField] private MoveComponent _moveComponent;
        [SerializeField] private SkillController _skillController;

        public int Id { get; private set; }
        public ArticleModel Model { get; private set; }
        public MoveComponent MoveComponent => _moveComponent;
        public SkillController SkillController => _skillController;
        public GraphicController GraphicController => _graphicController;

        private void Awake() {
            _moveComponent ??= GetComponent<MoveComponent>();
            _skillController ??= GetComponent<SkillController>();
            _graphicController ??= GetComponent<GraphicController>();
        }

        internal void Initialize(ArticleModel model, int id) {
            Model = model;
            Id = id;
            _moveComponent?.SetMoveSpeed(model.MoveSpeed);
        }

        public override void OnReleaseToPool() {
            if (_moveComponent != null) {
                _moveComponent.Stop();
            }

            _skillController?.ClearSkill();

            Model = null;
            Id = 0;
            base.OnReleaseToPool();
        }
    }

}
