using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSJ.State {
	// T: 상태를 구분할 Enum (예: EPlayerState)
	public class StateMachine<T> where T : Enum {
		// 상태들을 저장할 딕셔너리 (Enum -> State)
		private Dictionary<T, BaseState<T>> _stateMap = new Dictionary<T, BaseState<T>>();

		// 외부에서 현재 상태를 Enum으로 바로 확인 가능
		public T CurrentState { get; private set; }

		// 현재 실행 중인 상태 클래스
		private BaseState<T> _currentStateInstance;

		public void AddState(T stateEnum, BaseState<T> state) {
			if (!_stateMap.ContainsKey(stateEnum)) {
				_stateMap.Add(stateEnum, state);
			}
		}

		public void ChangeState(T newStateEnum) {
			if (!_stateMap.ContainsKey(newStateEnum)) {
				Debug.LogError($"상태 머신에 [{newStateEnum}] 상태가 등록되지 않았습니다.");
				return;
			}

			// 이전 상태 종료
			_currentStateInstance?.OnExit();

			// 상태 교체
			CurrentState = newStateEnum;
			_currentStateInstance = _stateMap[newStateEnum];

			// 새 상태 진입
			_currentStateInstance?.OnEnter();
		}

		public void Update() {
			_currentStateInstance?.OnUpdate();
		}

		public void FixedUpdate() {
			_currentStateInstance?.OnFixedUpdate();
		}
	}

	// 상태의 기본형 (제네릭 T는 Enum)
	public abstract class BaseState<T> where T : Enum {
		public virtual void OnEnter() { }
		public virtual void OnUpdate() { }
		public virtual void OnFixedUpdate() { }
		public virtual void OnExit() { }
	}
}