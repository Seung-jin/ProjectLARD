using UnityEngine;

namespace LARD.Effects {
    // 전투 효과 관리
    public sealed class EffectController {
        private float _damageTakenIncreasePercent;
        private float _damageTakenIncreaseUntil;
        private float _moveSpeedDecreasePercent;
        private float _moveSpeedDecreaseUntil;

        public float DamageTakenIncreasePercent => Time.time < _damageTakenIncreaseUntil
            ? _damageTakenIncreasePercent : 0f;
        public float MoveSpeedDecreasePercent => Time.time < _moveSpeedDecreaseUntil
            ? _moveSpeedDecreasePercent : 0f;

        // 받는 피해 증가 적용
        public void ApplyDamageTakenIncrease(float percent, float durationSeconds) {
            if (percent <= 0f || durationSeconds <= 0f) {
                return;
            }

            float current = DamageTakenIncreasePercent;
            _damageTakenIncreasePercent = Mathf.Max(current, percent);
            _damageTakenIncreaseUntil = Mathf.Max(_damageTakenIncreaseUntil, Time.time + durationSeconds);
        }

        // 이동 속도 감소 적용
        public void ApplyMoveSpeedDecrease(float percent, float durationSeconds) {
            if (percent <= 0f || durationSeconds <= 0f) {
                return;
            }

            float current = MoveSpeedDecreasePercent;
            _moveSpeedDecreasePercent = Mathf.Max(current, Mathf.Clamp(percent, 0f, 100f));
            _moveSpeedDecreaseUntil = Mathf.Max(_moveSpeedDecreaseUntil, Time.time + durationSeconds);
        }

        // 효과 초기화
        public void Clear() {
            _damageTakenIncreasePercent = 0f;
            _damageTakenIncreaseUntil = 0f;
            _moveSpeedDecreasePercent = 0f;
            _moveSpeedDecreaseUntil = 0f;
        }
    }
}
