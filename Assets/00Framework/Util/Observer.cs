using System;
using System.Collections.Generic;

namespace LSJ.Util {
	/// <summary>
	/// 열거형 이벤트를 구독·해제·발행하여 캐릭터 상태 변화에 따른 로직 처리와 UI 갱신을 구독자에게 전달한다.
	/// </summary>
	public class Observer<T> where T : Enum {
		// 인자 없는 델리게이트
		public delegate void EventHandler();

		// 가변 인자(params)를 받는 델리게이트
		public delegate void ParamEventHandler(params object[] args);

		private Dictionary<T, EventHandler> _events = new Dictionary<T, EventHandler>();
		private Dictionary<T, ParamEventHandler> _paramEvents = new Dictionary<T, ParamEventHandler>();

		// 1. 구독 (Add)

		public void AddListener(T eventType, EventHandler listener) {
			if (!_events.ContainsKey(eventType)) {
				_events[eventType] = null;
			}

			_events[eventType] += listener;
		}

		public void AddListener(T eventType, ParamEventHandler listener) {
			if (!_paramEvents.ContainsKey(eventType)) {
				_paramEvents[eventType] = null;
			}

			_paramEvents[eventType] += listener;
		}

		// 2. 해제 (Remove)
		public void RemoveListener(T eventType, EventHandler listener) {
			if (_events.ContainsKey(eventType)) {
				_events[eventType] -= listener;
			}
		}

		public void RemoveListener(T eventType, ParamEventHandler listener) {
			if (_paramEvents.ContainsKey(eventType)) {
				_paramEvents[eventType] -= listener;

				// 더 이상 듣는 사람이 없으면 키 삭제
				if (_paramEvents[eventType] == null) {
					_paramEvents.Remove(eventType);
				}
			}
		}

		// 3. 발송 (Trigger)
		public void TriggerEvent(T eventType, params object[] args) {
			if (_events.TryGetValue(eventType, out var listener)) {
				listener?.Invoke();
			}

			if (_paramEvents.TryGetValue(eventType, out var paramListener)) {
				paramListener?.Invoke(args);
			}
		}

		// 4. 전체 초기화 (ClearAll)
		public void ClearAll() {
			_events.Clear();
			_paramEvents.Clear();
		}
	}
}
