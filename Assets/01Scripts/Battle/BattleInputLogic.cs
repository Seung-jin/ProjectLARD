using UnityEngine;

namespace LARD.Battle {
    public sealed class BattleInputLogic : MonoBehaviour {
        [SerializeField] private Camera _worldCamera;
        [SerializeField, Min(0f)] private float _dragThresholdPixels = 5f;

        private BattleManager _battleManager;
        private Vector2 _dragStart;
        private bool _isDragging;

        public void Initialize(BattleManager battleManager) {
            _battleManager = battleManager;
        }

        public void OnSelectPressed(Vector2 screenPosition) {
            _dragStart = screenPosition;
            _isDragging = true;
            Debug.Log($"[BattleInput] Selection started at screen {screenPosition}.", this);
        }

        public void OnSelectReleased(Vector2 screenPosition) {
            if (!_isDragging || _battleManager == null) {
                Debug.LogWarning($"[BattleInput] Selection release ignored. Drag active: {_isDragging}, manager present: {_battleManager != null}", this);
                return;
            }

            _isDragging = false;
            if (!TryScreenToWorld(_dragStart, out Vector2 start) ||
                !TryScreenToWorld(screenPosition, out Vector2 end)) {
                return;
            }

            if ((screenPosition - _dragStart).sqrMagnitude < _dragThresholdPixels * _dragThresholdPixels) {
                Debug.Log($"[BattleInput] Click resolved at world {end}.", this);
                _battleManager.SelectAt(end);
            } else {
                Debug.Log($"[BattleInput] Drag resolved from world {start} to {end}.", this);
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

        private bool TryScreenToWorld(Vector2 screenPosition, out Vector2 worldPosition) {
            worldPosition = default;
            Camera camera = _worldCamera != null ? _worldCamera : Camera.main;
            if (camera == null) {
                Debug.LogWarning("[BattleInput] Screen-to-world failed: no camera assigned and Camera.main is null.", this);
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            Plane ground = new Plane(Vector3.forward, Vector3.zero);
            if (!ground.Raycast(ray, out float distance)) {
                Debug.LogWarning($"[BattleInput] Screen-to-world failed: camera ray does not reach the XY plane. Screen: {screenPosition}", this);
                return false;
            }

            worldPosition = ray.GetPoint(distance);
            return true;
        }
    }
}
