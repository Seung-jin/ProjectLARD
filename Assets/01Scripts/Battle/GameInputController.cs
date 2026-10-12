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
        private InputAction _upgradeArticleCommand;
        private event Action UpgradeArticleCommand;
        private InputAction _upgradeYozCommand;
        private event Action UpgradeYozCommand;
        #endregion

        private void Awake() {
            _selectCommand = new InputAction("SelectCommand", InputActionType.Button, "<Mouse>/leftButton");
            _moveCommand = new InputAction("MoveCommand", InputActionType.Button, "<Mouse>/rightButton");
            _upgradeArticleCommand = new InputAction("UpgradeArticleCommand", InputActionType.Button, "<Keyboard>/2");
            _upgradeYozCommand = new InputAction("UpgradeYozCommand", InputActionType.Button, "<Keyboard>/4");
        }

        private void OnEnable() {
            _selectCommand.started += OnSelectPressed;
            _selectCommand.canceled += OnSelectReleased;
            _moveCommand.performed += OnMoveCommand;
            _upgradeArticleCommand.performed += OnUpgradeArticleCommand;
            _upgradeYozCommand.performed += OnUpgradeYozCommand;
            _selectCommand.Enable();
            _moveCommand.Enable();
            _upgradeArticleCommand.Enable();
            _upgradeYozCommand.Enable();
        }

        private void OnDisable() {
            _selectCommand.started -= OnSelectPressed;
            _selectCommand.canceled -= OnSelectReleased;
            _moveCommand.performed -= OnMoveCommand;
            _upgradeArticleCommand.performed -= OnUpgradeArticleCommand;
            _upgradeYozCommand.performed -= OnUpgradeYozCommand;
            _selectCommand.Disable();
            _moveCommand.Disable();
            _upgradeArticleCommand.Disable();
            _upgradeYozCommand.Disable();
        }

        private void OnDestroy() {
            _selectCommand.Dispose();
            _moveCommand.Dispose();
            _upgradeArticleCommand.Dispose();
            _upgradeYozCommand.Dispose();
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
        public void AddUpgradeArticleListener(Action listener) {
            UpgradeArticleCommand += listener;
        }

        public void RemoveUpgradeArticleListener(Action listener) {
            UpgradeArticleCommand -= listener;
        }

        public void AddUpgradeYozListener(Action listener) {
            UpgradeYozCommand += listener;
        }

        public void RemoveUpgradeYozListener(Action listener) {
            UpgradeYozCommand -= listener;
        }

        private void OnUpgradeArticleCommand(InputAction.CallbackContext context) {
            Debug.Log("[BattleInput] Test upgrade requested with key 2.", this);
            UpgradeArticleCommand?.Invoke();
        }

        private void OnUpgradeYozCommand(InputAction.CallbackContext context) {
            Debug.Log("[BattleInput] Test Yoz upgrade requested with key 4.", this);
            UpgradeYozCommand?.Invoke();
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
                MoveCommand?.Invoke(position);
            } else {
                Debug.LogWarning("[BattleInput] Right click received, but Mouse.current is null.", this);
            }
        }
    }
}
