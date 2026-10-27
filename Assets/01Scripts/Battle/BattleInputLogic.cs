using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LARD.Battle {
    /// <summary>
    /// 화면 입력을 기물 선택과 이동 명령으로 해석하고 UI 위의 선택 입력을 걸러낸다.
    /// </summary>
    public sealed class BattleInputLogic : MonoBehaviour {
        [SerializeField] private Camera _worldCamera;
        [SerializeField, Min(0f)] private float _dragThresholdPixels = 5f;

        private BattleManager _battleManager;
        private Vector2 _dragStart;
        private bool _isDragging;
        private readonly List<RaycastResult> _uiRaycastResults = new();

        public void Init(BattleManager battleManager) {
            _battleManager = battleManager;
        }

        public void OnSelectPressed(Vector2 screenPosition) {
            if (IsOverUI(screenPosition)) {
                _isDragging = false;
                return;
            }

            _dragStart = screenPosition;
            _isDragging = true;
        }

        public void OnSelectReleased(Vector2 screenPosition) {
            if (!_isDragging || _battleManager == null) {
                return;
            }

            _isDragging = false;
            if (!TryScreenToWorld(_dragStart, out Vector2 start) ||
                !TryScreenToWorld(screenPosition, out Vector2 end)) {
                return;
            }

            if ((screenPosition - _dragStart).sqrMagnitude < _dragThresholdPixels * _dragThresholdPixels) {
                _battleManager.SelectAt(end);
            } else {
                _battleManager.SelectArea(start, end);
            }
        }

        public void OnMoveCommand(Vector2 screenPosition) {
            if (_battleManager != null && TryScreenToWorld(screenPosition, out Vector2 destination)) {
                _battleManager.MoveSelectedArticles(destination);
            }
        }

        public void CancelDrag() {
            _isDragging = false;
        }

        private bool IsOverUI(Vector2 screenPosition) {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) {
                return false;
            }

            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screenPosition }, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }

        private bool TryScreenToWorld(Vector2 screenPosition, out Vector2 worldPosition) {
            worldPosition = default;
            Camera camera = _worldCamera != null ? _worldCamera : Camera.main;
            if (camera == null) {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            Plane ground = new Plane(Vector3.forward, Vector3.zero);
            if (!ground.Raycast(ray, out float distance)) {
                return false;
            }

            worldPosition = ray.GetPoint(distance);
            return true;
        }
    }
}