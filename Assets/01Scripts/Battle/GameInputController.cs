using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LARD.Battle {
    public sealed class GameInputController : MonoBehaviour {
        private InputAction _selectCommand;
        private InputAction _moveCommand;
        private event Action<Vector2> SelectPressed;
        private event Action<Vector2> SelectReleased;
        private event Action<Vector2> MoveCommand;

        #region Test Code
        private InputAction _drawArticleCommand;
        private event Action DrawArticleCommand;
        #endregion

        private void Awake() {
            _selectCommand = new InputAction("SelectCommand", InputActionType.Button, "<Mouse>/leftButton");
            _moveCommand = new InputAction("MoveCommand", InputActionType.Button, "<Mouse>/rightButton");
            _drawArticleCommand = new InputAction("DrawArticleCommand", InputActionType.Button, "<Keyboard>/1");
        }

        private void OnEnable() {
            _selectCommand.started += OnSelectPressed;
            _selectCommand.canceled += OnSelectReleased;
            _moveCommand.performed += OnMoveCommand;
            _drawArticleCommand.performed += OnDrawArticleCommand;
            _selectCommand.Enable();
            _moveCommand.Enable();
            _drawArticleCommand.Enable();
            Debug.Log($"[BattleInput] Input enabled. Mouse present: {Mouse.current != null}, left controls: {_selectCommand.controls.Count}, right controls: {_moveCommand.controls.Count}", this);
        }

        private void OnDisable() {
            _selectCommand.started -= OnSelectPressed;
            _selectCommand.canceled -= OnSelectReleased;
            _moveCommand.performed -= OnMoveCommand;
            _drawArticleCommand.performed -= OnDrawArticleCommand;
            _selectCommand.Disable();
            _moveCommand.Disable();
            _drawArticleCommand.Disable();
        }

        private void OnDestroy() {
            _selectCommand.Dispose();
            _moveCommand.Dispose();
            _drawArticleCommand.Dispose();
        }

        public void AddSelectPressedListener(Action<Vector2> listener) {
            SelectPressed += listener;
        }

        public void RemoveSelectPressedListener(Action<Vector2> listener) {
            SelectPressed -= listener;
        }

        public void AddSelectReleasedListener(Action<Vector2> listener) {
            SelectReleased += listener;
        }

        public void RemoveSelectReleasedListener(Action<Vector2> listener) {
            SelectReleased -= listener;
        }

        public void AddMoveListener(Action<Vector2> listener) {
            MoveCommand += listener;
        }

        public void RemoveMoveListener(Action<Vector2> listener) {
            MoveCommand -= listener;
        }

        #region Test Code
        public void AddDrawArticleListener(Action listener) {
            DrawArticleCommand += listener;
        }

        public void RemoveDrawArticleListener(Action listener) {
            DrawArticleCommand -= listener;
        }

        private void OnDrawArticleCommand(InputAction.CallbackContext context) {
            Debug.Log("[BattleInput] Test draw requested with key 1.", this);
            DrawArticleCommand?.Invoke();
        }
        #endregion

        private void OnSelectPressed(InputAction.CallbackContext context) {
            if (Mouse.current != null) {
                Vector2 position = Mouse.current.position.ReadValue();
                Debug.Log($"[BattleInput] Left pressed at screen {position}. Listeners: {SelectPressed?.GetInvocationList().Length ?? 0}", this);
                SelectPressed?.Invoke(position);
            } else {
                Debug.LogWarning("[BattleInput] Left press received, but Mouse.current is null.", this);
            }
        }

        private void OnSelectReleased(InputAction.CallbackContext context) {
            if (Mouse.current != null) {
                Vector2 position = Mouse.current.position.ReadValue();
                Debug.Log($"[BattleInput] Left released at screen {position}. Listeners: {SelectReleased?.GetInvocationList().Length ?? 0}", this);
                SelectReleased?.Invoke(position);
            } else {
                Debug.LogWarning("[BattleInput] Left release received, but Mouse.current is null.", this);
            }
        }

        private void OnMoveCommand(InputAction.CallbackContext context) {
            if (Mouse.current != null) {
                Vector2 position = Mouse.current.position.ReadValue();
                Debug.Log($"[BattleInput] Right clicked at screen {position}. Listeners: {MoveCommand?.GetInvocationList().Length ?? 0}", this);
                MoveCommand?.Invoke(position);
            } else {
                Debug.LogWarning("[BattleInput] Right click received, but Mouse.current is null.", this);
            }
        }
    }
}
