using System.Collections.Generic;
using LARD.Article;
using UnityEngine;

namespace LARD.Battle {
    [RequireComponent(typeof(GameInputController), typeof(BattleInputLogic))]
    public sealed class BattleManager : MonoBehaviour {
        [SerializeField, Min(0f)] private float _formationGap = 0.1f;

        private readonly List<ArticleObj> _selectedArticles = new();
        private GameInputController _input;
        private BattleInputLogic _inputLogic;

        public IReadOnlyList<ArticleObj> SelectedArticles => _selectedArticles;

        private void Awake() {
            _input = GetComponent<GameInputController>();
            _inputLogic = GetComponent<BattleInputLogic>();
            _inputLogic.Initialize(this);
        }

        private void OnEnable() {
            _input.AddSelectPressedListener(_inputLogic.OnSelectPressed);
            _input.AddSelectReleasedListener(_inputLogic.OnSelectReleased);
            _input.AddMoveListener(_inputLogic.OnMoveCommand);
            Debug.Log("[BattleInput] BattleManager registered input listeners.", this);
        }

        private void OnDisable() {
            _input.RemoveSelectPressedListener(_inputLogic.OnSelectPressed);
            _input.RemoveSelectReleasedListener(_inputLogic.OnSelectReleased);
            _input.RemoveMoveListener(_inputLogic.OnMoveCommand);
            _inputLogic.CancelDrag();
        }

        public void Select(ArticleObj article) {
            if (article != null && !_selectedArticles.Contains(article)) {
                _selectedArticles.Add(article);
            }
        }

        public void Deselect(ArticleObj article) {
            _selectedArticles.Remove(article);
        }

        public void ClearSelection() {
            _selectedArticles.Clear();
        }

        public void MoveSelectedArticles(Vector2 destination) {
            _selectedArticles.RemoveAll(article => article == null || !article.gameObject.activeInHierarchy);

            int count = _selectedArticles.Count;
            if (count == 0) {
                return;
            }

            float width = 0f;
            float height = 0f;
            foreach (ArticleObj article in _selectedArticles) {
                CapsuleCollider2D collider = article.GetComponent<CapsuleCollider2D>();
                if (collider != null) {
                    width = Mathf.Max(width, collider.bounds.size.x);
                    height = Mathf.Max(height, collider.bounds.size.y);
                }
            }

            float stepX = width + _formationGap;
            float stepY = height + _formationGap;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / columns);

            for (int i = 0; i < count; i++) {
                if (_selectedArticles[i].MoveComponent is not MoveComponentDestination movement) {
                    continue;
                }

                int row = i / columns;
                int column = i % columns;
                Vector2 offset = new Vector2(
                    (column - (columns - 1) * 0.5f) * stepX,
                    ((rows - 1) * 0.5f - row) * stepY);
                movement.SetDestination(destination + offset);
            }
        }
    }

}
